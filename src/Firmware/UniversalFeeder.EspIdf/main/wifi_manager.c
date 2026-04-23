#include <stdbool.h>
#include <stdlib.h>
#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/timers.h"
#include "esp_event.h"
#include "esp_log.h"
#include "esp_netif.h"
#include "esp_wifi.h"
#include "esp_sntp.h"
#include "lwip/ip4_addr.h"
#include "wifi_manager.h"
#include "time_store.h"
#include "app_config.h"

static const char *TAG = "WifiManager";

static bool s_initialized;
static bool s_wifi_started;
static bool s_should_reconnect;
static int s_retry_count;
static feeder_wifi_credentials_t s_credentials;
static wifi_manager_ip_callback_t s_ip_callback;
static TimerHandle_t s_retry_timer;

static int retry_backoff_ms(int attempt)
{
    // Exponential backoff capped at 30s: 500ms, 1s, 2s, 4s, 8s, 16s, 30s, 30s...
    int base_ms = 500;
    int shift = attempt > 10 ? 10 : attempt;
    int ms = base_ms << shift;
    if (ms < 500) ms = 500;
    if (ms > 30000) ms = 30000;
    return ms;
}

static void retry_timer_cb(TimerHandle_t timer)
{
    (void)timer;
    if (!s_should_reconnect) return;
    ESP_LOGI(TAG, "Retrying Wi-Fi connect (attempt %d)", s_retry_count);
    esp_wifi_connect();
}

static void notify_ip(const char *ip_address)
{
    if (s_ip_callback != NULL) {
        s_ip_callback(ip_address);
    }
}

static void wifi_event_handler(void *arg, esp_event_base_t event_base, int32_t event_id, void *event_data)
{
    if (event_base == WIFI_EVENT) {
        switch (event_id) {
        case WIFI_EVENT_STA_START:
            if (s_should_reconnect) {
                ESP_LOGI(TAG, "Wi-Fi station started; connecting to '%s'", s_credentials.ssid);
                esp_wifi_connect();
            }
            break;
        case WIFI_EVENT_STA_DISCONNECTED:
            notify_ip(FEEDER_IP_ADDRESS_UNASSIGNED);
            if (!s_should_reconnect) {
                break;
            }

            {
                int delay_ms = retry_backoff_ms(s_retry_count);
                s_retry_count++;
                ESP_LOGW(TAG, "Wi-Fi disconnected; retrying in %d ms (attempt %d)", delay_ms, s_retry_count);
                if (s_retry_timer == NULL) {
                    s_retry_timer = xTimerCreate("wifi_retry", pdMS_TO_TICKS(delay_ms), pdFALSE, NULL, retry_timer_cb);
                } else {
                    xTimerChangePeriod(s_retry_timer, pdMS_TO_TICKS(delay_ms), 0);
                }
                if (s_retry_timer != NULL) {
                    xTimerStart(s_retry_timer, 0);
                }
            }
            break;
        default:
            break;
        }
    }

    if (event_base == IP_EVENT && event_id == IP_EVENT_STA_GOT_IP) {
        ip_event_got_ip_t *event = (ip_event_got_ip_t *)event_data;
        char ip_address[FEEDER_IP_ADDRESS_MAX_LEN] = {0};
        snprintf(ip_address, sizeof(ip_address), IPSTR, IP2STR(&event->ip_info.ip));
        s_retry_count = 0;
        ESP_LOGI(TAG, "Wi-Fi connected, IP address: %s", ip_address);

        // Start NTP sync so the schedule manager has correct wall-clock time
        if (sntp_get_sync_status() == SNTP_SYNC_STATUS_RESET) {
            // Set timezone before SNTP so localtime_r returns local time
            setenv("TZ", FEEDER_POSIX_TZ, 1);
            tzset();
            esp_sntp_setoperatingmode(SNTP_OPMODE_POLL);
            esp_sntp_setservername(0, "pool.ntp.org");
            esp_sntp_init();
            ESP_LOGI(TAG, "SNTP sync started (TZ=%s)", FEEDER_POSIX_TZ);
        }
        // Once Wi-Fi is up, start persisting time every 5 min so reboots
        // restore an accurate clock even before NTP responds.
        time_store_start_periodic_save();

        notify_ip(ip_address);
    }
}

esp_err_t wifi_manager_init(wifi_manager_ip_callback_t ip_callback)
{
    if (s_initialized) {
        return ESP_OK;
    }

    s_ip_callback = ip_callback;

    ESP_ERROR_CHECK(esp_netif_init());

    esp_err_t err = esp_event_loop_create_default();
    if (err != ESP_OK && err != ESP_ERR_INVALID_STATE) {
        return err;
    }

    esp_netif_create_default_wifi_sta();

    wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
    ESP_ERROR_CHECK(esp_wifi_init(&cfg));
    ESP_ERROR_CHECK(esp_event_handler_register(WIFI_EVENT, ESP_EVENT_ANY_ID, &wifi_event_handler, NULL));
    ESP_ERROR_CHECK(esp_event_handler_register(IP_EVENT, IP_EVENT_STA_GOT_IP, &wifi_event_handler, NULL));
    ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));

    s_initialized = true;
    return ESP_OK;
}

esp_err_t wifi_manager_connect(const feeder_wifi_credentials_t *credentials)
{
    if (!s_initialized || credentials == NULL || !credentials->is_configured || credentials->ssid[0] == '\0') {
        return ESP_ERR_INVALID_ARG;
    }

    memset(&s_credentials, 0, sizeof(s_credentials));
    s_credentials = *credentials;
    s_should_reconnect = true;
    s_retry_count = 0;

    wifi_config_t wifi_config = {0};
    memcpy(wifi_config.sta.ssid, credentials->ssid, strlen(credentials->ssid));
    memcpy(wifi_config.sta.password, credentials->password, strlen(credentials->password));
    wifi_config.sta.threshold.authmode = WIFI_AUTH_WPA2_PSK;
    wifi_config.sta.pmf_cfg.capable = true;
    wifi_config.sta.pmf_cfg.required = false;

    ESP_ERROR_CHECK(esp_wifi_set_config(WIFI_IF_STA, &wifi_config));

    if (!s_wifi_started) {
        s_wifi_started = true;
        return esp_wifi_start();
    }

    notify_ip(FEEDER_IP_ADDRESS_UNASSIGNED);
    return esp_wifi_connect();
}