using System.Runtime.CompilerServices;
using System.Text;

namespace SanShine.SaaS.Core.Common.Core.Utilities;

public static class LogHelper
{
    public static async Task WriteToFileAsync(string msg, string fileName, string folderName, [CallerMemberName] string caller = null)
    {
        try
        {
            string safeFileName = $"{DateTime.Now:dd.MM.yyyy} {SanitizeFileName(fileName)}.txt";

            string baseLogDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            string logDirectory = Path.Combine(baseLogDirectory, DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString(), folderName);

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logFilePath = Path.Combine(logDirectory, safeFileName);
            string separator = "----------";
            string logMessage = $"{Environment.NewLine}{separator}{Environment.NewLine}[{Utilities.GetTimeFormat()}] | [{caller}] | {msg}{Environment.NewLine}{separator}{Environment.NewLine}";

            await File.AppendAllTextAsync(logFilePath, logMessage, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LOGGING ERROR]: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_'); 
        }
        return fileName.Replace(" ", "_"); 
    }
}
