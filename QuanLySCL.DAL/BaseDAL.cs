using System;
using System.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace QuanLySCL.DAL
{
    public class BaseDAL
    {
        // Override nhanh bằng env var `QLSCL_CONNECTION_STRING` để không phải sửa code theo máy.
        protected string connectionString =
            Environment.GetEnvironmentVariable("QLSCL_CONNECTION_STRING")
            ?? @"Data Source=.\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";

        protected DataTable ExecuteQuery(string query, object[]? parameter = null)
        {
            DataTable data = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                if (parameter != null)
                {
                    AddParameters(command, query, parameter);
                }
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                adapter.Fill(data);
                connection.Close();
            }
            return data;
        }

        protected int ExecuteNonQuery(string query, object[]? parameter = null)
        {
            int data = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                if (parameter != null)
                {
                    AddParameters(command, query, parameter);
                }
                data = command.ExecuteNonQuery();
                connection.Close();
            }
            return data;
        }

        protected static void AddParameters(SqlCommand command, string query, object[] parameter)
        {
            // Extract parameters reliably from multi-line SQL.
            // The old split-by-space approach breaks when tokens include punctuation/newlines.
            var matches = Regex.Matches(query, @"@\w+");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var orderedNames = new List<string>();

            foreach (Match m in matches)
            {
                string name = m.Value;
                if (seen.Add(name))
                    orderedNames.Add(name);
            }

            for (int i = 0; i < orderedNames.Count && i < parameter.Length; i++)
            {
                command.Parameters.AddWithValue(orderedNames[i], parameter[i] ?? DBNull.Value);
            }
        }

        protected int ExecuteNonQueryTrans(SqlConnection conn, SqlTransaction trans, string query, object[] parameter = null)
        {
            SqlCommand command = new SqlCommand(query, conn, trans);
            if (parameter != null)
            {
                AddParameters(command, query, parameter);
            }
            return command.ExecuteNonQuery();
        }
    }
}
