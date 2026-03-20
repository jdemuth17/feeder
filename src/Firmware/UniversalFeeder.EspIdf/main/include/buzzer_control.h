#ifndef BUZZER_CONTROL_H
#define BUZZER_CONTROL_H

#include "esp_err.h"

esp_err_t buzzer_control_init(void);
void buzzer_control_play(float volume, int duration_ms);

#endif