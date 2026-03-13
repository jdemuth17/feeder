#include <string.h>
#include "nvs.h"
#include "nvs_flash.h"
#include "esp_log.h"
#include "provisioning_store.h"

static const char *TAG = "ProvisioningStore";
static const char *NAMESPACE_NAME = "feeder";
static const char *KEY_SSID = "wifi_ssid";
static const char *KEY_PASSWORD = "wifi_pass";
static const char *KEY_IP_ADDRESS = "wifi_ip";

static esp_err_t open_store(nvs_open_mode_t mode, nvs_handle_t *handle)
{
    return nvs_open(NAMESPACE_NAME, mode, handle);
}

esp_err_t provisioning_store_load_credentials(feeder_wifi_credentials_t *credentials)
{
    if (credentials == NULL) {
        return ESP_ERR_INVALID_ARG;
    }

    memset(credentials, 0, sizeof(*credentials));

    nvs_handle_t handle;
    esp_err_t err = open_store(NVS_READONLY, &handle);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        return ESP_OK;
    }
    if (err != ESP_OK) {
        return err;
    }

    size_t ssid_len = sizeof(credentials->ssid);
    err = nvs_get_str(handle, KEY_SSID, credentials->ssid, &ssid_len);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        nvs_close(handle);
        return ESP_OK;
    }
    if (err != ESP_OK) {
        nvs_close(handle);
        return err;
    }

    size_t password_len = sizeof(credentials->password);
    err = nvs_get_str(handle, KEY_PASSWORD, credentials->password, &password_len);
    nvs_close(handle);
    if (err != ESP_OK) {
        return err;
    }

    credentials->is_configured = credentials->ssid[0] != '\0';
    return ESP_OK;
}

esp_err_t provisioning_store_save_credentials(const feeder_wifi_credentials_t *credentials)
{
    if (credentials == NULL || credentials->ssid[0] == '\0') {
        return ESP_ERR_INVALID_ARG;
    }

    nvs_handle_t handle;
    esp_err_t err = open_store(NVS_READWRITE, &handle);
    if (err != ESP_OK) {
        return err;
    }

    err = nvs_set_str(handle, KEY_SSID, credentials->ssid);
    if (err == ESP_OK) {
        err = nvs_set_str(handle, KEY_PASSWORD, credentials->password);
    }
    if (err == ESP_OK) {
        err = nvs_commit(handle);
    }
    nvs_close(handle);

    if (err == ESP_OK) {
        ESP_LOGI(TAG, "Stored Wi-Fi credentials for SSID '%s'", credentials->ssid);
    }
    return err;
}

esp_err_t provisioning_store_clear_credentials(void)
{
    nvs_handle_t handle;
    esp_err_t err = open_store(NVS_READWRITE, &handle);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        return ESP_OK;
    }
    if (err != ESP_OK) {
        return err;
    }

    err = nvs_erase_key(handle, KEY_SSID);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        err = ESP_OK;
    }
    if (err == ESP_OK) {
        esp_err_t password_err = nvs_erase_key(handle, KEY_PASSWORD);
        if (password_err != ESP_OK && password_err != ESP_ERR_NVS_NOT_FOUND) {
            err = password_err;
        }
    }
    if (err == ESP_OK) {
        err = nvs_commit(handle);
    }
    nvs_close(handle);
    return err;
}

esp_err_t provisioning_store_load_ip_address(char *ip_address, size_t ip_address_size)
{
    if (ip_address == NULL || ip_address_size == 0) {
        return ESP_ERR_INVALID_ARG;
    }

    memset(ip_address, 0, ip_address_size);

    nvs_handle_t handle;
    esp_err_t err = open_store(NVS_READONLY, &handle);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        strncpy(ip_address, FEEDER_IP_ADDRESS_UNASSIGNED, ip_address_size - 1);
        return ESP_OK;
    }
    if (err != ESP_OK) {
        return err;
    }

    size_t ip_len = ip_address_size;
    err = nvs_get_str(handle, KEY_IP_ADDRESS, ip_address, &ip_len);
    nvs_close(handle);
    if (err == ESP_ERR_NVS_NOT_FOUND) {
        strncpy(ip_address, FEEDER_IP_ADDRESS_UNASSIGNED, ip_address_size - 1);
        return ESP_OK;
    }

    return err;
}

esp_err_t provisioning_store_save_ip_address(const char *ip_address)
{
    if (ip_address == NULL || ip_address[0] == '\0') {
        return ESP_ERR_INVALID_ARG;
    }

    nvs_handle_t handle;
    esp_err_t err = open_store(NVS_READWRITE, &handle);
    if (err != ESP_OK) {
        return err;
    }

    err = nvs_set_str(handle, KEY_IP_ADDRESS, ip_address);
    if (err == ESP_OK) {
        err = nvs_commit(handle);
    }
    nvs_close(handle);
    return err;
}