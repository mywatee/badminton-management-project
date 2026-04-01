using Microsoft.Data.SqlClient;
using QuanLySCL.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;

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
                    CT.NgaySuDung AS [Date],
                    (CAST(CG.GioBatDau AS varchar(5)) + ' - ' + CAST(CG.GioKetThuc AS varchar(5))) AS TimeString,
                    DS.LoaiDat AS [Type],
                    DS.TrangThai AS [Status],
                    ISNULL(DS.TongTien, 0) AS Amount
                FROM DAT_SAN DS
                INNER JOIN KHACH_HANG KH ON DS.MaKH = KH.MaKH
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                INNER JOIN SAN S ON CT.MaSan = S.MaSan
                INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                WHERE DS.MaKH = @makh
                ORDER BY CT.NgaySuDung DESC, CG.GioBatDau DESC, S.TenSan";

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
                    CT.NgaySuDung AS [Date],
                    (CAST(CG.GioBatDau AS varchar(5)) + ' - ' + CAST(CG.GioKetThuc AS varchar(5))) AS TimeString,
                    DS.LoaiDat AS [Type],
                    DS.TrangThai AS [Status],
                    ISNULL(DS.TongTien, 0) AS Amount
                FROM DAT_SAN DS
                INNER JOIN KHACH_HANG KH ON DS.MaKH = KH.MaKH
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                INNER JOIN SAN S ON CT.MaSan = S.MaSan
                INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                ORDER BY CT.NgaySuDung DESC, CG.GioBatDau DESC, S.TenSan";

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
            out string bookingId,
            out string error)
        {
            bookingId = null;
            error = null;

            if (string.IsNullOrWhiteSpace(customerId) ||
                string.IsNullOrWhiteSpace(courtId) ||
                string.IsNullOrWhiteSpace(slotId))
            {
                error = "Thiếu thông tin bắt buộc.";
                return false;
            }

            if (usageDate.Date < DateTime.Today)
            {
                error = "Không thể đặt sân trong quá khứ.";
                return false;
            }

            // New Validation: Prevent booking past slots on the current day
            TimeSpan slotStartTime = TimeSpan.Zero;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT GioBatDau FROM CA_GIO WHERE MaCa = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", slotId);
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value)
                        slotStartTime = (TimeSpan)val;
                    else
                    {
                        error = "Ca giờ không tồn tại.";
                        return false;
                    }
                }
            }

            if (usageDate.Date == DateTime.Today && slotStartTime < DateTime.Now.TimeOfDay)
            {
                error = "Không thể đặt ca đã qua trong ngày hôm nay.";
                return false;
            }

            if (IsCourtUnderMaintenance(courtId))
            {
                error = "Sân đang bảo trì, không thể đặt.";
                return false;
            }

            if (!IsCourtSlotFree(courtId, slotId, usageDate))
            {
                error = "Sân đã có lịch ở ca này.";
                return false;
            }

            decimal price = GetPriceForCourtSlot(courtId, slotId, bookingTypeVN);
            bookingId = "PD" + DateTime.Now.ToString("yyyyMMddHHmmssfff"); // Max 19 chars
            string detailId = "CT" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "1"; // Max 20 chars, DB limit is 22

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO DAT_SAN (MaPhieuDat, MaKH, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
                            VALUES (@id, @kh, GETDATE(), @type, N'Chờ', @amount)", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@id", bookingId);
                            cmd.Parameters.AddWithValue("@kh", customerId);
                            cmd.Parameters.AddWithValue("@type", bookingTypeVN);
                            cmd.Parameters.AddWithValue("@amount", price);
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru)
                            VALUES (@ct, @id, @san, @ca, @date, @price)", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@ct", detailId);
                            cmd.Parameters.AddWithValue("@id", bookingId);
                            cmd.Parameters.AddWithValue("@san", courtId);
                            cmd.Parameters.AddWithValue("@ca", slotId);
                            cmd.Parameters.AddWithValue("@date", usageDate.Date);
                            cmd.Parameters.AddWithValue("@price", price);
                            cmd.ExecuteNonQuery();
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
                    CT.NgaySuDung AS [Date],
                    (CAST(CG.GioBatDau AS varchar(5)) + ' - ' + CAST(CG.GioKetThuc AS varchar(5))) AS TimeString,
                    DS.LoaiDat AS [Type],
                    DS.TrangThai AS [Status],
                    ISNULL(DS.TongTien, 0) AS Amount
                FROM DAT_SAN DS
                INNER JOIN KHACH_HANG KH ON DS.MaKH = KH.MaKH
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                INNER JOIN SAN S ON CT.MaSan = S.MaSan
                INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                WHERE DS.MaPhieuDat = @id";

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
                    CT.NgaySuDung AS [Date],
                    (CAST(CG.GioBatDau AS varchar(5)) + ' - ' + CAST(CG.GioKetThuc AS varchar(5))) AS TimeString,
                    DS.LoaiDat AS [Type],
                    DS.TrangThai AS [Status],
                    ISNULL(DS.TongTien, 0) AS Amount
                FROM DAT_SAN DS
                INNER JOIN KHACH_HANG KH ON DS.MaKH = KH.MaKH
                INNER JOIN CT_DAT_SAN CT ON DS.MaPhieuDat = CT.MaPhieuDat
                INNER JOIN SAN S ON CT.MaSan = S.MaSan
                INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                WHERE DS.TrangThai = N'Nhận sân'
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
                // Find all bookings that are 'Checked-in' but the time slot has ended.
                // We join with CA_GIO to get the EndTime (GioKetThuc).
                string query = @"
                    SELECT CT.MaPhieuDat, CT.MaSan
                    FROM CT_DAT_SAN CT
                    INNER JOIN DAT_SAN DS ON CT.MaPhieuDat = DS.MaPhieuDat
                    INNER JOIN CA_GIO CG ON CT.MaCa = CG.MaCa
                    WHERE DS.TrangThai = N'Nhận sân'
                      AND (
                          (CAST(CT.NgaySuDung AS DATETIME) + CAST(CG.GioKetThuc AS DATETIME)) < GETDATE()
                      )";

                DataTable dt = ExecuteQuery(query);
                if (dt.Rows.Count == 0) return;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (DataRow row in dt.Rows)
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
