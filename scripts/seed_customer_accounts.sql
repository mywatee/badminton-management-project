-- Seed demo accounts for customers (TAI_KHOAN for KHACH_HANG)
-- Safe to run multiple times.
--
-- Password hashing matches QuanLySCL.DAL.SecurityHelper for ASCII passwords:
-- SHA256( password + saltGuidString )
--
-- Run after you already have KHACH_HANG rows (e.g., after seed_customers_bookings.sql).
USE QLSCL;

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @defaultPassword varchar(50) = '123456';
DECLARE @maxAccounts int = 200; -- create accounts for first N customers in KHACH_HANG

DECLARE @targets TABLE (RowNum int identity(1,1), MaKH varchar(15) NOT NULL);
INSERT INTO @targets (MaKH)
SELECT TOP (@maxAccounts) MaKH
FROM dbo.KHACH_HANG
ORDER BY MaKH;

DECLARE @i int = 1;
DECLARE @n int = (SELECT COUNT(*) FROM @targets);
DECLARE @inserted int = 0;

WHILE @i <= @n
BEGIN
    DECLARE @maKH varchar(15) = (SELECT MaKH FROM @targets WHERE RowNum = @i);
    DECLARE @username varchar(50) = LOWER(@maKH); -- kh001, kh002, ...

    IF EXISTS (SELECT 1 FROM dbo.KHACH_HANG WHERE MaKH = @maKH)
       AND NOT EXISTS (SELECT 1 FROM dbo.TAI_KHOAN WHERE TenDangNhap = @username)
       AND NOT EXISTS (SELECT 1 FROM dbo.TAI_KHOAN WHERE MaKH = @maKH)
    BEGIN
        DECLARE @salt uniqueidentifier = NEWID();
        DECLARE @hash varbinary(64) = HASHBYTES('SHA2_256', CONVERT(varchar(50), @defaultPassword) + CONVERT(varchar(36), @salt));

        INSERT INTO dbo.TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, MaNV, MaKH, VaiTro, TrangThai)
        VALUES (@username, @hash, @salt, NULL, @maKH, N'KhachHang', 1);

        SET @inserted += 1;
    END

    SET @i += 1;
END

PRINT 'Seed customer accounts completed.';
PRINT 'Inserted: ' + CAST(@inserted AS varchar(20)) + ' account(s). Default password: 123456';

