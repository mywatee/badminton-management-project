USE [QLSCL];
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Clear previous attempts
DELETE FROM HOA_DON WHERE MaHD LIKE 'HD_J%' OR MaHD LIKE 'HD_F%';
DELETE FROM DAT_SAN WHERE MaPhieuDat LIKE 'PD_J%' OR MaPhieuDat LIKE 'PD_F%';
GO

-- 2. Replication Jan (200 records)
INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
SELECT TOP 200 
    'PD_J_' + CAST(ROW_NUMBER() OVER(ORDER BY MaPhieuDat) AS VARCHAR(10)),
    MaKH, MaNV, DATEADD(MONTH, -2, NgayLapPhieu), LoaiDat, TrangThai, TongTien
FROM DAT_SAN 
WHERE (NgayLapPhieu >= '2026-03-01' AND TrangThai NOT LIKE 'H_y%');

INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
SELECT TOP 200
    'HD_J_' + CAST(ROW_NUMBER() OVER(ORDER BY HD.MaHD) AS VARCHAR(10)),
    'PD_J_' + CAST(ROW_NUMBER() OVER(ORDER BY HD.MaHD) AS VARCHAR(10)),
    HD.TongTienSan, HD.TongTienDV, HD.SoTienGiam, DATEADD(MONTH, -2, HD.NgayXuat), HD.HinhThucThanhToan
FROM HOA_DON HD
JOIN DAT_SAN DS ON DS.MaPhieuDat = HD.MaPhieuDat
WHERE (DS.NgayLapPhieu >= '2026-03-01' AND DS.TrangThai NOT LIKE 'H_y%');

-- 3. Replication Feb (200 records)
INSERT INTO DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
SELECT TOP 200 
    'PD_F_' + CAST(ROW_NUMBER() OVER(ORDER BY MaPhieuDat DESC) AS VARCHAR(10)),
    MaKH, MaNV, DATEADD(MONTH, -1, NgayLapPhieu), LoaiDat, TrangThai, TongTien
FROM DAT_SAN 
WHERE (NgayLapPhieu >= '2026-03-01' AND TrangThai NOT LIKE 'H_y%');

INSERT INTO HOA_DON (MaHD, MaPhieuDat, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
SELECT TOP 200
    'HD_F_' + CAST(ROW_NUMBER() OVER(ORDER BY HD.MaHD DESC) AS VARCHAR(10)),
    'PD_F_' + CAST(ROW_NUMBER() OVER(ORDER BY HD.MaHD DESC) AS VARCHAR(10)),
    HD.TongTienSan, HD.TongTienDV, HD.SoTienGiam, DATEADD(MONTH, -1, HD.NgayXuat), HD.HinhThucThanhToan
FROM HOA_DON HD
JOIN DAT_SAN DS ON DS.MaPhieuDat = HD.MaPhieuDat
WHERE (DS.NgayLapPhieu >= '2026-03-01' AND DS.TrangThai NOT LIKE 'H_y%');

GO
-- Check Result
SELECT FORMAT(NgayLapPhieu, 'MM/yyyy') as Month, COUNT(*) as Count, SUM(TongTien) as Sum 
FROM DAT_SAN 
WHERE MaPhieuDat LIKE 'PD_%' OR NgayLapPhieu >= '2026-03-01'
GROUP BY FORMAT(NgayLapPhieu, 'MM/yyyy')
ORDER BY Month;
GO
