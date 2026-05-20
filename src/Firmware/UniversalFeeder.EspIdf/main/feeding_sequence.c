#include <stdlib.h>
#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "feeding_sequence.h"
#include "motor_control.h"
#include "buzzer_control.h"
#include "app_config.h"
#include "mqtt_service.h"
#include <time.h>

static const char *TAG = "FeedingSequence";
static SemaphoreHandle_t s_operation_mutex;

// Publish a log after a feed event
void feeding_sequence_publish_log(bool success, const char *status, bool manual)
{
    // Compose JSON log message
    char log_json[256];
    snprintf(log_json, sizeof(log_json),
        "{\"timestamp\":%ld,\"success\":%s,\"status\":\"%s\",\"manual\":%s}",
        (long)time(NULL),
        success ? "true" : "false",
        status ? status : "",
        manual ? "true" : "false");
    // Use device_id if available (assume global or pass in as needed)
    extern char g_device_id[];
    mqtt_service_publish_log(g_device_id, log_json);
}

typedef struct {
    int duration_ms;
    int chime_count;
    int chime_duration_ms;
    int chime_lead_ms;
} feed_task_args_t;

typedef struct {
    float volume;
    int duration_ms;
    int chime_count;
} chime_task_args_t;

static void feed_task(void *arg)
{
    feed_task_args_t *task_args = (feed_task_args_t *)arg;
    int count = task_args->chime_count;
    int chime_ms = task_args->chime_duration_ms;

    for (int i = 0; i < count; ++i) {
        buzzer_control_play(CHIME_DEFAULT_VOLUME, chime_ms);
        if (i + 1 < count) {
            vTaskDelay(pdMS_TO_TICKS(FEEDING_SEQUENCE_PAUSE_MS));
        }
    }

    if (task_args->chime_lead_ms > 0) {
        vTaskDelay(pdMS_TO_TICKS(task_args->chime_lead_ms));
    }

    motor_control_rotate(task_args->duration_ms);
    xSemaphoreGive(s_operation_mutex);
    free(task_args);
    vTaskDelete(NULL);
}

static void chime_task(void *arg)
{
    chime_task_args_t *task_args = (chime_task_args_t *)arg;
    int count = task_args->chime_count > 0 ? task_args->chime_count : 1;
    for (int i = 0; i < count; ++i) {
        buzzer_control_play(task_args->volume, task_args->duration_ms);
        if (i + 1 < count) {
            vTaskDelay(pdMS_TO_TICKS(FEEDING_SEQUENCE_PAUSE_MS));
        }
    }
    xSemaphoreGive(s_operation_mutex);
    free(task_args);
    vTaskDelete(NULL);
}

esp_err_t feeding_sequence_init(void)
{
    if (s_operation_mutex == NULL) {
        s_operation_mutex = xSemaphoreCreateBinary();
        if (s_operation_mutex == NULL) {
            return ESP_ERR_NO_MEM;
        }

        xSemaphoreGive(s_operation_mutex);
    }

    ESP_ERROR_CHECK(motor_control_init());
    ESP_ERROR_CHECK(buzzer_control_init());
    return ESP_OK;
}

esp_err_t feeding_sequence_start_full(int duration_ms, int chime_count, int chime_duration_ms, int chime_lead_ms)
{
    if (duration_ms <= 0) {
        duration_ms = FEEDER_DEFAULT_DURATION_MS;
    }
    if (chime_count < 0) {
        chime_count = FEEDING_SEQUENCE_CHIME_COUNT;
    }
    if (chime_duration_ms <= 0) {
        chime_duration_ms = FEEDING_SEQUENCE_CHIME_DURATION_MS;
    }
    if (chime_lead_ms < 0) {
        chime_lead_ms = 0;
    }

    if (xSemaphoreTake(s_operation_mutex, 0) != pdTRUE) {
        ESP_LOGW(TAG, "Ignoring feed command because another operation is in progress");
        return ESP_ERR_INVALID_STATE;
    }

    feed_task_args_t *task_args = calloc(1, sizeof(feed_task_args_t));
    if (task_args == NULL) {
        xSemaphoreGive(s_operation_mutex);
        return ESP_ERR_NO_MEM;
    }

    task_args->duration_ms = duration_ms;
    task_args->chime_count = chime_count;
    task_args->chime_duration_ms = chime_duration_ms;
    task_args->chime_lead_ms = chime_lead_ms;
    if (xTaskCreate(feed_task, "feed_task", 4096, task_args, 5, NULL) != pdPASS) {
        free(task_args);
        xSemaphoreGive(s_operation_mutex);
        return ESP_FAIL;
    }

    return ESP_OK;
}

esp_err_t feeding_sequence_start_ex(int duration_ms, int chime_lead_ms)
{
    return feeding_sequence_start_full(duration_ms,
                                       FEEDING_SEQUENCE_CHIME_COUNT,
                                       FEEDING_SEQUENCE_CHIME_DURATION_MS,
                                       chime_lead_ms);
}

esp_err_t feeding_sequence_start(int duration_ms)
{
    return feeding_sequence_start_ex(duration_ms, 0);
}

esp_err_t feeding_sequence_play_chime_burst(float volume, int chime_count, int chime_duration_ms)
{
    if (chime_duration_ms <= 0) {
        chime_duration_ms = CHIME_DURATION_MS;
    }
    if (chime_count <= 0) {
        chime_count = 1;
    }
    if (volume <= 0.0f) {
        volume = CHIME_DEFAULT_VOLUME;
    }

    if (xSemaphoreTake(s_operation_mutex, 0) != pdTRUE) {
        ESP_LOGW(TAG, "Ignoring chime command because another operation is in progress");
        return ESP_ERR_INVALID_STATE;
    }

    chime_task_args_t *task_args = calloc(1, sizeof(chime_task_args_t));
    if (task_args == NULL) {
        xSemaphoreGive(s_operation_mutex);
        return ESP_ERR_NO_MEM;
    }

    task_args->volume = volume;
    task_args->duration_ms = chime_duration_ms;
    task_args->chime_count = chime_count;
    if (xTaskCreate(chime_task, "chime_task", 3072, task_args, 5, NULL) != pdPASS) {
        free(task_args);
        xSemaphoreGive(s_operation_mutex);
        return ESP_FAIL;
    }

    return ESP_OK;
}

esp_err_t feeding_sequence_play_chime(float volume, int duration_ms)
{
    return feeding_sequence_play_chime_burst(volume, 1, duration_ms);
}

bool feeding_sequence_is_busy(void)
{
    if (s_operation_mutex == NULL) {
        return false;
    }

    if (xSemaphoreTake(s_operation_mutex, 0) == pdTRUE) {
        xSemaphoreGive(s_operation_mutex);
        return false;
    }

    return true;
}