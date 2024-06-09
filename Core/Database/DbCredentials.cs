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

namespace Omni.Core
{
    public class DbCredentials
    {
        public string Server { get; }
        public string Database { get; }
        public string Username { get; }
        public string Password { get; }
        public string ConnectionString { get; internal set; }
        public DatabaseType Type { get; }

        public DbCredentials(
            DatabaseType type,
            string server,
            string database,
            string username,
            string password
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
