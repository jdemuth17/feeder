#ifndef FALLBACK_SCHEDULER_H
#define FALLBACK_SCHEDULER_H

#include "esp_err.h"

esp_err_t fallback_scheduler_init(void);
void fallback_scheduler_notify_mqtt_connected(void);
void fallback_scheduler_notify_mqtt_disconnected(void);
void fallback_scheduler_note_feed_event(void);

#endif