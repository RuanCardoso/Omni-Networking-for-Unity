using Omni.Shared;
using System;
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
                OnInput?.Invoke(input);

                // Internal commands
                if (input.ToLower() == "stats")
                {
                    PrintServerStats();
                }
                else if (input.ToLower() == "stats -l")
                {
                    while (true)
                    {
                        if (EscapeIsPressed())
                        {
                            break;
                        }

                        PrintServerStats();
                        await Task.Delay(500);
                    }
                }
                else if (input.ToLower() == "clear")
                {
                    Console.Clear();
                    Console.SetCursorPosition(0, 0);
                }
                else if (input.ToLower() == "printlog")
                {
                    NetworkLogger.PrintPlayerLog();
                }
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
            // Date
            Console.WriteLine($"\r\nDate: {DateTime.Now}");

            // Fps and Cpu Ms
            Console.WriteLine(
                $"Fps: {NetworkManager.Framerate} | Cpu Ms: {NetworkManager.CpuTimeMs}"
            );

            // Bandwidth Rec and Sent
            Console.WriteLine($"Received: {_receivedBytes.ToSizeSuffix()}");
            Console.WriteLine($"Sent: {_sentBytes.ToSizeSuffix()}");
        }
    }
}