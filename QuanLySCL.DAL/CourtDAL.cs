using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Data;

namespace QuanLySCL.DAL
{
    public class CourtDAL : BaseDAL
    {
        public ObservableCollection<Court> GetAllCourts()
        {
            ObservableCollection<Court> list = new ObservableCollection<Court>();
            const string query = "SELECT MaSan, TenSan, LoaiSan, TrangThai FROM SAN ORDER BY TenSan";

            DataTable dt = ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                string statusVN = row["TrangThai"]?.ToString()?.Trim();
                string statusEN = statusVN switch
                {
                    "Sẵn sàng" => "Available",
                    "Đang sử dụng" => "In-use",
                    "Bảo trì" => "Maintenance",
                    _ => "Available"
                };

                list.Add(new Court
                {
                    Id = row["MaSan"]?.ToString(),
                    Name = row["TenSan"]?.ToString(),
                    Type = row["LoaiSan"]?.ToString(),
                    Status = statusEN
                });
            }

            return list;
        }

        public ObservableCollection<CourtType> GetCourtTypes()
        {
            ObservableCollection<CourtType> list = new ObservableCollection<CourtType>();
            string query = @"
                SELECT MaLoaiSan, TenLoai, MoTa
                FROM LOAI_SAN
                ORDER BY TenLoai";

            DataTable dt = ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new CourtType
                {
                    Id = row["MaLoaiSan"]?.ToString(),
                    Name = row["TenLoai"]?.ToString(),
                    Description = row["MoTa"]?.ToString()
                });
            }

            return list;
        }

        public string GetNextCourtId()
        {
            const string query = "SELECT MaSan FROM SAN WHERE MaSan LIKE 'S%'";
            DataTable dt = ExecuteQuery(query);

            int max = 0;
            foreach (DataRow row in dt.Rows)
            {
                string id = row[0]?.ToString()?.Trim();
                if (string.IsNullOrEmpty(id)) continue;
                if (!id.StartsWith("S", StringComparison.OrdinalIgnoreCase)) continue;

                string numPart = id.Length > 1 ? id.Substring(1) : string.Empty;
                if (int.TryParse(numPart, out int n))
                    max = Math.Max(max, n);
            }

            return "S" + (max + 1).ToString("00");
        }

        public int CreateCourt(string courtId, string courtName, string courtTypeId, string statusEn)
        {
            string typeName = GetCourtTypeName(courtTypeId);
            string statusVN = MapCourtStatusToVN(statusEn);

            string query = @"
                INSERT INTO SAN (MaSan, TenSan, MaLoaiSan, TrangThai, LoaiSan)
                VALUES (@id, @name, @typeId, @status, @typeName)";

            return ExecuteNonQuery(query, new object[] { courtId, courtName, courtTypeId, statusVN, typeName });
        }

        public int UpdateCourt(string courtId, string courtName, string courtTypeId, string statusEn)
        {
            string typeName = GetCourtTypeName(courtTypeId);
            string statusVN = MapCourtStatusToVN(statusEn);

            string query = @"
                UPDATE SAN
                SET TenSan = @name,
                    MaLoaiSan = @typeId,
                    TrangThai = @status,
                    LoaiSan = @typeName
                WHERE MaSan = @id";

            return ExecuteNonQuery(query, new object[] { courtName, courtTypeId, statusVN, typeName, courtId });
        }

        public int DeleteCourt(string courtId)
        {
            string query = "DELETE FROM SAN WHERE MaSan = @id";
            return ExecuteNonQuery(query, new object[] { courtId });
        }

        public int UpdateCourtStatus(string courtId, string statusEn)
        {
            string statusVN = MapCourtStatusToVN(statusEn);
            const string query = "UPDATE SAN SET TrangThai = @status WHERE MaSan = @id";
            return ExecuteNonQuery(query, new object[] { statusVN, courtId });
        }

        private string GetCourtTypeName(string courtTypeId)
        {
            const string query = "SELECT TOP 1 TenLoai FROM LOAI_SAN WHERE MaLoaiSan = @id";
            DataTable dt = ExecuteQuery(query, new object[] { courtTypeId });
            return dt.Rows.Count > 0 ? (dt.Rows[0][0]?.ToString() ?? string.Empty) : string.Empty;
        }

        private static string MapCourtStatusToVN(string statusEn)
        {
            return statusEn?.Trim() switch
            {
                "Maintenance" => "Bảo trì",
                "In-use" => "Đang sử dụng",
                _ => "Sẵn sàng"
            };
        }
    }
}
