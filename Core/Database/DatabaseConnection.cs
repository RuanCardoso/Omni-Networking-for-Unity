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
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using FirebirdSql.Data.FirebirdClient;
using Mono.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using Omni.Compilers;
using Omni.Execution;
using Omni.Shared;
using Oracle.ManagedDataAccess.Client;

namespace Omni.Core
{
    public enum DatabaseType
    {
        SqlServer,
        MariaDb,
        MySql,
        PostgreSql,
        Oracle,
        SQLite,
        Firebird,
    }

    /// <summary>
    /// Represents a database management system used for performing operations such as insertion, filtering, update, delete and more.
    /// This class provides methods to interact with a database, facilitating common database tasks.
    /// </summary>
    public class DatabaseConnection : IDisposable, IAsyncDisposable, IDbConnection
    {
        private string tableName;
        private DbConnection dbConnection;
        private QueryFactory queryFactory;
        private Query query;

        /// <summary>
        /// Gets or sets the connection string used to open the database.
        /// </summary>
        public string ConnectionString
        {
            get => dbConnection.ConnectionString;
            set => dbConnection.ConnectionString = value;
        }

        /// <summary>
        /// Gets the time to wait while trying to establish a connection before terminating the attempt and generating an error.
        /// </summary>
        public int ConnectionTimeout => dbConnection.ConnectionTimeout;

        /// <summary>
        /// Gets the name of the current database or the database to be used after a connection is opened.
        /// </summary>
        public string Database => dbConnection.Database;

        /// <summary>
        /// Gets the current state of the connection.
        /// </summary>
        public ConnectionState State => dbConnection.State;

        /// <summary>
        /// Represents a factory for generating SQL queries for various database operations.
        /// This class encapsulates query creation logic for operations such as insertion, updating, deleting, and selecting data.
        /// </summary>
        public QueryFactory Factory
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return queryFactory;
            }
        }

        /// <summary>
        /// Represents a connection to a database.
        /// This class provides access to establish and manage connections with a database.
        /// </summary>
        public DbConnection Connection
        {
            get
            {
                ThrowErrorIfNotInitialized();
                return dbConnection;
            }
        }

        private DatabaseConnection()
        {
            tableName = "No Table Provided!";
        }

        /// <summary>
        /// Represents a database query builder used to create SQL queries for database operations.
        /// This class facilitates the creation of queries for operations including insertion, updating, deleting, and selecting data and more.
        /// </summary>
        /// <param name="tableName">The name of the database table to perform operations on.</param>
        /// <returns>A Query object configured for the specified table.</returns>
        public Query GetBuilder(string tableName)
        {
            this.tableName = tableName;
            query = Factory.Query(tableName);
            return query;
        }

        /// <summary>
        /// Initializes the Database Management System with the provided DbConnection, Compiler, table name, and timeout value.
        /// </summary>
        /// <param name="DbConnection">The DbConnection to be used by the database.</param>
        /// <param name="compiler">The Compiler to be used by the database.</param>
        /// <param name="tableName">The name of the table to be used by the database.</param>
        /// <param name="timeout">The timeout value in seconds for the database queries. Default is 30 seconds.</param>
        private void Initialize(DbConnection DbConnection, Compiler compiler, int timeout = 30)
        {
            try
            {
                dbConnection = DbConnection;
                dbConnection.Open();
                // Initialize the query builder to openned connection!
                if (dbConnection.State == ConnectionState.Open)
                {
                    queryFactory = new QueryFactory(dbConnection, compiler, timeout);
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"Connection to the database for table '{tableName}' is not open. Please check the connection parameters and try again.",
                        NetworkLogger.LogType.Error
                    );
                }
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"An error occurred while initializing the database connection: {ex.Message}",
                    NetworkLogger.LogType.Error
                );

                if (ex.InnerException != null)
                {
                    NetworkLogger.__Log__(
                        $"Inner exception: {ex.InnerException.Message}",
                        NetworkLogger.LogType.Error
                    );
                }

                throw ex;
            }
        }

        /// <summary>
        /// Initializes the Database Management System with the provided DbConnection, Compiler, table name, and timeout value.
        /// </summary>
        /// <param name="DbConnection">The DbConnection to be used by the database.</param>
        /// <param name="compiler">The Compiler to be used by the database.</param>
        /// <param name="tableName">The name of the table to be used by the database.</param>
        /// <param name="timeout">The timeout value in seconds for the database queries. Default is 30 seconds.</param>
        private async Task<DatabaseConnection> InitializeAsync(
            DbConnection DbConnection,
            Compiler compiler,
            int timeout = 30,
            CancellationToken token = default
        )
        {
            try
            {
                dbConnection = DbConnection;
                // async open connection to ConcurrentDatabaseManager!
                await dbConnection.OpenAsync(token);
                // Initialize the query builder to openned connection!
                if (dbConnection.State == ConnectionState.Open)
                {
                    queryFactory = new QueryFactory(dbConnection, compiler, timeout);
                }
                else
                {
                    NetworkLogger.__Log__(
                        $"Failed to establish a connection for table '{tableName}'.",
                        NetworkLogger.LogType.Error
                    );
                }

                return this;
            }
            catch (Exception ex)
            {
                NetworkLogger.__Log__(
                    $"Error while initializing the database: {ex.Message}",
                    NetworkLogger.LogType.Error
                );
                if (ex.InnerException != null)
                {
                    NetworkLogger.__Log__(ex.InnerException.Message, NetworkLogger.LogType.Error);
                }

                throw ex;
            }
        }

        /// <summary>
        /// Initializes the Database Management System with the specified parameters, allowing connection to various types of databases.
        /// </summary>
        /// <param name="dbType">The specific type of the database (e.g., MySQL, SQL Server, PostgreSQL).</param>
        /// <param name="connectionString">The connection string used to access the specified database.</param>
        /// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
        /// <param name="useLegacyPagination">Specifies whether to use legacy pagination for query results. Default is false.</param>
        /// <remarks>
        /// Example Usage:
        /// SQLite: Initialize(DatabaseType.SQLite, "Data Source=omni_server_db.sqlite3")
        /// MariaDb/MySQL: Initialize(DatabaseType.MariaDb, "Server=localhost;Database=omni_server_db;Uid=root;Pwd=******;")
        /// </remarks>
        public void Initialize(
            DatabaseType dbType,
            string connectionString,
            int timeout = 30,
            bool useLegacyPagination = false
        )
        {
            switch (dbType)
            {
                case DatabaseType.Firebird:
                    Initialize(new FbConnection(connectionString), new FirebirdCompiler(), timeout);
                    break;
                case DatabaseType.Oracle:
                    Initialize(
                        new OracleConnection(connectionString),
                        new OracleCompiler() { UseLegacyPagination = useLegacyPagination },
                        timeout
                    );
                    break;
                case DatabaseType.PostgreSql:
                    Initialize(
                        new NpgsqlConnection(connectionString),
                        new PostgresCompiler(),
                        timeout
                    );
                    break;
                case DatabaseType.SqlServer:
                    Initialize(
                        new SqlConnection(connectionString),
                        new SqlServerCompiler() { UseLegacyPagination = useLegacyPagination },
                        timeout
                    );
                    break;
                case DatabaseType.SQLite:
                    Initialize(
                        new SqliteConnection(connectionString),
                        new SqliteCompiler(),
                        timeout
                    );
                    break;
                case DatabaseType.MariaDb:
                case DatabaseType.MySql:
                    Initialize(new MySqlConnection(connectionString), new MySqlCompiler(), timeout);
                    break;
                default:
                    throw new Exception("DatabaseType Type not supported!");
            }
        }

        /// <summary>
        /// Initializes the Database Management System asynchronously with the specified parameters, allowing connection to various types of databases.
        /// </summary>
        /// <param name="tableName">The name of the table to be used within the database.</param>
        /// <param name="dbType">The specific type of the database (e.g., MySQL, SQL Server, PostgreSQL).</param>
        /// <param name="connectionString">The connection string used to access the specified database.</param>
        /// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
        /// <param name="useLegacyPagination">Specifies whether to use legacy pagination for query results. Default is false.</param>
        /// <returns>A Task representing the asynchronous operation, containing the initialized Database instance.</returns>
        /// <remarks>
        /// Example Usage:
        /// SQLite: await InitializeAsync("table_name", DatabaseType.SQLite, "Data Source=omni_server_db.sqlite3")
        /// MariaDb/MySQL: await InitializeAsync("table_name", DatabaseType.MariaDb, "Server=localhost;Database=omni_server_db;Uid=root;Pwd=*****;")
        /// </remarks>
        public Task<DatabaseConnection> InitializeAsync(
            DatabaseType dbType,
            string connectionString,
            int timeout = 30,
            bool useLegacyPagination = false,
            CancellationToken token = default
        )
        {
            switch (dbType)
            {
                case DatabaseType.Firebird:
                    return InitializeAsync(
                        new FbConnection(connectionString),
                        new FirebirdCompiler(),
                        timeout,
                        token
                    );
                case DatabaseType.Oracle:
                    return InitializeAsync(
                        new OracleConnection(connectionString),
                        new OracleCompiler() { UseLegacyPagination = useLegacyPagination },
                        timeout,
                        token
                    );
                    ;
                case DatabaseType.PostgreSql:
                    return InitializeAsync(
                        new NpgsqlConnection(connectionString),
                        new PostgresCompiler(),
                        timeout,
                        token
                    );
                case DatabaseType.SqlServer:
                    return InitializeAsync(
                        new SqlConnection(connectionString),
                        new SqlServerCompiler() { UseLegacyPagination = useLegacyPagination },
                        timeout,
                        token
                    );
                case DatabaseType.SQLite:
                    return InitializeAsync(
                        new SqliteConnection(connectionString),
                        new SqliteCompiler(),
                        timeout,
                        token
                    );
                case DatabaseType.MariaDb:
                case DatabaseType.MySql:
                    return InitializeAsync(
                        new MySqlConnection(connectionString),
                        new MySqlCompiler(),
                        timeout,
                        token
                    );
                default:
                    throw new Exception("DatabaseType Type not supported!");
            }
        }

        /// <summary>
        /// Initializes the Database Management System with the specified parameters for SQLite databases.
        /// </summary>
        /// <param name="sqliteConnection">The SQLiteConnection to be used by the database.</param>
        /// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
        /// <remarks>
        /// Example Usage:
        /// Initialize(sqliteConnection, timeout)
        /// </remarks>
        public void Initialize(SqliteConnection sqliteConnection, int timeout = 30)
        {
            Initialize(new SqliteConnection(sqliteConnection), new SqliteCompiler(), timeout);
        }

        /// <summary>
        /// Initializes the Database Management System asynchronously with the specified parameters for SQLite databases.
        /// </summary>
        /// <param name="sqliteConnection">The SQLiteConnection to be used by the database.</param>
        /// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
        /// <returns>A Task representing the asynchronous operation, containing the initialized Database instance.</returns>
        /// <remarks>
        /// Example Usage:
        /// await InitializeAsync(sqliteConnection, timeout)
        /// </remarks>
        public Task<DatabaseConnection> InitializeAsync(
            SqliteConnection sqliteConnection,
            int timeout = 30,
            CancellationToken token = default
        )
        {
            return InitializeAsync(
                new SqliteConnection(sqliteConnection),
                new SqliteCompiler(),
                timeout,
                token
            );
        }

        /// <summary>
        /// Initializes the Database Management System with the specified parameters for SQL Server databases.
        /// </summary>
        /// <param name="sqlCredential">The SqlCredential object containing the user ID and password used for authentication.</param>
        /// <param name="connectionString">The connection string used to access the specified database.</param>
        /// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
        /// <param name="useLegacyPagination">Specifies whether to use legacy pagination for query results. Default is false.</param>
        /// <remarks>
        /// Example Usage:
        /// Initialize(sqlCredential, connectionString, timeout)
        /// </remarks>
        public void Initialize(
            SqlCredential sqlCredential,
            string connectionString,
            int timeout = 30,
            bool useLegacyPagination = false
        )
        {
            Initialize(
                new SqlConnection(connectionString, sqlCredential),
                new SqlServerCompiler() { UseLegacyPagination = useLegacyPagination },
                timeout
            );
        }

        /// <summary>
        /// Initializes the Database Management System asynchronously with the specified parameters for SQL Server databases.
        /// </summary>
        /// <param name="sqlCredential">The SqlCredential object containing the user ID and password used for authentication.</param>
        /// <param name="connectionString">The connection string used to access the specified database.</param>
        /// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
        /// <param name="useLegacyPagination">Specifies whether to use legacy pagination for query results. Default is false.</param>
        /// <returns>A Task representing the asynchronous operation, containing the initialized Database instance.</returns>
        /// <remarks>
        /// Example Usage:
        /// await InitializeAsync(sqlCredential, connectionString, timeout)
        /// </remarks>
        public Task<DatabaseConnection> InitializeAsync(
            SqlCredential sqlCredential,
            string connectionString,
            int timeout = 30,
            bool useLegacyPagination = false,
            CancellationToken token = default
        )
        {
            return InitializeAsync(
                new SqlConnection(connectionString, sqlCredential),
                new SqlServerCompiler() { UseLegacyPagination = useLegacyPagination },
                timeout,
                token
            );
        }

        /// <summary>
        /// Initializes the Database Management System with the specified parameters for Oracle databases.
        /// </summary>
        /// <param name="oracleCredential">The OracleCredential object containing the user ID and password used for authentication.</param>
        /// <param name="connectionString">The connection string used to access the specified database.</param>
        /// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
        /// <param name="useLegacyPagination">Specifies whether to use legacy pagination for query results. Default is false.</param>
        /// <remarks>
        /// Example Usage:
        /// Initialize(oracleCredential, connectionString, timeout)
        /// </remarks>
        public void Initialize(
            OracleCredential oracleCredential,
            string connectionString,
            int timeout = 30,
            bool useLegacyPagination = false
        )
        {
            Initialize(
                new OracleConnection(connectionString, oracleCredential),
                new OracleCompiler() { UseLegacyPagination = useLegacyPagination },
                timeout
            );
        }

        /// <summary>
        /// Initializes the Database Management System asynchronously with the specified parameters for Oracle databases.
        /// </summary>
        /// <param name="oracleCredential">The OracleCredential object containing the user ID and password used for authentication.</param>
        /// <param name="connectionString">The connection string used to access the specified database.</param>
        /// <param name="timeout">The timeout value (in seconds) for database operations like Insert, Update, etc. Default is 30 seconds.</param>
        /// <param name="useLegacyPagination">Specifies whether to use legacy pagination for query results. Default is false.</param>
        /// <returns>A Task representing the asynchronous operation, containing the initialized Database instance.</returns>
        /// <remarks>
        /// This method asynchronously establishes a connection to the specified database and initializes the database for performing database operations.
        /// Example Usage:
        /// await InitializeAsync(oracleCredential, connectionString, timeout)
        /// </remarks>
        public Task<DatabaseConnection> InitializeAsync(
            OracleCredential oracleCredential,
            string connectionString,
            int timeout = 30,
            bool useLegacyPagination = false
        )
        {
            return InitializeAsync(
                new OracleConnection(connectionString, oracleCredential),
                new OracleCompiler() { UseLegacyPagination = useLegacyPagination },
                timeout
            );
        }

        /// <summary>
        /// Executes a SQL query and returns the number of rows affected.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="param">The parameters to use in the query.</param>
        /// <param name="transaction">The transaction to use for the command.</param>
        /// <param name="timeout">The command timeout (in seconds). If not specified, the default timeout is used.</param>
        /// <returns>The number of rows affected by the query.</returns>
        /// <remarks>
        /// This method executes the provided SQL query using the initialized Database Management System.
        /// If a transaction is specified, the query is executed within that transaction context.
        /// </remarks>
        public int Run(
            string query,
            object param = null,
            DbTransaction transaction = null,
            int? timeout = null
        )
        {
            ThrowErrorIfNotInitialized();
            return Factory.Statement(query, param, transaction, timeout);
        }

        /// <summary>
        /// Executes a SQL query asynchronously and returns the number of rows affected.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="param">The parameters to use in the query.</param>
        /// <param name="transaction">The transaction to use for the command. Default is null.</param>
        /// <param name="timeout">The command timeout (in seconds). If not specified, the default timeout is used.</param>
        /// <returns>A Task representing the asynchronous operation, containing the number of rows affected by the query.</returns>
        /// <remarks>
        /// This method asynchronously executes the provided SQL query using the initialized Database Management System (DBMS).
        /// If a transaction is specified, the query is executed within that transaction context.
        /// </remarks>
        public Task<int> RunAsync(
            string query,
            object param = null,
            DbTransaction transaction = null,
            int? timeout = null,
            CancellationToken token = default
        )
        {
            ThrowErrorIfNotInitialized();
            return Factory.StatementAsync(query, param, transaction, timeout, token);
        }

        /// <summary>
        /// Returns the raw SQL string of a given Query object.
        /// </summary>
        /// <param name="query">The Query object to compile.</param>
        /// <returns>The raw SQL string.</returns>
        /// <remarks>
        /// This method compiles the provided Query object and returns its raw SQL string,
        /// which includes parameter placeholders instead of actual values.
        /// </remarks>
        public string GetRawSql(Query query)
        {
            return queryFactory.Compiler.Compile(query).RawSql;
        }

        /// <summary>
        /// Returns the SQL string representation of the given Query object.
        /// </summary>
        /// <param name="query">The Query object to compile.</param>
        /// <returns>The SQL string representation of the given Query object.</returns>
        /// <remarks>
        /// This method compiles the provided Query object and returns its SQL string representation,
        /// with parameter placeholders replaced by their actual values.
        /// </remarks>
        public string GetSql(Query query)
        {
            return queryFactory.Compiler.Compile(query).Sql;
        }

        /// <summary>
        /// Starts a new persistent connection to the database. This connection must not be terminated.
        /// </summary>
        /// <returns>A new instance of the Database class.</returns>
        /// <remarks>
        /// This method initializes a new instance of the Database class, which can be used to interact with the database.
        /// The connection established by this instance should not be terminated prematurely.
        /// </remarks>
        public static DatabaseConnection New()
        {
            return new DatabaseConnection();
        }

        /// <summary>
        /// Closes the database connection.
        /// </summary>
        /// <remarks>
        /// This method closes the active connection to the database, ensuring that no further operations can be performed
        /// until the connection is reopened.
        /// </remarks>
        public void Close()
        {
            dbConnection.Close();
        }

        /// <summary>
        /// Asynchronously closes the database connection.
        /// </summary>
        /// <remarks>
        /// This method asynchronously closes the active connection to the database, ensuring that no further operations can be performed
        /// until the connection is reopened.
        /// </remarks>
        public void CloseAsync()
        {
            dbConnection.CloseAsync();
        }

        /// <summary>
        /// Asynchronously releases all resources used by the database.
        /// </summary>
        /// <returns>A ValueTask that represents the asynchronous dispose operation.</returns>
        /// <remarks>
        /// This method asynchronously disposes of the database connection and releases all associated resources. It should be called
        /// when the database object is no longer needed to ensure proper cleanup of resources.
        /// </remarks>
        public ValueTask DisposeAsync()
        {
            return dbConnection.DisposeAsync();
        }

        /// <summary>
        /// Releases all resources used by the database.
        /// </summary>
        /// <remarks>
        /// This method disposes of the database connection and releases all associated resources. It should be called
        /// when the database object is no longer needed to ensure proper cleanup of resources.
        /// </remarks>
        public void Dispose()
        {
            dbConnection.Dispose();
        }

        private void ThrowErrorIfNotInitialized()
        {
            if (queryFactory == null || dbConnection == null)
            {
                throw new Exception(
                    $"Call \"{nameof(Initialize)}()\" before accessing the QueryFactory! Connection state: {dbConnection?.State}"
                );
            }

            if (dbConnection != null)
            {
                switch (dbConnection.State)
                {
                    case ConnectionState.Open:
                        // Database connection is open.
                        break;
                    case ConnectionState.Closed:
                        NetworkLogger.__Log__(
                            $"The connection to the database is closed for table '{tableName}'.",
                            NetworkLogger.LogType.Error
                        );
                        // The database connection is closed; unable to perform operations.
                        break;
                    case ConnectionState.Broken:
                        NetworkLogger.__Log__(
                            $"The connection to the database is broken for table '{tableName}'.",
                            NetworkLogger.LogType.Error
                        );
                        // The database connection is broken; requires re-establishment.
                        break;
                    case ConnectionState.Connecting:
                        NetworkLogger.__Log__(
                            $"The connection to the database is currently in the process of establishing for table '{tableName}'.",
                            NetworkLogger.LogType.Error
                        );
                        // The database connection is in the process of establishing a connection.
                        break;
                    case ConnectionState.Executing:
                        NetworkLogger.__Log__(
                            $"The connection to the database is currently executing a command for table '{tableName}'.",
                            NetworkLogger.LogType.Error
                        );
                        // The database connection is executing a command.
                        break;
                    case ConnectionState.Fetching:
                        NetworkLogger.__Log__(
                            $"The connection to the database is currently fetching data for table '{tableName}'.",
                            NetworkLogger.LogType.Error
                        );
                        // The database connection is actively retrieving data.
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <returns>An object representing the new transaction.</returns>
        public IDbTransaction BeginTransaction()
        {
            return dbConnection.BeginTransaction();
        }

        /// <summary>
        /// Begins a database transaction with the specified isolation level.
        /// </summary>
        /// <param name="il">The isolation level under which the transaction should run.</param>
        /// <returns>An object representing the new transaction.</returns>
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return dbConnection.BeginTransaction(il);
        }

        /// <summary>
        /// Changes the current database for an open Connection object.
        /// </summary>
        /// <param name="databaseName">The name of the database to use instead of the current database.</param>
        public void ChangeDatabase(string databaseName)
        {
            dbConnection.ChangeDatabase(databaseName);
        }

        /// <summary>
        /// Creates and returns a command object associated with the connection.
        /// </summary>
        /// <returns>A command object associated with the connection.</returns>
        public IDbCommand CreateCommand()
        {
            return dbConnection.CreateCommand();
        }

        /// <summary>
        /// Opens a database connection with the settings specified by the ConnectionString property.
        /// </summary>
        public void Open()
        {
            dbConnection.Open();
        }
    }
}
