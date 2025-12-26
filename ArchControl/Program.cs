using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

namespace ArchControl
{
    class Program
    {
        private const string LibName = "./libinterop.so";

        // --- P/Invoke ---
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_keyboard_backlight(int value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_display_backlight(int percent);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double get_battery_percentage();

        // command: PlayPause, Next, Previous
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void media_control(string target, string command);

        // buffer заполняется в C, max_len - размер буфера
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void get_media_players(StringBuilder buffer, int max_len);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_notification(string title, string message);

        // action: PowerOff, Reboot, Suspend
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void system_power_control(string action);


        // --- Логика UI ---
        static string _selectedPlayer = null;

        static void Main(string[] args)
        {
            // Приветственное уведомление
            try { send_notification("Arch Control", "System Interface Loaded"); } catch { }

            bool running = true;
            while (running)
            {
                Console.Clear();
                Console.WriteLine("=== THINKPAD CONTROL HUB ===");
                Console.WriteLine($"Current Player: {(_selectedPlayer ?? "NONE")}");
                Console.WriteLine("----------------------------");
                Console.WriteLine("1. Media Controls >");
                Console.WriteLine("2. Hardware Control (Display/Kbd/Bat) >");
                Console.WriteLine("3. Power Menu >");
                Console.WriteLine("0. Exit");
                Console.Write("\nSelect: ");

                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.D1:
                        MediaMenu();
                        break;
                    case ConsoleKey.D2:
                        HardwareMenu();
                        break;
                    case ConsoleKey.D3:
                        PowerMenu();
                        break;
                    case ConsoleKey.D0:
                        running = false;
                        break;
                }
            }
        }

        static void MediaMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine($"=== MEDIA CONTROL ({(_selectedPlayer ?? "No Player Selected")}) ===");
                Console.WriteLine("1. Select Player (Refresh List)");
                
                if (!string.IsNullOrEmpty(_selectedPlayer))
                {
                    Console.WriteLine("2. Play / Pause");
                    Console.WriteLine("3. Next Track");
                    Console.WriteLine("4. Previous Track");
                }
                
                Console.WriteLine("0. Back");
                Console.Write("\nSelect: ");

                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.D0) return;

                if (key == ConsoleKey.D1)
                {
                    SelectPlayerScreen();
                }
                else if (!string.IsNullOrEmpty(_selectedPlayer))
                {
                    try 
                    {
                        switch (key)
                        {
                            case ConsoleKey.D2:
                                media_control(_selectedPlayer, "PlayPause");
                                send_notification("Media", "Play/Pause toggled");
                                break;
                            case ConsoleKey.D3:
                                media_control(_selectedPlayer, "Next");
                                send_notification("Media", "Next Track");
                                break;
                            case ConsoleKey.D4:
                                media_control(_selectedPlayer, "Previous");
                                send_notification("Media", "Previous Track");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        Console.ReadKey();
                    }
                }
            }
        }

        static void SelectPlayerScreen()
        {
            Console.Clear();
            Console.WriteLine("Scanning D-Bus for players...");
            
            StringBuilder buffer = new StringBuilder(1024);
            get_media_players(buffer, 1024);
            string raw = buffer.ToString();

            if (string.IsNullOrWhiteSpace(raw))
            {
                Console.WriteLine("No active MPRIS players found.");
                Console.WriteLine("Press any key to back...");
                Console.ReadKey();
                return;
            }

            var players = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            
            Console.WriteLine("\nAvailable Players:");
            for (int i = 0; i < players.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {players[i]}");
            }

            Console.Write("\nEnter number to select (or 0 to cancel): ");
            var input = Console.ReadLine();
            if (int.TryParse(input, out int idx) && idx > 0 && idx <= players.Length)
            {
                _selectedPlayer = players[idx - 1];
                Console.WriteLine($"Selected: {_selectedPlayer}");
            }
        }

        static void HardwareMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== HARDWARE CONTROL ===");
                Console.WriteLine("1. Keyboard Backlight: OFF");
                Console.WriteLine("2. Keyboard Backlight: MID");
                Console.WriteLine("3. Keyboard Backlight: MAX");
                Console.WriteLine("------------------------");
                Console.WriteLine("4. Display Brightness: 25%");
                Console.WriteLine("5. Display Brightness: 50%");
                Console.WriteLine("6. Display Brightness: 75%");
                Console.WriteLine("7. Display Brightness: 100%");
                Console.WriteLine("------------------------");
                Console.WriteLine("8. Show Battery Status");
                Console.WriteLine("0. Back");
                Console.Write("\nSelect: ");

                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.D0) return;

                try
                {
                    switch (key)
                    {
                        case ConsoleKey.D1: set_keyboard_backlight(0); break;
                        case ConsoleKey.D2: set_keyboard_backlight(1); break;
                        case ConsoleKey.D3: set_keyboard_backlight(2); break;
                        
                        case ConsoleKey.D4: set_display_backlight(25); break;
                        case ConsoleKey.D5: set_display_backlight(50); break;
                        case ConsoleKey.D6: set_display_backlight(75); break;
                        case ConsoleKey.D7: set_display_backlight(100); break;

                        case ConsoleKey.D8:
                            double bat = get_battery_percentage();
                            Console.WriteLine($"\nBattery: {(bat < 0 ? "Error" : bat.ToString("F1") + "%")}");
                            Console.WriteLine("Press any key...");
                            Console.ReadKey();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }

        static void PowerMenu()
        {
            Console.Clear();
            Console.WriteLine("=== SYSTEM POWER ===");
            Console.WriteLine("1. Suspend (Sleep)");
            Console.WriteLine("2. Reboot");
            Console.WriteLine("3. Power Off");
            Console.WriteLine("0. Back");
            Console.Write("\nSelect: ");

            var key = Console.ReadKey(true).Key;
            try
            {
                switch (key)
                {
                    case ConsoleKey.D1:
                        Console.WriteLine("Suspending...");
                        system_power_control("Suspend");
                        break;
                    case ConsoleKey.D2:
                        Console.WriteLine("Rebooting...");
                        system_power_control("Reboot");
                        break;
                    case ConsoleKey.D3:
                        Console.WriteLine("Shutting down...");
                        system_power_control("PowerOff");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }
        }
    }
}