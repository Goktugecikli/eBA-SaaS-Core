using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace SanShine.SaaS.Core.Common.Core.Utilities
{
    public static class Utilities
    {
        public static string GetTimeFormat()
        {
            return $"{DateTime.Now.TimeOfDay.Hours}:{DateTime.Now.TimeOfDay.Minutes}:{DateTime.Now.TimeOfDay.Seconds}";
        }
        public static string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }


            // Convert the input string to lowercase using Turkish culture
            input = input.ToLower(new CultureInfo("tr-TR", false));

            // Convert to title case using Turkish culture
            TextInfo textInfo = new CultureInfo("tr-TR", false).TextInfo;

            // Convert to title case
            return textInfo.ToTitleCase(input);
        }
        public static Microsoft.Extensions.Configuration.IConfiguration GetConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var settingsName = "appsettings.json";
            if (environment == "Development")
                settingsName = "appsettings.Development.json";

            return new ConfigurationBuilder()
                 .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                 .AddJsonFile(settingsName)
                 .Build();
        }
        public static string NormalizeToEnglish(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return input
                .Replace("Ç", "C")
                .Replace("Ğ", "G")
                .Replace("İ", "I")
                .Replace("Ö", "O")
                .Replace("Ş", "S")
                .Replace("Ü", "U")
                .Replace("ç", "c")
                .Replace("ğ", "g")
                .Replace("ı", "i")
                .Replace("ö", "o")
                .Replace("ş", "s")
                .Replace("ü", "u");
        }

        public static string AddCountryCode(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return "";
            }
            if (phoneNumber.Length == 10)
            {
                return "+90" + phoneNumber;
            }
            else if (phoneNumber.Length == 11)
            {
                return "+9" + phoneNumber;
            }
            else if (phoneNumber.Length == 12)
            {
                return "+" + phoneNumber;
            }
            return phoneNumber;
        }
    }
}
