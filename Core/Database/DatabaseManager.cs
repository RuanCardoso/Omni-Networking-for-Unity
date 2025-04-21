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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Omni.Shared;
using UnityEngine.Video;
#if OMNI_RELEASE
using System.Diagnostics;
#endif

#pragma warning disable

namespace Omni.Core
{
    /// <summary>
    /// Represents common local database stack environments used for development.
    /// These stacks typically include web server, database, and PHP components.
    /// </summary>
    public enum DatabaseStack
    {
        /// <summary>
        /// Default database stack option for general use cases.
        /// </summary>
        Common,
        /// <summary>
        /// Cross-platform Apache, MariaDB/MySQL, PHP and Perl stack.
        /// </summary>
        Xampp,

        /// <summary>
        /// Windows-based Apache, MySQL, and PHP stack.
        /// </summary>
        Wamp,

        /// <summary>
        /// Modern and lightweight development environment for Windows.
        /// </summary>
        Laragon,

        /// <summary>
        /// macOS-based Apache, MySQL, and PHP stack.
        /// </summary>
        Mamp
    }

    /// <summary>
    /// Database Manager is a base class responsible for managing database connections and operations in a Unity environment.
    /// Inherit from this class to implement custom database management functionalities tailored to specific project requirements.
    /// </summary>
    public class DatabaseManager : ServiceBehaviour
    {
        private SemaphoreSlim m_Semaphore;
        private ConnectionCredentials m_Credentials;

        private int m_Timeout;
        private bool m_UseLegacyPagination;

        protected CancellationTokenSource TokenSource { get; private set; }

        protected DatabaseManager()
        {
        } // do not remove.

        /// <summary>
        /// Configures the database connection using a predefined database stack and database name.
        /// This method simplifies database configuration for common local development environments.
        /// </summary>
        /// <param name="stack">The local database stack environment (Xampp, Wamp, Laragon, or Mamp).</param>
        /// <param name="database">The name of the database to connect to.</param>
        protected void ConfigureDatabase(string database, DatabaseStack stack = DatabaseStack.Common)
        {
            switch (stack)
            {
                case DatabaseStack.Common:
                case DatabaseStack.Xampp:
                case DatabaseStack.Wamp:
                case DatabaseStack.Laragon:
                    ConfigureDatabase(DatabaseType.MySql, $"Server=localhost;Port=3306;Database={database};Uid=root;Pwd=;");
                    break;
                case DatabaseStack.Mamp:
                    ConfigureDatabase(DatabaseType.MySql, $"Server=localhost;Port=8889;Database={database};Uid=root;Pwd=root;");
                    break;
                default:
                    throw new NotSupportedException($"Unsupported database stack: {stack}. Please use one of the defined DatabaseStack values.");
            }
        }

        /// <summary>
        /// Sets the database credentials and timeout values.
        /// </summary>
        /// <remarks>
        /// The credentials are automatically stripped from non-server builds if passed directly as a parameter without using declared variables. (<c>only release mode</c>).
        /// </remarks>
        /// <param name="credentials">The credentials used to connect to the database.</param>
        /// <param name="timeout">The timeout value for database operations (optional, default is 30 seconds).</param>
        /// <param name="useLegacyPagination">Specifies whether to use legacy pagination (optional, default is false).</param>
        /// <param name="concurrentConnections">The number of concurrent connections to the database (optional, default is 10).</param>
#if OMNI_RELEASE
        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
#endif
        protected void ConfigureDatabase(ConnectionCredentials credentials, int concurrentConnections = 10, int timeout = 30, bool useLegacyPagination = false)
        {
            m_Semaphore ??= new SemaphoreSlim(concurrentConnections);
            TokenSource ??= new CancellationTokenSource();

            m_Credentials = credentials;
            m_Timeout = timeout;
            m_UseLegacyPagination = useLegacyPagination;
        }

        /// <summary>
        /// Sets the database connection parameters using a connection string.
        /// </summary>
        /// <remarks>
        /// The credentials are automatically stripped from non-server builds(<b>only release mode</b>).
        /// </remarks>
        /// <param name="type">The type of the database.</param>
        /// <param name="connectionString">The connection string used to access the database.</param>
        /// <param name="timeout">The timeout value for database operations (optional, default is 30 seconds).</param>
        /// <param name="useLegacyPagination">Specifies whether to use legacy pagination (optional, default is false).</param>
        /// <param name="concurrentConnections">The number of concurrent connections to the database (optional, default is 10).</param>
#if OMNI_RELEASE
        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
#endif
        protected void ConfigureDatabase(DatabaseType type, string connectionString, int concurrentConnections = 10, int timeout = 30, bool useLegacyPagination = false)
        {
            m_Semaphore ??= new SemaphoreSlim(concurrentConnections);
            TokenSource ??= new CancellationTokenSource();

            m_Credentials = new ConnectionCredentials(type, default, default, default, default, default);
            m_Credentials.ConnectionString = connectionString;

            m_Timeout = timeout;
            m_UseLegacyPagination = useLegacyPagination;
        }

        /// <summary>
        /// Asynchronously initializes and returns a database instance.
        /// </summary>
        /// <param name="token">The cancellation token (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the initialized database instance.</returns>
        protected Task<DatabaseConnection> GetConnectionAsync(CancellationToken token = default)
        {
            if (m_Credentials == null)
            {
                throw new InvalidOperationException(
                    "Database credentials not configured. You must call ConfigureDatabase() before attempting any database operations."
                );
            }

            var db = DatabaseConnection.Create();
            return db.InitializeAsync(m_Credentials.Type, m_Credentials.ConnectionString, m_Timeout, m_UseLegacyPagination, token);
        }

        /// <summary>
        /// Initializes and returns a database instance.
        /// </summary>
        /// <param name="token">The cancellation token (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the initialized database instance.</returns>
        protected DatabaseConnection GetConnection()
        {
            if (m_Credentials == null)
            {
                throw new InvalidOperationException(
                    "Database credentials not configured. You must call ConfigureDatabase() before attempting any database operations."
                );
            }

            var db = DatabaseConnection.Create();
            db.Initialize(m_Credentials.Type, m_Credentials.ConnectionString, m_Timeout, m_UseLegacyPagination);
            return db;
        }

        /// <summary>
        /// Checks the connection to the database.
        /// </summary>
        /// <returns>True if the connection is successful, otherwise false.</returns>
        protected async Task<bool> CheckConnectionAsync(CancellationToken token = default)
        {
            try
            {
                using var _ = await GetConnectionAsync(token);
                return _.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"Database connection failed: The connection to the database could not be established, reason: {ex.Message}",
                    NetworkLogger.LogType.Error
                );

                return false;
            }
        }

        /// <summary>
        /// Checks the connection to the database.
        /// </summary>
        /// <returns>True if the connection is successful, otherwise false.</returns>
        protected bool CheckConnection()
        {
            try
            {
                using var _ = GetConnection();
                return _.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"Database connection failed: The connection to the database could not be established, reason: {ex.Message}",
                    NetworkLogger.LogType.Error
                );

                return false;
            }
        }

        /// <summary>
        /// Blocks the current thread until permission is granted to proceed with a database operation,
        /// according to the configured concurrency limit.
        /// </summary>
        /// <param name="token">
        /// An optional <see cref="CancellationToken"/> that can be used to cancel the wait operation.
        /// If canceled before access is granted, an <see cref="OperationCanceledException"/> is thrown.
        /// </param>
        /// <remarks>
        /// This method uses an internal <see cref="SemaphoreSlim"/> to enforce a maximum number of concurrent
        /// database operations. When the limit is reached, additional callers will block until another operation
        /// completes and releases its slot.
        ///
        /// Use this method only in synchronous execution contexts. For asynchronous workflows, prefer
        /// <see cref="WaitIfBusyAsync"/>.
        ///
        /// Proper use of this method helps to:
        /// - Prevent excessive simultaneous connections to the database
        /// - Avoid exhausting the connection pool
        /// - Maintain stability under high load scenarios
        ///
        /// Each call to this method **must** be followed by a corresponding call to <see cref="MarkDone"/> to
        /// release the slot for other operations. Failure to release may result in blocked threads or connection starvation.
        /// </remarks>
        protected void WaitIfBusy(CancellationToken token = default)
        {
            m_Semaphore.Wait(token);
        }

        /// <summary>
        /// Asynchronously waits for permission to proceed with a database operation,
        /// according to the configured concurrency limit.
        /// </summary>
        /// <param name="token">
        /// An optional <see cref="CancellationToken"/> that can be used to cancel the wait operation.
        /// If canceled before access is granted, the returned <see cref="Task"/> will be canceled.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that completes when the calling operation is allowed to proceed.
        /// </returns>
        /// <remarks>
        /// This method should be used in asynchronous code to throttle access to database resources.
        ///
        /// It ensures that only a defined number of operations can access the database concurrently,
        /// helping to avoid overloading the connection pool and reducing the risk of "too many connections"
        /// or "max pool size reached" errors.
        ///
        /// Each call to this method **must** be followed by a corresponding call to <see cref="MarkDone"/>
        /// to release the slot and allow other queued operations to proceed.
        /// </remarks>
        protected Task WaitIfBusyAsync(CancellationToken token = default)
        {
            try
            {
                return m_Semaphore.WaitAsync(token);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Signals that the current database operation has completed and releases the semaphore,
        /// allowing another queued operation to proceed.
        /// </summary>
        /// <returns>
        /// The number of remaining entries that can be released to the semaphore without blocking.
        /// </returns>
        /// <remarks>
        /// This method should be called **once** after each successful call to <see cref="WaitIfBusy"/>
        /// or <see cref="WaitIfBusyAsync"/>. It releases the concurrency slot, enabling the next operation
        /// in the queue to proceed.
        ///
        /// Failing to call this method may lead to resource starvation, deadlocks, or database connection
        /// pool exhaustion.
        ///
        /// It is recommended to call this method in a <c>finally</c> block to ensure proper release
        /// even in the event of exceptions.
        /// </remarks>
        protected int MarkDone()
        {
            return m_Semaphore.Release();
        }

        protected void Dispose()
        {
            try
            {
                if (TokenSource != null)
                    TokenSource.Dispose();
            }
            catch { }

            try
            {
                if (m_Semaphore != null)
                    m_Semaphore.Dispose();
            }
            catch { }
        }

        protected virtual void OnDisable()
        {
            Dispose();
        }

        protected virtual void OnApplicationQuit()
        {
            Dispose()
        }
    }
}