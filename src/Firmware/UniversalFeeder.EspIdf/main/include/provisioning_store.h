#ifndef PROVISIONING_STORE_H
#define PROVISIONING_STORE_H

#include <stdbool.h>
#include <stddef.h>
#include "esp_err.h"
#include "app_config.h"

typedef struct {
    bool is_configured;
    char ssid[FEEDER_WIFI_SSID_MAX_LEN];
    char password[FEEDER_WIFI_PASSWORD_MAX_LEN];
} feeder_wifi_credentials_t;

esp_err_t provisioning_store_load_credentials(feeder_wifi_credentials_t *credentials);
esp_err_t provisioning_store_save_credentials(const feeder_wifi_credentials_t *credentials);
esp_err_t provisioning_store_clear_credentials(void);

// Pending/partial credential storage used during BLE provisioning so that
// a connection dropped between SSID and password writes doesn't lose state.
esp_err_t provisioning_store_save_pending_ssid(const char *ssid);
esp_err_t provisioning_store_save_pending_password(const char *password);
esp_err_t provisioning_store_load_pending(feeder_wifi_credentials_t *credentials);
esp_err_t provisioning_store_clear_pending(void);

esp_err_t provisioning_store_load_ip_address(char *ip_address, size_t ip_address_size);
esp_err_t provisioning_store_save_ip_address(const char *ip_address);

#endif