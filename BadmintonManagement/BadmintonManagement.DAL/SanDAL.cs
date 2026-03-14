using System.Collections.Generic;
using System.Data;
using BadmintonManagement.DTO;
using Microsoft.Data.SqlClient; // Nếu báo lỗi đỏ ở đây, bạn cần cài NuGet Microsoft.Data.SqlClient

namespace BadmintonManagement.DAL
{
    public class SanDAL
    {
        private DataProvider dataProvider = new DataProvider();

        public List<SanDTO> GetAllSan()
        {
            List<SanDTO> dsSan = new List<SanDTO>();
            // Truy vấn đúng tên bảng SAN và các cột trong database của bạn
            string query = "SELECT MaSan, TenSan, MaLoaiSan, TrangThai FROM SAN";

            DataTable data = dataProvider.ExecuteQuery(query);

            foreach (DataRow row in data.Rows)
            {
                SanDTO san = new SanDTO
                {
                    MaSan = row["MaSan"].ToString(),
                    TenSan = row["TenSan"].ToString(),
                    MaLoaiSan = row["MaLoaiSan"].ToString(),
                    TrangThai = row["TrangThai"].ToString()
                };
                dsSan.Add(san);
            }
            return dsSan;
        }
    }
}