using Microsoft.Data.SqlClient;
using System.Data;

namespace BadmintonManagement.DAL
{
    public class DataProvider
    {
        // Nhớ thay đổi chuỗi connectionString này cho đúng với Server máy bạn
        private string connectionString = @"Data Source=localhost\SQLEXPRESS;Initial Catalog=QLSCL;Integrated Security=True;TrustServerCertificate=True";
        public DataTable ExecuteQuery(string query)
        {
            DataTable data = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataAdapter adapter = new SqlDataAdapter(command);
                adapter.Fill(data);
                connection.Close();
            }
            return data;
        }
    }
}