/*
  Seed / upsert 30-minute time slots into CA_GIO and inferred prices into BANG_GIA.

  What it does:
    - Generates slots from @OpenTime to @CloseTime in @StepMinutes (default 30).
    - Marks peak slots if @PeakStart/@PeakEnd provided (optional).
    - Upserts CA_GIO (insert missing; update existing only when @Overwrite = 1).
    - Upserts BANG_GIA for every LOAI_SAN and LoaiDat in (N'Lẻ', N'Cố định'):
        * Infers price by looking for an existing price entry for same court type + booking type,
          preferring slots with the same peak flag and duration closest to 60 minutes.
        * Computes per-minute price * @StepMinutes, rounded to nearest 1,000 VND.
        * Inserts missing rules; updates existing only when @Overwrite = 1.

  Requirements:
    - Database: QLSCL (or run inside your DB context).
    - Existing BANG_GIA should have at least some seed prices; otherwise inference may return NULL and will skip.
*/

SET NOCOUNT ON;

-- Avoid encoding issues with Vietnamese diacritics by building strings via Unicode code points.
DECLARE @LoaiDatLe nvarchar(20) = NCHAR(76) + NCHAR(7867); -- "Lẻ"
DECLARE @LoaiDatCoDinh nvarchar(20) = NCHAR(67) + NCHAR(7889) + NCHAR(32) + NCHAR(273) + NCHAR(7883) + NCHAR(110) + NCHAR(104); -- "Cố định"

DECLARE @OpenTime  time(0) = '06:00';
DECLARE @CloseTime time(0) = '22:00';
DECLARE @StepMinutes int = 30;

-- Optional peak window. Set to NULL to disable peak marking.
DECLARE @PeakStart time(0) = '18:00';
DECLARE @PeakEnd   time(0) = '21:00';

DECLARE @Overwrite bit = 0; -- 0 = only insert missing, 1 = update existing rows too

IF (@StepMinutes <= 0)
BEGIN
    RAISERROR(N'@StepMinutes must be > 0', 16, 1);
    RETURN;
END

IF (@CloseTime <= @OpenTime)
BEGIN
    RAISERROR(N'Close time must be after open time', 16, 1);
    RETURN;
END

IF (@PeakStart IS NOT NULL OR @PeakEnd IS NOT NULL)
BEGIN
    IF (@PeakStart IS NULL OR @PeakEnd IS NULL)
    BEGIN
        RAISERROR(N'PeakStart/PeakEnd must both be NULL or both have values', 16, 1);
        RETURN;
    END
    IF (@PeakEnd <= @PeakStart)
    BEGIN
        RAISERROR(N'PeakEnd must be after PeakStart', 16, 1);
        RETURN;
    END
END

DECLARE @Slots TABLE
(
    SlotId varchar(10) NOT NULL,
    SlotName nvarchar(50) NOT NULL,
    StartTime time(0) NOT NULL,
    EndTime time(0) NOT NULL,
    IsPeak bit NOT NULL
);

DECLARE @t time(0) = @OpenTime;
WHILE (DATEADD(minute, @StepMinutes, CAST(@t AS datetime2)) <= CAST(@CloseTime AS datetime2))
BEGIN
    DECLARE @end time(0) = CAST(DATEADD(minute, @StepMinutes, CAST(@t AS datetime2)) AS time(0));
    DECLARE @isPeakSlot bit = 0;
    IF (@PeakStart IS NOT NULL AND @PeakEnd IS NOT NULL AND @t >= @PeakStart AND @t < @PeakEnd)
        SET @isPeakSlot = 1;

    DECLARE @id varchar(10) = 'CA' + REPLACE(CONVERT(varchar(5), @t, 108), ':', '');
    DECLARE @name nvarchar(50) = CONVERT(nvarchar(5), @t, 108) + N' - ' + CONVERT(nvarchar(5), @end, 108);

    INSERT INTO @Slots (SlotId, SlotName, StartTime, EndTime, IsPeak)
    VALUES (@id, @name, @t, @end, @isPeakSlot);

    SET @t = @end;
END

DECLARE @SlotsInserted int = 0, @SlotsUpdated int = 0;

-- Upsert CA_GIO
;WITH s AS (SELECT * FROM @Slots)
INSERT INTO CA_GIO (MaCa, TenCa, GioBatDau, GioKetThuc, LaKhungGioVang)
SELECT s.SlotId, s.SlotName, s.StartTime, s.EndTime, s.IsPeak
FROM s
WHERE NOT EXISTS (SELECT 1 FROM CA_GIO cg WHERE cg.MaCa = s.SlotId);

SET @SlotsInserted = @@ROWCOUNT;

IF (@Overwrite = 1)
BEGIN
    UPDATE cg
    SET cg.TenCa = s.SlotName,
        cg.GioBatDau = s.StartTime,
        cg.GioKetThuc = s.EndTime,
        cg.LaKhungGioVang = s.IsPeak
    FROM CA_GIO cg
    INNER JOIN @Slots s ON s.SlotId = cg.MaCa;

    SET @SlotsUpdated = @@ROWCOUNT;
END

DECLARE @PricesInserted int = 0, @PricesUpdated int = 0, @PricesSkipped int = 0;

-- Upsert BANG_GIA inferred rules
DECLARE @CourtTypes TABLE (CourtTypeId varchar(10) NOT NULL);
INSERT INTO @CourtTypes(CourtTypeId)
SELECT MaLoaiSan FROM LOAI_SAN;

DECLARE @BookingTypes TABLE (LoaiDat nvarchar(20) NOT NULL);
INSERT INTO @BookingTypes(LoaiDat) VALUES (@LoaiDatLe), (@LoaiDatCoDinh);

DECLARE @ct varchar(10), @bt nvarchar(20), @slotId varchar(10), @isPeak bit;

DECLARE court_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT ct.CourtTypeId, bt.LoaiDat, s.SlotId, s.IsPeak
FROM @CourtTypes ct
CROSS JOIN @BookingTypes bt
CROSS JOIN @Slots s
ORDER BY ct.CourtTypeId, bt.LoaiDat, s.StartTime;

OPEN court_cursor;
FETCH NEXT FROM court_cursor INTO @ct, @bt, @slotId, @isPeak;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @existingId varchar(20) = NULL;
    SELECT @existingId = MaGia
    FROM BANG_GIA
    WHERE MaLoaiSan = @ct AND MaCa = @slotId AND LoaiDat = @bt;

    IF (@existingId IS NOT NULL AND @Overwrite = 0)
    BEGIN
        SET @PricesSkipped += 1;
        FETCH NEXT FROM court_cursor INTO @ct, @bt, @slotId, @isPeak;
        CONTINUE;
    END

    DECLARE @basePrice decimal(18,6) = NULL;
    DECLARE @baseMinutes int = NULL;

    -- Prefer same peak flag
    SELECT TOP 1
        @basePrice = CAST(bg.Gia AS decimal(18,6)),
        @baseMinutes = DATEDIFF(minute, cg.GioBatDau, cg.GioKetThuc)
    FROM BANG_GIA bg
    INNER JOIN CA_GIO cg ON cg.MaCa = bg.MaCa
    WHERE bg.MaLoaiSan = @ct
      AND bg.LoaiDat = @bt
      AND cg.LaKhungGioVang = @isPeak
      AND DATEDIFF(minute, cg.GioBatDau, cg.GioKetThuc) > 0
    ORDER BY ABS(DATEDIFF(minute, cg.GioBatDau, cg.GioKetThuc) - 60), cg.GioBatDau;

    -- Relax peak flag if needed
    IF (@basePrice IS NULL OR @baseMinutes IS NULL)
    BEGIN
        SELECT TOP 1
            @basePrice = CAST(bg.Gia AS decimal(18,6)),
            @baseMinutes = DATEDIFF(minute, cg.GioBatDau, cg.GioKetThuc)
        FROM BANG_GIA bg
        INNER JOIN CA_GIO cg ON cg.MaCa = bg.MaCa
        WHERE bg.MaLoaiSan = @ct
          AND bg.LoaiDat = @bt
          AND DATEDIFF(minute, cg.GioBatDau, cg.GioKetThuc) > 0
        ORDER BY ABS(DATEDIFF(minute, cg.GioBatDau, cg.GioKetThuc) - 60), cg.GioBatDau;
    END

    IF (@basePrice IS NULL OR @baseMinutes IS NULL OR @baseMinutes <= 0)
    BEGIN
        -- No base data to infer from
        SET @PricesSkipped += 1;
        FETCH NEXT FROM court_cursor INTO @ct, @bt, @slotId, @isPeak;
        CONTINUE;
    END

    DECLARE @perMinute decimal(18,6) = @basePrice / @baseMinutes;
    DECLARE @raw decimal(18,6) = @perMinute * @StepMinutes;
    DECLARE @inferred decimal(18,0) = CAST(ROUND(@raw / 1000.0, 0) * 1000 AS decimal(18,0));

    IF (@existingId IS NULL)
    BEGIN
        DECLARE @newId varchar(20) = 'G' + RIGHT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 19);
        INSERT INTO BANG_GIA (MaGia, MaLoaiSan, MaCa, LoaiDat, Gia)
        VALUES (@newId, @ct, @slotId, @bt, @inferred);
        SET @PricesInserted += 1;
    END
    ELSE
    BEGIN
        UPDATE BANG_GIA
        SET Gia = @inferred
        WHERE MaGia = @existingId;
        SET @PricesUpdated += 1;
    END

    FETCH NEXT FROM court_cursor INTO @ct, @bt, @slotId, @isPeak;
END

CLOSE court_cursor;
DEALLOCATE court_cursor;

PRINT N'CA_GIO: inserted=' + CAST(@SlotsInserted AS nvarchar(20)) + N', updated=' + CAST(@SlotsUpdated AS nvarchar(20));
PRINT N'BANG_GIA: inserted=' + CAST(@PricesInserted AS nvarchar(20)) + N', updated=' + CAST(@PricesUpdated AS nvarchar(20)) + N', skipped=' + CAST(@PricesSkipped AS nvarchar(20));
