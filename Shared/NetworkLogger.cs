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

#if UNITY_EDITOR
using ParrelSync;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Debug = UnityEngine.Debug;
using System.Reflection;
using Omni.Core;
using UnityEngine;
using System.ComponentModel;

namespace Omni.Shared
{
    public static class NetworkLogger
    {
        // Omni Networking Version, change this value to the current version of the package.json(Open UPM)
        // _LTS = Long Term Support
        public const string Version = "3.2.1 LTS";
        public static StreamWriter fileStream = null;
        public static string LogPath = "OmniDefaultLog.log";

        public enum LogType
        {
            Error = 0,
            Warning = 2,
            Log = 3,
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Initialize(string uniqueId)
        {
            // Using Application.persistentDataPath ensures:
            // - Write permissions are automatically handled by Unity
            // - Storage location is secure and sandboxed per application
            // - Avoids filesystem access errors across all platforms
            // - Path remains valid after app updates or system changes
            LogPath = Path.Combine(Application.persistentDataPath, $"OmniLog_{uniqueId}.log");
        }

        /// <summary>
        /// Prints a hyperlink to the console with the provided exception and log type.
        /// </summary>
        /// <param name="ex">The exception to print, or null for no exception.</param>
        /// <param name="logType">The type of log message (default: LogType.Error).</param>
        [Conditional("UNITY_EDITOR")]
        public static void PrintHyperlink(Exception ex = null, LogType logType = LogType.Error)
        {
#if OMNI_DEBUG
            string stacktrace = GetStackFramesToHyperlink(ex);
            if (!string.IsNullOrEmpty(stacktrace))
            {
                Print(stacktrace, logType);
            }
#endif
        }

#pragma warning disable IDE1006
#if OMNI_DEBUG
        /// <summary>
        /// Logs a message to both the console and the log file (debug builds only).
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logType">The log message type (default: <see cref="LogType.Log"/>).</param>
        public static void __Log__(string message, LogType logType = LogType.Log)
        {
            Log(message, true, logType);
        }
#else
        /// <summary>
        /// Logs a message to both the console and the log file (release builds only).
        /// </summary>
        /// <remarks>
        /// In the Unity Editor, the message is logged to the console. In other builds, the message is logged to a file.
        /// </remarks>
        /// <param name="message">The message to log.</param>
        /// <param name="logType">The log message type (default: <see cref="LogType.Log"/>).</param>
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
        /// <summary>
        /// Prints the contents of the player's log file to the log output.
        /// </summary>
        public static void PrintPlayerLog()
        {
            if (File.Exists(LogPath))
            {
                Log($"Log Path: {Path.GetFullPath(LogPath)}");
                if (fileStream == null)
                {
                    Log($"Player Log:\n\r{File.ReadAllText(LogPath)}");
                }
            }
            else
            {
                Log("No player log found. this file is empty.");
            }
        }

        /// <summary>
        /// Logs a message to a persistent log file, keeping the file stream open for better performance.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="logType">
        /// The type of log message, indicating its severity and category. Default is <see cref="LogType.Log"/>.
        /// </param>
        /// <remarks>
        /// This method appends the log message to a file, maintaining an open stream for improved performance when writing multiple log entries.
        /// </remarks>
        public static void LogToFile(object message, LogType logType = LogType.Log)
        {
            try
            {
                bool isClone = false;
#if UNITY_EDITOR
                if (ClonesManager.IsClone())
                {
                    isClone = true;
                }

#if OMNI_VIRTUAL_PLAYER_ENABLED && UNITY_6000_3_OR_NEWER && UNITY_EDITOR
                if (MPPM.IsVirtualPlayer)
                {
                    isClone = true;
                }
#endif
#endif
                if (!isClone)
                {
                    // Keep the stream open for better performance.
                    fileStream ??= new(LogPath, append: true)
                    {
                        AutoFlush = true
                    };

                    string dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    fileStream.WriteLine($"{dateTime}: {message} -> Thread Id: ({threadId}) - {logType}");
                }
            }
            catch (Exception ex)
            {
                Log("Error writing to log file. Maybe the file is in use or has permissions issues? or multiple threads are logging at the same time. -> " + ex.Message, false, LogType.Error);
                // Ignored -> IOException: Sharing violation
            }
        }

        /// <summary>
        /// Logs a message to the console and optionally writes it to a log file.
        /// </summary>
        /// <param name="message">The message to be logged.</param>
        /// <param name="writeToLogFile">
        /// Indicates whether the message should also be written to a log file. Default is <c>false</c>.
        /// </param>
        /// <param name="logType">
        /// The type of log message, determining its severity and appearance. Default is <see cref="LogType.Log"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// This method provides a flexible logging mechanism, supporting console output, optional file logging, and enhanced debug information.
        /// </para>
        /// <para>
        /// In server builds:
        /// <list type="bullet">
        /// <item><see cref="LogType.Error"/> messages are displayed in red.</item>
        /// <item><see cref="LogType.Warning"/> messages are displayed in yellow.</item>
        /// <item>Other messages are displayed in white.</item>
        /// </list>
        /// </para>
        /// </remarks>
        public static void Log(object message, bool writeToLogFile = false, LogType logType = LogType.Log)
        {
#if OMNI_SERVER && !UNITY_EDITOR
            ConsoleColor logColor = logType switch
            {
                LogType.Error => ConsoleColor.Red,
                LogType.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White,
            };

            string fmessage = $"[{logType}] -> {message}";
            Console.ForegroundColor = logColor;
            Console.WriteLine(fmessage);
            int length = Console.WindowWidth - 1;
            if (length <= 0)
                length = fmessage.Length;
            Console.WriteLine(new string('-', length));
#else
#if OMNI_DEBUG
            Debug.LogFormat((UnityEngine.LogType)logType, UnityEngine.LogOption.None, null, "{0}", message);
            if (logType == LogType.Error)
                PrintHyperlink(null, LogType.Error);
#else
            Debug.LogFormat(
                (UnityEngine.LogType)logType,
                UnityEngine.LogOption.NoStacktrace,
                null,
                "{0}",
                message
            );
#endif
#endif
            if (writeToLogFile)
                LogToFile(message, logType);
        }

        /// <summary>
        /// Prints a message to the console or debug output without a stack trace. This method does not log to a file.
        /// </summary>
        /// <param name="message">The message to be printed.</param>
        /// <param name="logType">
        /// The type of log message, determining its severity and appearance.
        /// Defaults to <see cref="LogType.Error"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// In a server build, the message is printed to the console with a color indicating its log type:
        /// <list type="bullet">
        /// <item><see cref="LogType.Error"/> messages are displayed in red.</item>
        /// <item><see cref="LogType.Warning"/> messages are displayed in yellow.</item>
        /// <item>Other messages are displayed in white.</item>
        /// </list>
        /// </para>
        /// <para>
        /// In non-server builds, the message is output using Unity's debug system without a stack trace.
        /// </para>
        /// <para>
        /// This method is designed for quick and lightweight logging and should not be used for persistent logs.
        /// </para>
        /// </remarks>
        public static void Print(string message, LogType logType = LogType.Error)
        {
#if OMNI_SERVER && !UNITY_EDITOR
            ConsoleColor logColor = logType switch
            {
                LogType.Error => ConsoleColor.Red,
                LogType.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.White,
            };

            string fmessage = $"[{logType}] -> {message}";
            Console.ForegroundColor = logColor;
            Console.WriteLine(fmessage);
            int length = Console.WindowWidth - 1;
            if (length <= 0)
                length = fmessage.Length;
            Console.WriteLine(new string('-', length));
#else
            Debug.LogFormat(
                (UnityEngine.LogType)logType,
                UnityEngine.LogOption.NoStacktrace,
                null,
                "{0}",
                message
            );
#endif
        }

        /// <summary>
        /// Retrieves detailed stack trace information, including class, method, and line number, for debugging purposes.
        /// </summary>
        /// <param name="exception">
        /// An optional <see cref="Exception"/> object. If provided, the stack trace for the exception is used.
        /// If <c>null</c>, the current call stack is retrieved.
        /// </param>
        /// <returns>
        /// A string containing the stack trace details, including class name, method name, and line number.
        /// </returns>
        /// <remarks>
        /// This method is useful for debugging scenarios to provide insights into the call stack.
        /// It performs a detailed analysis of the stack frames, which can be computationally expensive.
        /// Use this method primarily in debug builds or for diagnostic purposes.
        /// </remarks>
        public static string GetStackFramesToHyperlink(Exception exception = null)
        {
            var frames = GetStackFrames(exception);
            var _message = new StringBuilder();
            // Very slow operation, but useful for debugging. Debug mode only.
            foreach (var frame in frames)
            {
                try
                {
                    int line = frame.GetFileLineNumber();
                    string fileName = frame.GetFileName();
                    string filePath = fileName?.Replace("\\", "/") ?? "";
                    if (string.IsNullOrEmpty(filePath))
                        continue;

                    // Skip internal Omni framework types in the stack trace.  
                    // Only user script files will be processed and displayed.  
                    // do not change the name of the omni folder!!
                    if (filePath.Contains("/Assets/Omni-Networking-for-Unity") ||
                        filePath.Contains("/Packages/Omni-Networking-for-Unity") ||
                        filePath.Contains("/OmniNetSourceGenerator") ||
                        filePath.Contains("/Library/PackageCache"))
                        continue;

                    MethodBase method = frame.GetMethod();
                    if (method == null)
                        continue;

                    string declaringType = method.DeclaringType?.ToString() ?? "";
                    if (string.IsNullOrEmpty(declaringType))
                        continue;

                    bool hasStacktraceAttribute = method.GetCustomAttribute<StackTraceAttribute>(true) != null ||
                                                  method.DeclaringType.GetCustomAttribute<StackTraceAttribute>(true) !=
                                                  null;

                    bool hasBaseClasses = declaringType.Contains("NetworkBehaviour") ||
                                          declaringType.Contains("ServerBehaviour") ||
                                          declaringType.Contains("ClientBehaviour") ||
                                          declaringType.Contains("DualBehaviour");

                    if (hasStacktraceAttribute || hasBaseClasses)
                    {
                        int indexOf = filePath.IndexOf("/Assets");
                        string linkText = indexOf > 0 ? $"{filePath[indexOf..]}:{line}" : $"{filePath}:{line}";
#if UNITY_6000_0_OR_NEWER
                        string link =
                            $"<color=#40a0ff><link=\"href='{filePath}' line='{line}'\">{linkText}</link></color>";
#else
                        string link = $"<a href=\"{filePath}\" line=\"{line}\">{linkText}</a>";
#endif
                        _message.AppendLine(
                            $"Full Log -> " +
                            $"{link} | " +
                            $"Class: [{declaringType}] | " +
                            $"Method: [{method.Name}] | " +
                            $"Line: [{line}] | "
                        );
                    }
                    else continue;
                }
                catch (Exception ex)
                {
                    _message.AppendLine($"Hyperlink Exception: {ex.Message}");
                    continue; // Continue to the next frame after the exception
                }
            }

            return _message.ToString();
        }

        /// <summary>
        /// Creates a sequence of <see cref="StackFrame"/> objects representing the call stack.
        /// </summary>
        /// <param name="exception">
        /// An optional <see cref="Exception"/> object. If provided, the stack trace for the exception is analyzed.
        /// If <c>null</c>, the current call stack is analyzed.
        /// </param>
        /// <returns>
        /// An enumerable sequence of <see cref="StackFrame"/> objects, representing the frames in the call stack.
        /// </returns>
        /// <remarks>
        /// This method generates a detailed representation of the call stack for diagnostic purposes.
        /// Each <see cref="StackFrame"/> includes file information such as line numbers and method details,
        /// which may require the application to be compiled with debug symbols for complete accuracy.
        /// </remarks>
        public static IEnumerable<StackFrame> GetStackFrames(Exception exception = null)
        {
            if (!Bridge.EnableDeepDebug)
            {
                yield return default;
                yield break;
            }

            StackTrace stack = exception == null
                ? new StackTrace(fNeedFileInfo: true)
                : new StackTrace(exception, fNeedFileInfo: true);

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