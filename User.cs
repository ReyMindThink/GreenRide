using BCrypt.Net;
using System;

namespace GreenRide.Models
{
    public class User
    {
        public int UserId { get; private set; }
        public string Nama { get; set; }
        public string Email { get; private set; }
        private string PasswordHash { get; set; }

        // Konstruktor untuk registrasi user baru
        public User(string nama, string email, string password)
        {
            Nama = nama;
            Email = email;
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Konstruktor untuk memuat user yang sudah ada dari database
        public User(int userId, string nama, string email, string passwordHash)
        {
            UserId = userId;
            Nama = nama;
            Email = email;
            PasswordHash = passwordHash;
        }

        public void SetUserId(int id)
        {
            UserId = id;
        }

        // Dipanggil setelah User diambil dari database berdasarkan Email,
        // lalu cek apakah password sesuai dengan hash
        public bool Login(string passwordInput)
        {
            return BCrypt.Net.BCrypt.Verify(passwordInput, PasswordHash);
        }

        public void Logout()
        {
        }
    }
}
