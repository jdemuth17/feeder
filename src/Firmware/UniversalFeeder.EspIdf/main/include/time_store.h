#pragma once
#include "esp_err.h"

// Call once early in app_main after NVS is ready.
// Restores the last saved unix timestamp via settimeofday() so the
// schedule task has a reasonable clock even before NTP syncs.
esp_err_t time_store_restore(void);

// Call once after NTP has synced. Starts a background task that
// writes the current unix timestamp to NVS every 60 seconds.
void time_store_start_periodic_save(void);
