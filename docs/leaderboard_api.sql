-- Endpoint leaderboard (RPC Supabase / PostgREST).
-- Dipanggil aplikasi lewat HTTP: POST {SUPABASE_URL}/rest/v1/rpc/leaderboard_minggu_ini
-- Hanya mengembalikan peringkat, nama, dan total emisi. Tidak membuka tabel apa pun
-- dan tidak mengembalikan id atau hash password pengguna.

CREATE OR REPLACE FUNCTION public.leaderboard_minggu_ini(p_awal date, p_user_id uuid DEFAULT NULL)
RETURNS TABLE (peringkat integer, nama text, total numeric, is_current_user boolean)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
    SELECT ROW_NUMBER() OVER (ORDER BY COALESCE(SUM(tl.emisi_kg), 0), u.nama)::integer AS peringkat,
           u.nama::text,
           COALESCE(SUM(tl.emisi_kg), 0)::numeric AS total,
           (u.id = p_user_id) AS is_current_user
    FROM users u
    LEFT JOIN trip_logs tl
           ON tl.user_id = u.id
          AND tl.waktu_selesai IS NOT NULL
          AND tl.tanggal BETWEEN p_awal AND p_awal + 6
    WHERE ((u.created_at AT TIME ZONE 'UTC') AT TIME ZONE 'Asia/Jakarta') < p_awal
    GROUP BY u.id, u.nama
    ORDER BY total, u.nama;
$$;

-- Hanya fungsi ini yang boleh dipanggil lewat anon key.
REVOKE ALL ON FUNCTION public.leaderboard_minggu_ini(date, uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION public.leaderboard_minggu_ini(date, uuid) TO anon;

-- Cegah tabel dibaca langsung lewat REST dengan anon key (aplikasi memakai Npgsql, tidak terpengaruh).
ALTER TABLE users           ENABLE ROW LEVEL SECURITY;
ALTER TABLE trip_logs       ENABLE ROW LEVEL SECURITY;
ALTER TABLE weekly_badges   ENABLE ROW LEVEL SECURITY;
ALTER TABLE transport_modes ENABLE ROW LEVEL SECURITY;

-- Akun aplikasi (greenride_app) dan koneksi utama tetap boleh membaca/menulis seperti biasa.
-- Tanpa kebijakan ini, RLS akan menyembunyikan semua baris dari greenride_app.
CREATE POLICY app_semua ON users           FOR ALL TO greenride_app USING (true) WITH CHECK (true);
CREATE POLICY app_semua ON trip_logs       FOR ALL TO greenride_app USING (true) WITH CHECK (true);
CREATE POLICY app_semua ON weekly_badges   FOR ALL TO greenride_app USING (true) WITH CHECK (true);
CREATE POLICY app_semua ON transport_modes FOR ALL TO greenride_app USING (true) WITH CHECK (true);
