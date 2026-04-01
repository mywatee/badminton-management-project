using Microsoft.Data.SqlClient;
using QuanLySCL.Models;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace QuanLySCL.DAL
{
    public class ServiceDAL : BaseDAL
    {
        public ObservableCollection<Service> GetLowStockServices(int threshold = 5)
        {
            ObservableCollection<Service> list = new ObservableCollection<Service>();
            string query = "SELECT MaDV, TenDV, LoaiDV, DonViTinh, GiaHienHanh, SoLuongTon FROM DICH_VU WHERE SoLuongTon <= @p AND (LoaiDV IS NULL OR LoaiDV <> 'Equipment')";
            DataTable dt = ExecuteQuery(query, new object[] { threshold });
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Service
                {
                    Id = row[0].ToString(),
                    Name = row[1].ToString(),
                    Category = row[2].ToString(),
                    Unit = row[3].ToString(),
                    Price = Convert.ToDecimal(row[4]),
                    Stock = Convert.ToInt32(row[5])
                });
            }
            return list;
        }
        public ObservableCollection<ServiceSaleInvoice> GetServiceSaleInvoices(DateTime? fromDate, DateTime? toDate, string customerId, int limit = 20, int offset = 0)
        {
            string sql = @"
                SELECT
                    HD.MaHD,
                    HD.MaPhieuDat,
                    DS.MaKH,
                    KH.HoTen,
                    HD.TongTienDV,
                    HD.SoTienGiam,
                    HD.NgayXuat,
                    HD.HinhThucThanhToan
                FROM HOA_DON HD
                INNER JOIN DAT_SAN DS ON DS.MaPhieuDat = HD.MaPhieuDat
                LEFT JOIN KHACH_HANG KH ON KH.MaKH = DS.MaKH
                WHERE ISNULL(HD.TongTienDV, 0) > 0
                  AND (@from IS NULL OR HD.NgayXuat >= @from)
                  AND (@to IS NULL OR HD.NgayXuat < DATEADD(day, 1, @to))
                  AND (@kh IS NULL OR DS.MaKH = @kh)
                ORDER BY HD.NgayXuat DESC, HD.MaHD DESC
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY";

            object fromObj = fromDate.HasValue ? fromDate.Value.Date : DBNull.Value;
            object toObj = toDate.HasValue ? toDate.Value.Date : DBNull.Value;
            object khObj = string.IsNullOrWhiteSpace(customerId) ? DBNull.Value : customerId.Trim();
            object offsetObj = offset;
            object limitObj = limit;

            DataTable dt = ExecuteQuery(sql, new object[] { fromObj, toObj, khObj, offsetObj, limitObj });
            var list = new ObservableCollection<ServiceSaleInvoice>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new ServiceSaleInvoice
                {
                    InvoiceId = row["MaHD"]?.ToString(),
                    BookingId = row["MaPhieuDat"]?.ToString(),
                    CustomerId = row["MaKH"] == DBNull.Value ? null : row["MaKH"]?.ToString(),
                    CustomerName = row["HoTen"] == DBNull.Value ? null : row["HoTen"]?.ToString(),
                    ServiceSubtotal = row["TongTienDV"] != DBNull.Value ? Convert.ToDecimal(row["TongTienDV"]) : 0,
                    Discount = row["SoTienGiam"] != DBNull.Value ? Convert.ToDecimal(row["SoTienGiam"]) : 0,
                    IssuedAt = row["NgayXuat"] != DBNull.Value ? Convert.ToDateTime(row["NgayXuat"]) : DateTime.MinValue,
                    PaymentMethod = row["HinhThucThanhToan"]?.ToString()
                });
            }

            return list;
        }

        public ObservableCollection<Service> GetAllServices()
        {
            var list = new ObservableCollection<Service>();
            string query = @"
                SELECT MaDV, TenDV, DonViTinh, ISNULL(GiaHienHanh, 0) AS Gia,
                       LoaiDV,
                       ISNULL(SoLuongTon, 0) AS SoLuongTon
                FROM DICH_VU
                ORDER BY MaDV";

            DataTable dt = ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                string id = row["MaDV"]?.ToString() ?? string.Empty;
                list.Add(new Service
                {
                    Id = id,
                    Name = row["TenDV"]?.ToString() ?? string.Empty,
                    Unit = row["DonViTinh"]?.ToString() ?? string.Empty,
                    Price = row["Gia"] != DBNull.Value ? Convert.ToDecimal(row["Gia"]) : 0,
                    Stock = row["SoLuongTon"] != DBNull.Value ? Convert.ToInt32(row["SoLuongTon"]) : 0,
                    Category = row["LoaiDV"]?.ToString() ?? InferCategory(id)
                });
            }

            return list;
        }

        public string GetNextServiceId(string? category)
        {
            string prefix = string.Equals(category, "Equipment", StringComparison.OrdinalIgnoreCase) ? "E" : "D";
            string query = @"
                SELECT MaDV
                FROM DICH_VU
                WHERE MaDV LIKE @pfx + '%'";

            DataTable dt = ExecuteQuery(query, new object[] { prefix });
            int max = 0;
            foreach (DataRow row in dt.Rows)
            {
                string id = row[0]?.ToString()?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(id) || !id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                string numPart = id.Substring(prefix.Length);
                if (int.TryParse(numPart, out int n))
                    max = Math.Max(max, n);
            }

            return prefix + (max + 1).ToString("000");
        }

        public int CreateService(string id, string category, string name, string? unit, decimal price, int stock)
        {
            string query = @"
                INSERT INTO DICH_VU (MaDV, TenDV, DonViTinh, GiaHienHanh, LoaiDV, SoLuongTon)
                VALUES (@id, @name, @unit, @price, @cat, @stock)";
            return ExecuteNonQuery(query, new object[] { id, name, unit ?? (object)DBNull.Value, price, category, stock });
        }

        public int UpdateService(string id, string category, string name, string? unit, decimal price, int stock)
        {
            string query = @"
                UPDATE DICH_VU
                SET TenDV = @name,
                    DonViTinh = @unit,
                    GiaHienHanh = @price,
                    LoaiDV = @cat,
                    SoLuongTon = @stock
                WHERE MaDV = @id";

            return ExecuteNonQuery(query, new object[] { name, unit ?? (object)DBNull.Value, price, category, stock, id });
        }

        public int DeleteService(string id)
        {
            string query = "DELETE FROM DICH_VU WHERE MaDV = @id";
            return ExecuteNonQuery(query, new object[] { id });
        }

        public int AddServiceToBooking(BookingServiceDetail detail)
        {
            string query = @"
                INSERT INTO CT_DICH_VU (MaCTDV, MaPhieuDat, MaDV, SoLuong, DonGia)
                VALUES (@id, @pd, @dv, @sl, @dg)";

            return ExecuteNonQuery(query, new object[] { detail.Id, detail.BookingId, detail.ServiceId, detail.Quantity, detail.UnitPrice });
        }

        public ObservableCollection<BookingServiceDetail> GetServiceDetailsByBooking(string? bookingId)
        {
            var list = new ObservableCollection<BookingServiceDetail>();
            if (string.IsNullOrWhiteSpace(bookingId)) return list;
            string query = @"
                SELECT CT.MaCTDV, CT.MaPhieuDat, CT.MaDV, DV.TenDV, CT.SoLuong, CT.DonGia
                FROM CT_DICH_VU CT
                INNER JOIN DICH_VU DV ON CT.MaDV = DV.MaDV
                WHERE CT.MaPhieuDat = @pd
                ORDER BY CT.MaCTDV";

            DataTable dt = ExecuteQuery(query, new object[] { bookingId });
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new BookingServiceDetail
                {
                    Id = row["MaCTDV"]?.ToString() ?? string.Empty,
                    BookingId = row["MaPhieuDat"]?.ToString() ?? string.Empty,
                    ServiceId = row["MaDV"]?.ToString() ?? string.Empty,
                    ServiceName = row["TenDV"]?.ToString() ?? string.Empty,
                    Quantity = Convert.ToInt32(row["SoLuong"]),
                    Price = Convert.ToDecimal(row["DonGia"])
                });
            }
            return list;
        }

        public int UpdateStock(string id, int change)
        {
            string query = @"
                UPDATE DICH_VU
                SET SoLuongTon = SoLuongTon + @change
                WHERE MaDV = @id";
            return ExecuteNonQuery(query, new object[] { change, id });
        }

        public bool TryApplyPromotion(string? promoCode, decimal subtotal, out string? promotionId, out decimal discount, out string? error)
        {
            promotionId = null;
            discount = 0;
            error = null;

            string code = (promoCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                error = "Vui lòng nhập mã khuyến mãi.";
                return false;
            }

            if (subtotal <= 0)
            {
                error = "Tạm tính phải lớn hơn 0.";
                return false;
            }

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Table may not exist in some DBs yet.
                using (var check = new SqlCommand("SELECT CASE WHEN OBJECT_ID('dbo.KHUYEN_MAI','U') IS NULL THEN 0 ELSE 1 END", conn))
                {
                    int exists = Convert.ToInt32(check.ExecuteScalar() ?? 0);
                    if (exists == 0)
                    {
                        error = "Chưa có bảng KHUYEN_MAI. Hãy chạy scripts/migrate_promotions.sql.";
                        return false;
                    }
                }

                string sql = @"
                    SELECT TOP 1
                        MaKM,
                        Kieu,
                        GiaTri,
                        DonToiThieu,
                        NgayBD,
                        NgayKT,
                        TrangThai
                    FROM KHUYEN_MAI
                    WHERE UPPER(MaKM) = @code";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            error = "Mã khuyến mãi không tồn tại.";
                            return false;
                        }

                        bool active = reader["TrangThai"] != DBNull.Value && Convert.ToBoolean(reader["TrangThai"]);
                        if (!active)
                        {
                            error = "Mã khuyến mãi đang bị tắt.";
                            return false;
                        }

                        DateTime now = DateTime.Now;
                        if (reader["NgayBD"] != DBNull.Value && Convert.ToDateTime(reader["NgayBD"]) > now)
                        {
                            error = "Mã khuyến mãi chưa đến ngày áp dụng.";
                            return false;
                        }
                        if (reader["NgayKT"] != DBNull.Value && Convert.ToDateTime(reader["NgayKT"]) < now)
                        {
                            error = "Mã khuyến mãi đã hết hạn.";
                            return false;
                        }

                        if (reader["DonToiThieu"] != DBNull.Value)
                        {
                            decimal min = Convert.ToDecimal(reader["DonToiThieu"]);
                            if (subtotal < min)
                            {
                                error = $"Đơn tối thiểu {min:N0} để áp dụng mã này.";
                                return false;
                            }
                        }

                        string kieu = reader["Kieu"]?.ToString();
                        decimal giaTri = reader["GiaTri"] != DBNull.Value ? Convert.ToDecimal(reader["GiaTri"]) : 0;
                        if (giaTri <= 0)
                        {
                            error = "Mã khuyến mãi không hợp lệ.";
                            return false;
                        }

                        decimal giam;
                        if (string.Equals(kieu, "PERCENT", StringComparison.OrdinalIgnoreCase))
                            giam = Math.Round(subtotal * (giaTri / 100m), 0);
                        else if (string.Equals(kieu, "AMOUNT", StringComparison.OrdinalIgnoreCase))
                            giam = giaTri;
                        else
                            giam = 0;

                        if (giam <= 0)
                        {
                            error = "Mã khuyến mãi không hợp lệ.";
                            return false;
                        }

                        if (giam > subtotal) giam = subtotal;
                        promotionId = reader["MaKM"]?.ToString();
                        discount = giam;
                        return true;
                    }
                }
            }
        }

        public bool CheckoutPosSale(string? customerId, ObservableCollection<CartItem> items, string? promoCode, string? paymentMethod, out string? bookingId, out string? invoiceId, out string? error)
        {
            bookingId = null;
            invoiceId = null;
            error = null;

            if (items == null || items.Count == 0)
            {
                error = "Giỏ hàng trống.";
                return false;
            }

            bookingId = "PD" + DateTime.Now.ToString("yyyyMMddHHmmssfff"); // <= 20 chars
            invoiceId = "HD" + bookingId.Substring(2); // <= 20 chars

            decimal total = items.Sum(i => (i?.Price ?? 0) * (i?.Quantity ?? 0));
            if (total < 0)
            {
                error = "Tổng tiền không hợp lệ.";
                return false;
            }

            string appliedPromotionId = null;
            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                if (TryApplyPromotion(promoCode, total, out string km, out decimal giam, out string promoErr))
                {
                    appliedPromotionId = km;
                    discount = giam;
                }
                else
                {
                    error = promoErr;
                    return false;
                }
            }

            if (discount < 0) discount = 0;
            if (discount > total) discount = total;
            decimal payable = total - discount;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1) Validate stock (only for goods; rentals/equipment are not stock-tracked here)
                        foreach (var item in items)
                        {
                            if (item == null || string.IsNullOrWhiteSpace(item.ServiceId)) continue;
                            if (item.Quantity <= 0) continue;

                            bool isEquipment = InferCategory(item.ServiceId) == "Equipment";
                            if (isEquipment) continue;

                            using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SoLuongTon, 0) FROM DICH_VU WHERE MaDV = @id", conn, tran))
                            {
                                cmd.Parameters.AddWithValue("@id", item.ServiceId.Trim());
                                int stock = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                                if (stock < item.Quantity)
                                {
                                    error = $"Không đủ tồn kho cho '{item.ServiceName}'. Còn {stock}, cần {item.Quantity}.";
                                    tran.Rollback();
                                    return false;
                                }
                            }
                        }

                        // 2) Insert DAT_SAN (service-only)
                        // DAT_SAN.LoaiDat is guarded by a CHECK constraint: use N'Lẻ'
                        ExecuteNonQueryTrans(conn, tran, @"
                            INSERT INTO DAT_SAN (MaPhieuDat, MaKH, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
                            VALUES (@id, @kh, GETDATE(), N'Lẻ', N'Hoàn tất', @amount)",
                            new object[] { bookingId, string.IsNullOrWhiteSpace(customerId) ? null : customerId.Trim(), payable });

                        // 3) Insert CT_DICH_VU + update stock (skip stock update for Equipment)
                        foreach (var item in items)
                        {
                            if (item == null || string.IsNullOrWhiteSpace(item.ServiceId)) continue;
                            if (item.Quantity <= 0) continue;

                            string ctdvId = BuildServiceDetailId(conn, tran, bookingId);
                            ExecuteNonQueryTrans(conn, tran, @"
                                INSERT INTO CT_DICH_VU (MaCTDV, MaPhieuDat, MaDV, SoLuong, DonGia)
                                VALUES (@ct, @pd, @dv, @sl, @dg)",
                                new object[] { ctdvId, bookingId, item.ServiceId.Trim(), item.Quantity, item.Price });

                            bool isEquipment = InferCategory(item.ServiceId) == "Equipment";
                            if (!isEquipment)
                            {
                                ExecuteNonQueryTrans(conn, tran, @"
                                    UPDATE DICH_VU
                                    SET SoLuongTon = ISNULL(SoLuongTon, 0) - @sl
                                    WHERE MaDV = @dv",
                                    new object[] { item.Quantity, item.ServiceId.Trim() });
                            }
                        }

                        // 4) Insert HOA_DON (POS)
                        ExecuteNonQueryTrans(conn, tran, @"
                            INSERT INTO HOA_DON (MaHD, MaPhieuDat, MaKM, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
                            VALUES (@hd, @pd, @km, 0, @tdv, @giam, GETDATE(), @method)",
                            new object[]
                            {
                                invoiceId,
                                bookingId,
                                appliedPromotionId,
                                total,
                                discount,
                                string.IsNullOrWhiteSpace(paymentMethod) ? "Tiền mặt" : paymentMethod.Trim()
                            });

                        tran.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        try { tran.Rollback(); } catch { }
                        error = ex.Message;
                        bookingId = null;
                        invoiceId = null;
                        return false;
                    }
                }
            }
        }

        private static string BuildServiceDetailId(SqlConnection conn, SqlTransaction tran, string? bookingId)
        {
            string suffixBase = (bookingId ?? string.Empty).Trim();
            string last14 = suffixBase.Length <= 14 ? suffixBase : suffixBase.Substring(suffixBase.Length - 14);

            for (int attempt = 0; attempt < 10; attempt++)
            {
                string rand4 = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
                string id = "DV" + last14 + rand4;

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM CT_DICH_VU WHERE MaCTDV = @id", conn, tran))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int exists = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                    if (exists == 0) return id;
                }
            }

            return "DV" + last14 + "0000";
        }

        private static string InferCategory(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "Other";
            if (id.StartsWith("D", StringComparison.OrdinalIgnoreCase)) return "Drinks";
            if (id.StartsWith("E", StringComparison.OrdinalIgnoreCase)) return "Equipment";
            return "Other";
        }
    }
}

