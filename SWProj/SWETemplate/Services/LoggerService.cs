// using System.IO;

// namespace SWETemplate.Services
// {
//     public interface ILoggerService
//     {
//         void LogInformation(string message);
//         void LogWarning(string message);
//         void LogError(string message, Exception ex = null);
//     }

//     public class LoggerService : ILoggerService
//     {
//         private readonly string _logDirectory;

//         public LoggerService()
//         {
//             // Kreiraj folder za logove u osnovnom direktorijumu aplikacije
//             _logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
            
//             // Proveri da li folder postoji, ako ne postoji - kreiraj ga
//             if (!Directory.Exists(_logDirectory))
//             {
//                 Directory.CreateDirectory(_logDirectory);
//             }
//         }

//         public void LogInformation(string message)
//         {
//             Log("INFO", message);
//         }

//         public void LogWarning(string message)
//         {
//             Log("WARNING", message);
//         }

//         public void LogError(string message, Exception ex = null)
//         {
//             string fullMessage = ex != null ? $"{message} - Exception: {ex.Message}" : message;
//             Log("ERROR", fullMessage);
//         }

//         private void Log(string level, string message)
//         {
//             try
//             {
//                 // Kreiraj ime fajla na osnovu trenutnog datuma
//                 string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
//                 string filePath = Path.Combine(_logDirectory, fileName);
                
//                 // Formatiraj poruku za log
//                 string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
                
//                 // Upisi u fajl (dodaj na kraj postojećeg sadržaja)
//                 File.AppendAllText(filePath, logMessage);
//             }
//             catch (Exception ex)
//             {
//                 // Ako nešto pođe po zlu sa upisom u fajl, ispiši grešku u konzolu
//                 Console.WriteLine($"Greška pri upisu u log fajl: {ex.Message}");
//             }
//         }
//     }
// }