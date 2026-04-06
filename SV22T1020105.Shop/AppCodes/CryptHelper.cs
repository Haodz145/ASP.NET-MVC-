using System.Security.Cryptography;
using System.Text;

namespace SV22T1020105.Shop
{
    public static class CryptHelper
    {
        public static string HashMD5(string input)
        {
            using var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
