#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <time.h>
#include <sys/time.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/semphr.h"
#include "esp_log.h"
#include "cJSON.h"
#include "schedule_store.h"
#include "schedule_manager.h"
#include "feeding_sequence.h"
#include "app_config.h"

static const char *TAG = "ScheduleManager";

typedef struct {
    int hour;
    int minute;
    int duration_ms;
    int chime_lead_ms;
    int chime_count;
    int chime_duration_ms;
    bool enabled;
    int last_executed_date; // YYYYMMDD
} schedule_entry_t;

static schedule_entry_t *s_entries = NULL;
static size_t s_entry_count = 0;
static SemaphoreHandle_t s_mutex = NULL;

static int current_ymd(void)
{
    time_t t = time(NULL);
    struct tm tm;
    localtime_r(&t, &tm);
    int y = tm.tm_year + 1900;
    int m = tm.tm_mon + 1;
    int d = tm.tm_mday;
    return y * 10000 + m * 100 + d;
}

static void free_entries(void)
{
    if (s_entries != NULL) {
        free(s_entries);
        s_entries = NULL;
    }
    s_entry_count = 0;
}

static esp_err_t parse_and_replace_entries(const char *json)
{
    if (json == NULL) {
        return ESP_ERR_INVALID_ARG;
    }

    cJSON *root = cJSON_Parse(json);
    if (root == NULL) {
        return ESP_ERR_INVALID_ARG;
    }

    cJSON *arr = NULL;
    if (cJSON_IsArray(root)) {
        arr = root;
    } else {
        // allow object with key "schedule"
        cJSON *sched = cJSON_GetObjectItemCaseSensitive(root, "schedule");
        if (cJSON_IsArray(sched)) {
            arr = sched;
        }
    }

    if (arr == NULL) {
        cJSON_Delete(root);
        return ESP_ERR_INVALID_ARG;
    }

    size_t new_count = cJSON_GetArraySize(arr);
    schedule_entry_t *new_entries = calloc(new_count, sizeof(schedule_entry_t));
    if (new_entries == NULL) {
        cJSON_Delete(root);
        return ESP_ERR_NO_MEM;
    }

    size_t idx = 0;
    cJSON *item = NULL;
    cJSON_ArrayForEach(item, arr) {
        cJSON *time_item = cJSON_GetObjectItemCaseSensitive(item, "time");
        cJSON *duration_item = cJSON_GetObjectItemCaseSensitive(item, "duration_ms");
        cJSON *chime_lead_item = cJSON_GetObjectItemCaseSensitive(item, "chime_lead_ms");
        cJSON *chime_count_item = cJSON_GetObjectItemCaseSensitive(item, "chime_count");
        cJSON *chime_duration_item = cJSON_GetObjectItemCaseSensitive(item, "chime_duration_ms");
        cJSON *enabled_item = cJSON_GetObjectItemCaseSensitive(item, "enabled");

        if (!cJSON_IsString(time_item) || time_item->valuestring == NULL) {
            continue;
        }

        int hour = 0, minute = 0;
        if (sscanf(time_item->valuestring, "%d:%d", &hour, &minute) != 2) {
            continue;
        }

        int duration_ms = cJSON_IsNumber(duration_item) ? duration_item->valueint : 5000;
        int chime_lead_ms = cJSON_IsNumber(chime_lead_item) ? chime_lead_item->valueint : 0;
        int chime_count = cJSON_IsNumber(chime_count_item) ? chime_count_item->valueint : FEEDING_SEQUENCE_CHIME_COUNT;
        int chime_duration_ms = cJSON_IsNumber(chime_duration_item) ? chime_duration_item->valueint : FEEDING_SEQUENCE_CHIME_DURATION_MS;
        bool enabled = cJSON_IsBool(enabled_item) ? cJSON_IsTrue(enabled_item) : true;

        new_entries[idx].hour = hour;
        new_entries[idx].minute = minute;
        new_entries[idx].duration_ms = duration_ms;
        new_entries[idx].chime_lead_ms = chime_lead_ms;
        new_entries[idx].chime_count = chime_count;
        new_entries[idx].chime_duration_ms = chime_duration_ms;
        new_entries[idx].enabled = enabled;
        new_entries[idx].last_executed_date = 0;
        idx++;
    }

    size_t final_count = idx;
    // replace global entries under mutex
    if (s_mutex == NULL) {
        s_mutex = xSemaphoreCreateMutex();
    }
    if (xSemaphoreTake(s_mutex, portMAX_DELAY) == pdTRUE) {
        free_entries();
        s_entries = new_entries;
        s_entry_count = final_count;
        xSemaphoreGive(s_mutex);
    } else {
        free(new_entries);
        cJSON_Delete(root);
        return ESP_FAIL;
    }

    cJSON_Delete(root);
    ESP_LOGI(TAG, "Loaded %d schedule entries", (int)final_count);
    return ESP_OK;
}

char *schedule_manager_get_json(void)
{
    if (s_mutex == NULL) {
        return NULL;
    }
    char *result = NULL;
    if (xSemaphoreTake(s_mutex, portMAX_DELAY) == pdTRUE) {
        cJSON *arr = cJSON_CreateArray();
        if (arr != NULL) {
            for (size_t i = 0; i < s_entry_count; ++i) {
                schedule_entry_t *e = &s_entries[i];
                cJSON *item = cJSON_CreateObject();
                if (item == NULL) continue;
                char time_str[6];
                snprintf(time_str, sizeof(time_str), "%02d:%02d", e->hour, e->minute);
                cJSON_AddStringToObject(item, "time", time_str);
                cJSON_AddNumberToObject(item, "duration_ms", e->duration_ms);
                cJSON_AddNumberToObject(item, "chime_lead_ms", e->chime_lead_ms);
                cJSON_AddNumberToObject(item, "chime_count", e->chime_count);
                cJSON_AddNumberToObject(item, "chime_duration_ms", e->chime_duration_ms);
                cJSON_AddBoolToObject(item, "enabled", e->enabled);
                cJSON_AddItemToArray(arr, item);
            }
            result = cJSON_PrintUnformatted(arr);
            cJSON_Delete(arr);
        }
        xSemaphoreGive(s_mutex);
    }
    return result;
}

esp_err_t schedule_manager_apply_schedule_json(const char *json)
{
    if (json == NULL) {
        return ESP_ERR_INVALID_ARG;
    }

    esp_err_t err = schedule_store_save_json(json);
    if (err != ESP_OK) {
        ESP_LOGW(TAG, "Failed to save schedule: %s", esp_err_to_name(err));
        return err;
    }

    return parse_and_replace_entries(json);
}

static void schedule_task(void *arg)
{
    (void)arg;
    // Require SNTP to provide a real wall clock before we trust time for scheduling.
    // Otherwise a restored (possibly stale) clock could fire feeds at the wrong minute.
    const time_t kReasonableEpoch = 1577836800LL; // 2020-01-01
    while (true) {
        time_t t = time(NULL);
        struct tm tm;
        localtime_r(&t, &tm);
        int now_hour = tm.tm_hour;
        int now_min = tm.tm_min;
        int today = current_ymd();

        // Only run the actual schedule matching if we have a plausible wall clock.
        // Without this, stale NVS time could fire feeds at the wrong moment after power loss.
        bool clock_ok = (t > kReasonableEpoch);

        if (clock_ok && s_mutex != NULL && xSemaphoreTake(s_mutex, portMAX_DELAY) == pdTRUE) {
            for (size_t i = 0; i < s_entry_count; ++i) {
                schedule_entry_t *e = &s_entries[i];
                if (!e->enabled) continue;
                if (e->hour == now_hour && e->minute == now_min && e->last_executed_date != today) {
                    ESP_LOGI(TAG, "Firing scheduled feed [%d] at %02d:%02d", (int)i, e->hour, e->minute);
                    int duration_ms = e->duration_ms > 0 ? e->duration_ms : FEEDER_DEFAULT_DURATION_MS;
                    esp_err_t res = feeding_sequence_start_full(duration_ms,
                                                                e->chime_count,
                                                                e->chime_duration_ms,
                                                                e->chime_lead_ms);
                    if (res == ESP_OK) {
                        e->last_executed_date = today;
                        feeding_sequence_publish_log(true, "scheduled feed", false);
                    } else {
                        ESP_LOGW(TAG, "Scheduled feed skipped: %s", esp_err_to_name(res));
                    }
                }
            }
            xSemaphoreGive(s_mutex);
        }

        // Periodic heartbeat log (~once per 5 min) so users can see the tick is alive.
        static int heartbeat_count = 0;
        if (++heartbeat_count >= 300) {
            ESP_LOGI(TAG, "Schedule tick: local time %02d:%02d, %d entries loaded, clock_ok=%d",
                     now_hour, now_min, (int)s_entry_count, clock_ok);
            heartbeat_count = 0;
        }

        // Align next wakeup to the start of the next second so we don't drift
        // across minute boundaries and miss a scheduled feed.
        struct timeval tv;
        gettimeofday(&tv, NULL);
        int sleep_ms = 1000 - (int)(tv.tv_usec / 1000);
        if (sleep_ms < 50) sleep_ms += 1000; // avoid double-ticking the same second
        vTaskDelay(pdMS_TO_TICKS(sleep_ms));
    }
}

esp_err_t schedule_manager_init(void)
{
    // Initialize storage and load existing schedules
    schedule_store_init();

    // load schedules into buffer
    char *buf = malloc(2048);
    if (buf == NULL) {
        return ESP_ERR_NO_MEM;
    }
    esp_err_t err = schedule_store_load_json(buf, 2048);
    if (err == ESP_OK && buf[0] != '\0') {
        parse_and_replace_entries(buf);
    }
    free(buf);

    if (s_mutex == NULL) {
        s_mutex = xSemaphoreCreateMutex();
    }

    if (xTaskCreate(schedule_task, "schedule_task", 4096, NULL, 3, NULL) != pdPASS) {
        return ESP_FAIL;
    }

    ESP_LOGI(TAG, "Schedule manager initialized");
    return ESP_OK;
}
