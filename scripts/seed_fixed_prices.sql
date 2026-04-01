-- Seed fixed prices for all court types and shifts
-- Standard (LS01) Fixed: 50,000 VND
-- VIP (LS02) Fixed: 70,000 VND

INSERT INTO dbo.BANG_GIA (MaGia, MaLoaiSan, MaCa, LoaiDat, Gia)
SELECT 
    CONCAT('G', LS.MaLoaiSan, CG.MaCa, 'C'), 
    LS.MaLoaiSan, 
    CG.MaCa, 
    N'Cố định', 
    CASE 
        WHEN LS.MaLoaiSan = 'LS01' THEN 50000 
        ELSE 70000 
    END
FROM dbo.CA_GIO CG
CROSS JOIN dbo.LOAI_SAN LS
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.BANG_GIA BG
    WHERE BG.MaLoaiSan = LS.MaLoaiSan 
      AND BG.MaCa = CG.MaCa 
      AND BG.LoaiDat = N'Cố định'
);

SELECT 'Added ' + CAST(@@ROWCOUNT AS VARCHAR) + ' fixed price entries.' AS Result;
