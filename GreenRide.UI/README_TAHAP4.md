# GreenRide.UI, tahap 4

Ekstrak menimpa folder `GreenRide.UI/` dari tahap 3, lalu:

    cd GreenRide.UI
    dotnet run

## Akun demo (password semuanya `123456`)

| Email | Kondisinya |
|---|---|
| john@example.com | Pengguna aktif. Riwayat 13 minggu, badge Eco Initiate, Carbon Tamer, dan Terra Sentinel (Zero Trace masih terkunci). Minggu lalu Carbon Tamer. 3 perjalanan hari ini. |
| sari@example.com | Pengguna ramah lingkungan. Keempat badge terkumpul, minggu lalu Zero Trace. 2 perjalanan kereta hari ini. |
| budi@example.com | Pengguna baru. Minggu lalu hanya 4 dari 7 hari (tanpa badge), dan ada timer Mobil yang sudah berjalan 12 menit. |

Akun baru dari Sign Up tetap kosong, berguna untuk melihat tampilan "belum ada data".
Daftar akun demo tidak ditampilkan di aplikasi (lihat `MockGreenRideBackend.DemoAccounts`).
Data diseed relatif terhadap tanggal hari ini, jadi selalu terlihat "baru dipakai", dan semua hilang saat app ditutup (backend masih mock).

## Yang berubah

- Catat: tata letak responsif (satu kolom di bawah lebar 760 px: timer, badge minggu lalu, koleksi badge).
- Ringkasan (baru): grafik emisi mingguan, kalender emisi (klik tanggal untuk melihat perjalanannya), tabel perjalanan, pemilih bulan.
- Revisi Perjalanan (baru): dibuka dari tombol Revisi di tabel Ringkasan (hanya perjalanan hari ini).
- Data dummy: 3 akun di `Services/Mock/MockGreenRideBackend.cs`.
- File baru: `Helpers/Responsive.cs`, `Helpers/TimeInput.cs`, `Styles/Summary.axaml`, `ViewModels/RingkasanViewModel.cs`, `ViewModels/RevisiViewModel.cs`, `Views/RingkasanView.*`, `Views/RevisiView.*`.
- Splash tetap 1,5 detik.

## Tahap 5
Leaderboard: podium top 3 (2-1-3), tabel peringkat 4-10, baris pengguna ditandai "Kamu" (tetap tampil bila di luar 10 besar). Responsif di bawah 760 px.
