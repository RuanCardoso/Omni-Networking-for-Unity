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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

        /// <summary>
        /// Indicates whether buffer tracking is enabled. If true, additional tracking is performed to ensure
        /// that buffers are properly disposed of and returned to the pool. <c>Debug mode only.</c>
        /// </summary>
        public static bool EnableTracking { get; set; } = true;

#pragma warning disable IDE1006
#if OMNI_DEBUG
        /// <summary>
        /// Logs a message to the console and the log file. This method is available only in debug builds.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="logType">The type of log message. Default is <see cref="LogType.Log"/>.</param>
        public static void __Log__(string message, LogType logType = LogType.Log)
        {
            Log(message, true, logType);
        }
#else
        /// <summary>
        /// Logs a message to the console or to a file depending on the build configuration.
        /// </summary>
        /// <remarks>
        /// In the Unity Editor, the message is logged to the console. In other builds, the message is logged to a file.
        /// </remarks>
        /// <param name="message">The message to be logged.</param>
        /// <param name="logType">The type of log message. Default is <see cref="LogType.Log"/>.</param>
        public static void __Log__(string message, LogType logType = LogType.Log)
        {
#if UNITY_EDITOR
            Log(message, true, logType);
#else
            LogToFile(message, logType);
#endif
        }
#endif
#pragma warning restore IDE1006

        public static void PrintPlayerLog()
        {
            string path = Path.Combine(Application.persistentDataPath, "omni_player_log.txt");
            if (File.Exists(path))
            {
                Log($"Log Path: {path}");
                Log($"Player Log:\n\r{File.ReadAllText(path)}");
            }
            else
            {
                Log("No player log found. this file is empty.");
            }
        }

        /// <summary>
        /// Logs a message to the log file.<br/>
        /// Includes the current local date and time, thread ID, and log type.
        /// </summary>
        public static void LogToFile(object message, LogType logType = LogType.Log)
        {
            string path = Path.Combine(Application.persistentDataPath, "omni_player_log.txt");
            using StreamWriter file = new(path, true);
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            int threadId = Thread.CurrentThread.ManagedThreadId;
            file.WriteLine($"{dateTime}: {message} -> Thread Id: ({threadId}) - {logType}");
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

#if OMNI_DEBUG
            if (logType == LogType.Error && EnableTracking)
            {
                string _message = GetStackTrace();

                // Print the stack trace only in debug mode.
                Print(_message, logType);
            }
#endif
        }

        /// <summary>
        /// Prints a message to the console without a stack trace. not logging to a file.
        /// </summary>
        /// <param name="message">The message to be printed.</param>
        /// <param name="logType">The type of log message. Default is <see cref="LogType.Error"/>.</param>
        public static void Print(string message, LogType logType = LogType.Error)
        {
            Debug.LogFormat(
                (UnityEngine.LogType)logType,
                LogOption.NoStacktrace,
                null,
                "{0}",
                message
            );
        }

        /// <summary>
        /// Retrieves the stack trace information.
        /// </summary>
        /// <returns>The stack trace information as a string.</returns>
        public static string GetStackTrace()
        {
            var frames = CreateStackTrace();
            var _message = "";

            // Very slow operation, but useful for debugging. Debug mode only.
            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                int line = frame.GetFileLineNumber();
                _message +=
                    $"StackTrace -> Class: [{method.DeclaringType}] | Method: [{method.Name}] | Line: [{line}]\r\n";
            }

            return _message;
        }

        /// <summary>
        /// Creates a sequence of <see cref="StackFrame"/>s representing the call stack.
        /// </summary>
        /// <returns>A sequence of <see cref="StackFrame"/>s.</returns>
        public static IEnumerable<StackFrame> CreateStackTrace()
        {
            StackTrace stack = new(true);
            for (int i = stack.FrameCount - 1; i >= 0; i--)
            {
                StackFrame frame = stack.GetFrame(i);
                if (frame == null)
                    continue;

                yield return frame;
            }
        }
    }
}
