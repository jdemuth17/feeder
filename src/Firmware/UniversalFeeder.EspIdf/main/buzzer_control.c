#include <stdint.h>
#include <stdbool.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "driver/gpio.h"
#include "esp_log.h"
#include "buzzer_control.h"
#include "app_config.h"

// Active buzzer: drive GPIO HIGH to beep, LOW to stop.
// The buzzer has its own internal oscillator so no PWM is needed.
// The 'volume' parameter is accepted for API compatibility but ignored.

static const char *TAG = "BuzzerControl";
static bool s_initialized;

esp_err_t buzzer_control_init(void)
{
    if (s_initialized) {
        return ESP_OK;
    }

    gpio_config_t cfg = {
        .pin_bit_mask = (1ULL << FEEDER_BUZZER_PIN),
        .mode         = GPIO_MODE_OUTPUT,
        .pull_up_en   = GPIO_PULLUP_DISABLE,
        .pull_down_en = GPIO_PULLDOWN_DISABLE,
        .intr_type    = GPIO_INTR_DISABLE,
    };
    ESP_ERROR_CHECK(gpio_config(&cfg));
    gpio_set_level(FEEDER_BUZZER_PIN, 0); // silent on init

    s_initialized = true;
    return ESP_OK;
}

void buzzer_control_play(float volume, int duration_ms)
{
    if (!s_initialized || duration_ms <= 0) {
        return;
    }

    ESP_LOGI(TAG, "Playing buzzer for %d ms", duration_ms);
    gpio_set_level(FEEDER_BUZZER_PIN, 1);
    vTaskDelay(pdMS_TO_TICKS(duration_ms));
    gpio_set_level(FEEDER_BUZZER_PIN, 0);
}