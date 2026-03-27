idf_component_register(
    SRCS
        "app_main.c"
        "ble_provisioning.c"
        "buzzer_control.c"
        "device_identity.c"
        "fallback_scheduler.c"
        "feeding_sequence.c"
        "motor_control.c"
        "mqtt_service.c"
        "provisioning_store.c"
        "wifi_manager.c"
    INCLUDE_DIRS "include"
    REQUIRES bt driver esp_event esp_netif esp_wifi json mqtt nvs_flash
)
