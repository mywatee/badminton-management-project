using System;
using System.Security.Cryptography;
using System.Text;

namespace QuanLySCL.DAL
{
    public static class SecurityHelper
    {
        /// <summary>
        /// Mã hóa mật khẩu: SHA256(Password + SaltString)
        /// Trả về mảng byte để lưu vào cột VARBINARY
        /// </summary>
        public static byte[] HashPassword(string password, Guid salt)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Ghép mật khẩu và chuỗi Salt lại
                string saltedInput = password + salt.ToString();
                byte[] inputBytes = Encoding.UTF8.GetBytes(saltedInput);
                return sha256.ComputeHash(inputBytes);
            }
        }
    }
}