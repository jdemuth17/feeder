#ifndef BLE_PROVISIONING_H
#define BLE_PROVISIONING_H

#include "esp_err.h"
#include "provisioning_store.h"

typedef void (*ble_provisioning_credentials_cb_t)(const feeder_wifi_credentials_t *credentials);

esp_err_t ble_provisioning_start(
    const feeder_wifi_credentials_t *initial_credentials,
    const char *initial_ip_address,
    ble_provisioning_credentials_cb_t credentials_cb);

esp_err_t ble_provisioning_set_ip_address(const char *ip_address);

#endif