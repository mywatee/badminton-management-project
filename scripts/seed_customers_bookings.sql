-- Seed demo data: Customers + past bookings (linked)
-- Safe to run multiple times.
-- IMPORTANT: Run the whole file (do NOT run a selected fragment), to avoid variable scope issues.
USE QLSCL;

SET NOCOUNT ON;
SET XACT_ABORT ON;

--------------------------------------------------------------------------------
-- Config
--------------------------------------------------------------------------------
DECLARE @customerTarget int = 250;   -- ensure KH001..KH250
DECLARE @bookingTarget int = 1200;   -- number of past bookings to TRY creating

DECLARE @vipCustomerId varchar(15) = 'KH001';
DECLARE @vipExtraBookings int = 80;  -- extra bookings to ensure VIP demo
DECLARE @vipUnitPrice decimal(18,0) = 250000;

--------------------------------------------------------------------------------
-- 1) Seed customers (KHACH_HANG)
-- SDT is UNIQUE, so we skip/avoid duplicates.
--------------------------------------------------------------------------------
DECLARE @seedCustomers TABLE (
    MaKH varchar(15) NOT NULL,
    HoTen nvarchar(100) NOT NULL,
    SDT varchar(15) NOT NULL,
    Email varchar(100) NULL,
    DiemTichLuy int NULL,
    NgayDangKy datetime NULL
);

INSERT INTO @seedCustomers (MaKH, HoTen, SDT, Email, DiemTichLuy, NgayDangKy)
VALUES
    ('KH001', N'test01',            '0358448073', 'plhoang2005@gmail.com', 0, DATEADD(day, -30, GETDATE())),
    -- NOTE: Use less “common” SDT patterns to reduce collisions with existing DBs.
    ('KH002', N'Nguyễn Văn An',      '0901000002', 'an.nv@gmail.com',       0, DATEADD(day, -5,  GETDATE())),
    ('KH003', N'Phạm Minh Đức',      '0901000003', 'duc.pm@outlook.com',    0, DATEADD(day, -2,  GETDATE())),
    ('KH004', N'Lê Hoàng Cường',     '0901000004', 'cuong.lh@hotmail.com', 0, DATEADD(day, -1,  GETDATE())),
    ('KH005', N'Hoàng Anh Thư',      '0901000005', 'thu.ha@gmail.com',      0, DATEADD(day, -10, GETDATE())),
    ('KH006', N'Trần Thị Bình',      '0901000006', 'binh.tt@yahoo.com',     0, DATEADD(day, -15, GETDATE())),
    ('KH007', N'Vũ Đức Huy',         '0901000007', 'huy.vd@gmail.com',      0, DATEADD(day, -60, GETDATE())),
    ('KH008', N'Bùi Ngọc Linh',      '0901000008', 'linh.bn@gmail.com',     0, DATEADD(day, -90, GETDATE())),
    ('KH009', N'Đặng Quốc Bảo',      '0901000009', 'bao.dq@gmail.com',      0, DATEADD(day, -120,GETDATE())),
    ('KH010', N'Nguyễn Thị Mai',     '0901000010', 'mai.nt@gmail.com',      0, DATEADD(day, -45, GETDATE()));

-- Remove seed rows whose phone already exists for a different MaKH (SDT unique)
DELETE s
FROM @seedCustomers s
WHERE EXISTS (
    SELECT 1
    FROM dbo.KHACH_HANG k
    WHERE k.SDT = s.SDT AND k.MaKH <> s.MaKH
);

MERGE dbo.KHACH_HANG AS tgt
USING @seedCustomers AS src
ON (tgt.MaKH = src.MaKH)
WHEN MATCHED THEN
    UPDATE SET
        HoTen = src.HoTen,
        SDT = CASE
            WHEN EXISTS (SELECT 1 FROM dbo.KHACH_HANG k2 WHERE k2.SDT = src.SDT AND k2.MaKH <> src.MaKH)
                THEN tgt.SDT
            ELSE src.SDT
        END,
        Email = src.Email,
        DiemTichLuy = ISNULL(tgt.DiemTichLuy, src.DiemTichLuy),
        NgayDangKy = ISNULL(tgt.NgayDangKy, src.NgayDangKy)
WHEN NOT MATCHED THEN
    INSERT (MaKH, HoTen, SDT, Email, DiemTichLuy, NgayDangKy)
    VALUES (src.MaKH, src.HoTen, src.SDT, src.Email, src.DiemTichLuy, src.NgayDangKy);

-- Bulk customers KH011..KHxxx
DECLARE @k int = 11;
WHILE @k <= @customerTarget
BEGIN
    DECLARE @id varchar(15) = 'KH' + RIGHT('000' + CAST(@k AS varchar(3)), 3);
    DECLARE @name nvarchar(100) = N'Khách ' + RIGHT('000' + CAST(@k AS varchar(3)), 3);
    DECLARE @phone varchar(15) = '09' + RIGHT('00000000' + CAST(10000000 + @k AS varchar(8)), 8); -- unique 10 digits
    DECLARE @email varchar(100) = 'kh' + RIGHT('000' + CAST(@k AS varchar(3)), 3) + '@demo.local';
    DECLARE @join datetime = DATEADD(day, -(ABS(CHECKSUM(NEWID())) % 180), GETDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.KHACH_HANG WHERE MaKH = @id)
       AND NOT EXISTS (SELECT 1 FROM dbo.KHACH_HANG WHERE SDT = @phone)
    BEGIN
        INSERT INTO dbo.KHACH_HANG (MaKH, HoTen, SDT, Email, DiemTichLuy, NgayDangKy)
        VALUES (@id, @name, @phone, @email, 0, @join);
    END

    SET @k += 1;
END

--------------------------------------------------------------------------------
-- 2) Past bookings (DAT_SAN + CT_DAT_SAN) and invoices (HOA_DON)
-- Requirements:
-- - CA_GIO must have rows (standardize_data.sql does it)
-- - SAN must have rows
--------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.CA_GIO) OR NOT EXISTS (SELECT 1 FROM dbo.SAN)
BEGIN
    RAISERROR(N'Chưa có dữ liệu CA_GIO hoặc SAN. Hãy chạy standardize_data.sql / seed sân trước.', 16, 1);
    RETURN;
END

DECLARE @courts TABLE (RowNum int identity(0,1), MaSan varchar(10));
INSERT INTO @courts (MaSan)
SELECT MaSan FROM dbo.SAN ORDER BY MaSan;

DECLARE @slots TABLE (RowNum int identity(0,1), MaCa varchar(10));
INSERT INTO @slots (MaCa)
SELECT MaCa FROM dbo.CA_GIO ORDER BY GioBatDau;

DECLARE @customers TABLE (RowNum int identity(0,1), MaKH varchar(15));
INSERT INTO @customers (MaKH)
SELECT MaKH FROM dbo.KHACH_HANG WHERE MaKH LIKE 'KH%' ORDER BY MaKH;

DECLARE @courtCount int = (SELECT COUNT(*) FROM @courts);
DECLARE @slotCount int = (SELECT COUNT(*) FROM @slots);
DECLARE @customerCount int = (SELECT COUNT(*) FROM @customers);

IF @courtCount = 0 OR @slotCount = 0 OR @customerCount = 0
BEGIN
    RAISERROR(N'Không đủ dữ liệu SAN/CA_GIO/KHACH_HANG để seed.', 16, 1);
    RETURN;
END

DECLARE @i int = 1;
DECLARE @n int = @bookingTarget;

WHILE @i <= @n
BEGIN
    -- Spread over ~180 days to reduce UQ_LichSan collisions
    DECLARE @useDate date = DATEADD(day, -((@i - 1) % 180), CAST(GETDATE() AS date));
    DECLARE @customerIndex int = (@i - 1) % @customerCount;
    DECLARE @courtIndex int = (@i - 1) % @courtCount;
    DECLARE @slotIndex int = (@i * 3 - 1) % @slotCount;

    DECLARE @maKH varchar(15) = (SELECT MaKH FROM @customers WHERE RowNum = @customerIndex);
    DECLARE @maSan varchar(10) = (SELECT MaSan FROM @courts WHERE RowNum = @courtIndex);
    DECLARE @maCa varchar(10) = (SELECT MaCa FROM @slots WHERE RowNum = @slotIndex);

    DECLARE @bookingId varchar(20) =
        'PD' + CONVERT(varchar(8), @useDate, 112) + RIGHT('0000' + CAST(@i AS varchar(4)), 4) + RIGHT('00' + CAST(@slotIndex AS varchar(2)), 2);
    DECLARE @detailId varchar(25) = 'CT' + @bookingId;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.CT_DAT_SAN
        WHERE MaSan = @maSan AND MaCa = @maCa AND NgaySuDung = @useDate
    )
    BEGIN
        DECLARE @type nvarchar(20) = CASE WHEN (@i % 7 = 0) THEN N'Cố định' ELSE N'Lẻ' END;
        DECLARE @status nvarchar(20) = CASE WHEN (@i % 17 = 0) THEN N'Hủy' ELSE N'Hoàn tất' END;

        DECLARE @basePrice decimal(18,0) =
            CASE WHEN @maSan IN ('S06','S07','S08') THEN 80000 ELSE 60000 END
            + CASE WHEN (@i % 9 = 0) THEN 20000 ELSE 0 END;

        DECLARE @tongTien decimal(18,0) = @basePrice;
        DECLARE @ngayLap datetime = DATEADD(day, -((@i - 1) % 180), GETDATE());

        IF NOT EXISTS (SELECT 1 FROM dbo.DAT_SAN WHERE MaPhieuDat = @bookingId)
        BEGIN
            INSERT INTO dbo.DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
            VALUES (@bookingId, @maKH, NULL, @ngayLap, @type, @status, @tongTien);
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.CT_DAT_SAN WHERE MaCTDS = @detailId)
        BEGIN
            INSERT INTO dbo.CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru)
            VALUES (@detailId, @bookingId, @maSan, @maCa, @useDate, @basePrice);
        END

        IF @status = N'Hoàn tất'
        BEGIN
            DECLARE @maHD varchar(20) = 'HD' + RIGHT(@bookingId, 18);
            IF NOT EXISTS (SELECT 1 FROM dbo.HOA_DON WHERE MaPhieuDat = @bookingId)
            BEGIN
                INSERT INTO dbo.HOA_DON (MaHD, MaPhieuDat, MaKM, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
                VALUES (@maHD, @bookingId, NULL, @tongTien, 0, 0, DATEADD(minute, 30, @ngayLap), N'Tiền mặt');
            END
        END
    END

    SET @i += 1;
END

--------------------------------------------------------------------------------
-- 3) Extra high-value bookings for KH001 so it becomes VIP in demo
-- VIP rule in CustomerDAL: totalBookings > 50 OR totalSpent > 10,000,000
--------------------------------------------------------------------------------
DECLARE @j int = 1;
DECLARE @m int = @vipExtraBookings;

WHILE @j <= @m
BEGIN
    DECLARE @useDate2 date = DATEADD(day, -(200 + @j), CAST(GETDATE() AS date));
    DECLARE @courtIndex2 int = (@j - 1) % @courtCount;
    DECLARE @slotIndex2 int = (@j * 3 - 1) % @slotCount;

    DECLARE @maSan2 varchar(10) = (SELECT MaSan FROM @courts WHERE RowNum = @courtIndex2);
    DECLARE @maCa2 varchar(10) = (SELECT MaCa FROM @slots WHERE RowNum = @slotIndex2);

    DECLARE @bookingId2 varchar(20) =
        'PD' + CONVERT(varchar(8), @useDate2, 112) + RIGHT('0000' + CAST((1000 + @j) AS varchar(4)), 4) + RIGHT('00' + CAST(@slotIndex2 AS varchar(2)), 2);
    DECLARE @detailId2 varchar(25) = 'CT' + @bookingId2;

    IF NOT EXISTS (
        SELECT 1 FROM dbo.CT_DAT_SAN
        WHERE MaSan = @maSan2 AND MaCa = @maCa2 AND NgaySuDung = @useDate2
    )
    BEGIN
        DECLARE @basePrice2 decimal(18,0) = @vipUnitPrice;
        DECLARE @ngayLap2 datetime = DATEADD(day, -(200 + @j), GETDATE());

        IF NOT EXISTS (SELECT 1 FROM dbo.DAT_SAN WHERE MaPhieuDat = @bookingId2)
        BEGIN
            INSERT INTO dbo.DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien)
            VALUES (@bookingId2, @vipCustomerId, NULL, @ngayLap2, N'Lẻ', N'Hoàn tất', @basePrice2);
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.CT_DAT_SAN WHERE MaCTDS = @detailId2)
        BEGIN
            INSERT INTO dbo.CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru)
            VALUES (@detailId2, @bookingId2, @maSan2, @maCa2, @useDate2, @basePrice2);
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.HOA_DON WHERE MaPhieuDat = @bookingId2)
        BEGIN
            DECLARE @maHD2 varchar(20) = 'HD' + RIGHT(@bookingId2, 18);
            INSERT INTO dbo.HOA_DON (MaHD, MaPhieuDat, MaKM, TongTienSan, TongTienDV, SoTienGiam, NgayXuat, HinhThucThanhToan)
            VALUES (@maHD2, @bookingId2, NULL, @basePrice2, 0, 0, DATEADD(minute, 30, @ngayLap2), N'Tiền mặt');
        END
    END

    SET @j += 1;
END

PRINT 'Seed customers + bookings completed.';

