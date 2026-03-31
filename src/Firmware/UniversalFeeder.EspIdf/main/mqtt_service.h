#pragma once
#include "esp_err.h"

esp_err_t mqtt_service_init(void);
esp_err_t mqtt_service_start(const char *device_id);

// New: publish a log/event message
esp_err_t mqtt_service_publish_log(const char *device_id, const char *log_json);

// New: subscribe and handle schedule set messages
// (implementation will add handler registration)
