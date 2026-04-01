USE QLSCL;
GO

-- Drop restrictive constraints to allow all lifecycle statuses
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'CHK_TrangThaiDat' AND type = 'C')
    ALTER TABLE DAT_SAN DROP CONSTRAINT CHK_TrangThaiDat;
GO

IF EXISTS (SELECT * FROM sys.objects WHERE name = 'CHK_TrangThaiSan' AND type = 'C')
    ALTER TABLE SAN DROP CONSTRAINT CHK_TrangThaiSan;
GO

-- Optional: Clean up potential inconsistent values to 'Nhận sân' and 'Sẵn sàng' 
-- only if they look like the broken '?' versions, but let's skip for now to be safe.
GO
