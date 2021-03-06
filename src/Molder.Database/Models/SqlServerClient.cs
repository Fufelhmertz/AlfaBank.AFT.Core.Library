﻿using Molder.Database.Exceptions;
using Molder.Database.Infrastructures;
using System;
using System.Data;
using System.Data.SqlClient;
using Molder.Helpers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Molder.Models.DateTimeHelpers;
using System.Threading;

namespace Molder.Database.Models
{
    public class SqlServerClient : IDbClient, IDisposable
    {
        public AsyncLocal<ISqlProvider> _provider = new AsyncLocal<ISqlProvider>() { Value = new SqlProvider() };
        public AsyncLocal<IDateTimeHelper> dateTimeHelper = new AsyncLocal<IDateTimeHelper>() { Value = new DateTimeHelper() };
        public DateTime lastConnect;

        public SqlServerClient()
        {
            lastConnect = dateTimeHelper.Value.GetDateTimeNow();
        }

        public bool Create(DbConnectionParams parameters)
        {
            try
            {
                if ((dateTimeHelper.Value.GetDateTimeNow() - lastConnect).TotalSeconds < DbSetting.PERIOD)
                {
                    Thread.Sleep((int)Math.Ceiling(DbSetting.PERIOD - (dateTimeHelper.Value.GetDateTimeNow() - lastConnect).TotalSeconds) * 1000);
                }

                var connectionString = new SqlConnectionStringBuilder()
                {
                    DataSource = parameters.Source,
                    InitialCatalog = parameters.Database,
                    UserID = parameters.Login,
                    Password = parameters.Password
                };

                if (connectionString.LoadBalanceTimeout <= 0)
                {
                    connectionString.LoadBalanceTimeout = parameters.Timeout != null ? (int)parameters.Timeout : DbSetting.TIMEOUT;
                }

                connectionString.ConnectTimeout = parameters.Timeout != null ? (int)parameters.Timeout : DbSetting.TIMEOUT;
                connectionString.ConnectRetryCount = parameters.ConnectRetryCount;
                connectionString.ConnectRetryInterval = parameters.ConnectRetryInterval;

                Log.Logger().LogInformation($"Connection has parameters: {Database.Helpers.Message.CreateMessage(connectionString.ToString())}");

                var connect = _provider.Value.Create(connectionString.ToString());

                lastConnect = dateTimeHelper.Value.GetDateTimeNow();

                return connect;
            }
            catch (ConnectSqlException ex)
            {
                Log.Logger().LogError($"Connection failed.{ex.Message}");
                throw new ConnectSqlException($"Connection failed. {ex.Message}");
            }
            catch (System.Exception ex)
            {
                Log.Logger().LogError($"Connection failed: {ex.Message}");
                throw new ConnectSqlException($"Connection failed: {ex.Message}");
            }
        }
        public bool IsConnectAlive()
        {
            var result = _provider.Value.IsConnectAlive();

            if (result == true)
                Log.Logger().LogInformation("Connect is alive");
            else
                Log.Logger().LogInformation("Connect isn't alive");

            return result;
        }

        [ExcludeFromCodeCoverage]
        public void Dispose()
        {
            _provider.Value.Disconnect();
        }

        public int ExecuteNonQuery(string query, int? timeout = null)
        {
            var command = _provider.Value.SetupCommand(query, timeout);

            var affectedRows = 0;
            _provider.Value.UsingTransaction(
                (transaction) =>
                {
                    command.Transaction = transaction;
                    affectedRows = command.ExecuteNonQuery();
                },
                (ex) =>
                {
                    Log.Logger().LogError($"Failed to execute SQL Query.{Environment.NewLine}Error Message: {ex.Message}{Environment.NewLine}Query:{Environment.NewLine}{Helpers.Message.CreateMessage(command)}");
                    affectedRows = 0;
                },
                () =>
                {
                    Log.Logger().LogInformation("SQL Non-Query: {0}", Helpers.Message.CreateMessage(command));
                });

            return affectedRows;
        }

        public (object, int) ExecuteQuery(string query, int? timeout = null)
        {
            var results = new DataTable();

            var command = _provider.Value.SetupCommand(query, timeout);

            IDataReader reader = null;
            var tmpResults = results;

            _provider.Value.UsingTransaction(
                (transaction) =>
                {
                    command.Transaction = transaction;
                    reader = command.ExecuteReader();
                    tmpResults.Load(reader);

                    reader.Dispose();
                },
                (ex) =>
                {
                    Log.Logger().LogError($"Failed to execute SQL Query.{Environment.NewLine} Error Message: {ex.Message}{Environment.NewLine}Query:{Environment.NewLine}{Helpers.Message.CreateMessage(command)}");
                    reader?.Dispose();
                    tmpResults = null;
                },
                () =>
                {
                    Log.Logger().LogInformation("SQL Query: {0}", Helpers.Message.CreateMessage(command));
                });

            if (tmpResults == null)
            {
                return (null, 0);
            }

            results = tmpResults;
            if (string.IsNullOrWhiteSpace(results.TableName))
            {
                results.TableName = "SQLResults";
            }

            var affectedRows = tmpResults?.Rows?.Count ?? 0;
            tmpResults.Dispose();
            return (results, affectedRows);
        }

        public object ExecuteScalar(string query, int? timeout = null)  //todo
        {
            var command = _provider.Value.SetupCommand(query, timeout);

            object result = null;
            _provider.Value.UsingTransaction(
                (transaction) =>
                {
                    command.Transaction = transaction;
                    result = command.ExecuteScalar();
                },
                (ex) =>
                {
                    Log.Logger().LogError(ex.Message);
                    result = null;
                },
                () =>
                {
                    Log.Logger().LogInformation("SQL Query (Scalar): {0}", Helpers.Message.CreateMessage(command));
                });
            return result; //возвращает affectedrow
        }
    }
}
