using System;
using System.Runtime.InteropServices;

namespace ArchControl
{
    class Program
    {
        private const string LibName = "./libinterop.so";

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_keyboard_backlight(int value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern double get_battery_percentage();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void media_control(string command);

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Arch Control System ===");
                Console.WriteLine("1. Media: Play/Pause");
                Console.WriteLine("2. Media: Next");
                Console.WriteLine("3. Media: Previous");
                Console.WriteLine("4. Kbd Backlight: Off (0)");
                Console.WriteLine("5. Kbd Backlight: Mid (1)");
                Console.WriteLine("6. Kbd Backlight: Max (2)");
                Console.WriteLine("7. Show Battery Info");
                Console.WriteLine("0. Exit");
                Console.Write("\nSelect: ");

                var key = Console.ReadKey(true).Key;

                try 
                {
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            media_control("PlayPause");
                            break;
                        case ConsoleKey.D2:
                            media_control("Next");
                            break;
                        case ConsoleKey.D3:
                            media_control("Previous");
                            break;
                        case ConsoleKey.D4:
                            set_keyboard_backlight(0);
                            break;
                        case ConsoleKey.D5:
                            set_keyboard_backlight(1);
                            break;
                        case ConsoleKey.D6:
                            set_keyboard_backlight(2);
                            break;
                        case ConsoleKey.D7:
                            ShowBattery();
                            break;
                        case ConsoleKey.D0:
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }

        static void ShowBattery()
        {
            Console.WriteLine("\nReading battery info...");
            double pct = get_battery_percentage();
            if (pct < 0)
                Console.WriteLine("Failed to read battery or no battery present.");
            else
                Console.WriteLine($"Battery: {pct:F1}%");
            
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}