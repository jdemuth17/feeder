#ifndef MOTOR_CONTROL_H
#define MOTOR_CONTROL_H

#include "esp_err.h"

esp_err_t motor_control_init(void);
void motor_control_rotate(int duration_ms);

#endif