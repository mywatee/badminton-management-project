/*
Migration 001: Allow SAN.TrangThai = N'Đang sử dụng' in addition to N'Sẵn sàng' and N'Bảo trì'.

Applies to DB: QLSCL
*/

USE [QLSCL];
GO

-- Drop existing CHECK constraint if present
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CHK_TrangThaiSan'
      AND parent_object_id = OBJECT_ID(N'dbo.SAN')
)
BEGIN
    ALTER TABLE dbo.SAN DROP CONSTRAINT [CHK_TrangThaiSan];
END
GO

-- Recreate with expanded allowed values
ALTER TABLE dbo.SAN WITH CHECK
ADD CONSTRAINT [CHK_TrangThaiSan]
CHECK (
    [TrangThai] IN (N'Sẵn sàng', N'Bảo trì', N'Đang sử dụng')
);
GO

ALTER TABLE dbo.SAN CHECK CONSTRAINT [CHK_TrangThaiSan];
GO

