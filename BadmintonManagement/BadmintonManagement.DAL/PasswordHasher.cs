using System;
using System.Security.Cryptography;
using System.Text;

namespace BadmintonManagement.DAL
{
    public class PasswordHasher
    {
        public static byte[] HashPasswordWithSalt(string password, Guid salt)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Kết hợp mật khẩu và Salt (đúng theo cấu trúc file test01.sql của bạn)
                string saltedPassword = password + salt.ToString();
                return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            }
        }
    }
}