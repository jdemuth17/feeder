#include <stdbool.h>
#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_event.h"
#include "esp_log.h"
#include "esp_netif.h"
#include "esp_wifi.h"
#include "lwip/ip4_addr.h"
#include "wifi_manager.h"
#include "app_config.h"

static const char *TAG = "WifiManager";

static bool s_initialized;
static bool s_wifi_started;
static bool s_should_reconnect;
static int s_retry_count;
static feeder_wifi_credentials_t s_credentials;
static wifi_manager_ip_callback_t s_ip_callback;

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

            if (s_retry_count < FEEDER_WIFI_MAX_RETRY_ATTEMPTS) {
                s_retry_count++;
                ESP_LOGW(TAG, "Wi-Fi disconnected; retrying (%d/%d)", s_retry_count, FEEDER_WIFI_MAX_RETRY_ATTEMPTS);
                esp_wifi_connect();
            } else {
                ESP_LOGE(TAG, "Wi-Fi connection failed after %d attempts", FEEDER_WIFI_MAX_RETRY_ATTEMPTS);
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