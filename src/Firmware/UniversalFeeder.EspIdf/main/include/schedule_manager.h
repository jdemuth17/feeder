#pragma once
#include "esp_err.h"

esp_err_t schedule_manager_init(void);
esp_err_t schedule_manager_apply_schedule_json(const char *json);
