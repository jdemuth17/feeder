#ifndef FEEDING_SEQUENCE_H
#define FEEDING_SEQUENCE_H

#include <stdbool.h>
#include "esp_err.h"

esp_err_t feeding_sequence_init(void);
esp_err_t feeding_sequence_start(int duration_ms);
esp_err_t feeding_sequence_play_chime(float volume, int duration_ms);
bool feeding_sequence_is_busy(void);

#endif