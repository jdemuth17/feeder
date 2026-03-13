#include <stdbool.h>
#include <string.h>
#include "esp_bt.h"
#include "esp_err.h"
#include "esp_log.h"
#include "host/ble_hs.h"
#include "host/ble_gatt.h"
#include "host/ble_uuid.h"
#include "nimble/nimble_port.h"
#include "nimble/nimble_port_freertos.h"
#include "services/gap/ble_svc_gap.h"
#include "services/gatt/ble_svc_gatt.h"
#include "ble_provisioning.h"
#include "app_config.h"
#include "provisioning_store.h"

static const char *TAG = "BleProvisioning";

static ble_provisioning_credentials_cb_t s_credentials_cb;
static feeder_wifi_credentials_t s_pending_credentials;
static char s_ip_address[FEEDER_IP_ADDRESS_MAX_LEN] = FEEDER_IP_ADDRESS_UNASSIGNED;
static bool s_stack_started;
static uint16_t s_connection_handle = BLE_HS_CONN_HANDLE_NONE;
static uint16_t s_ip_char_handle;
static uint8_t s_own_addr_type;

static const ble_uuid128_t s_service_uuid = BLE_UUID128_INIT(
    0x4b, 0x91, 0x31, 0xc3, 0xc9, 0xc5, 0xcc, 0x8f,
    0x9e, 0x45, 0xb5, 0x1f, 0x01, 0xc2, 0xaf, 0x4f);
static const ble_uuid128_t s_ssid_uuid = BLE_UUID128_INIT(
    0xa8, 0x26, 0x1b, 0x36, 0x07, 0xea, 0xf5, 0xb7,
    0x88, 0x46, 0xe1, 0x36, 0x3e, 0x48, 0xb5, 0xbe);
static const ble_uuid128_t s_password_uuid = BLE_UUID128_INIT(
    0x01, 0x00, 0x00, 0xea, 0x90, 0x03, 0x04, 0xba,
    0x94, 0x45, 0xf4, 0x8e, 0xa1, 0x8b, 0xe9, 0xd6);
static const ble_uuid128_t s_ip_uuid = BLE_UUID128_INIT(
    0x01, 0x00, 0x00, 0xea, 0x90, 0x03, 0x04, 0xba,
    0x94, 0x45, 0xf4, 0x8e, 0x01, 0x00, 0xa0, 0xe2);

static void ble_app_advertise(void);

static esp_err_t maybe_commit_credentials(void)
{
    if (s_pending_credentials.ssid[0] == '\0' || s_pending_credentials.password[0] == '\0') {
        return ESP_OK;
    }

    s_pending_credentials.is_configured = true;
    esp_err_t err = provisioning_store_save_credentials(&s_pending_credentials);
    if (err != ESP_OK) {
        return err;
    }

    if (s_credentials_cb != NULL) {
        s_credentials_cb(&s_pending_credentials);
    }

    return ESP_OK;
}

static int ssid_access_cb(uint16_t conn_handle, uint16_t attr_handle, struct ble_gatt_access_ctxt *ctxt, void *arg)
{
    if (ctxt->op != BLE_GATT_ACCESS_OP_WRITE_CHR) {
        return BLE_ATT_ERR_UNLIKELY;
    }

    uint16_t copy_len = OS_MBUF_PKTLEN(ctxt->om);
    if (copy_len >= sizeof(s_pending_credentials.ssid)) {
        return BLE_ATT_ERR_INVALID_ATTR_VALUE_LEN;
    }

    int rc = ble_hs_mbuf_to_flat(ctxt->om, s_pending_credentials.ssid, sizeof(s_pending_credentials.ssid) - 1, &copy_len);
    if (rc != 0) {
        return BLE_ATT_ERR_UNLIKELY;
    }

    s_pending_credentials.ssid[copy_len] = '\0';
    ESP_LOGI(TAG, "Received SSID over BLE: %s", s_pending_credentials.ssid);

    esp_err_t err = maybe_commit_credentials();
    return err == ESP_OK ? 0 : BLE_ATT_ERR_UNLIKELY;
}

static int password_access_cb(uint16_t conn_handle, uint16_t attr_handle, struct ble_gatt_access_ctxt *ctxt, void *arg)
{
    if (ctxt->op != BLE_GATT_ACCESS_OP_WRITE_CHR) {
        return BLE_ATT_ERR_UNLIKELY;
    }

    uint16_t copy_len = OS_MBUF_PKTLEN(ctxt->om);
    if (copy_len >= sizeof(s_pending_credentials.password)) {
        return BLE_ATT_ERR_INVALID_ATTR_VALUE_LEN;
    }

    int rc = ble_hs_mbuf_to_flat(ctxt->om, s_pending_credentials.password, sizeof(s_pending_credentials.password) - 1, &copy_len);
    if (rc != 0) {
        return BLE_ATT_ERR_UNLIKELY;
    }

    s_pending_credentials.password[copy_len] = '\0';
    ESP_LOGI(TAG, "Received Wi-Fi password over BLE (%u bytes)", (unsigned)copy_len);

    esp_err_t err = maybe_commit_credentials();
    return err == ESP_OK ? 0 : BLE_ATT_ERR_UNLIKELY;
}

static int ip_access_cb(uint16_t conn_handle, uint16_t attr_handle, struct ble_gatt_access_ctxt *ctxt, void *arg)
{
    if (ctxt->op != BLE_GATT_ACCESS_OP_READ_CHR) {
        return BLE_ATT_ERR_UNLIKELY;
    }

    int rc = os_mbuf_append(ctxt->om, s_ip_address, strlen(s_ip_address));
    return rc == 0 ? 0 : BLE_ATT_ERR_INSUFFICIENT_RES;
}

static const struct ble_gatt_svc_def s_services[] = {
    {
        .type = BLE_GATT_SVC_TYPE_PRIMARY,
        .uuid = &s_service_uuid.u,
        .characteristics = (struct ble_gatt_chr_def[]) {
            {
                .uuid = &s_ssid_uuid.u,
                .access_cb = ssid_access_cb,
                .flags = BLE_GATT_CHR_F_WRITE,
            },
            {
                .uuid = &s_password_uuid.u,
                .access_cb = password_access_cb,
                .flags = BLE_GATT_CHR_F_WRITE,
            },
            {
                .uuid = &s_ip_uuid.u,
                .access_cb = ip_access_cb,
                .flags = BLE_GATT_CHR_F_READ | BLE_GATT_CHR_F_NOTIFY,
                .val_handle = &s_ip_char_handle,
            },
            {0}
        },
    },
    {0},
};

static void ble_host_task(void *param)
{
    nimble_port_run();
    nimble_port_freertos_deinit();
}

static int gap_event_cb(struct ble_gap_event *event, void *arg)
{
    switch (event->type) {
    case BLE_GAP_EVENT_CONNECT:
        if (event->connect.status == 0) {
            s_connection_handle = event->connect.conn_handle;
            ESP_LOGI(TAG, "BLE client connected");
        } else {
            ESP_LOGW(TAG, "BLE connection failed; restarting advertisement");
            ble_app_advertise();
        }
        return 0;
    case BLE_GAP_EVENT_DISCONNECT:
        ESP_LOGI(TAG, "BLE client disconnected");
        s_connection_handle = BLE_HS_CONN_HANDLE_NONE;
        ble_app_advertise();
        return 0;
    case BLE_GAP_EVENT_SUBSCRIBE:
        ESP_LOGI(TAG, "Client subscription updated for IP notifications");
        return 0;
    case BLE_GAP_EVENT_ADV_COMPLETE:
        ble_app_advertise();
        return 0;
    default:
        return 0;
    }
}

static void ble_app_on_sync(void)
{
    int rc = ble_hs_id_infer_auto(0, &s_own_addr_type);
    if (rc != 0) {
        ESP_LOGE(TAG, "Failed to infer BLE address type: %d", rc);
        return;
    }

    ble_addr_t address;
    rc = ble_hs_id_copy_addr(s_own_addr_type, address.val, NULL);
    if (rc != 0) {
        ESP_LOGE(TAG, "Failed to copy BLE address: %d", rc);
        return;
    }

    ble_app_advertise();
}

static void ble_app_advertise(void)
{
    struct ble_hs_adv_fields fields = {0};
    fields.flags = BLE_HS_ADV_F_DISC_GEN | BLE_HS_ADV_F_BREDR_UNSUP;
    fields.uuids128 = (ble_uuid128_t *)&s_service_uuid;
    fields.num_uuids128 = 1;
    fields.uuids128_is_complete = 1;

    int rc = ble_gap_adv_set_fields(&fields);
    if (rc != 0) {
        ESP_LOGE(TAG, "Failed to set advertisement fields: %d", rc);
        return;
    }

    struct ble_hs_adv_fields scan_response_fields = {0};
    scan_response_fields.name = (const uint8_t *)DEVICE_NAME_PREFIX;
    scan_response_fields.name_len = strlen(DEVICE_NAME_PREFIX);
    scan_response_fields.name_is_complete = 1;

    rc = ble_gap_adv_rsp_set_fields(&scan_response_fields);
    if (rc != 0) {
        ESP_LOGE(TAG, "Failed to set scan response fields: %d", rc);
        return;
    }

    struct ble_gap_adv_params adv_params = {0};
    adv_params.conn_mode = BLE_GAP_CONN_MODE_UND;
    adv_params.disc_mode = BLE_GAP_DISC_MODE_GEN;

    rc = ble_gap_adv_start(s_own_addr_type, NULL, BLE_HS_FOREVER, &adv_params, gap_event_cb, NULL);
    if (rc != 0) {
        ESP_LOGE(TAG, "Failed to start advertising: %d", rc);
        return;
    }

    ESP_LOGI(TAG, "Advertising provisioning service as %s", DEVICE_NAME_PREFIX);
}

esp_err_t ble_provisioning_set_ip_address(const char *ip_address)
{
    if (ip_address == NULL || ip_address[0] == '\0') {
        return ESP_ERR_INVALID_ARG;
    }

    strncpy(s_ip_address, ip_address, sizeof(s_ip_address) - 1);
    s_ip_address[sizeof(s_ip_address) - 1] = '\0';

    esp_err_t err = provisioning_store_save_ip_address(s_ip_address);
    if (err != ESP_OK) {
        return err;
    }

    if (s_connection_handle != BLE_HS_CONN_HANDLE_NONE) {
        ble_gatts_chr_updated(s_ip_char_handle);
    }

    ESP_LOGI(TAG, "Updated IP characteristic to %s", s_ip_address);
    return ESP_OK;
}

esp_err_t ble_provisioning_start(
    const feeder_wifi_credentials_t *initial_credentials,
    const char *initial_ip_address,
    ble_provisioning_credentials_cb_t credentials_cb)
{
    if (s_stack_started) {
        return ESP_OK;
    }

    memset(&s_pending_credentials, 0, sizeof(s_pending_credentials));
    if (initial_credentials != NULL) {
        s_pending_credentials = *initial_credentials;
    }

    if (initial_ip_address != NULL && initial_ip_address[0] != '\0') {
        strncpy(s_ip_address, initial_ip_address, sizeof(s_ip_address) - 1);
        s_ip_address[sizeof(s_ip_address) - 1] = '\0';
    }

    s_credentials_cb = credentials_cb;

    esp_err_t err = esp_bt_controller_mem_release(ESP_BT_MODE_CLASSIC_BT);
    if (err != ESP_OK && err != ESP_ERR_INVALID_STATE) {
        ESP_LOGE(TAG, "Failed to release Classic BT memory: %s", esp_err_to_name(err));
        return err;
    }

    err = nimble_port_init();
    if (err != ESP_OK) {
        ESP_LOGE(TAG, "nimble_port_init failed: %s", esp_err_to_name(err));
        return err;
    }

    ble_hs_cfg.sync_cb = ble_app_on_sync;

    ble_svc_gap_init();
    ble_svc_gatt_init();

    int rc;

    rc = ble_svc_gap_device_name_set(DEVICE_NAME_PREFIX);
    if (rc != 0) {
        ESP_LOGE(TAG, "ble_svc_gap_device_name_set failed: rc=%d", rc);
        return ESP_FAIL;
    }

    rc = ble_gatts_count_cfg(s_services);
    if (rc != 0) {
        ESP_LOGE(TAG, "ble_gatts_count_cfg failed: rc=%d", rc);
        return ESP_FAIL;
    }

    rc = ble_gatts_add_svcs(s_services);
    if (rc != 0) {
        ESP_LOGE(TAG, "ble_gatts_add_svcs failed: rc=%d", rc);
        return ESP_FAIL;
    }

    s_stack_started = true;
    nimble_port_freertos_init(ble_host_task);
    return ESP_OK;
}