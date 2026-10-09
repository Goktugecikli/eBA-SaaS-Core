using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace SanShine.SaaS.Core.Application.Services
{
    public interface IPasswordHashService
    {
        string HashPassword(string PasswordToHash);
        bool VerifyPassword(string Password, string HashedPassword);
    }

    public class PasswordHashService : IPasswordHashService
    {
        public string HashPassword(string PasswordToHash)
        {
            var CryptoStrength = 12;
            var DerivationPrf = KeyDerivationPrf.HMACSHA256;
            var Randomized = RandomNumberGenerator.Create();
            var IterationCount = CryptoStrength * 1000;
            const int SaltSize = 128 / 8;
            const int NumBytesRequested = 256 / 8;

            // Produce a version 3 (see comment above) text hash.
            var Salty = new byte[SaltSize];
            Randomized.GetBytes(Salty);
            var subkey = KeyDerivation.Pbkdf2(PasswordToHash, Salty, DerivationPrf, IterationCount, NumBytesRequested);

            var OutputBytes = new byte[13 + Salty.Length + subkey.Length];
            OutputBytes[0] = 0x01; // format marker
            WriteNetworkByteOrder(OutputBytes, 1, (uint)DerivationPrf);
            WriteNetworkByteOrder(OutputBytes, 5, (uint)IterationCount);
            WriteNetworkByteOrder(OutputBytes, 9, SaltSize);
            Buffer.BlockCopy(Salty, 0, OutputBytes, 13, Salty.Length);
            Buffer.BlockCopy(subkey, 0, OutputBytes, 13 + SaltSize, subkey.Length);
            return Convert.ToBase64String(OutputBytes);
        }

        public bool VerifyPassword(string Password, string HashedPassword)
        {
            var decodedHashedPassword = Convert.FromBase64String(HashedPassword);

            // Wrong version
            if (decodedHashedPassword[0] != 0x01)
                return false;

            // Read header information
            var prf = (KeyDerivationPrf)ReadNetworkByteOrder(decodedHashedPassword, 1);
            var iterCount = (int)ReadNetworkByteOrder(decodedHashedPassword, 5);
            var saltLength = (int)ReadNetworkByteOrder(decodedHashedPassword, 9);

            // Read the salt: must be >= 128 bits
            if (saltLength < 128 / 8) return false;
            var salt = new byte[saltLength];
            Buffer.BlockCopy(decodedHashedPassword, 13, salt, 0, salt.Length);

            // Read the subkey (the rest of the payload): must be >= 128 bits
            var subkeyLength = decodedHashedPassword.Length - 13 - salt.Length;
            if (subkeyLength < 128 / 8) return false;
            var expectedSubkey = new byte[subkeyLength];
            Buffer.BlockCopy(decodedHashedPassword, 13 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

            // Hash the incoming password and verify it
            var actualSubkey = KeyDerivation.Pbkdf2(Password, salt, prf, iterCount, subkeyLength);
            return actualSubkey.SequenceEqual(expectedSubkey);
        }

        private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)(value >> 0);
        }

        private static uint ReadNetworkByteOrder(byte[] buffer, int offset)
        {
            return (uint)buffer[offset + 0] << 24
                   | (uint)buffer[offset + 1] << 16
                   | (uint)buffer[offset + 2] << 8
                   | buffer[offset + 3];
        }
    }
}
