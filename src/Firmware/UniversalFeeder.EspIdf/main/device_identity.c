#include <stdio.h>
#include <stdint.h>
#include "esp_mac.h"
#include "device_identity.h"
#include "app_config.h"

esp_err_t device_identity_get(char *device_id, size_t device_id_size)
{
    if (device_id == NULL || device_id_size < FEEDER_DEVICE_ID_MAX_LEN) {
        return ESP_ERR_INVALID_ARG;
    }

    uint8_t mac[6] = {0};
    esp_err_t err = esp_read_mac(mac, ESP_MAC_WIFI_STA);
    if (err != ESP_OK) {
        return err;
    }

    snprintf(
        device_id,
        device_id_size,
        "%02X%02X%02X%02X%02X%02X",
        mac[0],
        mac[1],
        mac[2],
        mac[3],
        mac[4],
        mac[5]);

    return ESP_OK;
}