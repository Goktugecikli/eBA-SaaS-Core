using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SanShine.SaaS.Core.Common.Core.Extensions.CustomExceptions;

namespace SanShine.SaaS.Core.Infrastucture.DbManagerRepository
{
    using SanShine.SaaS.Core.Common.Core.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;
    using System.Threading.Tasks;

    public static class DbManager
    {
        private static readonly string _connectionString;
        public class DbManagerException : Exception
        {
            public int ErrorCode { get; }
            public string ProcedureName { get; }

            public DbManagerException(string message, int errorCode = 0, string procedureName = null,
                Exception innerException = null)
                : base(message, innerException)
            {
                ErrorCode = errorCode;
                ProcedureName = procedureName;
            }
        }
        static DbManager()
        {
            var configuration = Utilities.GetConfiguration();

            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public static async Task<bool> RunProcedureAsync(string procedureName, Dictionary<string, object> parameters)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(procedureName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                            }
                        }
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected == -1;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DbManagerException($"Error : {ex.Message}", ex.Number, procedureName, ex);
            }
            catch (Exception ex)
            {
                throw new DbManagerException("An error occurred while running the procedure.", innerException: ex);
            }
        }

        public static bool RunProcedure(string procedureName, Dictionary<string, object> parameters)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(procedureName, connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                            }
                        }
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected == -1;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DbManagerException($"Error : {ex.Message}", ex.Number, procedureName, ex);
            }
            catch (Exception ex)
            {
                throw new DbManagerException("An error occurred while running the procedure.", innerException: ex);
            }
        }

        public static async Task<List<T>> ReadAsync<T>(string query, Dictionary<string, object> parameters) where T : new()
        {
            var columnMappings = GetColumnMappings<T>();
            DataTable resultTable = await RunQueryAsync(query, parameters);
            List<T> resultList = MapResultToModelList<T>(resultTable, columnMappings);
            return resultList;
        }

        public static List<T> Read<T>(string query, Dictionary<string, object> parameters) where T : new()
        {
            var columnMappings = GetColumnMappings<T>();
            DataTable resultTable = RunQuery(query, parameters);
            List<T> resultList = MapResultToModelList<T>(resultTable, columnMappings);
            return resultList;
        }
		public static async Task<bool> ExecuteNonQueryTransactionAsync(string query, Dictionary<string, object> parameters)
        {
            SqlConnection connection = null;
            SqlTransaction transaction = null;

            try
            {
                connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                transaction = connection.BeginTransaction();
                using (var command = new SqlCommand(query, connection, transaction))
                {
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }
                    }

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 1)
                    {
                        transaction.Commit();
                        return true;
                    }
                    else
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {

                try
                {
                    transaction?.Rollback();
                }
                catch (Exception rollbackEx)
                {
                    throw new DatabaseException("An error occurred during transaction rollback: " + rollbackEx.Message);
                }
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    await connection.CloseAsync();
                    connection.Dispose();
                }
            }
        }

		public static async Task<bool> ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                            }
                        }
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException("An error occurred during database operations: " + ex.Message, innerException:ex);
            }
        }

        public static bool ExecuteNonQuery(string query, Dictionary<string, object> parameters)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                            }
                        }
                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException("An error occurred during database operations: " + ex.Message);
                return false;
            }
        }

        private static Dictionary<string, PropertyInfo> GetColumnMappings<T>() where T : new()
        {
            Dictionary<string, PropertyInfo> columnMappings = new Dictionary<string, PropertyInfo>();
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                var columnAttribute = property.GetCustomAttribute<sColumnAttribute>();
                if (columnAttribute != null)
                {
                    columnMappings[columnAttribute.ColumnName] = property;
                }
            }
            return columnMappings;
        }

        private static async Task<DataTable> RunQueryAsync(string query, Dictionary<string, object> parameters)
        {
            DataTable resultTable = new DataTable();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                            }
                        }
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            resultTable.Load(reader);
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                throw new DatabaseException("SQL error occurred: " + sqlEx.Message);
            }
			catch (Exception ex)
            {
                throw new DatabaseException("An error occurred during database operations: " + ex.Message);
            }

            return resultTable;
        }

        private static DataTable RunQuery(string query, Dictionary<string, object> parameters)
        {
            DataTable resultTable = new DataTable();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                            }
                        }
                        using (var adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(resultTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException("An error occurred during database operations: " + ex.Message);
            }
            return resultTable;
        }

        private static List<T> MapResultToModelList<T>(DataTable resultTable,
            Dictionary<string, PropertyInfo> columnMappings) where T : new()
        {
            List<T> resultList = new List<T>();
            foreach (DataRow row in resultTable.Rows)
            {
                T model = new T();
                foreach (DataColumn column in resultTable.Columns)
                {
                    string columnName = column.ColumnName;
                    if (columnMappings.ContainsKey(columnName))
                    {
                        PropertyInfo property = columnMappings[columnName];
                        object value = row[columnName];
                        if (value != DBNull.Value)
                        {
                            if (property.PropertyType == typeof(string))
                            {
                                property.SetValue(model, value.ToString());
                            }
                            else if (property.PropertyType == typeof(Int64))
                            {
                                property.SetValue(model, (long)(ulong)value);
                            }
                            else
                            {
                                property.SetValue(model, value);
                            }
                        }
                        else
                        {
                            property.SetValue(model, null);
                        }
                    }
                }
                resultList.Add(model);
            }
            return resultList;
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class sColumnAttribute : Attribute
        {
            public string ColumnName { get; }
            public sColumnAttribute(string columnName)
            {
                ColumnName = columnName;
            }
        }
    }

}
