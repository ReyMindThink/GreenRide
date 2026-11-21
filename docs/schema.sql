-- =============================================================
-- GreenRide — Database Schema v1
-- Jalankan file ini di Supabase SQL Editor
-- =============================================================

CREATE TABLE IF NOT EXISTS users (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    nama            VARCHAR(100)  NOT NULL,
    email           VARCHAR(100)  NOT NULL UNIQUE,
    password_hash   VARCHAR(255)  NOT NULL,
    created_at      TIMESTAMP     DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS transport_modes (
    id              INT           PRIMARY KEY,
    nama            VARCHAR(50)   NOT NULL,
    faktor_emisi    DECIMAL(6,4)  NOT NULL
);

INSERT INTO transport_modes (id, nama, faktor_emisi) VALUES
    (1, 'Motor',           0.0270),
    (2, 'Mobil',           0.0720),
    (3, 'Bus Trans Jogja', 0.0430),
    (4, 'Kereta Api',      0.0160)
ON CONFLICT (id) DO NOTHING;

CREATE TABLE IF NOT EXISTS trip_logs (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID          NOT NULL REFERENCES users(id),
    moda_id         INT           NOT NULL REFERENCES transport_modes(id),
    tanggal         DATE          NOT NULL,
    waktu_mulai     TIMESTAMP     NOT NULL,
    waktu_selesai   TIMESTAMP,
    durasi_menit    DECIMAL(8,2),
    emisi_kg        DECIMAL(8,4),
    is_revised      BOOLEAN       DEFAULT FALSE,
    created_at      TIMESTAMP     DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_active_trip
    ON trip_logs (user_id)
    WHERE waktu_selesai IS NULL;

CREATE TABLE IF NOT EXISTS weekly_badges (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID          NOT NULL REFERENCES users(id),
    minggu_mulai    DATE          NOT NULL,
    minggu_selesai  DATE          NOT NULL,
    total_emisi_kg  DECIMAL(8,4)  NOT NULL,
    badge_name      VARCHAR(50),
    hari_tercatat   INT           NOT NULL,
    created_at      TIMESTAMP     DEFAULT NOW(),
    UNIQUE (user_id, minggu_mulai)
);
