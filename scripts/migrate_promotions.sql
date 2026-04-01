-- Migration: Promotions (KHUYEN_MAI) schema + seed data
-- Compatible with the existing schema in `qlscltest01.sql` (TenChuongTrinh/PhanTramGiam/NgayBatDau/NgayKetThuc).
-- Safe to run multiple times.
USE QLSCL;
GO

IF OBJECT_ID('dbo.KHUYEN_MAI', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KHUYEN_MAI
    (
        MaKM        varchar(15)   NOT NULL PRIMARY KEY,
        TenKM       nvarchar(100) NOT NULL,
        Kieu        varchar(10)   NOT NULL, -- PERCENT | AMOUNT
        GiaTri      decimal(18,0) NOT NULL,
        DonToiThieu decimal(18,0) NULL,
        NgayBD      datetime      NULL,
        NgayKT      datetime      NULL,
        TrangThai   bit           NOT NULL CONSTRAINT DF_KHUYEN_MAI_TrangThai DEFAULT(1)
    );
END
GO

-- If KHUYEN_MAI already exists with the older schema, add missing columns used by the app.
IF COL_LENGTH('dbo.KHUYEN_MAI', 'TenKM') IS NULL
BEGIN
    ALTER TABLE dbo.KHUYEN_MAI ADD TenKM nvarchar(100) NULL;
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'Kieu') IS NULL
BEGIN
    ALTER TABLE dbo.KHUYEN_MAI ADD Kieu varchar(10) NULL; -- PERCENT | AMOUNT
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'GiaTri') IS NULL
BEGIN
    ALTER TABLE dbo.KHUYEN_MAI ADD GiaTri decimal(18,0) NULL;
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'DonToiThieu') IS NULL
BEGIN
    ALTER TABLE dbo.KHUYEN_MAI ADD DonToiThieu decimal(18,0) NULL;
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'NgayBD') IS NULL
BEGIN
    ALTER TABLE dbo.KHUYEN_MAI ADD NgayBD datetime NULL;
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'NgayKT') IS NULL
BEGIN
    ALTER TABLE dbo.KHUYEN_MAI ADD NgayKT datetime NULL;
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'TrangThai') IS NULL
BEGIN
    ALTER TABLE dbo.KHUYEN_MAI ADD TrangThai bit NOT NULL CONSTRAINT DF_KHUYEN_MAI_TrangThai2 DEFAULT(1);
END
GO

-- Map older columns (if present) to the newer columns.
-- Old: TenChuongTrinh, PhanTramGiam, NgayBatDau, NgayKetThuc
IF COL_LENGTH('dbo.KHUYEN_MAI', 'TenChuongTrinh') IS NOT NULL
BEGIN
    UPDATE dbo.KHUYEN_MAI
    SET TenKM = COALESCE(TenKM, TenChuongTrinh)
    WHERE TenKM IS NULL;
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'PhanTramGiam') IS NOT NULL
BEGIN
    UPDATE dbo.KHUYEN_MAI
    SET Kieu = COALESCE(Kieu, 'PERCENT'),
        GiaTri = COALESCE(GiaTri, CONVERT(decimal(18,0), PhanTramGiam))
    WHERE (Kieu IS NULL OR GiaTri IS NULL) AND PhanTramGiam IS NOT NULL;
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'NgayBatDau') IS NOT NULL
BEGIN
    UPDATE dbo.KHUYEN_MAI
    SET NgayBD = COALESCE(NgayBD, CONVERT(datetime, NgayBatDau))
    WHERE NgayBD IS NULL AND NgayBatDau IS NOT NULL;
END
GO

IF COL_LENGTH('dbo.KHUYEN_MAI', 'NgayKetThuc') IS NOT NULL
BEGIN
    UPDATE dbo.KHUYEN_MAI
    SET NgayKT = COALESCE(NgayKT, CONVERT(datetime, NgayKetThuc))
    WHERE NgayKT IS NULL AND NgayKetThuc IS NOT NULL;
END
GO

-- Ensure non-null core columns for newer logic
UPDATE dbo.KHUYEN_MAI
SET TenKM = COALESCE(TenKM, MaKM),
    Kieu = COALESCE(Kieu, 'AMOUNT'),
    GiaTri = COALESCE(GiaTri, 0),
    TrangThai = COALESCE(TrangThai, 1)
WHERE TenKM IS NULL OR Kieu IS NULL OR GiaTri IS NULL OR TrangThai IS NULL;
GO

-- Upsert seed promotions (codes used in UI)
MERGE dbo.KHUYEN_MAI AS tgt
USING (VALUES
    ('GIAM10',  N'Giảm 10% hóa đơn',     'PERCENT', 10,  NULL, DATEADD(day,-365,GETDATE()), DATEADD(day,365,GETDATE()), 1),
    ('GIAM20',  N'Giảm 20% hóa đơn',     'PERCENT', 20,  NULL, DATEADD(day,-365,GETDATE()), DATEADD(day,365,GETDATE()), 1),
    ('GIAM20K', N'Giảm 20.000đ',         'AMOUNT',  20000, NULL, DATEADD(day,-365,GETDATE()), DATEADD(day,365,GETDATE()), 1),
    ('GIAM50K', N'Giảm 50.000đ',         'AMOUNT',  50000, 200000, DATEADD(day,-365,GETDATE()), DATEADD(day,365,GETDATE()), 1)
) AS src(MaKM, TenKM, Kieu, GiaTri, DonToiThieu, NgayBD, NgayKT, TrangThai)
ON (tgt.MaKM = src.MaKM)
WHEN MATCHED THEN
    UPDATE SET
        TenKM = src.TenKM,
        Kieu = src.Kieu,
        GiaTri = src.GiaTri,
        DonToiThieu = src.DonToiThieu,
        NgayBD = src.NgayBD,
        NgayKT = src.NgayKT,
        TrangThai = src.TrangThai
WHEN NOT MATCHED THEN
    INSERT (MaKM, TenKM, Kieu, GiaTri, DonToiThieu, NgayBD, NgayKT, TrangThai)
    VALUES (src.MaKM, src.TenKM, src.Kieu, src.GiaTri, src.DonToiThieu, src.NgayBD, src.NgayKT, src.TrangThai);
GO
