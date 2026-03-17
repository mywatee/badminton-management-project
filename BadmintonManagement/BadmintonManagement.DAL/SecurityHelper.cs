using System;
using System.Security.Cryptography;
using System.Text;

namespace BadmintonManagement.DAL
{
    public class SecurityHelper
    {
        // Hàm băm mật khẩu chuẩn SHA-256 kèm Salt (uniqueidentifier)
        public static byte[] HashPasswordWithSalt(string password, Guid salt)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Công thức: Mật khẩu + Chuỗi Salt
                string saltedPassword = password + salt.ToString();
                return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            }
        }
    }
}