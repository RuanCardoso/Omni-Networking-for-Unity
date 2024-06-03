/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Omni.Shared
{
    public static class NetworkLogger
    {
        public enum LogType
        {
            Error = 0,
            Warning = 2,
            Log = 3,
        }

#pragma warning disable IDE1006
#if OMNI_DEBUG
        /// <summary>
        /// Logs a message to the console and to the log file.<br/>
        /// This method is available only in Debug mode and will not be called in Release mode.
        /// </summary>
        public static void __Log__(string message, LogType logType = LogType.Log)
        {
            Log(message, true, logType);
        }
#else
        /// <summary>
        /// Logs a message to the log file only in Release mode.<br/>
        /// In Release mode, messages will not be logged to the console.
        /// </summary>
        public static void __Log__(string message, LogType logType = LogType.Log)
        {
            LogToFile(message, logType);
        }
#endif
#pragma warning restore IDE1006

        /// <summary>
        /// Logs a message to the log file.<br/>
        /// Includes the current local date and time, thread ID, and log type.
        /// </summary>
        public static void LogToFile(object message, LogType logType = LogType.Log)
        {
            using (StreamWriter file = new("omni_log.txt", true))
            {
                string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                int threadId = Thread.CurrentThread.ManagedThreadId;
                file.WriteLine($"{dateTime}: {message} -> ThreadId: ({threadId}) - {logType}");
            }
        }

        /// <summary>
        /// Logs a message to the console and optionally to a log file.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="writeToLogFile">Specifies whether to write the message to a log file (default is false).</param>
        /// <param name="logType">The type of log message (default is LogType.Log).</param>
        public static void Log(
            object message,
            bool writeToLogFile = false,
            LogType logType = LogType.Log
        )
        {
            if (writeToLogFile)
            {
                LogToFile(message, logType);
            }

#if OMNI_SERVER && !UNITY_EDITOR
            ConsoleColor logColor = logType switch
            {
                LogType.Error => ConsoleColor.Red,
                LogType.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White,
            };

            Console.ForegroundColor = logColor;
            Console.WriteLine($"[{logType}] -> {message}");
            Console.WriteLine(new string('-', Console.WindowWidth - 1));
#else
#if OMNI_DEBUG
            Debug.LogFormat((UnityEngine.LogType)logType, LogOption.None, null, "{0}", message);
#else
            Debug.LogFormat(
                (UnityEngine.LogType)logType,
                LogOption.NoStacktrace,
                null,
                "{0}",
                message
            );
#endif
#endif
        }
    }
}
