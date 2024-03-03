using System;
using System.Security.Cryptography;

public class JwtKeyGeneratorProgram
{
    public static void Main()
    {
        using (var random = RandomNumberGenerator.Create())
        {
            byte[] key = new byte[32]; // 256 bits
            random.GetBytes(key);
            string base64Key = Convert.ToBase64String(key);
            Console.WriteLine(base64Key);
        }
    }
}
