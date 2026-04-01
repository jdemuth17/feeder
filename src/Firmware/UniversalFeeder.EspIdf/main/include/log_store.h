#pragma once
#include "esp_err.h"
#include <stddef.h>

esp_err_t log_store_init(void);
esp_err_t log_store_append_json(const char *json);
esp_err_t log_store_get_all_json(char *out_buf, size_t buf_len);
