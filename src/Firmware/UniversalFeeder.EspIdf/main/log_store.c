#include <string.h>
#include <stdlib.h>
#include "nvs.h"
#include "nvs_flash.h"
#include "esp_log.h"
#include "cJSON.h"
#include "log_store.h"

static const char *TAG = "LogStore";
static const char *NAMESPACE_NAME = "feeder";
static const char *KEY_LOGS = "logs";

// Maximum number of log entries to keep
#define LOG_STORE_MAX_ENTRIES 30

esp_err_t log_store_init(void)
{
    // NVS already initialized in app_main; nothing else to do here
    return ESP_OK;
}

esp_err_t log_store_append_json(const char *json)
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

    // Load existing
    size_t required_len = 0;
    err = nvs_get_str(handle, KEY_LOGS, NULL, &required_len);
    cJSON *root = NULL;
    if (err == ESP_OK && required_len > 0) {
        char *buf = malloc(required_len + 1);
        if (buf == NULL) {
            nvs_close(handle);
            return ESP_ERR_NO_MEM;
        }
        err = nvs_get_str(handle, KEY_LOGS, buf, &required_len);
        if (err == ESP_OK) {
            root = cJSON_Parse(buf);
        }
        free(buf);
    }

    if (root == NULL) {
        root = cJSON_CreateArray();
        if (root == NULL) {
            nvs_close(handle);
            return ESP_ERR_NO_MEM;
        }
    }

    // Parse incoming JSON entry
    cJSON *entry = cJSON_Parse(json);
    if (entry == NULL) {
        // Fallback: store as object with raw string
        entry = cJSON_CreateObject();
        cJSON_AddStringToObject(entry, "raw", json);
    }

    cJSON_AddItemToArray(root, entry); // ownership transferred

    // Trim to max entries
    while (cJSON_GetArraySize(root) > LOG_STORE_MAX_ENTRIES) {
        cJSON *old = cJSON_DetachItemFromArray(root, 0);
        if (old) cJSON_Delete(old);
    }

    char *out = cJSON_PrintUnformatted(root);
    if (out == NULL) {
        cJSON_Delete(root);
        nvs_close(handle);
        return ESP_ERR_NO_MEM;
    }

    err = nvs_set_str(handle, KEY_LOGS, out);
    if (err == ESP_OK) {
        err = nvs_commit(handle);
    }

    free(out);
    cJSON_Delete(root);
    nvs_close(handle);

    if (err == ESP_OK) {
        ESP_LOGI(TAG, "Appended log entry to NVS");
    }
    return err;
}

esp_err_t log_store_get_all_json(char *out_buf, size_t buf_len)
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
    err = nvs_get_str(handle, KEY_LOGS, NULL, &required_len);
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
        nvs_close(handle);
        return ESP_ERR_NO_MEM;
    }

    err = nvs_get_str(handle, KEY_LOGS, out_buf, &required_len);
    nvs_close(handle);
    return err;
}
