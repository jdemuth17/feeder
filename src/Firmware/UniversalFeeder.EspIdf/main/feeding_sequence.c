#include <stdlib.h>
#include "freertos/FreeRTOS.h"
#include "freertos/semphr.h"
#include "freertos/task.h"
#include "esp_log.h"
#include "feeding_sequence.h"
#include "motor_control.h"
#include "buzzer_control.h"
#include "app_config.h"

static const char *TAG = "FeedingSequence";
static SemaphoreHandle_t s_operation_mutex;

typedef struct {
    int duration_ms;
} feed_task_args_t;

typedef struct {
    float volume;
    int duration_ms;
} chime_task_args_t;

static void feed_task(void *arg)
{
    feed_task_args_t *task_args = (feed_task_args_t *)arg;

    for (int i = 0; i < FEEDING_SEQUENCE_CHIME_COUNT; ++i) {
        buzzer_control_play(CHIME_DEFAULT_VOLUME, FEEDING_SEQUENCE_CHIME_DURATION_MS);
        if (i + 1 < FEEDING_SEQUENCE_CHIME_COUNT) {
            vTaskDelay(pdMS_TO_TICKS(FEEDING_SEQUENCE_PAUSE_MS));
        }
    }

    motor_control_rotate(task_args->duration_ms);
    xSemaphoreGive(s_operation_mutex);
    free(task_args);
    vTaskDelete(NULL);
}

static void chime_task(void *arg)
{
    chime_task_args_t *task_args = (chime_task_args_t *)arg;
    buzzer_control_play(task_args->volume, task_args->duration_ms);
    xSemaphoreGive(s_operation_mutex);
    free(task_args);
    vTaskDelete(NULL);
}

esp_err_t feeding_sequence_init(void)
{
    if (s_operation_mutex == NULL) {
        s_operation_mutex = xSemaphoreCreateMutex();
        if (s_operation_mutex == NULL) {
            return ESP_ERR_NO_MEM;
        }
    }

    ESP_ERROR_CHECK(motor_control_init());
    ESP_ERROR_CHECK(buzzer_control_init());
    return ESP_OK;
}

esp_err_t feeding_sequence_start(int duration_ms)
{
    if (duration_ms <= 0) {
        duration_ms = FEEDER_DEFAULT_DURATION_MS;
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
    if (xTaskCreate(feed_task, "feed_task", 4096, task_args, 5, NULL) != pdPASS) {
        free(task_args);
        xSemaphoreGive(s_operation_mutex);
        return ESP_FAIL;
    }

    return ESP_OK;
}

esp_err_t feeding_sequence_play_chime(float volume, int duration_ms)
{
    if (duration_ms <= 0) {
        duration_ms = CHIME_DURATION_MS;
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
    task_args->duration_ms = duration_ms;
    if (xTaskCreate(chime_task, "chime_task", 3072, task_args, 5, NULL) != pdPASS) {
        free(task_args);
        xSemaphoreGive(s_operation_mutex);
        return ESP_FAIL;
    }

    return ESP_OK;
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