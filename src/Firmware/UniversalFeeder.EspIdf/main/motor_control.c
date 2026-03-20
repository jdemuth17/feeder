#include <stdbool.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "esp_log.h"
#include "motor_control.h"
#include "app_config.h"

static const char *TAG = "MotorControl";
static bool s_initialized;

esp_err_t motor_control_init(void)
{
    if (s_initialized) {
        return ESP_OK;
    }

    gpio_config_t output_config = {
        .pin_bit_mask = (1ULL << FEEDER_MOTOR_STEP_PIN) |
                        (1ULL << FEEDER_MOTOR_DIR_PIN) |
                        (1ULL << FEEDER_MOTOR_ENABLE_PIN),
        .mode = GPIO_MODE_OUTPUT,
        .pull_up_en = GPIO_PULLUP_DISABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type = GPIO_INTR_DISABLE,
    };

    ESP_ERROR_CHECK(gpio_config(&output_config));
    ESP_ERROR_CHECK(gpio_set_level(FEEDER_MOTOR_ENABLE_PIN, 1));
    ESP_ERROR_CHECK(gpio_set_level(FEEDER_MOTOR_DIR_PIN, 1));
    ESP_ERROR_CHECK(gpio_set_level(FEEDER_MOTOR_STEP_PIN, 0));

    s_initialized = true;
    return ESP_OK;
}

void motor_control_rotate(int duration_ms)
{
    if (!s_initialized || duration_ms <= 0) {
        return;
    }

    ESP_LOGI(TAG, "Rotating motor for %d ms", duration_ms);
    gpio_set_level(FEEDER_MOTOR_ENABLE_PIN, 0);
    gpio_set_level(FEEDER_MOTOR_DIR_PIN, 1);

    TickType_t deadline = xTaskGetTickCount() + pdMS_TO_TICKS(duration_ms);
    while (xTaskGetTickCount() < deadline) {
        gpio_set_level(FEEDER_MOTOR_STEP_PIN, 1);
        vTaskDelay(pdMS_TO_TICKS(1));
        gpio_set_level(FEEDER_MOTOR_STEP_PIN, 0);
        vTaskDelay(pdMS_TO_TICKS(1));
    }

    gpio_set_level(FEEDER_MOTOR_ENABLE_PIN, 1);
}