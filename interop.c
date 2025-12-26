#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <systemd/sd-bus.h>
#include <unistd.h>

// --- Экспортируемые функции ---

// Устройства
void set_keyboard_backlight(int value);
void set_display_backlight(int percent); 
double get_battery_percentage();

// Медиа
void get_media_players(char* buffer, int max_len); 
void media_control(const char* target, const char* command); //хх

// Система
void send_notification(const char* title, const char* message);
void system_power_control(const char* action); 

static int call_system_method_simple(const char* service, const char* path, const char* interface, const char* method, const char* arg_type, void* arg_val) {
    sd_bus *bus = NULL;
    sd_bus_error error = SD_BUS_ERROR_NULL;
    int r;

    r = sd_bus_open_system(&bus);
    if (r < 0) return r;

    if (arg_type && arg_val) {
        if (strcmp(arg_type, "i") == 0) {
            r = sd_bus_call_method(bus, service, path, interface, method, &error, NULL, "i", *(int*)arg_val);
        } else if (strcmp(arg_type, "b") == 0) {
            r = sd_bus_call_method(bus, service, path, interface, method, &error, NULL, "b", *(int*)arg_val);
        }
    } else {
        r = sd_bus_call_method(bus, service, path, interface, method, &error, NULL, NULL);
    }
    
    sd_bus_error_free(&error);
    sd_bus_unref(bus);
    return r;
}

static int call_session_method_int(const char* service, const char* path, const char* interface, const char* method, int arg) {
    sd_bus *bus = NULL;
    sd_bus_error error = SD_BUS_ERROR_NULL;
    int r;

    r = sd_bus_open_user(&bus);
    if (r < 0) return r;

    r = sd_bus_call_method(bus, service, path, interface, method, &error, NULL, "i", arg);
    
    sd_bus_error_free(&error);
    sd_bus_unref(bus);
    return r;
}



void set_keyboard_backlight(int value) {
    // UPower (System Bus)
    call_system_method_simple(
        "org.freedesktop.UPower",
        "/org/freedesktop/UPower/KbdBacklight",
        "org.freedesktop.UPower.KbdBacklight",
        "SetBrightness",
        "i", &value
    );
}

void set_display_backlight(int percent) {
    // KDE Plasma PowerManagement (Session Bus)
    // Принимает значение от 0 до 100
    if (percent < 0) percent = 0;
    if (percent > 100) percent = 100;

    percent = percent *100;

    call_session_method_int(
        "org.kde.Solid.PowerManagement",
        "/org/kde/Solid/PowerManagement/Actions/BrightnessControl",
        "org.kde.Solid.PowerManagement.Actions.BrightnessControl",
        "setBrightness",
        percent
    );
}

double get_battery_percentage() {
    sd_bus *bus = NULL;
    sd_bus_error error = SD_BUS_ERROR_NULL;
    double level = -1.0;
    int r;

    r = sd_bus_open_system(&bus);
    if (r < 0) return -1.0;

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


void get_media_players(char* buffer, int max_len) {
    sd_bus *bus = NULL;
    char **names = NULL;
    int r;

    memset(buffer, 0, max_len);

    r = sd_bus_open_user(&bus);
    if (r < 0) return;

    r = sd_bus_list_names(bus, &names, NULL);
    if (r < 0) {
        sd_bus_unref(bus);
        return;
    }

    int offset = 0;
    for (char **ptr = names; *ptr; ptr++) {
        if (strstr(*ptr, "org.mpris.MediaPlayer2.")) {
            //трезаем префикс "org.mpris.MediaPlayer2." для красоты
            char* short_name = *ptr + 23; 
            
            // Проверка на переполнение буфера
            int len = strlen(short_name);
            if (offset + len + 2 >= max_len) break;

            if (offset > 0) {
                buffer[offset] = '|';
                offset++;
            }
            
            strcpy(buffer + offset, short_name);
            offset += len;
        }
    }

    free(names);
    sd_bus_unref(bus);
}

void media_control(const char* target_name, const char* command) {
    sd_bus *bus = NULL;
    sd_bus_error error = SD_BUS_ERROR_NULL;
    
    char full_service[256];
    snprintf(full_service, sizeof(full_service), "org.mpris.MediaPlayer2.%s", target_name);

    int r = sd_bus_open_user(&bus);
    if (r < 0) return;

    sd_bus_call_method(
        bus,
        full_service,
        "/org/mpris/MediaPlayer2",
        "org.mpris.MediaPlayer2.Player",
        command,
        &error,
        NULL,
        NULL
    );

    sd_bus_error_free(&error);
    sd_bus_unref(bus);
}


void send_notification(const char* title, const char* message) {
    sd_bus *bus = NULL;
    sd_bus_message *m = NULL;
    int r;

    r = sd_bus_open_user(&bus);
    if (r < 0) return;

    r = sd_bus_message_new_method_call(
        bus,
        &m,
        "org.freedesktop.Notifications",
        "/org/freedesktop/Notifications",
        "org.freedesktop.Notifications",
        "Notify"
    );
    if (r < 0) goto finish;

    // Сборка сообщения по частям
    r = sd_bus_message_append(m, "s", "ArchControl"); // app_name
    if (r < 0) goto finish;
    r = sd_bus_message_append(m, "u", 0);             // replaces_id
    if (r < 0) goto finish;
    r = sd_bus_message_append(m, "s", "input-gaming"); // app_icon (стандартный значок геймпада/управления)
    if (r < 0) goto finish;
    r = sd_bus_message_append(m, "s", title);         // summary
    if (r < 0) goto finish;
    r = sd_bus_message_append(m, "s", message);       // body
    if (r < 0) goto finish;
    
    // Пустой массив действий (actions)
    r = sd_bus_message_open_container(m, 'a', "s");
    r = sd_bus_message_close_container(m);
    
    // Пустой словарь подсказок (hints)
    r = sd_bus_message_open_container(m, 'a', "{sv}");
    r = sd_bus_message_close_container(m);

    r = sd_bus_message_append(m, "i", 3000);          // timeout (ms)

    // Отправка
    sd_bus_call(bus, m, 0, NULL, NULL);

finish:
    sd_bus_message_unref(m);
    sd_bus_unref(bus);
}

void system_power_control(const char* action) {
    int interactive = 1; // для запороленных 

    call_system_method_simple(
        "org.freedesktop.login1",
        "/org/freedesktop/login1",
        "org.freedesktop.login1.Manager",
        action,
        "b", &interactive
    );
}