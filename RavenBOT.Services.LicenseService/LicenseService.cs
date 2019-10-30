using System;

namespace RavenBOT.Common.LicenseService
{
    public class LicenseService
    {
        private static Random Random { get; } = new Random();

        public enum RedemptionResult
        {
            AlreadyClaimed,
            InvalidKey,
            Success
        }
        public static string GenerateRandomNo()
        {
            return Random.Next(0, 9999).ToString("D4");
        }
    }
}
