#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "cJSON.h"
#include "esp_event.h"
#include "esp_crt_bundle.h"
#include "esp_log.h"
#include "mqtt_client.h"
#include "fallback_scheduler.h"
#include "mqtt_service.h"
#include "feeding_sequence.h"
#include "schedule_manager.h"
#include "app_config.h"
#include "log_store.h"

static const char *TAG = "MqttService";

static esp_mqtt_client_handle_t s_client;
static char s_topic[64] = {0};

static char s_log_topic[64] = {0};
static char s_schedule_topic[64] = {0};
static char s_device_id_local[FEEDER_DEVICE_ID_MAX_LEN] = {0};

static void handle_command(const char *payload, size_t payload_len)
{
    cJSON *root = cJSON_ParseWithLength(payload, payload_len);
    if (root == NULL) {
        ESP_LOGW(TAG, "Ignoring invalid MQTT payload");
        return;
    }

    cJSON *action = cJSON_GetObjectItemCaseSensitive(root, "action");
    if (!cJSON_IsString(action) || action->valuestring == NULL) {
        ESP_LOGW(TAG, "Ignoring MQTT payload without action");
        cJSON_Delete(root);
        return;
    }

    if (strcmp(action->valuestring, "feed") == 0) {
        cJSON *duration = cJSON_GetObjectItemCaseSensitive(root, "ms");
        int duration_ms = cJSON_IsNumber(duration) ? duration->valueint : FEEDER_DEFAULT_DURATION_MS;
        esp_err_t err = feeding_sequence_start(duration_ms);
        if (err != ESP_OK) {
            ESP_LOGW(TAG, "Feed command was rejected: %s", esp_err_to_name(err));
        } else {
            fallback_scheduler_note_feed_event();
            feeding_sequence_publish_log(true, "manual feed", true);
        }
    } else if (strcmp(action->valuestring, "chime") == 0) {
        cJSON *volume = cJSON_GetObjectItemCaseSensitive(root, "vol");
        float level = cJSON_IsNumber(volume) ? (float)volume->valuedouble : CHIME_DEFAULT_VOLUME;
        esp_err_t err = feeding_sequence_play_chime(level, CHIME_DURATION_MS);
        if (err != ESP_OK) {
            ESP_LOGW(TAG, "Chime command was rejected: %s", esp_err_to_name(err));
        }
    } else if (strcmp(action->valuestring, "get_schedule") == 0) {
        char *sched_json = schedule_manager_get_json();
        if (sched_json != NULL) {
            // Wrap in {"action":"schedule_list","schedule":[...]}
            char *response = malloc(strlen(sched_json) + 64);
            if (response != NULL) {
                snprintf(response, strlen(sched_json) + 64,
                         "{\"action\":\"schedule_list\",\"schedule\":%s}", sched_json);
                esp_mqtt_client_publish(s_client, s_schedule_topic, response, 0, 1, 0);
                free(response);
            }
            free(sched_json);
        } else {
            esp_mqtt_client_publish(s_client, s_schedule_topic,
                                    "{\"action\":\"schedule_list\",\"schedule\":[]}", 0, 1, 0);
        }
    } else if (strcmp(action->valuestring, "request_logs") == 0) {
        // App requested stored logs; publish each stored entry to log topic
        char *buf = malloc(16384);
        if (buf != NULL) {
            esp_err_t r = log_store_get_all_json(buf, 16384);
            if (r == ESP_OK && buf[0] != '\0') {
                cJSON *arr = cJSON_Parse(buf);
                if (arr != NULL && cJSON_IsArray(arr)) {
                    cJSON *item = NULL;
                    cJSON_ArrayForEach(item, arr) {
                        char *entry = cJSON_PrintUnformatted(item);
                        if (entry != NULL) {
                            if (s_client != NULL) {
                                esp_mqtt_client_publish(s_client, s_log_topic, entry, 0, 1, 0);
                            }
                            free(entry);
                        }
                    }
                    cJSON_Delete(arr);
                }
            }
            free(buf);
        }
    } else {
        ESP_LOGW(TAG, "Ignoring unknown MQTT action '%s'", action->valuestring);
    }

    cJSON_Delete(root);
}

static void handle_schedule(const char *payload, size_t payload_len)
{
    if (payload == NULL || payload_len == 0) {
        ESP_LOGW(TAG, "Empty schedule payload");
        return;
    }

    char *buf = malloc(payload_len + 1);
    if (buf == NULL) {
        ESP_LOGW(TAG, "OOM copying schedule payload");
        return;
    }
    memcpy(buf, payload, payload_len);
    buf[payload_len] = '\0';

    bool applied = false;
    cJSON *root = cJSON_Parse(buf);
    if (root != NULL) {
        if (cJSON_IsArray(root)) {
            applied = (schedule_manager_apply_schedule_json(buf) == ESP_OK);
        } else {
            cJSON *action = cJSON_GetObjectItemCaseSensitive(root, "action");
            if (cJSON_IsString(action) && action->valuestring != NULL && strcmp(action->valuestring, "set_schedule") == 0) {
                cJSON *schedule = cJSON_GetObjectItemCaseSensitive(root, "schedule");
                if (cJSON_IsArray(schedule)) {
                    char *sched_str = cJSON_PrintUnformatted(schedule);
                    if (sched_str != NULL) {
                        applied = (schedule_manager_apply_schedule_json(sched_str) == ESP_OK);
                        free(sched_str);
                    }
                }
            }
        }
        cJSON_Delete(root);
    }

    // publish ack as a log entry
    char ack[128];
    snprintf(ack, sizeof(ack), "{\"action\":\"ack_schedule\",\"success\":%s}", applied ? "true" : "false");
    mqtt_service_publish_log(s_device_id_local, ack);

    free(buf);
}

static void mqtt_event_handler(void *handler_args, esp_event_base_t base, int32_t event_id, void *event_data)
{
    esp_mqtt_event_handle_t event = event_data;

    switch ((esp_mqtt_event_id_t)event_id) {
    case MQTT_EVENT_CONNECTED:
        ESP_LOGI(TAG, "Connected to broker; subscribing to %s and %s", s_topic, s_schedule_topic);
        fallback_scheduler_notify_mqtt_connected();
        esp_mqtt_client_subscribe(event->client, s_topic, 1);
        esp_mqtt_client_subscribe(event->client, s_schedule_topic, 1);
        break;
    case MQTT_EVENT_DISCONNECTED:
        ESP_LOGW(TAG, "Disconnected from broker");
        fallback_scheduler_notify_mqtt_disconnected();
        break;
    case MQTT_EVENT_DATA:
        if ((size_t)event->topic_len == strlen(s_topic) && strncmp(event->topic, s_topic, event->topic_len) == 0) {
            handle_command(event->data, event->data_len);
        } else if ((size_t)event->topic_len == strlen(s_schedule_topic) && strncmp(event->topic, s_schedule_topic, event->topic_len) == 0) {
            handle_schedule(event->data, event->data_len);
        }
        break;
    case MQTT_EVENT_ERROR:
        ESP_LOGW(TAG, "MQTT event error");
        if (event->error_handle != NULL) {
            ESP_LOGW(
                TAG,
                "MQTT transport error type=%d esp_err=0x%x tls_stack=0x%x cert_flags=0x%x sock_errno=%d connect_rc=%d",
                event->error_handle->error_type,
                event->error_handle->esp_tls_last_esp_err,
                event->error_handle->esp_tls_stack_err,
                event->error_handle->esp_tls_cert_verify_flags,
                event->error_handle->esp_transport_sock_errno,
                event->error_handle->connect_return_code);
        }
        break;
    default:
        break;
    }
}

esp_err_t mqtt_service_init(void)
{
    // initialize log store
    log_store_init();
    return ESP_OK;
}

esp_err_t mqtt_service_start(const char *device_id)
{
    if (device_id == NULL || device_id[0] == '\0') {
        return ESP_ERR_INVALID_ARG;
    }

    if (s_client != NULL) {
        return ESP_OK;
    }

    fallback_scheduler_notify_mqtt_disconnected();


    snprintf(s_topic, sizeof(s_topic), "%s%s%s", MQTT_TOPIC_PREFIX, device_id, MQTT_TOPIC_SUFFIX);
    snprintf(s_log_topic, sizeof(s_log_topic), "%s%s/logs", MQTT_TOPIC_PREFIX, device_id);
    snprintf(s_schedule_topic, sizeof(s_schedule_topic), "%s%s/schedule", MQTT_TOPIC_PREFIX, device_id);
    // store a local copy of the device id for publish helpers
    strncpy(s_device_id_local, device_id, sizeof(s_device_id_local) - 1);
    s_device_id_local[sizeof(s_device_id_local) - 1] = '\0';

    esp_mqtt_client_config_t mqtt_config = {
        .broker.address.uri = MQTT_BROKER_URI,
        .broker.verification.crt_bundle_attach = esp_crt_bundle_attach,
        .credentials.username = MQTT_USERNAME,
        .credentials.authentication.password = MQTT_PASSWORD,
        .session.keepalive = 30,
        .network.disable_auto_reconnect = false,
    };

    s_client = esp_mqtt_client_init(&mqtt_config);
    if (s_client == NULL) {
        return ESP_FAIL;
    }

    esp_mqtt_client_register_event(s_client, ESP_EVENT_ANY_ID, mqtt_event_handler, NULL);
    return esp_mqtt_client_start(s_client);
}

// Publish a log/event message to the log topic
esp_err_t mqtt_service_publish_log(const char *device_id, const char *log_json)
{
    if (s_client == NULL || device_id == NULL || log_json == NULL) {
        return ESP_ERR_INVALID_STATE;
    }
    // Persist the log locally first
    log_store_append_json(log_json);

    // Use precomputed s_log_topic
    int msg_id = esp_mqtt_client_publish(s_client, s_log_topic, log_json, 0, 1, 0);
    if (msg_id < 0) {
        ESP_LOGW(TAG, "Failed to publish log message");
        return ESP_FAIL;
    }
    ESP_LOGI(TAG, "Published log message: %s", log_json);
    return ESP_OK;
}