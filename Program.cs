using GreenRide;
using GreenRide.Models;

Console.WriteLine("=== APLIKASI GREENRIDE ===");

// Uji coba class User & hashing password BCrypt
string hash = BCrypt.Net.BCrypt.HashPassword("password123");
Console.WriteLine($"Password terenkripsi: {hash}");

bool isMatch = BCrypt.Net.BCrypt.Verify("password123", hash);
Console.WriteLine($"Apakah password cocok? {isMatch}");


// ===== Uji Homework 9: Enkapsulasi pada TransportMode =====
Console.WriteLine();
Console.WriteLine("=== UJI ENKAPSULASI: TransportMode ===");

// 0.1 hanya angka contoh untuk uji, bukan data resmi
var motor = new TransportMode("Motor", 0.1);
Console.WriteLine($"Moda: {motor.NamaModa}, faktor emisi: {motor.FaktorEmisi} kg CO2/menit");
Console.WriteLine($"Emisi 20 menit: {motor.HitungEmisi(20):F2} kg CO2");

try
{
    motor.FaktorEmisi = -5;   // coba isi angka negatif
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Ditolak: {ex.Message}");
}
Console.WriteLine($"Faktor emisi setelah percobaan: {motor.FaktorEmisi}");
