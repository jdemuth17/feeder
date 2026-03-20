#ifndef MQTT_SERVICE_H
#define MQTT_SERVICE_H

#include "esp_err.h"

esp_err_t mqtt_service_init(void);
esp_err_t mqtt_service_start(const char *device_id);

#endif