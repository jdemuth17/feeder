#pragma once
#include "esp_err.h"
#include <stdbool.h>

esp_err_t feeding_sequence_init(void);
esp_err_t feeding_sequence_start(int duration_ms);
esp_err_t feeding_sequence_start_ex(int duration_ms, int chime_lead_ms);
esp_err_t feeding_sequence_start_full(int duration_ms, int chime_count, int chime_duration_ms, int chime_lead_ms);
esp_err_t feeding_sequence_play_chime(float volume, int duration_ms);
esp_err_t feeding_sequence_play_chime_burst(float volume, int chime_count, int chime_duration_ms);
bool feeding_sequence_is_busy(void);

// Called after a feed event to publish a log
void feeding_sequence_publish_log(bool success, const char *status, bool manual);
