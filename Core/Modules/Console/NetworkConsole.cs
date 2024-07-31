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
                NetworkManager.Server.ReceivedBandwidth.OnAverageChanged += (avg) =>
                    _receivedBytes = avg;
                NetworkManager.Server.SentBandwidth.OnAverageChanged += (avg) => _sentBytes = avg;
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
                        if (Console.KeyAvailable)
                        {
                            if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                            {
                                break;
                            }
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
            }
        }

        private void PrintServerStats()
        {
            // Date
            Console.WriteLine($"\r\nDate: {DateTime.Now}\r\n");

            // Fps and Cpu Ms
            Console.WriteLine(
                $"Fps: {NetworkManager.Framerate} | Cpu Ms: {NetworkManager.CpuTimeMs}\r\n"
            );

            // Bandwidth Rec and Sent
            Console.WriteLine($"Received: {_receivedBytes.ToSizeSuffix()}\r\n");
            Console.WriteLine($"Sent: {_sentBytes.ToSizeSuffix()}\r\n");
        }
    }
}
