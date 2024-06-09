using System;
using Omni.Shared;
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
                    Name = "Omni Console",
                    Priority = System.Threading.ThreadPriority.Lowest
                };

            thread.Start();
#endif
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
