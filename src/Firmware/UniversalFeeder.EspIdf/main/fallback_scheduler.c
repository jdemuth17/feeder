#include <stdbool.h>
#include <stdint.h>
#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"
#include "freertos/task.h"
#include "esp_err.h"
#include "esp_log.h"
#include "esp_timer.h"
#include "app_config.h"
#include "fallback_scheduler.h"
#include "feeding_sequence.h"

static const char *TAG = "FallbackScheduler";

static SemaphoreHandle_t s_state_mutex;
static bool s_initialized;
static bool s_mqtt_connected;
static uint64_t s_disconnected_since_ms;
static uint64_t s_last_feed_ms;

static uint64_t now_ms(void)
{
    return (uint64_t)(esp_timer_get_time() / 1000ULL);
}

static uint64_t max_u64(uint64_t left, uint64_t right)
{
    return left > right ? left : right;
}

static void fallback_scheduler_task(void *arg)
{
    while (true) {
        vTaskDelay(pdMS_TO_TICKS((TickType_t)FALLBACK_CHECK_INTERVAL_MS));

        if (s_state_mutex == NULL) {
            continue;
        }

        uint64_t disconnected_since_ms = 0;
        uint64_t last_feed_ms = 0;
        bool mqtt_connected = false;

        if (xSemaphoreTake(s_state_mutex, portMAX_DELAY) == pdTRUE) {
            disconnected_since_ms = s_disconnected_since_ms;
            last_feed_ms = s_last_feed_ms;
            mqtt_connected = s_mqtt_connected;
            xSemaphoreGive(s_state_mutex);
        }

        if (mqtt_connected || disconnected_since_ms == 0) {
            continue;
        }

        uint64_t current_ms = now_ms();
        uint64_t disconnected_for_ms = current_ms - disconnected_since_ms;
        uint64_t baseline_ms = last_feed_ms == 0 ? disconnected_since_ms : max_u64(last_feed_ms, disconnected_since_ms);
        uint64_t elapsed_since_feed_ms = current_ms - baseline_ms;

        if (disconnected_for_ms < FALLBACK_ARM_DELAY_MS || elapsed_since_feed_ms < FALLBACK_FEED_INTERVAL_MS) {
            continue;
        }

        ESP_LOGW(TAG, "MQTT offline for %llu ms; triggering fallback feed", disconnected_for_ms);
        esp_err_t err = feeding_sequence_start(FEEDER_DEFAULT_DURATION_MS);
        if (err != ESP_OK) {
            ESP_LOGW(TAG, "Fallback feed skipped: %s", esp_err_to_name(err));
            continue;
        }

        fallback_scheduler_note_feed_event();
    }
}

esp_err_t fallback_scheduler_init(void)
{
    if (s_initialized) {
        return ESP_OK;
    }

    s_state_mutex = xSemaphoreCreateMutex();
    if (s_state_mutex == NULL) {
        return ESP_ERR_NO_MEM;
    }

    s_last_feed_ms = now_ms();
    s_initialized = true;

    if (xTaskCreate(fallback_scheduler_task, "fallback_task", 4096, NULL, 4, NULL) != pdPASS) {
        vSemaphoreDelete(s_state_mutex);
        s_state_mutex = NULL;
        s_initialized = false;
        return ESP_FAIL;
    }

    return ESP_OK;
}

void fallback_scheduler_notify_mqtt_connected(void)
{
    if (s_state_mutex == NULL) {
        return;
    }

    if (xSemaphoreTake(s_state_mutex, portMAX_DELAY) == pdTRUE) {
        s_mqtt_connected = true;
        s_disconnected_since_ms = 0;
        xSemaphoreGive(s_state_mutex);
    }
}

void fallback_scheduler_notify_mqtt_disconnected(void)
{
    if (s_state_mutex == NULL) {
        return;
    }

    if (xSemaphoreTake(s_state_mutex, portMAX_DELAY) == pdTRUE) {
        s_mqtt_connected = false;
        if (s_disconnected_since_ms == 0) {
            s_disconnected_since_ms = now_ms();
        }
        xSemaphoreGive(s_state_mutex);
    }
}

void fallback_scheduler_note_feed_event(void)
{
    if (s_state_mutex == NULL) {
        return;
    }

    if (xSemaphoreTake(s_state_mutex, portMAX_DELAY) == pdTRUE) {
        s_last_feed_ms = now_ms();
        xSemaphoreGive(s_state_mutex);
    }
}