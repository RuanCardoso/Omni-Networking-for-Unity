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
using System.Runtime.CompilerServices;
#if OMNI_RELEASE
using System.Diagnostics;
#endif

namespace Omni.Core
{
    /// <summary>
    /// Represents database connection credentials for establishing connections to various database systems.
    /// This class encapsulates all necessary information such as server address, database name, 
    /// authentication details, and connection string generation logic.
    /// </summary>
    public sealed class ConnectionCredentials
    {
        /// <summary>
        /// Gets the server address or file path for the database connection.
        /// For file-based databases like SQLite, this represents the file path.
        /// For server-based databases, this represents the server address or hostname.
        /// </summary>
        public string ServerOrFilePath { get; private set; }

        /// <summary>
        /// Gets the name of the database to connect to.
        /// This property is not used for file-based databases like SQLite.
        /// </summary>
        public string Database { get; private set; }

        /// <summary>
        /// Gets the username for authentication with the database server.
        /// This property is not used for file-based databases like SQLite.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Gets the password for authentication with the database server.
        /// This property is not used for file-based databases like SQLite.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Gets or sets the complete connection string used to establish a database connection.
        /// This can be set directly or generated from individual credential properties.
        /// </summary>
        public string ConnectionString { get; internal set; }

        /// <summary>
        /// Gets the type of database system being connected to (e.g., SqlServer, MySql, SQLite).
        /// This determines the format of the connection string and the appropriate database provider.
        /// </summary>
        public DatabaseType Type { get; private set; }

        /// <summary>
        /// Gets or sets the port number for the database connection.
        /// </summary>
        public int Port { get; private set; }

        internal ConnectionCredentials(DatabaseType type, string serverOrFilePath, string database, string username, string password, int port)
        {
            SetConnectionString(type, serverOrFilePath, database, username, password, port);
        }

        public ConnectionCredentials([CallerMemberName] string _ = "")
        {
            if (_ != "Awake" && _ != "Start" && _ != "OnAwake" && _ != "OnStart")
            {
                throw new InvalidOperationException(
                    $"{nameof(ConnectionCredentials)} constructor should be called from within a method (Awake, Start, OnAwake or OnStart), not directly within the class scope. {_}"
                );
            }
        }

        /// <summary>
        /// Configures the database connection string with provided credentials. For security reasons,
        /// always pass credentials as arguments instead of storing them in variables.
        /// If credentials are passed directly, they are automatically stripped from non-server builds.
        /// </summary>
        /// <remarks>Alternatively, you can use <c>#if OMNI_SERVER</c> to strip credentials from non-server builds.</remarks>
#if OMNI_RELEASE
        [Conditional("UNITY_SERVER"), Conditional("UNITY_EDITOR")]
#endif
        public void SetConnectionString(DatabaseType type, string serverOrFilePath, string database, string username,
            string password, int port, [CallerMemberName] string _ = "")
        {
            Type = type;
            ServerOrFilePath = serverOrFilePath;
            Database = database;
            Username = username;
            Password = password;
            Port = port;
            ConnectionString = GetConnectionString();
        }

        /// <summary>
        /// Generates a connection string based on the configured database type and credentials.
        /// </summary>
        /// <returns>A formatted connection string appropriate for the selected database type.</returns>
        /// <exception cref="NotSupportedException">Thrown when an unsupported database type is specified.</exception>
        private string GetConnectionString()
        {
            return Type switch
            {
                DatabaseType.SqlServer => $"Server={ServerOrFilePath};Port={(Port == 0 ? 1433 : Port)};Database={Database};User Id={Username};Password={Password};",
                DatabaseType.MariaDb or DatabaseType.MySql => $"Server={ServerOrFilePath};Port={(Port == 0 ? 3306 : Port)};Database={Database};Uid={Username};Pwd={Password};",
                DatabaseType.PostgreSql => $"Host={ServerOrFilePath};Port={(Port == 0 ? 5432 : Port)};Database={Database};Username={Username};Password={Password};",
                DatabaseType.Oracle => $"Data Source={ServerOrFilePath}:{(Port == 0 ? 1521 : Port)}/{Database};User Id={Username};Password={Password};",
                DatabaseType.SQLite => $"Data Source={ServerOrFilePath};Version=3;Pooling=True;Max Pool Size=100;",
                DatabaseType.Firebird => $"Database={ServerOrFilePath};Port={(Port == 0 ? 3050 : Port)};User={Username};Password={Password};Dialect=3;Charset=UTF8;",
                _ => throw new NotSupportedException($"Unsupported database type: {Type}. Please use one of the supported database types."),
            };
        }
    }
}