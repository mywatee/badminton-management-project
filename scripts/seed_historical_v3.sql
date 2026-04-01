USE [QLSCL];
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_NULLS ON;
GO
SET ARITHABORT ON;
GO

-- JAN 2026
INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
VALUES ('PD_H011', 'KH001', 'NV000', '2026-01-05 08:00', N'Lẻ', N'Đã thanh toán', 150000);
GO
INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
VALUES ('HD_H011', 'PD_H011', 100000, 50000, 0, '2026-01-05 09:30', N'Tiền mặt');
GO

INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
VALUES ('PD_H012', 'KH002', 'NV000', '2026-01-10 09:30', N'Lẻ', N'Đã thanh toán', 200000);
GO
INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
VALUES ('HD_H012', 'PD_H012', 150000, 50000, 0, '2026-01-10 11:00', N'Tiền mặt');
GO

INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
VALUES ('PD_H013', 'KH003', 'NV000', '2026-01-15 14:00', N'Lẻ', N'Đã thanh toán', 120000);
GO
INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
VALUES ('HD_H013', 'PD_H013', 100000, 20000, 0, '2026-01-15 15:30', N'Tiền mặt');
GO

-- FEB 2026
INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
VALUES ('PD_H021', 'KH001', 'NV000', '2026-02-05 08:00', N'Lẻ', N'Đã thanh toán', 250000);
GO
INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
VALUES ('HD_H021', 'PD_H021', 200000, 50000, 0, '2026-02-05 09:30', N'Tiền mặt');
GO

INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
VALUES ('PD_H022', 'KH002', 'NV000', '2026-02-10 09:30', N'Lẻ', N'Đã thanh toán', 400000);
GO
INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
VALUES ('HD_H022', 'PD_H022', 350000, 50000, 0, '2026-02-10 11:00', N'Tiền mặt');
GO

INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
VALUES ('PD_H023', 'KH003', 'NV000', '2026-02-15 14:00', N'Lẻ', N'Đã thanh toán', 180000);
GO
INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
VALUES ('HD_H023', 'PD_H023', 150000, 30000, 0, '2026-02-15 15:30', N'Tiền mặt');
GO

-- Final check
SELECT 
    FORMAT(NgayLapPhieu, 'MM/yyyy') as [Month], 
    COUNT(*) as Bookings, 
    SUM(TongTien) as Revenue 
FROM DAT_SAN 
WHERE MaPhieuDat LIKE 'PD_H%'
GROUP BY FORMAT(NgayLapPhieu, 'MM/yyyy');
GO
