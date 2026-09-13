using GreenRide;

Console.WriteLine("=== APLIKASI GREENRIDE ===");

// Uji coba class User & hashing password BCrypt
string hash = BCrypt.Net.BCrypt.HashPassword("password123");
Console.WriteLine($"Password terenkripsi: {hash}");

bool isMatch = BCrypt.Net.BCrypt.Verify("password123", hash);
Console.WriteLine($"Apakah password cocok? {isMatch}");