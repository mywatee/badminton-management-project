USE [QLSCL];
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;
GO

-- Seeding historical data for Jan and Feb 2026
-- We insert in one go to ensure SET options are picked up correctly.

-- Insert into DAT_SAN
INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
VALUES 
('PD_H011', 'KH001', 'NV000', '2026-01-05 08:00', N'Lẻ', N'Đã thanh toán', 150000),
('PD_H012', 'KH002', 'NV000', '2026-01-10 09:30', N'Lẻ', N'Đã thanh toán', 200000),
('PD_H013', 'KH003', 'NV000', '2026-01-15 14:00', N'Lẻ', N'Đã thanh toán', 120000),
('PD_H014', 'KH004', 'NV000', '2026-01-20 18:00', N'Lẻ', N'Đã thanh toán', 300000),
('PD_H015', 'KH005', 'NV000', '2026-01-25 20:00', N'Lẻ', N'Đã thanh toán', 80000),
('PD_H021', 'KH001', 'NV000', '2026-02-05 08:00', N'Lẻ', N'Đã thanh toán', 250000),
('PD_H022', 'KH002', 'NV000', '2026-02-10 09:30', N'Lẻ', N'Đã thanh toán', 400000),
('PD_H023', 'KH003', 'NV000', '2026-02-15 14:00', N'Lẻ', N'Đã thanh toán', 180000),
('PD_H024', 'KH004', 'NV000', '2026-02-20 18:00', N'Lẻ', N'Đã thanh toán', 220000),
('PD_H025', 'KH005', 'NV000', '2026-02-25 20:00', N'Lẻ', N'Đã thanh toán', 110000);

-- Insert into HOA_DON (This is needed for Category Revenue)
INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
VALUES 
('HD_H011', 'PD_H011', 100000, 50000, 0, '2026-01-05 09:30', N'Tiền mặt'),
('HD_H012', 'PD_H012', 150000, 50000, 0, '2026-01-10 11:00', N'Tiền mặt'),
('HD_H013', 'PD_H013', 100000, 20000, 0, '2026-01-15 15:30', N'Tiền mặt'),
('HD_H014', 'PD_H014', 250000, 50000, 0, '2026-01-20 19:30', N'Tiền mặt'),
('HD_H015', 'PD_H015', 80000, 0, 0, '2026-01-25 21:00', N'Tiền mặt'),
('HD_H021', 'PD_H021', 200000, 50000, 0, '2026-02-05 09:30', N'Tiền mặt'),
('HD_H022', 'PD_H022', 350000, 50000, 0, '2026-02-10 11:00', N'Tiền mặt'),
('HD_H023', 'PD_H023', 150000, 30000, 0, '2026-02-15 15:30', N'Tiền mặt'),
('HD_H024', 'PD_H024', 200000, 20000, 0, '2026-02-20 19:30', N'Tiền mặt'),
('HD_H025', 'PD_H025', 100000, 10000, 0, '2026-02-25 21:00', N'Tiền mặt');

-- Check Results
SELECT 
    FORMAT(NgayLapPhieu, 'MM/yyyy') as [Month], 
    COUNT(*) as Bookings, 
    SUM(TongTien) as Revenue 
FROM DAT_SAN 
WHERE MaPhieuDat LIKE 'PD_H%'
GROUP BY FORMAT(NgayLapPhieu, 'MM/yyyy');
GO
