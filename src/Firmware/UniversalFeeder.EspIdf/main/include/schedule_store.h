#pragma once
#include "esp_err.h"
#include <stddef.h>

esp_err_t schedule_store_init(void);
esp_err_t schedule_store_save_json(const char *json);
esp_err_t schedule_store_load_json(char *out_buf, size_t buf_len);
