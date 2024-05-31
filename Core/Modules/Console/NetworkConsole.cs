using System;
#if OMNI_SERVER && !UNITY_EDITOR
using System.Threading;
#endif

#pragma warning disable IDE0051

namespace Omni.Core.Modules.UConsole
{
    public class NetworkConsole
    {
        public event Action<string> OnInput;

        internal void Initialize()
        {
#if OMNI_SERVER && !UNITY_EDITOR
            Thread thread =
                new(() => Read())
                {
                    Name = "Server Console",
                    Priority = System.Threading.ThreadPriority.Lowest
                };

            SkipDefaultUnityLog();
            ShowDefaultOmniLog();
            thread.Start();
#endif
        }

        private void SkipDefaultUnityLog()
        {
            Console.Clear();
        }

        private void ShowDefaultOmniLog()
        {
            Console.WriteLine("Welcome to Omni Server Console.");
#if OMNI_DEBUG
            Console.WriteLine("You are in Debug Mode.");
#else
            Console.WriteLine("You are in Release Mode.");
#endif
            Console.Write("\n");
        }

        private void Read()
        {
            while (true)
            {
                string input = Console.ReadLine();
                OnInput?.Invoke(input);
            }
        }
    }
}
