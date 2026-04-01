using System;
using System.Collections.Generic;
using System.Data;
using QuanLySCL.DAL;

namespace QuanLySCL.BUS
{
    public class KhuyenMaiBUS
    {
        private readonly KhuyenMaiDAL _dal = new KhuyenMaiDAL();

        public (decimal discount, string error) CalculateDiscount(string code, decimal subtotal)
        {
            if (string.IsNullOrWhiteSpace(code)) return (0, "Vui lòng nhập mã khuyến mãi.");
            if (subtotal <= 0) return (0, "Tạm tính phải lớn hơn 0.");

            DataRow promo = _dal.GetPromotionByCode(code.Trim().ToUpperInvariant());
            if (promo == null) return (0, "Mã khuyến mãi không tồn tại hoặc đã hết hạn.");

            // Standardize column check (using names from ServiceDAL.TryApplyPromotion)
            if (promo["NgayBD"] != DBNull.Value && (DateTime)promo["NgayBD"] > DateTime.Now)
                return (0, "Mã khuyến mãi chưa đến ngày áp dụng.");
            if (promo["NgayKT"] != DBNull.Value && (DateTime)promo["NgayKT"] < DateTime.Now)
                return (0, "Mã khuyến mãi đã hết hạn.");

            decimal minOrder = promo["DonToiThieu"] != DBNull.Value ? (decimal)promo["DonToiThieu"] : 0;
            if (subtotal < minOrder)
                return (0, $"Đơn tối thiểu {minOrder:N0} để áp dụng mã này.");

            string type = promo["Kieu"]?.ToString()?.ToUpperInvariant();
            decimal val = promo["GiaTri"] != DBNull.Value ? (decimal)promo["GiaTri"] : 0;

            decimal discount = 0;
            if (type == "PERCENT") discount = subtotal * (val / 100m);
            else if (type == "AMOUNT") discount = val;

            if (discount > subtotal) discount = subtotal;
            return (Math.Round(discount, 0), null);
        }

        public System.Collections.ObjectModel.ObservableCollection<QuanLySCL.Models.Promotion> GetAllPromotions()
        {
            var dt = _dal.GetAllPromotions();
            var list = new System.Collections.ObjectModel.ObservableCollection<QuanLySCL.Models.Promotion>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new QuanLySCL.Models.Promotion
                {
                    MaKM = row["MaKM"].ToString(),
                    TenKM = row["TenKM"] != DBNull.Value ? row["TenKM"].ToString() : string.Empty,
                    Kieu = row["Kieu"] != DBNull.Value ? row["Kieu"].ToString() : "AMOUNT",
                    GiaTri = row["GiaTri"] != DBNull.Value ? Convert.ToDecimal(row["GiaTri"]) : 0,
                    DonToiThieu = row["DonToiThieu"] != DBNull.Value ? Convert.ToDecimal(row["DonToiThieu"]) : (decimal?)null,
                    NgayBD = row["NgayBD"] != DBNull.Value ? Convert.ToDateTime(row["NgayBD"]) : (DateTime?)null,
                    NgayKT = row["NgayKT"] != DBNull.Value ? Convert.ToDateTime(row["NgayKT"]) : (DateTime?)null,
                    TrangThai = row["TrangThai"] != DBNull.Value ? Convert.ToBoolean(row["TrangThai"]) : true,
                });
            }
            return list;
        }

        public (bool ok, string error) CreateOrUpdatePromotion(bool isNew, QuanLySCL.Models.Promotion promo)
        {
            if (string.IsNullOrWhiteSpace(promo.MaKM))
                return (false, "Mã khuyến mãi không được trống.");

            try
            {
                bool result;
                if (isNew)
                {
                    var existing = _dal.GetPromotionByCode(promo.MaKM);
                    if (existing != null)
                        return (false, "Mã này đã tồn tại.");

                    result = _dal.InsertPromotion(
                        promo.MaKM, promo.TenKM, promo.Kieu, promo.GiaTri, 
                        promo.DonToiThieu, promo.NgayBD, promo.NgayKT, promo.TrangThai);
                }
                else
                {
                    result = _dal.UpdatePromotion(
                        promo.MaKM, promo.TenKM, promo.Kieu, promo.GiaTri, 
                        promo.DonToiThieu, promo.NgayBD, promo.NgayKT, promo.TrangThai);
                }

                if (result) return (true, null);
                return (false, "Không thể lưu khuyến mãi vào CSDL.");
            }
            catch (Exception ex)
            {
                return (false, "Lỗi: " + ex.Message);
            }
        }

        public (bool ok, string error) DeletePromotion(string maKM)
        {
            try
            {
                bool result = _dal.DeletePromotion(maKM);
                return result ? (true, null) : (false, "Không thể xóa khuyến mãi do lỗi CSDL.");
            }
            catch (Exception ex)
            {
                return (false, "Lỗi: " + ex.Message);
            }
        }
    }
}
