#include <time.h>
#include <sys/time.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "nvs.h"
#include "nvs_flash.h"
#include "time_store.h"

static const char *TAG       = "TimeStore";
static const char *NAMESPACE = "feeder";
static const char *KEY_TIME  = "last_time";

static bool s_task_started = false;

esp_err_t time_store_restore(void)
{
    nvs_handle_t handle;
    esp_err_t err = nvs_open(NAMESPACE, NVS_READONLY, &handle);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        ESP_LOGI(TAG, "No saved time in NVS — starting from epoch");
        return ESP_OK;
    }
    if (err != ESP_OK) {
        ESP_LOGW(TAG, "NVS open failed: %s", esp_err_to_name(err));
        return err;
    }

    int64_t saved_time = 0;
    err = nvs_get_i64(handle, KEY_TIME, &saved_time);
    nvs_close(handle);

    if (err == ESP_OK && saved_time > 0) {
        struct timeval tv = { .tv_sec = (time_t)saved_time, .tv_usec = 0 };
        settimeofday(&tv, NULL);
        ESP_LOGI(TAG, "Restored last known time from NVS (%lld) — accurate to ~1 min before power loss",
                 (long long)saved_time);
    } else {
        ESP_LOGI(TAG, "No valid saved time found (err=%s)", esp_err_to_name(err));
    }

    return ESP_OK;
}

static void time_save_task(void *arg)
{
    // Save every 5 minutes — gives ~5 min clock accuracy after power loss
    // (~288 writes/day vs 100k cycle endurance = ~100+ year flash lifespan)
    while (true) {
        vTaskDelay(pdMS_TO_TICKS(300000));

        time_t now = time(NULL);
        // Only save if time looks reasonable (post-2020)
        if (now < 1577836800LL) {
            continue;
        }

        nvs_handle_t handle;
        esp_err_t err = nvs_open(NAMESPACE, NVS_READWRITE, &handle);
        if (err == ESP_OK) {
            nvs_set_i64(handle, KEY_TIME, (int64_t)now);
            nvs_commit(handle);
            nvs_close(handle);
        }
    }
}

void time_store_start_periodic_save(void)
{
    if (s_task_started) return;
    s_task_started = true;
    xTaskCreate(time_save_task, "time_save", 2048, NULL, 1, NULL);
    ESP_LOGI(TAG, "Periodic time save started (every 5 min)");
}
