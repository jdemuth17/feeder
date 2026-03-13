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

esp_err_t provisioning_store_load_ip_address(char *ip_address, size_t ip_address_size);
esp_err_t provisioning_store_save_ip_address(const char *ip_address);

#endif