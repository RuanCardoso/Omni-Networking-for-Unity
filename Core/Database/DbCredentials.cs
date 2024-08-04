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
    public sealed class DbCredentials
    {
        public string Server { get; private set; }
        public string Database { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string ConnectionString { get; internal set; }
        public DatabaseType Type { get; private set; }

        internal DbCredentials(
            DatabaseType type,
            string server,
            string database,
            string username,
            string password
        )
        {
            SetConnectionString(type, server, database, username, password);
        }

        public DbCredentials([CallerMemberName] string _ = "")
        {
            if (_ != "Awake" && _ != "Start" && _ != "OnAwake" && _ != "OnStart")
            {
                throw new InvalidOperationException(
                    $"DbCredentials constructor should be called from within a method (Awake or Start), not directly within the class scope. {_}"
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
        public void SetConnectionString(
            DatabaseType type,
            string server,
            string database,
            string username,
            string password,
            [CallerMemberName] string _ = ""
        )
        {
            Type = type;
            Server = server;
            Database = database;
            Username = username;
            Password = password;
            ConnectionString = GetConnectionString();
        }

        private string GetConnectionString()
        {
            return Type switch
            {
                DatabaseType.SqlServer
                    => $"Server={Server};Database={Database};User Id={Username};Password={Password};",
                DatabaseType.MariaDb
                or DatabaseType.MySql
                    => $"Server={Server};Database={Database};Uid={Username};Pwd={Password};",
                DatabaseType.PostgreSql
                    => $"Host={Server};Database={Database};Username={Username};Password={Password};",
                DatabaseType.Oracle
                    => $"Data Source={Server};User Id={Username};Password={Password};",
                DatabaseType.SQLite
                    => $"Data Source={Server};Version=3;Pooling=True;Max Pool Size=100;",
                DatabaseType.Firebird => $"Database={Server};User={Username};Password={Password};",
                _ => throw new ArgumentException("Invalid database type"),
            };
        }
    }
}
