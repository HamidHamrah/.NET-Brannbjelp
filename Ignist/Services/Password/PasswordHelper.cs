using System;
using Ignist.Models;
using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace Ignist.Data
{
    public class PasswordHelper
    {
        private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

        // Denne metoden sjekker om passordet oppfyller de angitte kravene
        public bool ValidatePassword(string password)
        {
            // Sjekk om passordet er minst 6 tegn langt
            if (password.Length < 6)
            {
                Console.WriteLine("Password must be at least 6 characters long.");
                return false;
            }

            // Sjekk om passordet inneholder minst én stor bokstav
            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                Console.WriteLine("Password must contain at least one uppercase letter.");
                return false;
            }

            return true;
        }

        public string HashPassword(string password)
        {
            if (!ValidatePassword(password))
            {
                throw new ArgumentException("Password does not meet complexity requirements.");
            }

            var hashedPassword = _passwordHasher.HashPassword(null, password);
            Console.WriteLine($"Hashed password: {hashedPassword}");
            return hashedPassword;
        }

        public PasswordVerificationResult VerifyPassword(string hashedPassword, string providedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
            Console.WriteLine($"Password verification result: {result}");
            return result;
        }
    }
}
