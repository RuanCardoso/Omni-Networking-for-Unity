using Omni.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;
#if OMNI_SERVER && !UNITY_EDITOR
using System.Threading;
#endif

#pragma warning disable IDE0051
#pragma warning disable IDE0044

namespace Omni.Core.Modules.UConsole
{
    public class NetworkConsole
    {
        private double _receivedBytes;
        private double _sentBytes;

        /// <summary>
        /// Triggered when the user provides an input, passing it as a string.
        /// </summary>
        public event Action<string> OnInput;

        internal void Initialize()
        {
#if OMNI_SERVER && !UNITY_EDITOR
            Thread thread =
                new(() => Read())
                {
                    Name = "Omni Console",
                    Priority = System.Threading.ThreadPriority.Lowest
                };

            thread.Start();
            NetworkManager.OnServerInitialized += () =>
            {
                // Bandwidth register
                NetworkManager.ServerSide.ReceivedBandwidth.OnAverageChanged += (avg, pps) => _receivedBytes = avg;
                NetworkManager.ServerSide.SentBandwidth.OnAverageChanged += (avg, pps) => _sentBytes = avg;
            };
#endif
        }

        private async void Read()
        {
            while (true)
            {
                string input = Console.ReadLine().Trim();
                string lowerInput = input.ToLowerInvariant();
                NetworkHelper.RunOnMainThread(() =>
                {
                    OnInput?.Invoke(lowerInput);
                    ProcessInternalCommands(lowerInput);
                });

                // async internal commands
                if (lowerInput == "stats -l")
                {
                    while (true)
                    {
                        if (EscapeIsPressed())
                            break;

                        await NetworkHelper.RunOnMainThreadAsync(() =>
                        {
                            PrintServerStats();
                        });

                        await Task.Delay(300);
                    }
                }
            }
        }

        /// <summary>
        /// Processes internal console commands.
        /// </summary>
        /// <param name="input">The command input string to process.</param>
        /// <remarks>
        /// Supported commands:
        /// - "stats": Displays server statistics including date, FPS, CPU usage, and bandwidth.
        /// - "clear": Clears the console screen and resets cursor position.
        /// - "printlog": Prints the player log using NetworkLogger.
        /// </remarks>
        public void ProcessInternalCommands(string input)
        {
            NetworkHelper.EnsureRunningOnMainThread();
            switch (input)
            {
                case "stats":
                    PrintServerStats();
                    break;
                case "clear":
                    Console.Clear();
                    Console.SetCursorPosition(0, 0);
                    break;
                case "printlog":
                    NetworkLogger.PrintPlayerLog();
                    break;
            }
        }

        /// <summary>
        /// Checks if the Escape key has been pressed.
        /// </summary>
        /// <returns>
        /// Returns true if the Escape key has been pressed, otherwise false.
        /// </returns>
        public bool EscapeIsPressed()
        {
            if (Console.KeyAvailable)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    Console.WriteLine("[ESC] Pressed. The loop has been stopped.");
                    return true;
                }
            }

            return false;
        }

        private void PrintServerStats()
        {
            NetworkHelper.EnsureRunningOnMainThread();
            // Date
            Console.WriteLine($"\r\nDate: {DateTime.Now}");

            // Fps and Cpu Ms
            Console.WriteLine(
                $"Fps: {NetworkManager.Framerate} | Cpu Ms: {NetworkManager.CpuTimeMs}"
            );

            // Bandwidth Rec and Sent
            Console.WriteLine($"Received: {_receivedBytes.ToSizeSuffix()}");
            Console.WriteLine($"Sent: {_sentBytes.ToSizeSuffix()}");

            // Players
            Console.WriteLine($"Players connected: {NetworkManager.ServerSide.Peers.Count}");
            Console.WriteLine($"Memory Usage: {((double)GC.GetTotalMemory(false)).ToSizeSuffix()}");
            Console.WriteLine($"Active Rooms: {NetworkManager.ServerSide.Groups.Where(g => !g.Value.IsSubGroup).Count()}");
        }
    }
}