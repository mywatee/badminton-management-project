-- Cập nhật dữ liệu DỊCH_VU để khớp với bảng giá thực tế.
-- Lưu ý: Script này tác động trực tiếp lên DB (mặc định: QLSCL). Hãy sao lưu trước khi chạy.

USE [QLSCL];
GO

-- 1) (Tuỳ chọn) Xoá dịch vụ thuê giày nếu còn tồn tại
DELETE FROM DICH_VU
WHERE (LoaiDV = N'Equipment' OR MaDV LIKE 'E%')
  AND TenDV LIKE N'%giày%';
GO

-- 2) Đổi "Ống cầu" thành các sản phẩm ống cầu cụ thể + cập nhật giá
--    - Nếu đã có dịch vụ "Ống cầu" (legacy), đổi tên + giá thành "Ống cầu Hải Yến S70" để giữ nguyên MaDV (tránh vỡ tham chiếu).
--    - Thêm 2 sản phẩm còn lại nếu chưa có.

DECLARE @Category NVARCHAR(50) = N'Equipment';
DECLARE @Unit NVARCHAR(50) = N'Ống';

DECLARE @LegacyId NVARCHAR(50) =
(
    SELECT TOP 1 MaDV
    FROM DICH_VU
    WHERE (LoaiDV = @Category OR MaDV LIKE 'E%')
      AND LTRIM(RTRIM(TenDV)) = N'Ống cầu'
    ORDER BY MaDV
);

IF @LegacyId IS NOT NULL
BEGIN
    UPDATE DICH_VU
    SET TenDV = N'Ống cầu Hải Yến S70',
        DonViTinh = @Unit,
        GiaHienHanh = 300000,
        LoaiDV = @Category
    WHERE MaDV = @LegacyId;
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM DICH_VU WHERE LTRIM(RTRIM(TenDV)) = N'Ống cầu Hải Yến S70')
    BEGIN
        DECLARE @MaxE INT =
        (
            SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(MaDV, 2, 10) AS INT)), 0)
            FROM DICH_VU
            WHERE MaDV LIKE 'E%'
        );
        DECLARE @NewId NVARCHAR(50) = 'E' + RIGHT('000' + CAST(@MaxE + 1 AS VARCHAR(10)), 3);

        INSERT INTO DICH_VU (MaDV, TenDV, DonViTinh, GiaHienHanh, LoaiDV, SoLuongTon)
        VALUES (@NewId, N'Ống cầu Hải Yến S70', @Unit, 300000, @Category, 300);
    END
END
GO

-- Thêm "Ống cầu lông Yonex AS40" nếu chưa có
IF NOT EXISTS (SELECT 1 FROM DICH_VU WHERE LTRIM(RTRIM(TenDV)) = N'Ống cầu lông Yonex AS40')
BEGIN
    DECLARE @MaxE INT =
    (
        SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(MaDV, 2, 10) AS INT)), 0)
        FROM DICH_VU
        WHERE MaDV LIKE 'E%'
    );
    DECLARE @NewId NVARCHAR(50) = 'E' + RIGHT('000' + CAST(@MaxE + 1 AS VARCHAR(10)), 3);

    INSERT INTO DICH_VU (MaDV, TenDV, DonViTinh, GiaHienHanh, LoaiDV, SoLuongTon)
    VALUES (@NewId, N'Ống cầu lông Yonex AS40', N'Ống', 1650000, N'Equipment', 300);
END
GO

-- Thêm "Ống cầu lông Thành Công" nếu chưa có
IF NOT EXISTS (SELECT 1 FROM DICH_VU WHERE LTRIM(RTRIM(TenDV)) = N'Ống cầu lông Thành Công')
BEGIN
    DECLARE @MaxE INT =
    (
        SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(MaDV, 2, 10) AS INT)), 0)
        FROM DICH_VU
        WHERE MaDV LIKE 'E%'
    );
    DECLARE @NewId NVARCHAR(50) = 'E' + RIGHT('000' + CAST(@MaxE + 1 AS VARCHAR(10)), 3);

    INSERT INTO DICH_VU (MaDV, TenDV, DonViTinh, GiaHienHanh, LoaiDV, SoLuongTon)
    VALUES (@NewId, N'Ống cầu lông Thành Công', N'Ống', 335000, N'Equipment', 300);
END
GO


