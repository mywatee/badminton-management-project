import random
from datetime import datetime, timedelta

pd_records = []
ct_records = []
dv_records = []
hd_records = []

# Generate data for Jan and Feb 2026
start_dates = [datetime(2026, 1, 1), datetime(2026, 2, 1)]
for m_idx, start_date in enumerate(start_dates):
    for i in range(1, 61):
        idx = m_idx * 60 + i
        # Distribute randomly over the month
        curr_date = start_date + timedelta(days=random.randint(0, 27), hours=random.randint(6, 21))
        date_str = curr_date.strftime('%Y-%m-%d %H:%M:%S')
        only_date = curr_date.strftime('%Y-%m-%d')
        
        pd_id = f'PD_H_{idx:03}'
        hd_id = f'HD_H_{idx:03}'
        ct_id = f'CTDS_H_{idx:03}'
        dv_id = f'CTDV_H_{idx:03}'
        
        # Consistent amounts for charts
        tien_san = random.randint(1, 4) * 50000
        tien_dv = random.choice([0, 20000, 40000, 60000])
        tong = tien_san + tien_dv
        
        pd_records.append(f"INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien) VALUES ('{pd_id}', 'KH001', 'NV000', '{date_str}', N'Lẻ', N'Đã thanh toán', {tong});")
        ct_records.append(f"INSERT INTO CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru) VALUES ('{ct_id}', '{pd_id}', 'S01', 'C05', '{only_date}', {tien_san});")
        if tien_dv > 0:
            dv_records.append(f"INSERT INTO CT_DICH_VU (MaCTDV, MaPhieuDat, MaDV, SoLuong, DonGia) VALUES ('{dv_id}', '{pd_id}', 'D001', 1, {tien_dv});")
        hd_records.append(f"INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan) VALUES ('{hd_id}', '{pd_id}', {tien_san}, {tien_dv}, 0, '{date_str}', N'Tiền mặt');")

with open('scripts/seed_historical_final.sql', 'w', encoding='utf-8') as f:
    f.write('USE [QLSCL];\nGO\nSET QUOTED_IDENTIFIER ON;\nSET ANSI_NULLS ON;\nSET ARITHABORT ON;\nGO\n\n')
    f.write('-- BOOKINGS\n')
    f.write('\n'.join(pd_records) + '\nGO\n\n')
    f.write('-- DETAILS\n')
    f.write('\n'.join(ct_records) + '\nGO\n\n')
    f.write('-- SERVICES\n')
    f.write('\n'.join(dv_records) + '\nGO\n\n')
    f.write('-- INVOICES\n')
    f.write('\n'.join(hd_records) + '\nGO\n\n')
    f.write('SELECT FORMAT(NgayLapPhieu, \'MM/yyyy\') as Month, COUNT(*) as Bookings, SUM(TongTien) as Revenue FROM DAT_SAN WHERE MaPhieuDat LIKE \'PD_H_%\' GROUP BY FORMAT(NgayLapPhieu, \'MM/yyyy\');\nGO\n')

print("SQL script generated: scripts/seed_historical_final.sql")
