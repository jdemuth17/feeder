#include <stdio.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "esp_err.h"
#include "esp_log.h"
#include "nvs_flash.h"
#include "app_config.h"
#include "ble_provisioning.h"
#include "provisioning_store.h"
#include "wifi_manager.h"

static const char *TAG = "UniversalFeeder";

static void on_ip_address_changed(const char *ip_address)
{
    ESP_LOGI(TAG, "IP address update: %s", ip_address);
    ESP_ERROR_CHECK(ble_provisioning_set_ip_address(ip_address));
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

    ESP_ERROR_CHECK(provisioning_store_load_credentials(&credentials));
    ESP_ERROR_CHECK(provisioning_store_load_ip_address(ip_address, sizeof(ip_address)));
    ESP_ERROR_CHECK(wifi_manager_init(on_ip_address_changed));

    ESP_LOGI(TAG, "UniversalFeeder ESP-IDF Firmware");
    ESP_LOGI(TAG, "Device: %s", DEVICE_NAME_PREFIX);
    ESP_LOGI(TAG, "BLE Service UUID: %s", BLE_SERVICE_UUID);
    ESP_LOGI(TAG, "MQTT Topic Pattern: %s{feederId}%s", MQTT_TOPIC_PREFIX, MQTT_TOPIC_SUFFIX);

    if (credentials.is_configured) {
        ESP_LOGI(TAG, "Stored Wi-Fi credentials found for SSID '%s'", credentials.ssid);
        ESP_LOGI(TAG, "Last known IP: %s", ip_address);
        ESP_ERROR_CHECK(wifi_manager_connect(&credentials));
    } else {
        ESP_LOGI(TAG, "No Wi-Fi credentials stored; entering provisioning mode");
    }

    ret = ble_provisioning_start(&credentials, ip_address, on_credentials_received);
    if (ret != ESP_OK) {
        ESP_LOGE(TAG, "BLE provisioning startup failed: %s", esp_err_to_name(ret));
        return;
    }

    ESP_LOGI(TAG, "Firmware initialization complete");

    while (1) {
        vTaskDelay(pdMS_TO_TICKS(10000));
    }
}
