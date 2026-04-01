USE [QLSCL];
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DECLARE @loaiL NVARCHAR(20) = (SELECT TOP 1 LoaiDat FROM DAT_SAN WHERE MaPhieuDat = 'PD20260329014417');
DECLARE @ttHT NVARCHAR(20) = (SELECT TOP 1 TrangThai FROM DAT_SAN WHERE MaPhieuDat = 'PD20260329014417');

-- 1. Promote KH011 (Booking-based VIP)
DECLARE @i INT = 1;
DECLARE @pdID VARCHAR(20);
DECLARE @hdID VARCHAR(20);
DECLARE @ngay DATE;

WHILE @i <= 60
BEGIN
    SET @pdID = 'PD_V1_011_' + CAST(@i AS VARCHAR(10));
    SET @hdID = 'HD_V1_011_' + CAST(@i AS VARCHAR(10));
    SET @ngay = CAST(DATEADD(DAY, -@i - 10, GETDATE()) AS DATE); -- Offset to avoid today's conflicts
    
    IF NOT EXISTS (SELECT 1 FROM DAT_SAN WHERE MaPhieuDat = @pdID)
    BEGIN
        INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
        VALUES (@pdID, 'KH011', 'NV000', DATEADD(DAY, -@i, GETDATE()), @loaiL, @ttHT, 100000);

        -- Use different slots/courts if needed, but unique dates should suffice
        INSERT INTO CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru)
        VALUES ('CTV1_' + CAST(@i AS VARCHAR(10)), @pdID, 'S01', 'C05', @ngay, 100000);

        INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
        VALUES (@hdID, @pdID, 100000, 0, 0, GETDATE(), N'Tiền mặt');
    END

    SET @i = @i + 1;
END

-- 2. KH013 is already VIP by spending (>10M), but let's give him one more unique booking to be sure
IF NOT EXISTS (SELECT 1 FROM DAT_SAN WHERE MaPhieuDat = 'PD_V2_013_FIN')
BEGIN
    INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
    VALUES ('PD_V2_013_FIN', 'KH013', 'NV000', GETDATE(), @loaiL, @ttHT, 100000);

    INSERT INTO CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru)
    VALUES ('CTV2F', 'PD_V2_013_FIN', 'S03', 'C07', CAST(DATEADD(DAY, 1, GETDATE()) AS DATE), 100000);

    INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
    VALUES ('HD_V2_013_FIN', 'PD_V2_013_FIN', 100000, 0, 0, GETDATE(), N'Tiền mặt');
END

GO
-- Verification
SELECT 
    KH.MaKH, KH.HoTen, 
    COUNT(DISTINCT CT.MaPhieuDat) as TotalBookings, 
    SUM(HD.TongThanhToan) as TotalSpent
FROM KHACH_HANG KH
JOIN DAT_SAN DS ON DS.MaKH = KH.MaKH
JOIN CT_DAT_SAN CT ON CT.MaPhieuDat = DS.MaPhieuDat
JOIN HOA_DON HD ON HD.MaPhieuDat = DS.MaPhieuDat
WHERE KH.MaKH IN ('KH011', 'KH013')
GROUP BY KH.MaKH, KH.HoTen;
GO
