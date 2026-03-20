#ifndef DEVICE_IDENTITY_H
#define DEVICE_IDENTITY_H

#include <stddef.h>
#include "esp_err.h"

esp_err_t device_identity_get(char *device_id, size_t device_id_size);

#endif