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
        private ConnectionCredentials Credentials { get; set; }
        private int Timeout { get; set; }
        private bool UseLegacyPagination { get; set; }

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
#if OMNI_RELEASE
        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
#endif
        protected void ConfigureDatabase(ConnectionCredentials credentials, int timeout = 30, bool useLegacyPagination = false)
        {
            Credentials = credentials;
            Timeout = timeout;
            UseLegacyPagination = useLegacyPagination;
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
#if OMNI_RELEASE
        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
#endif
        protected void ConfigureDatabase(DatabaseType type, string connectionString, int timeout = 30, bool useLegacyPagination = false)
        {
            Credentials = new ConnectionCredentials(type, default, default, default, default, default);
            Credentials.ConnectionString = connectionString;
            Timeout = timeout;
            UseLegacyPagination = useLegacyPagination;
        }

        /// <summary>
        /// Asynchronously initializes and returns a database instance.
        /// </summary>
        /// <param name="token">The cancellation token (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the initialized database instance.</returns>
        protected Task<DatabaseConnection> GetConnectionAsync(CancellationToken token = default)
        {
            if (Credentials == null)
            {
                throw new InvalidOperationException(
                    "Database credentials not configured. You must call ConfigureDatabase() before attempting any database operations."
                );
            }

            var db = DatabaseConnection.Create();
            return db.InitializeAsync(Credentials.Type, Credentials.ConnectionString, Timeout, UseLegacyPagination, token);
        }

        /// <summary>
        /// Initializes and returns a database instance.
        /// </summary>
        /// <param name="token">The cancellation token (optional).</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the initialized database instance.</returns>
        protected DatabaseConnection GetConnection()
        {
            if (Credentials == null)
            {
                throw new InvalidOperationException(
                    "Database credentials not configured. You must call ConfigureDatabase() before attempting any database operations."
                );
            }

            var db = DatabaseConnection.Create();
            db.Initialize(Credentials.Type, Credentials.ConnectionString, Timeout, UseLegacyPagination);
            return db;
        }

        /// <summary>
        /// Checks the connection to the database.
        /// </summary>
        /// <returns>True if the connection is successful, otherwise false.</returns>
        protected async Task<bool> CheckConnectionAsync()
        {
            try
            {
                using var _ = await GetConnectionAsync();
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
    }
}