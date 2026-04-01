-- Seed demo dataset for QLSCL (final123.sql schema)
-- Targets:
-- - 8 courts (SAN)
-- - 100 customers (KHACH_HANG)
-- - 1000 past bookings (DAT_SAN + CT_DAT_SAN + HOA_DON)
-- - 30 accounts (TAI_KHOAN): 1 admin, 5 staff, 24 customers
-- - 8 services (DICH_VU): 5 drinks, 3 rentals (equipment)
--
-- Safe to run multiple times (uses MERGE / IF NOT EXISTS patterns).
USE QLSCL;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

--------------------------------------------------------------------------------
-- 1) Time slots (CA_GIO): 05:00-23:00 each 1 hour (18 slots)
--------------------------------------------------------------------------------
;WITH Slots AS
(
    SELECT 5 AS H
    UNION ALL SELECT H + 1 FROM Slots WHERE H < 22
)
MERGE dbo.CA_GIO AS tgt
USING
(
    SELECT
        CONCAT('C', RIGHT('00' + CAST(H AS varchar(2)), 2)) AS MaCa,
        (RIGHT('00' + CAST(H AS varchar(2)), 2) + N':00 - ' + RIGHT('00' + CAST(H + 1 AS varchar(2)), 2) + N':00') AS TenCa,
        CAST(CONCAT(RIGHT('00' + CAST(H AS varchar(2)), 2), ':00:00') AS time) AS GioBatDau,
        CAST(CONCAT(RIGHT('00' + CAST(H + 1 AS varchar(2)), 2), ':00:00') AS time) AS GioKetThuc,
        CAST(CASE WHEN H IN (18,19,20) THEN 1 ELSE 0 END AS bit) AS LaKhungGioVang
    FROM Slots
) AS src(MaCa, TenCa, GioBatDau, GioKetThuc, LaKhungGioVang)
ON (tgt.MaCa = src.MaCa)
WHEN MATCHED THEN
    UPDATE SET
        TenCa = src.TenCa,
        GioBatDau = src.GioBatDau,
        GioKetThuc = src.GioKetThuc,
        LaKhungGioVang = src.LaKhungGioVang
WHEN NOT MATCHED THEN
    INSERT (MaCa, TenCa, GioBatDau, GioKetThuc, LaKhungGioVang)
    VALUES (src.MaCa, src.TenCa, src.GioBatDau, src.GioKetThuc, src.LaKhungGioVang);
GO

--------------------------------------------------------------------------------
-- 2) Court types + courts: 8 courts (S01..S08)
--------------------------------------------------------------------------------
MERGE dbo.LOAI_SAN AS tgt
USING (VALUES
    ('LS01', N'Sân tiêu chuẩn', N'Sân tiêu chuẩn'),
    ('LS02', N'Sân VIP',       N'Sân VIP')
) AS src(MaLoaiSan, TenLoai, MoTa)
ON (tgt.MaLoaiSan = src.MaLoaiSan)
WHEN MATCHED THEN
    UPDATE SET TenLoai = src.TenLoai, MoTa = src.MoTa
WHEN NOT MATCHED THEN
    INSERT (MaLoaiSan, TenLoai, MoTa) VALUES (src.MaLoaiSan, src.TenLoai, src.MoTa);
GO

DECLARE @c int = 1;
WHILE @c <= 8
BEGIN
    DECLARE @maSan varchar(10) = CONCAT('S', RIGHT('00' + CAST(@c AS varchar(2)), 2));
    DECLARE @tenSan nvarchar(50) = N'Sân số ' + CAST(@c AS nvarchar(10));
    DECLARE @maLoaiSan varchar(10) = CASE WHEN @c IN (6,7,8) THEN 'LS02' ELSE 'LS01' END;

    IF NOT EXISTS (SELECT 1 FROM dbo.SAN WHERE MaSan = @maSan)
    BEGIN
        INSERT INTO dbo.SAN (MaSan, TenSan, MaLoaiSan, TrangThai, LoaiSan)
        VALUES (@maSan, @tenSan, @maLoaiSan, N'Sẵn sàng', CASE WHEN @maLoaiSan='LS02' THEN N'VIP Premium' ELSE N'Tiêu chuẩn' END);
    END

    SET @c += 1;
END
GO

--------------------------------------------------------------------------------
-- 3) Pricing rules (BANG_GIA): minimal set (per type + per slot + LoaiDat=Lẻ)
--------------------------------------------------------------------------------
DECLARE @bg TABLE (MaCa varchar(10), GioBatDau time);
INSERT INTO @bg(MaCa, GioBatDau)
SELECT MaCa, GioBatDau FROM dbo.CA_GIO;

DECLARE @row int = 1;
DECLARE @rows int = (SELECT COUNT(*) FROM @bg);
WHILE @row <= @rows
BEGIN
    DECLARE @maCa varchar(10) = (SELECT MaCa FROM (SELECT ROW_NUMBER() OVER (ORDER BY GioBatDau) AS rn, MaCa FROM @bg) x WHERE rn=@row);
    DECLARE @giaStd decimal(18,0) = 60000;
    DECLARE @giaVip decimal(18,0) = 80000;

    -- Golden hours more expensive
    DECLARE @isGolden bit = (SELECT LaKhungGioVang FROM dbo.CA_GIO WHERE MaCa=@maCa);
    IF @isGolden = 1
    BEGIN
        SET @giaStd += 20000;
        SET @giaVip += 20000;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.BANG_GIA WHERE MaGia = CONCAT('G', 'LS01', @maCa, 'L'))
    BEGIN
        INSERT INTO dbo.BANG_GIA (MaGia, MaLoaiSan, MaCa, LoaiDat, Gia)
        VALUES (CONCAT('G','LS01',@maCa,'L'), 'LS01', @maCa, N'Lẻ', @giaStd);
    END
    IF NOT EXISTS (SELECT 1 FROM dbo.BANG_GIA WHERE MaGia = CONCAT('G', 'LS02', @maCa, 'L'))
    BEGIN
        INSERT INTO dbo.BANG_GIA (MaGia, MaLoaiSan, MaCa, LoaiDat, Gia)
        VALUES (CONCAT('G','LS02',@maCa,'L'), 'LS02', @maCa, N'Lẻ', @giaVip);
    END

    SET @row += 1;
END
GO

--------------------------------------------------------------------------------
-- 4) Employees: 1 admin + 5 staff
--------------------------------------------------------------------------------
MERGE dbo.NHAN_VIEN AS tgt
USING (VALUES
    ('NV000', N'Quản trị viên',  '0900000000', N'Admin'),
    ('NV001', N'Nhân viên 01',   '0900000001', N'Nhân viên'),
    ('NV002', N'Nhân viên 02',   '0900000002', N'Nhân viên'),
    ('NV003', N'Nhân viên 03',   '0900000003', N'Nhân viên'),
    ('NV004', N'Nhân viên 04',   '0900000004', N'Nhân viên'),
    ('NV005', N'Nhân viên 05',   '0900000005', N'Nhân viên')
) AS src(MaNV, HoTen, SDT, ChucVu)
ON (tgt.MaNV = src.MaNV)
WHEN MATCHED THEN
    UPDATE SET HoTen = src.HoTen, SDT = src.SDT, ChucVu = src.ChucVu
WHEN NOT MATCHED THEN
    INSERT (MaNV, HoTen, SDT, ChucVu) VALUES (src.MaNV, src.HoTen, src.SDT, src.ChucVu);
GO

--------------------------------------------------------------------------------
-- 5) Customers: 100 (KH001..KH100)
--------------------------------------------------------------------------------
DECLARE @k int = 1;
WHILE @k <= 100
BEGIN
    DECLARE @maKH varchar(15) = 'KH' + RIGHT('000' + CAST(@k AS varchar(3)), 3);
    DECLARE @name nvarchar(100) = CASE WHEN @k = 1 THEN N'test01' ELSE N'Khách ' + RIGHT('000' + CAST(@k AS varchar(3)), 3) END;
    DECLARE @phone varchar(15) = '09' + RIGHT('00000000' + CAST(10000000 + @k AS varchar(8)), 8);
    DECLARE @email varchar(100) = CASE WHEN @k = 1 THEN 'plhoang2005@gmail.com' ELSE CONCAT('kh', RIGHT('000' + CAST(@k AS varchar(3)), 3), '@demo.local') END;
    DECLARE @join datetime = DATEADD(day, -(ABS(CHECKSUM(NEWID())) % 180), GETDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.KHACH_HANG WHERE MaKH = @maKH)
       AND NOT EXISTS (SELECT 1 FROM dbo.KHACH_HANG WHERE SDT = @phone)
    BEGIN
        INSERT INTO dbo.KHACH_HANG (MaKH, HoTen, SDT, Email, DiemTichLuy, NgayDangKy)
        VALUES (@maKH, @name, @phone, @email, 0, @join);
    END

    SET @k += 1;
END
GO

--------------------------------------------------------------------------------
-- 6) Services: 8 items (5 drinks + 3 rentals)
--------------------------------------------------------------------------------
MERGE dbo.DICH_VU AS tgt
USING (VALUES
    ('D001', N'Nước suối',   N'Chai', 10000, N'Drinks',    300),
    ('D002', N'Sting',       N'Chai', 15000, N'Drinks',    300),
    ('D003', N'Bò húc',      N'Lon',  18000, N'Drinks',    300),
    ('D004', N'Coca-Cola',   N'Lon',  15000, N'Drinks',    300),
    ('D005', N'Pepsi',       N'Lon',  15000, N'Drinks',    300),

    ('E001', N'Thuê vợt',    N'Giờ',  50000, N'Equipment', 0),
    ('E002', N'Thuê giày',   N'Giờ',  30000, N'Equipment', 0),
    ('E003', N'Ống cầu',     N'Ống',  40000, N'Equipment', 100)
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

--------------------------------------------------------------------------------
-- 7) Accounts: 30 (1 admin + 5 staff + 24 customers). Default password: 123456
--------------------------------------------------------------------------------
DECLARE @defaultPassword varchar(50) = '123456';

-- IMPORTANT:
-- App hashing (QuanLySCL.DAL.SecurityHelper):
--   SHA256( UTF8( password + saltGuidString ) )
-- saltGuidString = Guid.ToString() default format "D" (lowercase, with hyphens).
-- In SQL Server, to match UTF8 bytes we must use an UTF8 collation when converting to VARBINARY.
DECLARE @utf8Collation sysname = N'Latin1_General_100_BIN2_UTF8';

-- Admin account
DECLARE @saltAdmin uniqueidentifier = (SELECT MuoiSalt FROM dbo.TAI_KHOAN WHERE TenDangNhap = 'admin');
IF @saltAdmin IS NULL SET @saltAdmin = NEWID();
DECLARE @saltStrAdmin varchar(36) = LOWER(CONVERT(varchar(36), @saltAdmin));
DECLARE @hashAdmin varbinary(64) =
    HASHBYTES(
        'SHA2_256',
        CONVERT(varbinary(max), (CONVERT(varchar(50), @defaultPassword) + @saltStrAdmin) COLLATE Latin1_General_100_BIN2_UTF8)
    );

IF EXISTS (SELECT 1 FROM dbo.TAI_KHOAN WHERE TenDangNhap = 'admin')
BEGIN
    UPDATE dbo.TAI_KHOAN
    SET MatKhauHash = @hashAdmin,
        MuoiSalt = @saltAdmin,
        MaNV = 'NV000',
        MaKH = NULL,
        VaiTro = N'Admin',
        TrangThai = 1
    WHERE TenDangNhap = 'admin';
END
ELSE
BEGIN
    INSERT INTO dbo.TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, MaNV, MaKH, VaiTro, TrangThai)
    VALUES ('admin', @hashAdmin, @saltAdmin, 'NV000', NULL, N'Admin', 1);
END

-- Staff accounts (nv001..nv005)
DECLARE @nv int = 1;
WHILE @nv <= 5
BEGIN
    DECLARE @maNV varchar(15) = 'NV' + RIGHT('000' + CAST(@nv AS varchar(3)), 3);
    DECLARE @usernameNV varchar(50) = LOWER(@maNV);

    IF EXISTS (SELECT 1 FROM dbo.NHAN_VIEN WHERE MaNV = @maNV)
    BEGIN
        DECLARE @saltNV uniqueidentifier = (SELECT MuoiSalt FROM dbo.TAI_KHOAN WHERE TenDangNhap = @usernameNV);
        IF @saltNV IS NULL SET @saltNV = NEWID();
        DECLARE @saltStrNV varchar(36) = LOWER(CONVERT(varchar(36), @saltNV));
        DECLARE @hashNV varbinary(64) =
            HASHBYTES(
                'SHA2_256',
                CONVERT(varbinary(max), (CONVERT(varchar(50), @defaultPassword) + @saltStrNV) COLLATE Latin1_General_100_BIN2_UTF8)
            );

        IF EXISTS (SELECT 1 FROM dbo.TAI_KHOAN WHERE TenDangNhap = @usernameNV)
        BEGIN
            UPDATE dbo.TAI_KHOAN
            SET MatKhauHash = @hashNV,
                MuoiSalt = @saltNV,
                MaNV = @maNV,
                MaKH = NULL,
                VaiTro = N'NhanVien',
                TrangThai = 1
            WHERE TenDangNhap = @usernameNV;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, MaNV, MaKH, VaiTro, TrangThai)
            VALUES (@usernameNV, @hashNV, @saltNV, @maNV, NULL, N'NhanVien', 1);
        END
    END

    SET @nv += 1;
END

-- Customer accounts (kh001..kh024)
DECLARE @kh int = 1;
WHILE @kh <= 24
BEGIN
    DECLARE @maKH2 varchar(15) = 'KH' + RIGHT('000' + CAST(@kh AS varchar(3)), 3);
    DECLARE @usernameKH varchar(50) = LOWER(@maKH2);

    IF EXISTS (SELECT 1 FROM dbo.KHACH_HANG WHERE MaKH = @maKH2)
    BEGIN
        DECLARE @saltKH uniqueidentifier = (SELECT MuoiSalt FROM dbo.TAI_KHOAN WHERE TenDangNhap = @usernameKH);
        IF @saltKH IS NULL SET @saltKH = NEWID();
        DECLARE @saltStrKH varchar(36) = LOWER(CONVERT(varchar(36), @saltKH));
        DECLARE @hashKH varbinary(64) =
            HASHBYTES(
                'SHA2_256',
                CONVERT(varbinary(max), (CONVERT(varchar(50), @defaultPassword) + @saltStrKH) COLLATE Latin1_General_100_BIN2_UTF8)
            );

        IF EXISTS (SELECT 1 FROM dbo.TAI_KHOAN WHERE TenDangNhap = @usernameKH)
        BEGIN
            UPDATE dbo.TAI_KHOAN
            SET MatKhauHash = @hashKH,
                MuoiSalt = @saltKH,
                MaNV = NULL,
                MaKH = @maKH2,
                VaiTro = N'KhachHang',
                TrangThai = 1
            WHERE TenDangNhap = @usernameKH;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, MaNV, MaKH, VaiTro, TrangThai)
            VALUES (@usernameKH, @hashKH, @saltKH, NULL, @maKH2, N'KhachHang', 1);
        END
    END

    SET @kh += 1;
END
GO

--------------------------------------------------------------------------------
-- 8) Bookings: 1000 past bookings with invoices (Hoàn tất) and some cancelled
--------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.CA_GIO) OR NOT EXISTS (SELECT 1 FROM dbo.SAN) OR NOT EXISTS (SELECT 1 FROM dbo.KHACH_HANG)
BEGIN
    RAISERROR(N'Chưa có dữ liệu CA_GIO / SAN / KHACH_HANG.', 16, 1);
    RETURN;
END
GO

DECLARE @courts TABLE (RowNum int identity(0,1), MaSan varchar(10));
INSERT INTO @courts (MaSan)
SELECT TOP (8) MaSan FROM dbo.SAN ORDER BY MaSan;

DECLARE @slots TABLE (RowNum int identity(0,1), MaCa varchar(10));
INSERT INTO @slots (MaCa)
SELECT MaCa FROM dbo.CA_GIO ORDER BY GioBatDau;

DECLARE @customers TABLE (RowNum int identity(0,1), MaKH varchar(15));
INSERT INTO @customers (MaKH)
SELECT TOP (100) MaKH FROM dbo.KHACH_HANG ORDER BY MaKH;

DECLARE @courtCount int = (SELECT COUNT(*) FROM @courts);
DECLARE @slotCount int = (SELECT COUNT(*) FROM @slots);
DECLARE @customerCount int = (SELECT COUNT(*) FROM @customers);

DECLARE @i int = 1;
DECLARE @n int = 1000;

WHILE @i <= @n
BEGIN
    -- Make (date, court, slot) unique by mapping i-1 to a unique combination
    DECLARE @combo int = @i - 1;
    DECLARE @dayIndex int = @combo / (@courtCount * @slotCount);
    DECLARE @rest int = @combo % (@courtCount * @slotCount);
    DECLARE @courtIndex int = @rest / @slotCount;
    DECLARE @slotIndex int = @rest % @slotCount;

    DECLARE @useDate date = DATEADD(day, -@dayIndex, CAST(GETDATE() AS date));
    DECLARE @maSan varchar(10) = (SELECT MaSan FROM @courts WHERE RowNum = @courtIndex);
    DECLARE @maCa varchar(10) = (SELECT MaCa FROM @slots WHERE RowNum = @slotIndex);
    DECLARE @maKH varchar(15) = (SELECT MaKH FROM @customers WHERE RowNum = (@i - 1) % @customerCount);

    DECLARE @bookingId varchar(20) =
        'PD' + CONVERT(varchar(8), @useDate, 112) + RIGHT('0000' + CAST(@i AS varchar(4)), 4) + RIGHT('00' + CAST(@slotIndex AS varchar(2)), 2);
    DECLARE @detailId varchar(22) = 'CT' + @bookingId;

    IF NOT EXISTS (SELECT 1 FROM dbo.DAT_SAN WHERE MaPhieuDat = @bookingId)
    BEGIN
        DECLARE @status nvarchar(20) = CASE WHEN (@i % 17 = 0) THEN N'Hủy' ELSE N'Hoàn tất' END;
        DECLARE @type nvarchar(20) = CASE WHEN (@i % 9 = 0) THEN N'Cố định' ELSE N'Lẻ' END;
        DECLARE @basePrice decimal(18,0) = CASE WHEN @maSan IN ('S06','S07','S08') THEN 80000 ELSE 60000 END;
        DECLARE @tongTien decimal(18,0) = @basePrice + CASE WHEN (@i % 11 = 0) THEN 20000 ELSE 0 END;

        INSERT INTO dbo.DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
        VALUES (@bookingId, @maKH, NULL, DATEADD(day, -@dayIndex, GETDATE()), @type, @status, @tongTien);
    END

    -- CT_DAT_SAN unique constraint ensures no collisions. Only insert if not exists.
    IF NOT EXISTS (SELECT 1 FROM dbo.CT_DAT_SAN WHERE MaSan=@maSan AND MaCa=@maCa AND NgaySuDung=@useDate)
    BEGIN
        DECLARE @giaLuuTru decimal(18,0) = CASE WHEN @maSan IN ('S06','S07','S08') THEN 80000 ELSE 60000 END;
        INSERT INTO dbo.CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru)
        VALUES (@detailId, @bookingId, @maSan, @maCa, @useDate, @giaLuuTru);
    END

    -- Invoice only for completed bookings
    IF EXISTS (SELECT 1 FROM dbo.DAT_SAN WHERE MaPhieuDat=@bookingId AND TrangThai=N'Hoàn tất')
       AND NOT EXISTS (SELECT 1 FROM dbo.HOA_DON WHERE MaPhieuDat = @bookingId)
    BEGIN
        DECLARE @maHD varchar(20) = 'HD' + RIGHT(@bookingId, 18);
        DECLARE @tongTienSan decimal(18,0) = (SELECT TongTien FROM dbo.DAT_SAN WHERE MaPhieuDat=@bookingId);
        INSERT INTO dbo.HOA_DON (MaHD, MaPhieuDat, MaKM, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
        VALUES (@maHD, @bookingId, NULL, @tongTienSan, 0, 0, DATEADD(minute, 30, (SELECT NgayLapPhieu FROM dbo.DAT_SAN WHERE MaPhieuDat=@bookingId)), N'Tiền mặt');
    END

    SET @i += 1;
END
GO

PRINT N'Seed demo data completed.';
PRINT N'Accounts: admin / nv001..nv005 / kh001..kh024 (password: 123456)';
GO

--------------------------------------------------------------------------------
-- 9) POS service-only invoices (for "Lịch sử bán dịch vụ")
-- Creates invoices with TongTienSan=0 and TongTienDV>0, linked via DAT_SAN.MaPhieuDat.
--------------------------------------------------------------------------------
DECLARE @posInvoices int = 80;
DECLARE @posIndex int = 1;

DECLARE @drink1 varchar(10) = 'D002'; -- Sting
DECLARE @drink2 varchar(10) = 'D004'; -- Coca-Cola
DECLARE @equip1 varchar(10) = 'E001'; -- Thuê vợt (Giờ)

WHILE @posIndex <= @posInvoices
BEGIN
    DECLARE @posDate datetime = DATEADD(minute, -(@posIndex * 37), GETDATE());
    DECLARE @posCustomer varchar(15) = 'KH' + RIGHT('000' + CAST(((@posIndex - 1) % 24) + 1 AS varchar(3)), 3);

    -- Keep within 20 chars
    DECLARE @posBookingId varchar(20) = 'DV' + FORMAT(@posDate, 'yyMMddHHmmss') + RIGHT('00' + CAST(@posIndex AS varchar(2)), 2);
    DECLARE @posInvoiceId varchar(20) = 'HD' + RIGHT(@posBookingId, 18);

    IF NOT EXISTS (SELECT 1 FROM dbo.DAT_SAN WHERE MaPhieuDat = @posBookingId)
    BEGIN
        INSERT INTO dbo.DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
        VALUES (@posBookingId, @posCustomer, NULL, @posDate, N'Lẻ', N'Hoàn tất', 0);
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.HOA_DON WHERE MaPhieuDat = @posBookingId)
    BEGIN
        -- Items
        DECLARE @q1 int = 1 + (@posIndex % 4);
        DECLARE @q2 int = 1 + (@posIndex % 3);
        DECLARE @q3 int = 1 + (@posIndex % 2);

        DECLARE @p1 decimal(18,0) = (SELECT ISNULL(GiaHienHanh,0) FROM dbo.DICH_VU WHERE MaDV=@drink1);
        DECLARE @p2 decimal(18,0) = (SELECT ISNULL(GiaHienHanh,0) FROM dbo.DICH_VU WHERE MaDV=@drink2);
        DECLARE @p3 decimal(18,0) = (SELECT ISNULL(GiaHienHanh,0) FROM dbo.DICH_VU WHERE MaDV=@equip1);

        DECLARE @sub decimal(18,0) = (@q1 * @p1) + (@q2 * @p2) + (@q3 * @p3);
        DECLARE @discount decimal(18,0) = CASE WHEN (@posIndex % 7 = 0) THEN ROUND(@sub * 0.1, 0) ELSE 0 END;
        DECLARE @promo varchar(15) = CASE WHEN (@discount > 0) THEN 'GIAM10' ELSE NULL END;

        -- CT_DICH_VU ids must be <= 20 chars
        INSERT INTO dbo.CT_DICH_VU (MaCTDV, MaPhieuDat, MaDV, SoLuong, DonGia)
        VALUES ('CT' + RIGHT(@posBookingId, 18), @posBookingId, @drink1, @q1, @p1);

        INSERT INTO dbo.CT_DICH_VU (MaCTDV, MaPhieuDat, MaDV, SoLuong, DonGia)
        VALUES ('C2' + RIGHT(@posBookingId, 18), @posBookingId, @drink2, @q2, @p2);

        INSERT INTO dbo.CT_DICH_VU (MaCTDV, MaPhieuDat, MaDV, SoLuong, DonGia)
        VALUES ('C3' + RIGHT(@posBookingId, 18), @posBookingId, @equip1, @q3, @p3);

        INSERT INTO dbo.HOA_DON (MaHD, MaPhieuDat, MaKM, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
        VALUES (@posInvoiceId, @posBookingId, @promo, 0, @sub, @discount, @posDate, CASE WHEN (@posIndex % 2 = 0) THEN N'Tiền mặt' ELSE N'Chuyển khoản' END);
    END

    SET @posIndex += 1;
END
GO

PRINT N'Seed POS invoices completed.';
GO
