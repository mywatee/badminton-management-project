USE [QLSCL];
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- 1. Helper for Randomized Seeding
DECLARE @i INT = 1;
DECLARE @max INT = 120; 
DECLARE @currentDate DATETIME;
DECLARE @pdID VARCHAR(20);
DECLARE @hdID VARCHAR(20);
DECLARE @khID VARCHAR(15) = 'KH001';
DECLARE @nvID VARCHAR(15) = 'NV000';
DECLARE @sanID VARCHAR(10) = 'S01';
DECLARE @caID VARCHAR(10) = 'C05';
DECLARE @tienSan DECIMAL(18,0);
DECLARE @tienDV DECIMAL(18,0);
DECLARE @tongTien DECIMAL(18,0);

WHILE @i <= @max
BEGIN
    SET @currentDate = CASE 
        WHEN @i <= 60 THEN DATEADD(DAY, (ABS(CHECKSUM(NEWID())) % 31), '2026-01-01')
        ELSE DATEADD(DAY, (ABS(CHECKSUM(NEWID())) % 28), '2026-02-01')
    END;

    SET @pdID = 'PD_HIST_' + CAST(@i AS VARCHAR(10));
    SET @hdID = 'HD_HIST_' + CAST(@i AS VARCHAR(10));
    
    SET @tienSan = (ABS(CHECKSUM(NEWID())) % 5 + 1) * 50000;
    SET @tienDV = (ABS(CHECKSUM(NEWID())) % 3) * 20000;
    SET @tongTien = @tienSan + @tienDV;

    IF NOT EXISTS (SELECT 1 FROM DAT_SAN WHERE MaPhieuDat = @pdID)
    BEGIN
        INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
        VALUES (@pdID, @khID, @nvID, @currentDate, N'Lẻ', N'Đã thanh toán', @tongTien);

        INSERT INTO CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru)
        VALUES ('CTDS_H' + CAST(@i AS VARCHAR(10)), @pdID, @sanID, @caID, CAST(@currentDate AS DATE), @tienSan);

        IF @tienDV > 0
        BEGIN
            INSERT INTO CT_DICH_VU (MaCTDV, MaPhieuDat, MaDV, SoLuong, DonGia)
            VALUES ('CTDV_H' + CAST(@i AS VARCHAR(10)), @pdID, 'D001', 1, @tienDV);
        END

        INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
        VALUES (@hdID, @pdID, @tienSan, @tienDV, 0, @currentDate, N'Tiền mặt');
    END

    SET @i = @i + 1;
END

-- 2. Final Output
SELECT 
    FORMAT(NgayLapPhieu, 'MM/yyyy') as Month, 
    COUNT(*) as Bookings, 
    SUM(TongTien) as Revenue 
FROM DAT_SAN 
WHERE MaPhieuDat LIKE 'PD_HIST_%'
GROUP BY FORMAT(NgayLapPhieu, 'MM/yyyy');
GO
