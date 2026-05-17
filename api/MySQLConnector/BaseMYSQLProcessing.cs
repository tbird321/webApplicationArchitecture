using MySql.Data.MySqlClient;
using MySQLConnector.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MySQLConnector
{
    abstract public class BaseMYSQLProcessing
    {
        private ConnectionModel _connectionModel;
        public BaseMYSQLProcessing(ConnectionModel connectionModel)
        {
            _connectionModel = connectionModel;
        }

        public ConnectionModel CurrentConnectionInfo
        {
            get{
                return _connectionModel;
            }
        }

        public MySqlConnection CreateMySqlConnection()
        {

            var connectionString = $"Server={_connectionModel.host};Database={_connectionModel.dbname};Uid={_connectionModel.username};Pwd={_connectionModel.password};";

            return new MySqlConnection(connectionString);
        }

    }
}
