#pragma once
#include "esp_err.h"

esp_err_t schedule_manager_init(void);
esp_err_t schedule_manager_apply_schedule_json(const char *json);

// Serialises current schedule entries to a JSON array string.
// Caller must free() the returned string. Returns NULL on failure.
char *schedule_manager_get_json(void);
