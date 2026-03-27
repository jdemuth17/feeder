#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "cJSON.h"
#include "esp_event.h"
#include "esp_log.h"
#include "mqtt_client.h"
#include "fallback_scheduler.h"
#include "mqtt_service.h"
#include "feeding_sequence.h"
#include "app_config.h"

static const char *TAG = "MqttService";

static esp_mqtt_client_handle_t s_client;
static char s_topic[64] = {0};

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
        }
    } else if (strcmp(action->valuestring, "chime") == 0) {
        cJSON *volume = cJSON_GetObjectItemCaseSensitive(root, "vol");
        float level = cJSON_IsNumber(volume) ? (float)volume->valuedouble : CHIME_DEFAULT_VOLUME;
        esp_err_t err = feeding_sequence_play_chime(level, CHIME_DURATION_MS);
        if (err != ESP_OK) {
            ESP_LOGW(TAG, "Chime command was rejected: %s", esp_err_to_name(err));
        }
    } else {
        ESP_LOGW(TAG, "Ignoring unknown MQTT action '%s'", action->valuestring);
    }

    cJSON_Delete(root);
}

static void mqtt_event_handler(void *handler_args, esp_event_base_t base, int32_t event_id, void *event_data)
{
    esp_mqtt_event_handle_t event = event_data;

    switch ((esp_mqtt_event_id_t)event_id) {
    case MQTT_EVENT_CONNECTED:
        ESP_LOGI(TAG, "Connected to broker; subscribing to %s", s_topic);
        fallback_scheduler_notify_mqtt_connected();
        esp_mqtt_client_subscribe(event->client, s_topic, 1);
        break;
    case MQTT_EVENT_DISCONNECTED:
        ESP_LOGW(TAG, "Disconnected from broker");
        fallback_scheduler_notify_mqtt_disconnected();
        break;
    case MQTT_EVENT_DATA:
        if ((size_t)event->topic_len == strlen(s_topic) && strncmp(event->topic, s_topic, event->topic_len) == 0) {
            handle_command(event->data, event->data_len);
        }
        break;
    case MQTT_EVENT_ERROR:
        ESP_LOGW(TAG, "MQTT event error");
        break;
    default:
        break;
    }
}

esp_err_t mqtt_service_init(void)
{
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

    esp_mqtt_client_config_t mqtt_config = {
        .broker.address.uri = MQTT_BROKER_URI,
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