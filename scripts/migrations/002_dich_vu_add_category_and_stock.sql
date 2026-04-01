/*
Migration 002: Add inventory + category columns for DICH_VU to match POS UI.

Adds:
- LoaiDV (nvarchar(20))  : category key, e.g. 'Drinks' / 'Equipment'
- SoLuongTon (int)       : stock on hand
*/

USE [QLSCL];
GO

IF COL_LENGTH('dbo.DICH_VU', 'LoaiDV') IS NULL
BEGIN
    ALTER TABLE dbo.DICH_VU
    ADD LoaiDV nvarchar(20) NULL;
END
GO

IF COL_LENGTH('dbo.DICH_VU', 'SoLuongTon') IS NULL
BEGIN
    ALTER TABLE dbo.DICH_VU
    ADD SoLuongTon int NULL;
END
GO

-- Defaults for existing rows
UPDATE dbo.DICH_VU
SET
    LoaiDV = COALESCE(LoaiDV,
        CASE
            WHEN MaDV LIKE 'E%' THEN N'Equipment'
            WHEN MaDV LIKE 'D%' THEN N'Drinks'
            ELSE N'Drinks'
        END
    ),
    SoLuongTon = COALESCE(SoLuongTon, 0);
GO

-- Add defaults for new inserts
IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.DICH_VU')
      AND name = N'DF_DICH_VU_LoaiDV'
)
BEGIN
    ALTER TABLE dbo.DICH_VU ADD CONSTRAINT [DF_DICH_VU_LoaiDV] DEFAULT (N'Drinks') FOR [LoaiDV];
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.DICH_VU')
      AND name = N'DF_DICH_VU_SoLuongTon'
)
BEGIN
    ALTER TABLE dbo.DICH_VU ADD CONSTRAINT [DF_DICH_VU_SoLuongTon] DEFAULT ((0)) FOR [SoLuongTon];
END
GO

-- Optional: keep values consistent
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CHK_DICH_VU_LoaiDV'
      AND parent_object_id = OBJECT_ID(N'dbo.DICH_VU')
)
BEGIN
    ALTER TABLE dbo.DICH_VU DROP CONSTRAINT [CHK_DICH_VU_LoaiDV];
END
GO

ALTER TABLE dbo.DICH_VU WITH CHECK
ADD CONSTRAINT [CHK_DICH_VU_LoaiDV]
CHECK (LoaiDV IN (N'Drinks', N'Equipment'));
GO

ALTER TABLE dbo.DICH_VU CHECK CONSTRAINT [CHK_DICH_VU_LoaiDV];
GO

