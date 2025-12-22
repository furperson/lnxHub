#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <systemd/sd-bus.h>

// Экспортируемые функции
void set_keyboard_backlight(int value);
double get_battery_percentage();
void media_control(const char* command);

// Хелпер для System Bus (UPower)
static int call_system_method(const char* service, const char* path, const char* interface, const char* method, int arg) {
    sd_bus *bus = NULL;
    sd_bus_error error = SD_BUS_ERROR_NULL;
    sd_bus_message *m = NULL;
    int r;

    r = sd_bus_open_system(&bus);
    if (r < 0) return r;

    r = sd_bus_call_method(bus, service, path, interface, method, &error, &m, "i", arg);
    
    sd_bus_error_free(&error);
    sd_bus_message_unref(m);
    sd_bus_unref(bus);
    return r;
}

void set_keyboard_backlight(int value) {
    // UPower KbdBacklight interface from XML
    call_system_method(
        "org.freedesktop.UPower",
        "/org/freedesktop/UPower/KbdBacklight",
        "org.freedesktop.UPower.KbdBacklight",
        "SetBrightness",
        value
    );
}

double get_battery_percentage() {
    sd_bus *bus = NULL;
    sd_bus_error error = SD_BUS_ERROR_NULL;
    double level = -1.0;
    int r;

    r = sd_bus_open_system(&bus);
    if (r < 0) return -1.0;

    // Используем DisplayDevice - это виртуальное устройство, объединяющее батареи
    r = sd_bus_get_property_trivial(
        bus,
        "org.freedesktop.UPower",
        "/org/freedesktop/UPower/devices/DisplayDevice",
        "org.freedesktop.UPower.Device",
        "Percentage",
        &error,
        'd',
        &level
    );

    sd_bus_error_free(&error);
    sd_bus_unref(bus);
    return level;
}

// Поиск первого активного плеера MPRIS
void media_control(const char* command) {
    sd_bus *bus = NULL;
    char **names = NULL;
    int r;

    r = sd_bus_open_user(&bus);
    if (r < 0) return;

    r = sd_bus_list_names(bus, &names, NULL);
    if (r < 0) {
        sd_bus_unref(bus);
        return;
    }

    char *dest = NULL;
    for (char **ptr = names; *ptr; ptr++) {
        if (strstr(*ptr, "org.mpris.MediaPlayer2")) {
            dest = *ptr;
            break; 
        }
    }

    if (dest) {
        sd_bus_error error = SD_BUS_ERROR_NULL;
        sd_bus_call_method(
            bus,
            dest,
            "/org/mpris/MediaPlayer2",
            "org.mpris.MediaPlayer2.Player",
            command,
            &error,
            NULL,
            NULL
        );
        sd_bus_error_free(&error);
    }

    free(names);
    sd_bus_unref(bus);
}