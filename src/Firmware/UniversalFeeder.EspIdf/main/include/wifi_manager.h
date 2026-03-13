#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include "esp_err.h"
#include "provisioning_store.h"

typedef void (*wifi_manager_ip_callback_t)(const char *ip_address);

esp_err_t wifi_manager_init(wifi_manager_ip_callback_t ip_callback);
esp_err_t wifi_manager_connect(const feeder_wifi_credentials_t *credentials);

#endif