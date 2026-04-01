/*
Seed data for QLSCL (schema from test03.sql).

Run after you have created the database + tables.

Creates:
- Courts (SAN) + court types (LOAI_SAN)
- Time slots (CA_GIO)
- Customers (KHACH_HANG)
- One employee (NHAN_VIEN)
- Login accounts in TAI_KHOAN:
  - admin / admin123 (Role: Admin)
  - kh001 / 123456   (Role: KhachHang)
- Sample bookings (DAT_SAN + CT_DAT_SAN) for the current week (Mon..Sun)

Note: Password hash matches SecurityHelper.HashPassword for ASCII passwords:
SHA256( password + saltGuidString )
*/

USE [QLSCL];
GO

SET NOCOUNT ON;

DECLARE @today date = CAST(GETDATE() AS date);
DECLARE @weekStart date = DATEADD(day, -(DATEDIFF(day, 0, @today) % 7), @today); -- Monday (1900-01-01 is Monday)

/* LOAI_SAN */
IF NOT EXISTS (SELECT 1 FROM dbo.LOAI_SAN)
BEGIN
    INSERT INTO dbo.LOAI_SAN (MaLoaiSan, TenLoai, MoTa) VALUES
    ('LS01', N'Trong nhà', N'Sân trong nhà'),
    ('LS02', N'Ngoài trời', N'Sân ngoài trời'),
    ('LS03', N'Thảm chuyên nghiệp', N'Sân thảm chuyên nghiệp');
END

/* CA_GIO */
IF NOT EXISTS (SELECT 1 FROM dbo.CA_GIO)
BEGIN
    INSERT INTO dbo.CA_GIO (MaCa, TenCa, GioBatDau, GioKetThuc, LaKhungGioVang) VALUES
    ('CA01', N'06:00 - 07:00', '06:00', '07:00', 0),
    ('CA02', N'07:00 - 08:00', '07:00', '08:00', 0),
    ('CA03', N'08:00 - 09:00', '08:00', '09:00', 0),
    ('CA04', N'09:00 - 10:00', '09:00', '10:00', 0),
    ('CA05', N'10:00 - 11:00', '10:00', '11:00', 0),
    ('CA06', N'11:00 - 12:00', '11:00', '12:00', 0),
    ('CA07', N'12:00 - 13:00', '12:00', '13:00', 0),
    ('CA08', N'13:00 - 14:00', '13:00', '14:00', 0),
    ('CA09', N'14:00 - 15:00', '14:00', '15:00', 0),
    ('CA10', N'15:00 - 16:00', '15:00', '16:00', 0),
    ('CA11', N'16:00 - 17:00', '16:00', '17:00', 0),
    ('CA12', N'17:00 - 18:00', '17:00', '18:00', 0),
    ('CA13', N'18:00 - 19:00', '18:00', '19:00', 1),
    ('CA14', N'19:00 - 20:00', '19:00', '20:00', 1),
    ('CA15', N'20:00 - 21:00', '20:00', '21:00', 1),
    ('CA16', N'21:00 - 22:00', '21:00', '22:00', 0);
END

/* SAN */
IF NOT EXISTS (SELECT 1 FROM dbo.SAN)
BEGIN
    INSERT INTO dbo.SAN (MaSan, TenSan, MaLoaiSan, TrangThai, LoaiSan) VALUES
    ('S01', N'Sân 1', 'LS01', N'Sẵn sàng', N'Trong nhà'),
    ('S02', N'Sân 2', 'LS01', N'Sẵn sàng', N'Trong nhà'),
    ('S03', N'Sân 3', 'LS01', N'Bảo trì',  N'Trong nhà'),
    ('S04', N'Sân 4', 'LS02', N'Sẵn sàng', N'Ngoài trời'),
    ('S05', N'Sân 5', 'LS02', N'Sẵn sàng', N'Ngoài trời'),
    ('S06', N'Sân 6', 'LS02', N'Sẵn sàng', N'Ngoài trời'),
    ('S07', N'Sân 7', 'LS03', N'Sẵn sàng', N'Thảm chuyên nghiệp'),
    ('S08', N'Sân 8', 'LS03', N'Sẵn sàng', N'Thảm chuyên nghiệp');
END

/* DICH_VU */
IF NOT EXISTS (SELECT 1 FROM dbo.DICH_VU)
BEGIN
    -- Base schema
    INSERT INTO dbo.DICH_VU (MaDV, TenDV, DonViTinh, GiaHienHanh) VALUES
    ('D001', N'Nước suối',       N'Chai', 10000),
    ('D002', N'Nước tăng lực',   N'Lon',  25000),
    ('E001', N'Thuê vợt',        N'Lần',  50000),
    ('E002', N'Thuê giày',       N'Lần',  30000);

    -- If migrations added extra columns, set them too.
    IF COL_LENGTH('dbo.DICH_VU', 'LoaiDV') IS NOT NULL
    BEGIN
        UPDATE dbo.DICH_VU
        SET LoaiDV = CASE WHEN MaDV LIKE 'E%' THEN N'Equipment' ELSE N'Drinks' END;
    END

    IF COL_LENGTH('dbo.DICH_VU', 'SoLuongTon') IS NOT NULL
    BEGIN
        UPDATE dbo.DICH_VU
        SET SoLuongTon =
            CASE
                WHEN MaDV = 'D001' THEN 50
                WHEN MaDV = 'D002' THEN 30
                WHEN MaDV = 'E001' THEN 20
                WHEN MaDV = 'E002' THEN 15
                ELSE 0
            END;
    END
END

/* KHACH_HANG */
IF NOT EXISTS (SELECT 1 FROM dbo.KHACH_HANG)
BEGIN
    INSERT INTO dbo.KHACH_HANG (MaKH, HoTen, SDT, Email, DiemTichLuy, NgayDangKy) VALUES
    ('KH001', N'Nguyễn Văn A', '0901000001', 'a@example.com', 0, GETDATE()),
    ('KH002', N'Trần Thị B',   '0901000002', 'b@example.com', 0, GETDATE()),
    ('KH003', N'Lê Văn C',     '0901000003', 'c@example.com', 0, GETDATE()),
    ('KH004', N'Phạm Thị D',   '0901000004', 'd@example.com', 0, GETDATE()),
    ('KH005', N'Hoàng Văn E',  '0901000005', 'e@example.com', 0, GETDATE());
END

/* NHAN_VIEN */
IF NOT EXISTS (SELECT 1 FROM dbo.NHAN_VIEN)
BEGIN
    INSERT INTO dbo.NHAN_VIEN (MaNV, HoTen, SDT, ChucVu) VALUES
    ('NV001', N'Admin User', '0909000000', N'Quản trị');
END

/* TAI_KHOAN: admin */
IF NOT EXISTS (SELECT 1 FROM dbo.TAI_KHOAN WHERE TenDangNhap = 'admin')
BEGIN
    DECLARE @saltAdmin uniqueidentifier = NEWID();
    DECLARE @passAdmin varchar(100) = 'admin123';
    DECLARE @hashAdmin varbinary(64) = HASHBYTES('SHA2_256', CONVERT(varchar(100), @passAdmin) + CONVERT(varchar(36), @saltAdmin));

    INSERT INTO dbo.TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, MaNV, MaKH, VaiTro, TrangThai)
    VALUES ('admin', @hashAdmin, @saltAdmin, 'NV001', NULL, N'Admin', 1);
END

/* TAI_KHOAN: kh001 */
IF NOT EXISTS (SELECT 1 FROM dbo.TAI_KHOAN WHERE TenDangNhap = 'kh001')
BEGIN
    DECLARE @saltKH uniqueidentifier = NEWID();
    DECLARE @passKH varchar(100) = '123456';
    DECLARE @hashKH varbinary(64) = HASHBYTES('SHA2_256', CONVERT(varchar(100), @passKH) + CONVERT(varchar(36), @saltKH));

    INSERT INTO dbo.TAI_KHOAN (TenDangNhap, MatKhauHash, MuoiSalt, MaNV, MaKH, VaiTro, TrangThai)
    VALUES ('kh001', @hashKH, @saltKH, NULL, 'KH001', N'KhachHang', 1);
END

/* Sample bookings for current week */
DECLARE @pd1 varchar(20) = 'PD' + CONVERT(varchar(8), @weekStart, 112) + '01';
DECLARE @pd2 varchar(20) = 'PD' + CONVERT(varchar(8), DATEADD(day, 1, @weekStart), 112) + '02';
DECLARE @pd3 varchar(20) = 'PD' + CONVERT(varchar(8), DATEADD(day, 3, @weekStart), 112) + '03';
DECLARE @pd4 varchar(20) = 'PD' + CONVERT(varchar(8), DATEADD(day, 5, @weekStart), 112) + '04';

IF NOT EXISTS (SELECT 1 FROM dbo.DAT_SAN WHERE MaPhieuDat IN (@pd1, @pd2, @pd3, @pd4))
BEGIN
    INSERT INTO dbo.DAT_SAN (MaPhieuDat, MaKH, MaNV, NgayLapPhieu, LoaiDat, TrangThai, TongTien) VALUES
    (@pd1, 'KH001', 'NV001', DATEADD(hour, 8, CAST(@weekStart AS datetime)),           N'Lẻ',       N'Nhận sân', 150000),
    (@pd2, 'KH002', 'NV001', DATEADD(hour, 9, CAST(DATEADD(day,1,@weekStart) AS datetime)), N'Lẻ',   N'Chờ',      150000),
    (@pd3, 'KH003', 'NV001', DATEADD(hour, 10, CAST(DATEADD(day,3,@weekStart) AS datetime)), N'Cố định', N'Chờ',   200000),
    (@pd4, 'KH004', 'NV001', DATEADD(hour, 11, CAST(DATEADD(day,5,@weekStart) AS datetime)), N'Lẻ',   N'Hủy',      150000);

    INSERT INTO dbo.CT_DAT_SAN (MaCTDS, MaPhieuDat, MaSan, MaCa, NgaySuDung, GiaLuuTru) VALUES
    ('CT' + @pd1 + '_1', @pd1, 'S01', 'CA09', @weekStart, 150000),
    ('CT' + @pd2 + '_1', @pd2, 'S02', 'CA10', DATEADD(day, 1, @weekStart), 150000),
    ('CT' + @pd3 + '_1', @pd3, 'S07', 'CA13', DATEADD(day, 3, @weekStart), 200000),
    ('CT' + @pd4 + '_1', @pd4, 'S05', 'CA14', DATEADD(day, 5, @weekStart), 150000);
END

PRINT 'Seed completed. Accounts: admin/admin123, kh001/123456';
GO
