using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Data;

namespace QuanLySCL.DAL
{
    public class StaffDAL : BaseDAL
    {
        public ObservableCollection<Staff> GetAllStaff()
        {
            ObservableCollection<Staff> list = new ObservableCollection<Staff>();
            // `test03.sql` schema: NHAN_VIEN(MaNV, HoTen, SDT, ChucVu)
            string query = "SELECT MaNV as Id, HoTen as Name, SDT as Phone, ChucVu as Role FROM NHAN_VIEN";

            DataTable dt = ExecuteQuery(query);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Staff
                {
                    Id = row["Id"].ToString(),
                    Name = row["Name"].ToString(),
                    Phone = row["Phone"] == DBNull.Value ? string.Empty : row["Phone"].ToString(),
                    Email = string.Empty,
                    Role = row["Role"] == DBNull.Value ? string.Empty : row["Role"].ToString(),
                    Department = "Chung",
                    Status = "Active",
                    JoinDate = DateTime.Today
                });
            }

            return list;
        }

        public string GetNextStaffId()
        {
            string sql = "SELECT ISNULL(MAX(CAST(SUBSTRING(MaNV, 3, LEN(MaNV)-2) AS INT)), 0) + 1 FROM NHAN_VIEN WHERE MaNV LIKE 'NV%'";
            DataTable dt = ExecuteQuery(sql);
            int nextNum = 1;
            if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                nextNum = Convert.ToInt32(dt.Rows[0][0]);
            return "NV" + nextNum.ToString("D3");
        }

        public int InsertStaff(Staff staff)
        {
            string sql = @"
                INSERT INTO NHAN_VIEN (MaNV, HoTen, SDT, ChucVu)
                VALUES (@id, @name, @phone, @role)";

            return ExecuteNonQuery(sql, new object[]
            {
                staff.Id,
                staff.Name,
                string.IsNullOrWhiteSpace(staff.Phone) ? DBNull.Value : staff.Phone,
                string.IsNullOrWhiteSpace(staff.Role) ? DBNull.Value : staff.Role
            });
        }

        public int UpdateStaff(Staff staff)
        {
            string sql = @"
                UPDATE NHAN_VIEN
                SET HoTen = @name,
                    SDT = @phone,
                    ChucVu = @role
                WHERE MaNV = @id";

            return ExecuteNonQuery(sql, new object[]
            {
                staff.Name,
                string.IsNullOrWhiteSpace(staff.Phone) ? DBNull.Value : staff.Phone,
                string.IsNullOrWhiteSpace(staff.Role) ? DBNull.Value : staff.Role,
                staff.Id
            });
        }

        public int DeleteStaff(string id)
        {
            string sql = "DELETE FROM NHAN_VIEN WHERE MaNV = @id";
            return ExecuteNonQuery(sql, new object[] { id });
        }

        public Staff GetStaffById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            string query = "SELECT TOP 1 MaNV as Id, HoTen as Name, SDT as Phone, ChucVu as Role FROM NHAN_VIEN WHERE MaNV = @id";
            DataTable dt = ExecuteQuery(query, new object[] { id.Trim() });
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Staff
            {
                Id = row["Id"].ToString(),
                Name = row["Name"].ToString(),
                Phone = row["Phone"] == DBNull.Value ? string.Empty : row["Phone"].ToString(),
                Email = string.Empty,
                Role = row["Role"] == DBNull.Value ? string.Empty : row["Role"].ToString(),
                Department = "Chung",
                Status = "Active",
                JoinDate = DateTime.Today
            };
        }
    }
}
