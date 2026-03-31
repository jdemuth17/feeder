#include <string.h>
#include "nvs.h"
#include "nvs_flash.h"
#include "esp_log.h"
#include "schedule_store.h"

static const char *TAG = "ScheduleStore";
static const char *NAMESPACE_NAME = "feeder";
static const char *KEY_SCHEDULES = "schedules";

esp_err_t schedule_store_init(void)
{
    // NVS is initialized in app_main; nothing to do here but check availability
    return ESP_OK;
}

esp_err_t schedule_store_save_json(const char *json)
{
    if (json == NULL) {
        return ESP_ERR_INVALID_ARG;
    }

    nvs_handle_t handle;
    esp_err_t err = nvs_open(NAMESPACE_NAME, NVS_READWRITE, &handle);
    if (err != ESP_OK) {
        ESP_LOGW(TAG, "Failed to open NVS: %s", esp_err_to_name(err));
        return err;
    }

    err = nvs_set_str(handle, KEY_SCHEDULES, json);
    if (err == ESP_OK) {
        err = nvs_commit(handle);
    }
    nvs_close(handle);
    if (err == ESP_OK) {
        ESP_LOGI(TAG, "Saved schedules to NVS");
    }
    return err;
}

esp_err_t schedule_store_load_json(char *out_buf, size_t buf_len)
{
    if (out_buf == NULL || buf_len == 0) {
        return ESP_ERR_INVALID_ARG;
    }

    nvs_handle_t handle;
    esp_err_t err = nvs_open(NAMESPACE_NAME, NVS_READONLY, &handle);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        out_buf[0] = '\0';
        return ESP_OK;
    }
    if (err != ESP_OK) {
        return err;
    }

    size_t required_len = 0;
    err = nvs_get_str(handle, KEY_SCHEDULES, NULL, &required_len);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        out_buf[0] = '\0';
        nvs_close(handle);
        return ESP_OK;
    }
    if (err != ESP_OK) {
        nvs_close(handle);
        return err;
    }

    if (required_len > buf_len) {
        // Not enough space in buffer
        nvs_close(handle);
        return ESP_ERR_NO_MEM;
    }

    err = nvs_get_str(handle, KEY_SCHEDULES, out_buf, &required_len);
    nvs_close(handle);
    if (err == ESP_OK) {
        ESP_LOGI(TAG, "Loaded schedules from NVS");
    }
    return err;
}
