#include <stdio.h>
#include <string.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_err.h"
#include "esp_log.h"
#include "nvs_flash.h"
#include "app_config.h"
#include "ble_provisioning.h"
#include "device_identity.h"
#include "fallback_scheduler.h"
#include "feeding_sequence.h"
#include "mqtt_service.h"
#include "schedule_manager.h"
#include "provisioning_store.h"
#include "wifi_manager.h"
#include "time_store.h"

static const char *TAG = "UniversalFeeder";
char g_device_id[FEEDER_DEVICE_ID_MAX_LEN] = {0};
static bool s_mqtt_started;

static void on_ip_address_changed(const char *ip_address)
{
    ESP_LOGI(TAG, "IP address update: %s", ip_address);
    ESP_ERROR_CHECK(ble_provisioning_set_ip_address(ip_address));

    if (!s_mqtt_started && strcmp(ip_address, FEEDER_IP_ADDRESS_UNASSIGNED) != 0) {
        ESP_ERROR_CHECK(mqtt_service_start(g_device_id));
        s_mqtt_started = true;
    }
}

static void on_credentials_received(const feeder_wifi_credentials_t *credentials)
{
    ESP_LOGI(TAG, "Provisioning received for SSID '%s'", credentials->ssid);
    ESP_ERROR_CHECK(wifi_manager_connect(credentials));
}

void app_main(void)
{
    esp_err_t ret = nvs_flash_init();
    if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND) {
        ESP_ERROR_CHECK(nvs_flash_erase());
        ret = nvs_flash_init();
    }
    ESP_ERROR_CHECK(ret);

    feeder_wifi_credentials_t credentials = {0};
    char ip_address[FEEDER_IP_ADDRESS_MAX_LEN] = {0};

    ESP_ERROR_CHECK(device_identity_get(g_device_id, sizeof(g_device_id)));
    time_store_restore(); // Restore last known time before schedule task starts
    ESP_ERROR_CHECK(provisioning_store_load_credentials(&credentials));
    ESP_ERROR_CHECK(provisioning_store_load_ip_address(ip_address, sizeof(ip_address)));
    ESP_ERROR_CHECK(wifi_manager_init(on_ip_address_changed));
    ESP_ERROR_CHECK(feeding_sequence_init());
    ESP_ERROR_CHECK(fallback_scheduler_init());
    ESP_ERROR_CHECK(schedule_manager_init());
    ESP_ERROR_CHECK(mqtt_service_init());

    ESP_LOGI(TAG, "UniversalFeeder ESP-IDF Firmware");
    ESP_LOGI(TAG, "Device: %s (%s)", DEVICE_NAME_PREFIX, g_device_id);
    ESP_LOGI(TAG, "BLE Service UUID: %s", BLE_SERVICE_UUID);
    ESP_LOGI(TAG, "MQTT Topic Pattern: %s{feederId}%s", MQTT_TOPIC_PREFIX, MQTT_TOPIC_SUFFIX);

    if (credentials.is_configured) {
        ESP_LOGI(TAG, "Stored Wi-Fi credentials found for SSID '%s'", credentials.ssid);
        ESP_LOGI(TAG, "Last known IP: %s", ip_address);
        ESP_ERROR_CHECK(wifi_manager_connect(&credentials));
    } else {
        ESP_LOGI(TAG, "No Wi-Fi credentials stored; entering provisioning mode");
    }

    ret = ble_provisioning_start(&credentials, ip_address, g_device_id, on_credentials_received);
    if (ret != ESP_OK) {
        ESP_LOGE(TAG, "BLE provisioning startup failed: %s", esp_err_to_name(ret));
        return;
    }

    ESP_LOGI(TAG, "Firmware initialization complete");

    while (1) {
        vTaskDelay(pdMS_TO_TICKS(10000));
    }
}
