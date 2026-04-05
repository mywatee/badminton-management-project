using Microsoft.Data.SqlClient;
using QuanLySCL.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace QuanLySCL.DAL
{
    public class BookingDAL : BaseDAL
    {
        public ObservableCollection<Booking> GetBookingsByCustomerId(string customerId)
        {
            ObservableCollection<Booking> list = new ObservableCollection<Booking>();
            if (string.IsNullOrWhiteSpace(customerId)) return list;

            string query = @"
                SELECT
                    DS.MaPhieuDat AS Id,
                    KH.HoTen AS Customer,
                    KH.SDT AS Phone,
                    S.TenSan AS Court,
                    MIN(CT.NgaySuDung) AS [Date],
                    (CAST(MIN(CG.GioBatDau) AS varchar(5)) + ' - ' + CAST(MAX(CG.GioKetThuc) AS varchar(5))) AS TimeString,
                    DS.LoaiDat AS [Type],
                    DS.TrangThai AS [Status],
                    ISNULL(DS.TongTien, 0) AS Amount
                FROM DAT_SAN DS
                INNER JOIN KHACH_HANG KH ON DS.MaKH = KH.MaKH
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                INNER JOIN SAN S ON CT.MaSan = S.MaSan
                INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                WHERE DS.MaKH = @makh
                GROUP BY DS.MaPhieuDat, KH.HoTen, KH.SDT, S.TenSan, DS.LoaiDat, DS.TrangThai, DS.TongTien
                ORDER BY MIN(CT.NgaySuDung) DESC, MIN(CG.GioBatDau) DESC, S.TenSan";

            DataTable dt = ExecuteQuery(query, new object[] { customerId.Trim() });
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Booking
                {
                    Id = row["Id"].ToString(),
                    Customer = row["Customer"].ToString(),
                    Phone = row["Phone"].ToString(),
                    Court = row["Court"].ToString(),
                    Date = row["Date"] != DBNull.Value ? (DateTime)row["Date"] : DateTime.Now,
                    Time = row["TimeString"].ToString(),
                    Type = MapBookingType(row["Type"]?.ToString()),
                    Status = MapBookingStatus(row["Status"]?.ToString()),
                    Amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : 0
                });
            }

            return list;
        }

        public ObservableCollection<Booking> GetAllBookings()
        {
            ObservableCollection<Booking> list = new ObservableCollection<Booking>();

            string query = @"
                SELECT
                    DS.MaPhieuDat AS Id,
                    KH.HoTen AS Customer,
                    KH.SDT AS Phone,
                    S.TenSan AS Court,
                    MIN(CT.NgaySuDung) AS [Date],
                    (CAST(MIN(CG.GioBatDau) AS varchar(5)) + ' - ' + CAST(MAX(CG.GioKetThuc) AS varchar(5))) AS TimeString,
                    DS.LoaiDat AS [Type],
                    DS.TrangThai AS [Status],
                    ISNULL(DS.TongTien, 0) AS Amount
                FROM DAT_SAN DS
                INNER JOIN KHACH_HANG KH ON DS.MaKH = KH.MaKH
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                INNER JOIN SAN S ON CT.MaSan = S.MaSan
                INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                GROUP BY DS.MaPhieuDat, KH.HoTen, KH.SDT, S.TenSan, DS.LoaiDat, DS.TrangThai, DS.TongTien
                ORDER BY MIN(CT.NgaySuDung) DESC, MIN(CG.GioBatDau) DESC, S.TenSan";

            DataTable dt = ExecuteQuery(query);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Booking
                {
                    Id = row["Id"].ToString(),
                    Customer = row["Customer"].ToString(),
                    Phone = row["Phone"].ToString(),
                    Court = row["Court"].ToString(),
                    Date = row["Date"] != DBNull.Value ? (DateTime)row["Date"] : DateTime.Now,
                    Time = row["TimeString"].ToString(),
                    Type = MapBookingType(row["Type"]?.ToString()),
                    Status = MapBookingStatus(row["Status"]?.ToString()),
                    Amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : 0
                });
            }

            return list;
        }

        public int CountActiveCustomersToday()
        {
            // Used by Customers dashboard card. Much cheaper than loading all bookings.
            string query = @"
                SELECT COUNT(DISTINCT DS.MaKH)
                FROM DAT_SAN DS
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                WHERE CAST(CT.NgaySuDung AS date) = CAST(GETDATE() AS date)
                  AND DS.MaKH IS NOT NULL";

            DataTable dt = ExecuteQuery(query);
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        public ObservableCollection<TimeSlot> GetAllTimeSlots()
        {
            ObservableCollection<TimeSlot> list = new ObservableCollection<TimeSlot>();
            string query = @"
                SELECT MaCa, TenCa, GioBatDau, GioKetThuc, ISNULL(LaKhungGioVang, 0) AS LaKhungGioVang
                FROM CA_GIO
                ORDER BY GioBatDau";

            DataTable dt = ExecuteQuery(query);

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new TimeSlot
                {
                    Id = row["MaCa"].ToString(),
                    Name = row["TenCa"].ToString(),
                    StartTime = row["GioBatDau"] != DBNull.Value ? (TimeSpan)row["GioBatDau"] : TimeSpan.Zero,
                    EndTime = row["GioKetThuc"] != DBNull.Value ? (TimeSpan)row["GioKetThuc"] : TimeSpan.Zero,
                    LaKhungGioVang = row["LaKhungGioVang"] != DBNull.Value && Convert.ToBoolean(row["LaKhungGioVang"])
                });
            }

            return list;
        }

        public ObservableCollection<CourtScheduleItem> GetScheduleBookings(DateTime startDate, DateTime endDate)
        {
            ObservableCollection<CourtScheduleItem> list = new ObservableCollection<CourtScheduleItem>();
            
            // Use LEFT JOIN to avoid losing records due to mapping orphans
            // and use UPPER/TRIM for maximum resilience.
            string query = @"
                SELECT 
                    DS.MaPhieuDat, 
                    KH.HoTen, 
                    KH.SDT, 
                    DS.LoaiDat, 
                    DS.TrangThai, 
                    ISNULL(DS.TongTien, 0) AS TongTien,
                    S.MaSan, 
                    S.TenSan,
                    CG.MaCa, 
                    CG.TenCa, 
                    CG.GioBatDau, 
                    CG.GioKetThuc, 
                    CT.NgaySuDung
                FROM CT_DAT_SAN CT
                INNER JOIN DAT_SAN DS ON DS.MaPhieuDat = CT.MaPhieuDat
                LEFT JOIN KHACH_HANG KH ON KH.MaKH = DS.MaKH
                LEFT JOIN SAN S ON S.MaSan = CT.MaSan
                LEFT JOIN CA_GIO CG ON CG.MaCa = CT.MaCa
                WHERE CAST(CT.NgaySuDung AS DATE) >= @start 
                  AND CAST(CT.NgaySuDung AS DATE) <= @end
                  AND CT.TrangThaiHieuLuc = 1";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@start", startDate.Date);
                cmd.Parameters.AddWithValue("@end", endDate.Date);
                conn.Open();
                
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            list.Add(new CourtScheduleItem
                            {
                                BookingId = reader["MaPhieuDat"]?.ToString()?.Trim(),
                                Customer = reader["HoTen"]?.ToString()?.Trim() ?? "N/A",
                                Phone = reader["SDT"]?.ToString()?.Trim() ?? "N/A",
                                Type = MapBookingType(reader["LoaiDat"]?.ToString()),
                                Status = MapBookingStatus(reader["TrangThai"]?.ToString()),
                                Amount = reader["TongTien"] != DBNull.Value ? Convert.ToDecimal(reader["TongTien"]) : 0,
                                CourtId = reader["MaSan"]?.ToString()?.Trim(),
                                CourtName = reader["TenSan"]?.ToString()?.Trim() ?? "Unknown",
                                SlotId = reader["MaCa"]?.ToString()?.Trim(),
                                SlotName = reader["TenCa"]?.ToString()?.Trim() ?? "Unknown",
                                StartTime = reader["GioBatDau"] != DBNull.Value ? (TimeSpan)reader["GioBatDau"] : TimeSpan.Zero,
                                EndTime = reader["GioKetThuc"] != DBNull.Value ? (TimeSpan)reader["GioKetThuc"] : TimeSpan.Zero,
                                Date = reader["NgaySuDung"] != DBNull.Value ? Convert.ToDateTime(reader["NgaySuDung"]).Date : DateTime.MinValue
                            });
                        }
                        catch { }
                    }
                }
            }

            return list;
        }

        public bool CreateBookingWithDetail(
            string customerId,
            string courtId,
            string slotId,
            DateTime usageDate,
            string bookingTypeVN,
            List<(string serviceId, int quantity, decimal price)> selectedServices, 
            bool ignorePastCheck,
            out string bookingId,
            out string error)
        {
            return CreateBookingWithDetails(
                customerId,
                courtId,
                new List<string> { slotId },
                usageDate,
                bookingTypeVN,
                selectedServices,
                ignorePastCheck,
                out bookingId,
                out error);
        }

        public bool CreateBookingWithDetails(
            string customerId,
            string courtId,
            IReadOnlyList<string> slotIds,
            DateTime usageDate,
            string bookingTypeVN,
            List<(string serviceId, int quantity, decimal price)> selectedServices,
            bool ignorePastCheck,
            out string bookingId,
            out string error)
        {
            bookingId = null;
            error = null;

            if (string.IsNullOrWhiteSpace(courtId) ||
                slotIds == null ||
                slotIds.Count == 0 ||
                slotIds.Any(s => string.IsNullOrWhiteSpace(s)))
            {
                error = "Thiếu thông tin bắt buộc.";
                return false;
            }

            if (usageDate.Date < DateTime.Today)
            {
                error = "Không thể đặt sân trong quá khứ.";
                return false;
            }

            string stamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            bookingId = "PD" + stamp; // Max 19 chars

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        var cleanSlotIds = slotIds
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        if (cleanSlotIds.Count == 0)
                        {
                            error = "Thiếu thông tin ca giờ.";
                            tran.Rollback();
                            bookingId = null;
                            return false;
                        }

                        string firstSlotId = cleanSlotIds[0];

                        // Prevent booking past slots on the current day (based on start time of the first slot).
                        TimeSpan slotStartTime = TimeSpan.Zero;
                        using (SqlCommand cmd = new SqlCommand("SELECT GioBatDau FROM CA_GIO WHERE MaCa = @id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", firstSlotId);
                            object val = cmd.ExecuteScalar();
                            if (val != null && val != DBNull.Value)
                                slotStartTime = (TimeSpan)val;
                            else
                            {
                                error = "Ca giờ không tồn tại.";
                                tran.Rollback();
                                bookingId = null;
                                return false;
                            }
                        }

                        if (!ignorePastCheck && usageDate.Date == DateTime.Today && slotStartTime < DateTime.Now.TimeOfDay)
                        {
                            error = "Không thể đặt ca đã qua trong ngày hôm nay.";
                            tran.Rollback();
                            bookingId = null;
                            return false;
                        }

                        // Court maintenance check in the same transaction.
                        using (SqlCommand cmd = new SqlCommand("SELECT TrangThai FROM SAN WHERE MaSan = @id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", courtId.Trim());
                            string status = (cmd.ExecuteScalar() ?? string.Empty)?.ToString()?.Trim() ?? string.Empty;
                            if (string.Equals(status, "Bảo trì", StringComparison.OrdinalIgnoreCase))
                            {
                                error = "Sân đang bảo trì, không thể đặt.";
                                tran.Rollback();
                                bookingId = null;
                                return false;
                            }
                        }

                        // Availability check (batch) in the same transaction.
                        using (SqlCommand checkCmd = new SqlCommand())
                        {
                            checkCmd.Connection = conn;
                            checkCmd.Transaction = tran;

                            string inClause = BuildInClause(checkCmd, "@ca", cleanSlotIds);
                            checkCmd.CommandText = $@"
                                SELECT CT.MaCa
                                FROM CT_DAT_SAN CT
                                WHERE CT.MaSan = @san AND CT.NgaySuDung = @date
                                  AND CT.TrangThaiHieuLuc = 1
                                  AND CT.MaCa IN ({inClause})";

                            checkCmd.Parameters.AddWithValue("@san", courtId.Trim());
                            checkCmd.Parameters.AddWithValue("@date", usageDate.Date);

                            var busy = new List<string>();
                            using (SqlDataReader reader = checkCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var s = reader[0]?.ToString()?.Trim();
                                    if (!string.IsNullOrWhiteSpace(s)) busy.Add(s);
                                }
                            }

                            if (busy.Count > 0)
                            {
                                error = "Sân đã có lịch ở ca: " + string.Join(", ", busy);
                                tran.Rollback();
                                bookingId = null;
                                return false;
                            }
                        }

                        // Price is stored as court fee only (services are charged at checkout).
                        var slotPrices = new List<(string slotId, decimal price)>();
                        decimal totalCourtPrice = 0;

                        using (SqlCommand priceCmd = new SqlCommand())
                        {
                            priceCmd.Connection = conn;
                            priceCmd.Transaction = tran;

                            string inClause = BuildInClause(priceCmd, "@pca", cleanSlotIds);
                            priceCmd.CommandText = $@"
                                SELECT BG.MaCa, BG.Gia
                                FROM BANG_GIA BG
                                INNER JOIN SAN S ON S.MaLoaiSan = BG.MaLoaiSan
                                WHERE S.MaSan = @san AND BG.LoaiDat = @type AND BG.MaCa IN ({inClause})";

                            priceCmd.Parameters.AddWithValue("@san", courtId.Trim());
                            priceCmd.Parameters.AddWithValue("@type", bookingTypeVN?.Trim() ?? string.Empty);

                            var map = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                            using (SqlDataReader reader = priceCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string ca = reader["MaCa"]?.ToString()?.Trim();
                                    if (string.IsNullOrWhiteSpace(ca)) continue;
                                    decimal gia = reader["Gia"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Gia"]);
                                    map[ca] = gia;
                                }
                            }

                            foreach (var ca in cleanSlotIds)
                            {
                                decimal price = map.TryGetValue(ca, out var v) ? v : 0;
                                slotPrices.Add((ca, price));
                                totalCourtPrice += price;
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO DAT_SAN (MaPhieuDat, MaKH, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
                            VALUES (@id, @kh, GETDATE(), @type, N'Chờ', @amount)", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", bookingId);
                            cmd.Parameters.AddWithValue("@kh", string.IsNullOrWhiteSpace(customerId) ? (object)DBNull.Value : customerId.Trim());
                            cmd.Parameters.AddWithValue("@type", bookingTypeVN);
                            cmd.Parameters.AddWithValue("@amount", totalCourtPrice);
                            cmd.ExecuteNonQuery();
                        }

                        for (int i = 0; i < slotPrices.Count; i++)
                        {
                            // CT_DAT_SAN.MaCTDS varchar(22) => keep IDs short.
                            // Pattern: CT + stamp (17) + 2-digit index => 21 chars
                            string detailId = "CT" + stamp + (i + 1).ToString("00");

                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru)
                                VALUES (@ct, @id, @san, @ca, @date, @price)", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@ct", detailId);
                                cmd.Parameters.AddWithValue("@id", bookingId);
                                cmd.Parameters.AddWithValue("@san", courtId.Trim());
                                cmd.Parameters.AddWithValue("@ca", slotPrices[i].slotId);
                                cmd.Parameters.AddWithValue("@date", usageDate.Date);
                                cmd.Parameters.AddWithValue("@price", slotPrices[i].price);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (selectedServices != null)
                        {
                            foreach (var svc in selectedServices)
                            {
                                // CT_DICH_VU.MaCTDV is typically short (often varchar(20)). Keep IDs <= 20 chars.
                                // Pattern: DV + last 14 of bookingId + 4 random hex => 20 chars
                                string last14 = bookingId.Length <= 14 ? bookingId : bookingId.Substring(bookingId.Length - 14);
                                string ctdvId = "DV" + last14 + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
                                using (SqlCommand cmd = new SqlCommand(@"
                                    INSERT INTO CT_DICH_VU (MaCTDV, MaPhieuDat, MaDV, SoLuong, DonGia)
                                    VALUES (@ct, @id, @dv, @sl, @p)", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@ct", ctdvId);
                                    cmd.Parameters.AddWithValue("@id", bookingId);
                                    cmd.Parameters.AddWithValue("@dv", svc.serviceId);
                                    cmd.Parameters.AddWithValue("@sl", svc.quantity);
                                    cmd.Parameters.AddWithValue("@p", svc.price);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        tran.Commit();
                        return true;
                    }
                    catch (SqlException ex)
                    {
                        tran.Rollback();
                        error = "Không thể đặt sân: " + ex.Message;
                        bookingId = null;
                        return false;
                    }
                }
            }
        }

        public bool IsCourtSlotFree(string courtId, string slotId, DateTime usageDate)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM CT_DAT_SAN
                WHERE MaSan = @san AND MaCa = @ca AND NgaySuDung = @date
                  AND TrangThaiHieuLuc = 1";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@san", courtId);
                cmd.Parameters.AddWithValue("@ca", slotId);
                cmd.Parameters.AddWithValue("@date", usageDate.Date);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count == 0;
            }
        }

        public decimal GetPriceForCourtSlot(string courtId, string slotId, string bookingType)
        {
            string query = @"
                SELECT TOP 1 BG.Gia
                FROM BANG_GIA BG
                INNER JOIN SAN S ON S.MaLoaiSan = BG.MaLoaiSan
                WHERE S.MaSan = @san AND BG.MaCa = @ca AND BG.LoaiDat = @type";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@san", courtId);
                cmd.Parameters.AddWithValue("@ca", slotId);
                cmd.Parameters.AddWithValue("@type", bookingType);
                object val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? 0 : Convert.ToDecimal(val);
            }
        }

        public decimal GetTotalPriceForCourtSlots(string courtId, IReadOnlyList<string> slotIds, string bookingType)
        {
            if (string.IsNullOrWhiteSpace(courtId) || slotIds == null || slotIds.Count == 0 || string.IsNullOrWhiteSpace(bookingType))
                return 0;

            var cleanSlotIds = slotIds
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleanSlotIds.Count == 0) return 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                string inClause = BuildInClause(cmd, "@ca", cleanSlotIds);

                cmd.CommandText = $@"
                    SELECT ISNULL(SUM(BG.Gia), 0)
                    FROM BANG_GIA BG
                    INNER JOIN SAN S ON S.MaLoaiSan = BG.MaLoaiSan
                    WHERE S.MaSan = @san AND BG.LoaiDat = @type AND BG.MaCa IN ({inClause})";

                cmd.Parameters.AddWithValue("@san", courtId.Trim());
                cmd.Parameters.AddWithValue("@type", bookingType.Trim());

                conn.Open();
                object val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? 0 : Convert.ToDecimal(val);
            }
        }

        public List<string> GetBusyCourtSlots(string courtId, IReadOnlyList<string> slotIds, DateTime usageDate)
        {
            var busy = new List<string>();

            if (string.IsNullOrWhiteSpace(courtId) || slotIds == null || slotIds.Count == 0)
                return busy;

            var cleanSlotIds = slotIds
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleanSlotIds.Count == 0) return busy;

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                string inClause = BuildInClause(cmd, "@ca", cleanSlotIds);

                cmd.CommandText = $@"
                    SELECT CT.MaCa
                    FROM CT_DAT_SAN CT
                    WHERE CT.MaSan = @san AND CT.NgaySuDung = @date
                      AND CT.TrangThaiHieuLuc = 1
                      AND CT.MaCa IN ({inClause})";

                cmd.Parameters.AddWithValue("@san", courtId.Trim());
                cmd.Parameters.AddWithValue("@date", usageDate.Date);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var s = reader[0]?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(s)) busy.Add(s);
                    }
                }
            }

            return busy;
        }

        private static string BuildInClause(SqlCommand cmd, string paramPrefix, IReadOnlyList<string> values)
        {
            // Returns: @ca0,@ca1,... and adds parameters to cmd.
            var names = new List<string>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                string name = paramPrefix + i.ToString();
                names.Add(name);
                cmd.Parameters.AddWithValue(name, values[i]);
            }
            return string.Join(", ", names);
        }

        public bool IsCourtUnderMaintenance(string courtId)
        {
            string query = "SELECT TrangThai FROM SAN WHERE MaSan = @san";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@san", courtId);
                object val = cmd.ExecuteScalar();
                string status = val?.ToString()?.Trim();
                return string.Equals(status, "Bảo trì", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string MapBookingStatus(string? statusVN)
        {
            return (statusVN?.Trim()) switch
            {
                "Chờ" => "Pending",
                "Nhận sân" => "Checked-in",
                "Hoàn tất" => "Completed",
                "Hủy" => "Cancelled",
                _ => "Pending"
            };
        }

        public bool UpdateBookingStatus(string bookingId, string statusEn, out string error)
        {
            error = null;
            string statusVN = statusEn switch
            {
                "Checked-in" => "Nhận sân",
                "Completed" => "Hoàn tất",
                "Cancelled" => "Hủy",
                _ => "Chờ"
            };

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction trans = conn.BeginTransaction())
                    {
                        try
                        {
                            string queryDS = "UPDATE DAT_SAN SET TrangThai = @status WHERE MaPhieuDat = @id";
                            using (SqlCommand cmd = new SqlCommand(queryDS, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@status", statusVN);
                                cmd.Parameters.AddWithValue("@id", bookingId);
                                cmd.ExecuteNonQuery();
                            }

                            // If cancelled, also update details to release the slot constraint
                            if (statusEn == "Cancelled")
                            {
                                string queryCT = "UPDATE CT_DAT_SAN SET TrangThaiHieuLuc = 0 WHERE MaPhieuDat = @id";
                                using (SqlCommand cmd = new SqlCommand(queryCT, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@id", bookingId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            trans.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            trans.Rollback();
                            error = ex.Message;
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public Booking GetBookingById(string id)
        {
            string query = @"
                SELECT
                    DS.MaPhieuDat AS Id,
                    DS.MaKH AS CustomerId,
                    KH.HoTen AS Customer,
                    KH.SDT AS Phone,
                    S.MaSan AS CourtId,
                    S.TenSan AS Court,
                    MIN(CT.NgaySuDung) AS [Date],
                    (CAST(MIN(CG.GioBatDau) AS varchar(5)) + ' - ' + CAST(MAX(CG.GioKetThuc) AS varchar(5))) AS TimeString,
                    DS.LoaiDat AS [Type],
                    DS.TrangThai AS [Status],
                    ISNULL(DS.TongTien, 0) AS Amount
                FROM DAT_SAN DS
                INNER JOIN KHACH_HANG KH ON DS.MaKH = KH.MaKH
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                INNER JOIN SAN S ON CT.MaSan = S.MaSan
                INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                WHERE DS.MaPhieuDat = @id
                GROUP BY DS.MaPhieuDat, DS.MaKH, KH.HoTen, KH.SDT, S.MaSan, S.TenSan, DS.LoaiDat, DS.TrangThai, DS.TongTien";

            DataTable dt = ExecuteQuery(query, new object[] { id });
            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new Booking
            {
                Id = row["Id"].ToString(),
                CustomerId = row["CustomerId"].ToString(),
                Customer = row["Customer"].ToString(),
                Phone = row["Phone"].ToString(),
                Court = row["Court"].ToString(),
                CourtId = row["CourtId"].ToString(),
                Date = row["Date"] != DBNull.Value ? (DateTime)row["Date"] : DateTime.Now,
                Time = row["TimeString"].ToString(),
                Type = MapBookingType(row["Type"]?.ToString()),
                Status = MapBookingStatus(row["Status"]?.ToString()),
                Amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : 0
            };
        }

        public bool CheckOutWithTransaction(Invoice inv, string courtId, out string error)
        {
            error = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Check if invoice already exists for this booking
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM HOA_DON WHERE MaPhieuDat = @pd", conn, tran))
                        {
                            checkCmd.Parameters.AddWithValue("@pd", inv.BookingId);
                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                            {
                                error = "Lịch đặt này đã được thanh toán trước đó.";
                                return false;
                            }
                        }

                        // 2. Insert Invoice
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
                            VALUES (@id, @pd, @ts, @tdv, @giam, GETDATE(), @method)", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", inv.Id);
                            cmd.Parameters.AddWithValue("@pd", inv.BookingId);
                            cmd.Parameters.AddWithValue("@ts", inv.CourtFee);
                            cmd.Parameters.AddWithValue("@tdv", inv.ServiceFee);
                            cmd.Parameters.AddWithValue("@giam", inv.Discount);
                            cmd.Parameters.AddWithValue("@method", inv.PaymentMethod ?? "Tiền mặt");
                            cmd.ExecuteNonQuery();
                        }

                        // 3. Update Booking Status
                        using (SqlCommand cmd = new SqlCommand("UPDATE DAT_SAN SET TrangThai = N'Hoàn tất' WHERE MaPhieuDat = @id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", inv.BookingId);
                            cmd.ExecuteNonQuery();
                        }

                        // 4. Update Court Status
                        using (SqlCommand cmd = new SqlCommand("UPDATE SAN SET TrangThai = N'Sẵn sàng' WHERE MaSan = @id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", courtId);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        error = ex.Message;
                        return false;
                    }
                }
            }
        }

        public bool InsertInvoice(Invoice inv, out string error)
        {
            error = null;
            try
            {
                string query = @"
                    INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
                    VALUES (@id, @pd, @ts, @tdv, @giam, GETDATE(), @method)";

                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", inv.Id);
                    cmd.Parameters.AddWithValue("@pd", inv.BookingId);
                    cmd.Parameters.AddWithValue("@ts", inv.CourtFee);
                    cmd.Parameters.AddWithValue("@tdv", inv.ServiceFee);
                    cmd.Parameters.AddWithValue("@giam", inv.Discount);
                    cmd.Parameters.AddWithValue("@method", inv.PaymentMethod ?? "Tiền mặt");
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string MapBookingType(string? typeVN)
        {
            return (typeVN?.Trim()) switch
            {
                "Lẻ" => "Casual",
                "Cố định" => "Fixed",
                _ => "Casual"
            };
        }

        public Booking GetActiveBookingByCourt(string courtId)
        {
            string query = @"
                SELECT TOP 1
                    DS.MaPhieuDat AS Id
                FROM DAT_SAN DS
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                WHERE CT.MaSan = @san 
                  AND DS.TrangThai = N'Nhận sân'";

            DataTable dt = ExecuteQuery(query, new object[] { courtId });
            if (dt.Rows.Count == 0) return null;

            return GetBookingById(dt.Rows[0]["Id"].ToString());
        }

        public ObservableCollection<Booking> GetActiveBookings()
        {
            ObservableCollection<Booking> list = new ObservableCollection<Booking>();
            string query = @"
                SELECT
                    DS.MaPhieuDat AS Id,
                    KH.HoTen AS Customer,
                    KH.SDT AS Phone,
                    S.TenSan AS Court,
                    MIN(CT.NgaySuDung) AS [Date],
                    (CAST(MIN(CG.GioBatDau) AS varchar(5)) + ' - ' + CAST(MAX(CG.GioKetThuc) AS varchar(5))) AS TimeString,
                    DS.LoaiDat AS [Type],
                    DS.TrangThai AS [Status],
                    ISNULL(DS.TongTien, 0) AS Amount
                FROM DAT_SAN DS
                INNER JOIN KHACH_HANG KH ON DS.MaKH = KH.MaKH
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                INNER JOIN SAN S ON CT.MaSan = S.MaSan
                INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                WHERE DS.TrangThai = N'Nhận sân'
                GROUP BY DS.MaPhieuDat, KH.HoTen, KH.SDT, S.TenSan, DS.LoaiDat, DS.TrangThai, DS.TongTien
                ORDER BY S.TenSan";

            DataTable dt = ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Booking
                {
                    Id = row["Id"].ToString(),
                    Customer = row["Customer"].ToString(),
                    Phone = row["Phone"].ToString(),
                    Court = row["Court"].ToString(),
                    Date = row["Date"] != DBNull.Value ? (DateTime)row["Date"] : DateTime.Now,
                    Time = row["TimeString"].ToString(),
                    Type = MapBookingType(row["Type"]?.ToString()),
                    Status = MapBookingStatus(row["Status"]?.ToString()),
                    Amount = row["Amount"] != DBNull.Value ? Convert.ToDecimal(row["Amount"]) : 0
                });
            }
            return list;
        }

        // --- ADMIN CRUD FOR TIME SLOTS ---


        public bool AddTimeSlot(TimeSlot slot, out string error)
        {
            error = null;
            try
            {
                string query = "INSERT INTO CA_GIO (MaCa, TenCa, GioBatDau, GioKetThuc, LaKhungGioVang) VALUES (@id, @name, @start, @end, @v)";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", slot.Id);
                    cmd.Parameters.AddWithValue("@name", slot.Name);
                    cmd.Parameters.AddWithValue("@start", slot.StartTime);
                    cmd.Parameters.AddWithValue("@end", slot.EndTime);
                    cmd.Parameters.AddWithValue("@v", slot.LaKhungGioVang ? 1 : 0);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // duplicate key
            {
                error = "Khoảng thời gian này đã tồn tại, vui lòng chọn khung giờ khác!";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool UpdateTimeSlot(TimeSlot slot, out string error)
        {
            error = null;
            try
            {
                string query = "UPDATE CA_GIO SET TenCa = @name, GioBatDau = @start, GioKetThuc = @end, LaKhungGioVang = @v WHERE MaCa = @id";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", slot.Id);
                    cmd.Parameters.AddWithValue("@name", slot.Name);
                    cmd.Parameters.AddWithValue("@start", slot.StartTime);
                    cmd.Parameters.AddWithValue("@end", slot.EndTime);
                    cmd.Parameters.AddWithValue("@v", slot.LaKhungGioVang ? 1 : 0);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // duplicate key
            {
                error = "Khoảng thời gian này đã tồn tại, vui lòng chọn khung giờ khác!";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool DeleteTimeSlot(string id, out string error)
        {
            error = null;
            try
            {
                string query = "DELETE FROM CA_GIO WHERE MaCa = @id";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = "Có thể ca này đang được sử dụng trong bảng giá hoặc lịch đặt. " + ex.Message;
                return false;
            }
        }

        // --- ADMIN CRUD FOR PRICING ---

        public ObservableCollection<PriceEntry> GetAllPriceEntries()
        {
            ObservableCollection<PriceEntry> list = new ObservableCollection<PriceEntry>();
            string query = @"
                SELECT 
                    BG.MaGia, 
                    BG.MaLoaiSan, 
                    LS.TenLoai, 
                    BG.MaCa, 
                    CG.TenCa, 
                    BG.LoaiDat, 
                    BG.Gia
                FROM BANG_GIA BG
                INNER JOIN LOAI_SAN LS ON LS.MaLoaiSan = BG.MaLoaiSan
                INNER JOIN CA_GIO CG ON CG.MaCa = BG.MaCa
                ORDER BY LS.TenLoai, CG.GioBatDau";

            DataTable dt = ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new PriceEntry
                {
                    Id = row["MaGia"].ToString()?.Trim(),
                    CourtTypeId = row["MaLoaiSan"].ToString()?.Trim(),
                    CourtTypeName = row["TenLoai"].ToString()?.Trim(),
                    SlotId = row["MaCa"].ToString()?.Trim(),
                    SlotName = row["TenCa"].ToString()?.Trim(),
                    BookingType = row["LoaiDat"].ToString()?.Trim(),
                    Price = Convert.ToDecimal(row["Gia"])
                });
            }
            return list;
        }

        public bool AddPriceEntry(PriceEntry entry, out string error)
        {
            error = null;
            try
            {
                string id = "T" + DateTime.Now.ToString("fffssmm"); // Simple unique id
                string query = "INSERT INTO BANG_GIA (MaGia, MaLoaiSan, MaCa, LoaiDat, Gia) VALUES (@id, @typeid, @slotid, @type, @price)";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@typeid", entry.CourtTypeId);
                    cmd.Parameters.AddWithValue("@slotid", entry.SlotId);
                    cmd.Parameters.AddWithValue("@type", entry.BookingType);
                    cmd.Parameters.AddWithValue("@price", entry.Price);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool UpdatePriceEntry(PriceEntry entry, out string error)
        {
            error = null;
            try
            {
                string query = "UPDATE BANG_GIA SET MaLoaiSan = @typeid, MaCa = @slotid, LoaiDat = @type, Gia = @price WHERE MaGia = @id";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", entry.Id);
                    cmd.Parameters.AddWithValue("@typeid", entry.CourtTypeId);
                    cmd.Parameters.AddWithValue("@slotid", entry.SlotId);
                    cmd.Parameters.AddWithValue("@type", entry.BookingType);
                    cmd.Parameters.AddWithValue("@price", entry.Price);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool DeletePriceEntry(string id, out string error)
        {
            error = null;
            try
            {
                string query = "DELETE FROM BANG_GIA WHERE MaGia = @id";
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public bool UpsertTimeSlotsAndPrices(
            IReadOnlyList<TimeSlot> slots,
            IReadOnlyList<PriceEntry> prices,
            bool overwriteExisting,
            out int slotsInserted,
            out int slotsUpdated,
            out int pricesInserted,
            out int pricesUpdated,
            out string error)
        {
            slotsInserted = 0;
            slotsUpdated = 0;
            pricesInserted = 0;
            pricesUpdated = 0;
            error = null;

            if (slots == null || slots.Count == 0)
            {
                error = "Danh sách ca giờ trống.";
                return false;
            }

            if (prices == null) prices = Array.Empty<PriceEntry>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1) Upsert CA_GIO
                            foreach (var slot in slots)
                            {
                                if (slot == null || string.IsNullOrWhiteSpace(slot.Id)) continue;

                                using (SqlCommand existsCmd = new SqlCommand("SELECT COUNT(*) FROM CA_GIO WHERE MaCa = @id", conn, tran))
                                {
                                    existsCmd.Parameters.AddWithValue("@id", slot.Id.Trim());
                                    int exists = Convert.ToInt32(existsCmd.ExecuteScalar() ?? 0);

                                    if (exists > 0)
                                    {
                                        if (!overwriteExisting) continue;

                                        using (SqlCommand updateCmd = new SqlCommand(@"
                                            UPDATE CA_GIO
                                            SET TenCa = @name, GioBatDau = @start, GioKetThuc = @end, LaKhungGioVang = @v
                                            WHERE MaCa = @id", conn, tran))
                                        {
                                            updateCmd.Parameters.AddWithValue("@id", slot.Id.Trim());
                                            updateCmd.Parameters.AddWithValue("@name", slot.Name ?? string.Empty);
                                            updateCmd.Parameters.AddWithValue("@start", slot.StartTime);
                                            updateCmd.Parameters.AddWithValue("@end", slot.EndTime);
                                            updateCmd.Parameters.AddWithValue("@v", slot.LaKhungGioVang ? 1 : 0);
                                            updateCmd.ExecuteNonQuery();
                                            slotsUpdated++;
                                        }
                                    }
                                    else
                                    {
                                        using (SqlCommand insertCmd = new SqlCommand(@"
                                            INSERT INTO CA_GIO (MaCa, TenCa, GioBatDau, GioKetThuc, LaKhungGioVang)
                                            VALUES (@id, @name, @start, @end, @v)", conn, tran))
                                        {
                                            insertCmd.Parameters.AddWithValue("@id", slot.Id.Trim());
                                            insertCmd.Parameters.AddWithValue("@name", slot.Name ?? string.Empty);
                                            insertCmd.Parameters.AddWithValue("@start", slot.StartTime);
                                            insertCmd.Parameters.AddWithValue("@end", slot.EndTime);
                                            insertCmd.Parameters.AddWithValue("@v", slot.LaKhungGioVang ? 1 : 0);
                                            insertCmd.ExecuteNonQuery();
                                            slotsInserted++;
                                        }
                                    }
                                }
                            }

                            // 2) Upsert BANG_GIA by unique rule (MaLoaiSan, MaCa, LoaiDat)
                            string stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                            int seq = 1;

                            foreach (var p in prices)
                            {
                                if (p == null) continue;
                                if (string.IsNullOrWhiteSpace(p.CourtTypeId) || string.IsNullOrWhiteSpace(p.SlotId) || string.IsNullOrWhiteSpace(p.BookingType))
                                    continue;

                                string courtTypeId = p.CourtTypeId.Trim();
                                string slotId = p.SlotId.Trim();
                                string bookingType = p.BookingType.Trim();

                                string existingId = null;
                                using (SqlCommand findCmd = new SqlCommand(@"
                                    SELECT MaGia
                                    FROM BANG_GIA
                                    WHERE MaLoaiSan = @typeId AND MaCa = @slotId AND LoaiDat = @type", conn, tran))
                                {
                                    findCmd.Parameters.AddWithValue("@typeId", courtTypeId);
                                    findCmd.Parameters.AddWithValue("@slotId", slotId);
                                    findCmd.Parameters.AddWithValue("@type", bookingType);
                                    var val = findCmd.ExecuteScalar();
                                    existingId = val == null || val == DBNull.Value ? null : val.ToString()?.Trim();
                                }

                                if (!string.IsNullOrWhiteSpace(existingId))
                                {
                                    if (!overwriteExisting) continue;

                                    using (SqlCommand updateCmd = new SqlCommand(@"
                                        UPDATE BANG_GIA
                                        SET Gia = @price
                                        WHERE MaGia = @id", conn, tran))
                                    {
                                        updateCmd.Parameters.AddWithValue("@id", existingId);
                                        updateCmd.Parameters.AddWithValue("@price", p.Price);
                                        updateCmd.ExecuteNonQuery();
                                        pricesUpdated++;
                                    }

                                    continue;
                                }

                                string newId = "G" + stamp + seq.ToString("D5");
                                seq++;

                                using (SqlCommand insertCmd = new SqlCommand(@"
                                    INSERT INTO BANG_GIA (MaGia, MaLoaiSan, MaCa, LoaiDat, Gia)
                                    VALUES (@id, @typeId, @slotId, @type, @price)", conn, tran))
                                {
                                    insertCmd.Parameters.AddWithValue("@id", newId);
                                    insertCmd.Parameters.AddWithValue("@typeId", courtTypeId);
                                    insertCmd.Parameters.AddWithValue("@slotId", slotId);
                                    insertCmd.Parameters.AddWithValue("@type", bookingType);
                                    insertCmd.Parameters.AddWithValue("@price", p.Price);
                                    insertCmd.ExecuteNonQuery();
                                    pricesInserted++;
                                }
                            }

                            tran.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            try { tran.Rollback(); } catch { }
                            error = ex.Message;
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
        public (decimal dailyRevenue, int activeBookings, decimal monthlyGrowth) GetBookingStats()
        {
            decimal dailyRevenue = 0;
            int activeBookings = 0;
            decimal monthlyGrowth = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                
                // 1. Daily Revenue (From Invoices)
                string dailyRevQuery = "SELECT ISNULL(SUM(TongTienSan + TongTienDV - SoTienGiam), 0) FROM HOA_DON WHERE CAST(NgayXuat AS DATE) = CAST(GETDATE() AS DATE)";
                using (SqlCommand cmd = new SqlCommand(dailyRevQuery, conn))
                {
                    dailyRevenue = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // 2. Active Bookings
                string activeQuery = "SELECT COUNT(*) FROM DAT_SAN WHERE TrangThai IN (N'Chờ', N'Nhận sân')";
                using (SqlCommand cmd = new SqlCommand(activeQuery, conn))
                {
                    activeBookings = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 3. Monthly Growth (This month vs Previous month from Invoices)
                string currentMonthRevQuery = "SELECT ISNULL(SUM(TongTienSan + TongTienDV - SoTienGiam), 0) FROM HOA_DON WHERE MONTH(NgayXuat) = MONTH(GETDATE()) AND YEAR(NgayXuat) = YEAR(GETDATE())";
                string lastMonthRevQuery = "SELECT ISNULL(SUM(TongTienSan + TongTienDV - SoTienGiam), 0) FROM HOA_DON WHERE MONTH(NgayXuat) = MONTH(DATEADD(month, -1, GETDATE())) AND YEAR(NgayXuat) = YEAR(DATEADD(month, -1, GETDATE()))";
                
                decimal currentMonth = 0;
                decimal lastMonth = 0;

                using (SqlCommand cmd = new SqlCommand(currentMonthRevQuery, conn))
                {
                    currentMonth = Convert.ToDecimal(cmd.ExecuteScalar());
                }
                using (SqlCommand cmd = new SqlCommand(lastMonthRevQuery, conn))
                {
                    lastMonth = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                if (lastMonth > 0)
                {
                    monthlyGrowth = ((currentMonth - lastMonth) / lastMonth) * 100;
                }
                else if (currentMonth > 0)
                {
                    monthlyGrowth = 100;
                }
            }

            return (dailyRevenue, activeBookings, monthlyGrowth);
        }
        public void AutoCompleteOverdueBookings()
        {
            try
            {
                // 1) Auto-complete: bookings that are 'Checked-in' but the time slot has ended.
                // 2) Auto-cancel no-show: bookings that are still 'Pending' after a grace window from start time.
                const int NoShowGraceMinutes = 15;

                string completeQuery = @"
                    SELECT CT.MaPhieuDat, CT.MaSan
                    FROM CT_DAT_SAN CT
                    INNER JOIN DAT_SAN DS ON CT.MaPhieuDat = DS.MaPhieuDat
                    INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                    WHERE DS.TrangThai = N'Nhận sân'
                      AND CT.TrangThaiHieuLuc = 1
                    GROUP BY CT.MaPhieuDat, CT.MaSan
                    HAVING MAX(CAST(CT.NgaySuDung AS DATETIME) + CAST(CG.GioKetThuc AS DATETIME)) < GETDATE()";

                string cancelNoShowQuery = $@"
                    SELECT CT.MaPhieuDat
                    FROM CT_DAT_SAN CT
                    INNER JOIN DAT_SAN DS ON CT.MaPhieuDat = DS.MaPhieuDat
                    INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                    WHERE DS.TrangThai = N'Chờ'
                      AND CT.TrangThaiHieuLuc = 1
                    GROUP BY CT.MaPhieuDat
                    HAVING MIN(CAST(CT.NgaySuDung AS DATETIME) + CAST(CG.GioBatDau AS DATETIME)) < DATEADD(minute, -{NoShowGraceMinutes}, GETDATE())";

                DataTable toComplete = ExecuteQuery(completeQuery);
                DataTable toCancel = ExecuteQuery(cancelNoShowQuery);
                if (toComplete.Rows.Count == 0 && toCancel.Rows.Count == 0) return;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (DataRow row in toComplete.Rows)
                            {
                                string bookingId = row["MaPhieuDat"].ToString();
                                string courtId = row["MaSan"].ToString();

                                // Update Booking to Completed
                                using (SqlCommand cmd = new SqlCommand("UPDATE DAT_SAN SET TrangThai = N'Hoàn tất' WHERE MaPhieuDat = @id", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@id", bookingId);
                                    cmd.ExecuteNonQuery();
                                }

                                // Update Court to Available
                                using (SqlCommand cmd = new SqlCommand("UPDATE SAN SET TrangThai = N'Sẵn sàng' WHERE MaSan = @id", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@id", courtId);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            foreach (DataRow row in toCancel.Rows)
                            {
                                string bookingId = row["MaPhieuDat"].ToString();

                                // Auto-cancel no-show (Pending -> Cancelled) and release slot constraint.
                                using (SqlCommand cmd = new SqlCommand("UPDATE DAT_SAN SET TrangThai = N'Hủy' WHERE MaPhieuDat = @id AND TrangThai = N'Chờ'", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@id", bookingId);
                                    cmd.ExecuteNonQuery();
                                }

                                using (SqlCommand cmd = new SqlCommand("UPDATE CT_DAT_SAN SET TrangThaiHieuLuc = 0 WHERE MaPhieuDat = @id", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("@id", bookingId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle error silently as this runs in background
                System.Diagnostics.Debug.WriteLine("AutoComplete Error: " + ex.Message);
            }
        }
    }
}
