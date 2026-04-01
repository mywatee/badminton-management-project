-- Migration: Services (DICH_VU) schema + seed data
-- Safe to run multiple times.
USE QLSCL;
GO

-- 1) Ensure columns exist (for current app DAL: LoaiDV, SoLuongTon)
IF COL_LENGTH('dbo.DICH_VU', 'LoaiDV') IS NULL
BEGIN
    ALTER TABLE dbo.DICH_VU ADD LoaiDV nvarchar(30) NULL;
END
GO

IF COL_LENGTH('dbo.DICH_VU', 'SoLuongTon') IS NULL
BEGIN
    ALTER TABLE dbo.DICH_VU ADD SoLuongTon int NULL;
END
GO

-- Default stock to 0 if null
UPDATE dbo.DICH_VU SET SoLuongTon = 0 WHERE SoLuongTon IS NULL;
GO

-- 2) Seed / upsert service items
-- Convention: Drinks => D###, Equipment => E###
MERGE dbo.DICH_VU AS tgt
USING (VALUES
    ('D001', N'Nước suối',        N'Chai', 10000, N'Drinks',    200),
    ('D002', N'Sting',            N'Chai', 15000, N'Drinks',    200),
    ('D003', N'Bò húc',           N'Lon',  18000, N'Drinks',    200),
    ('D004', N'Coca-Cola',        N'Lon',  15000, N'Drinks',    200),
    ('D005', N'Pepsi',            N'Lon',  15000, N'Drinks',    200),
    ('D006', N'Trà xanh 0 độ',     N'Chai', 15000, N'Drinks',    200),
    ('D007', N'C2',               N'Chai', 15000, N'Drinks',    200),
    ('D008', N'Khăn lạnh',         N'Cái',   5000, N'Drinks',    500),

    ('E001', N'Thuê vợt',         N'Giờ',  50000, N'Equipment', 50),
    ('E002', N'Thuê giày',        N'Giờ',  30000, N'Equipment', 50),
    ('E003', N'Ống cầu',          N'Ống',   40000, N'Equipment', 100)
) AS src(MaDV, TenDV, DonViTinh, GiaHienHanh, LoaiDV, SoLuongTon)
ON (tgt.MaDV = src.MaDV)
WHEN MATCHED THEN
    UPDATE SET
        TenDV = src.TenDV,
        DonViTinh = src.DonViTinh,
        GiaHienHanh = src.GiaHienHanh,
        LoaiDV = src.LoaiDV,
        SoLuongTon = src.SoLuongTon
WHEN NOT MATCHED THEN
    INSERT (MaDV, TenDV, DonViTinh, GiaHienHanh, LoaiDV, SoLuongTon)
    VALUES (src.MaDV, src.TenDV, src.DonViTinh, src.GiaHienHanh, src.LoaiDV, src.SoLuongTon);
GO
