USE [QLSCL];
GO
SET QUOTED_IDENTIFIER ON;
DECLARE @sql NVARCHAR(MAX) = N'
INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien) VALUES 
(''PD_H101'', ''KH001'', ''NV000'', ''2026-01-05'', N''Lẻ'', N''Đã thanh toán'', 150000),
(''PD_H102'', ''KH002'', ''NV000'', ''2026-01-10'', N''Lẻ'', N''Đã thanh toán'', 200000),
(''PD_H103'', ''KH003'', ''NV000'', ''2026-01-15'', N''Lẻ'', N''Đã thanh toán'', 120000),
(''PD_H104'', ''KH004'', ''NV000'', ''2026-01-20'', N''Lẻ'', N''Đã thanh toán'', 300000),
(''PD_H105'', ''KH005'', ''NV000'', ''2026-01-25'', N''Lẻ'', N''Đã thanh toán'', 80000),
(''PD_H201'', ''KH001'', ''NV000'', ''2026-02-05'', N''Lẻ'', N''Đã thanh toán'', 250000),
(''PD_H202'', ''KH002'', ''NV000'', ''2026-02-10'', N''Lẻ'', N''Đã thanh toán'', 400000),
(''PD_H203'', ''KH003'', ''NV000'', ''2026-02-15'', N''Lẻ'', N''Đã thanh toán'', 180000),
(''PD_H204'', ''KH004'', ''NV000'', ''2026-02-20'', N''Lẻ'', N''Đã thanh toán'', 220000),
(''PD_H205'', ''KH005'', ''NV000'', ''2026-02-25'', N''Lẻ'', N''Đã thanh toán'', 110000);

INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan) VALUES 
(''HD_H101'', ''PD_H101'', 100000, 50000, 0, ''2026-01-05'', N''Tiền mặt''),
(''HD_H102'', ''PD_H102'', 150000, 50000, 0, ''2026-01-10'', N''Tiền mặt''),
(''HD_H103'', ''PD_H103'', 100000, 20000, 0, ''2026-01-15'', N''Tiền mặt''),
(''HD_H104'', ''PD_H104'', 250000, 50000, 0, ''2026-01-20'', N''Tiền mặt''),
(''HD_H105'', ''PD_H105'', 80000, 0, 0, ''2026-01-25'', N''Tiền mặt''),
(''HD_H201'', ''PD_H201'', 200000, 50000, 0, ''2026-02-05'', N''Tiền mặt''),
(''HD_H202'', ''PD_H202'', 350000, 50000, 0, ''2026-02-10'', N''Tiền mặt''),
(''HD_H203'', ''PD_H203'', 150000, 30000, 0, ''2026-02-15'', N''Tiền mặt''),
(''HD_H204'', ''PD_H204'', 200000, 20000, 0, ''2026-02-20'', N''Tiền mặt''),
(''HD_H205'', ''PD_H205'', 100000, 10000, 0, ''2026-02-25'', N''Tiền mặt'');
';
EXEC sp_executesql @sql;
GO

SELECT 
    FORMAT(NgayLapPhieu, 'MM/yyyy') as [Month], 
    COUNT(*) as Bookings, 
    SUM(TongTien) as Revenue 
FROM DAT_SAN 
WHERE MaPhieuDat LIKE 'PD_H%'
GROUP BY FORMAT(NgayLapPhieu, 'MM/yyyy');
GO
