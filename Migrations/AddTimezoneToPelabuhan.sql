-- Migration: Add timezone column to Pelabuhan table
-- Date: 2025-11-06
-- Description: Add timezone support for Indonesian time zones (WIB, WITA, WIT)

-- Step 1: Add timezone column with default value
ALTER TABLE "Pelabuhan" 
ADD COLUMN IF NOT EXISTS timezone VARCHAR(10) NOT NULL DEFAULT 'WIB';

-- Step 2: Update existing data based on location
-- WIB (UTC+7): Sumatera, Jawa, Kalimantan Barat, Kalimantan Tengah
UPDATE "Pelabuhan" 
SET timezone = 'WIB' 
WHERE provinsi IN ('Lampung', 'Banten', 'Jawa Timur', 'Bali', 'Kalimantan Barat')
   OR kota IN ('Bakauheni', 'Merak', 'Ketapang', 'Gilimanuk');

-- WITA (UTC+8): Kalimantan Timur, Kalimantan Selatan, Sulawesi, Bali, NTB, NTT
UPDATE "Pelabuhan" 
SET timezone = 'WITA' 
WHERE provinsi IN ('Kalimantan Selatan', 'Nusa Tenggara Barat', 'Sulawesi Selatan', 'Bali')
   OR kota IN ('Lembar', 'Gilimanuk');

-- WIT (UTC+9): Maluku, Papua
UPDATE "Pelabuhan" 
SET timezone = 'WIT' 
WHERE provinsi IN ('Maluku', 'Papua', 'Papua Barat');

-- Step 3: Add comment to column
COMMENT ON COLUMN "Pelabuhan".timezone IS 'Indonesian timezone: WIB (UTC+7), WITA (UTC+8), WIT (UTC+9)';

-- Verify changes
SELECT pelabuhan_id, nama_pelabuhan, kota, provinsi, timezone 
FROM "Pelabuhan" 
ORDER BY pelabuhan_id;
