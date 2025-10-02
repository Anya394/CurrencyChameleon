namespace CurrencyChameleon
{
    
    public static class FileLogger
    {
        private static readonly object LockObject = new();
        private static readonly string LogDirectory = String.Empty;

        static FileLogger()
        {
            var projectDirectory = FindProjectDirectory(Directory.GetCurrentDirectory());
            LogDirectory = Path.Combine(
                Directory.GetParent(projectDirectory)?.FullName!,
                "Logs",
                "CurrencyBot"
            );

            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            var userInfoPath = Path.Combine(
                LogDirectory,
                "UserInfo"
            );
            if (!Directory.Exists(userInfoPath))
            {
                Directory.CreateDirectory(userInfoPath);
            }
        }

        public static string FindProjectDirectory(string startPath)
        {
            var directory = new DirectoryInfo(startPath);
            while (directory != null)
            {
                if (directory.GetFiles("*.csproj").Length != 0)
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
            return startPath;
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            lock (LockObject)
            {
                var logFile = level == LogLevel.UserInfo ? GetUserInfoFile() : GetCurrentLogFile();
                var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] - {message}";

                try
                {
                    using (var writer = new StreamWriter(logFile, true))
                    {
                        writer.WriteLine(logMessage);
                    }

                    var consoleColor = GetConsoleColor(level);
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = consoleColor;
                    Console.WriteLine(logMessage);
                    Console.ForegroundColor = originalColor;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED TO LOG: {ex.Message}");
                    Console.WriteLine(logMessage);
                }
            }
        }

        private static string GetCurrentLogFile()
        {
            return Path.Combine(LogDirectory, $"bot_{DateTime.Now:yyyy-MM-dd}.txt");
        }

        private static string GetUserInfoFile()
        {
            return Path.Combine(LogDirectory, "UserInfo", $"user_info_{DateTime.Now:yyyy-MM-dd}.txt");
        }

        private static ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Debug => ConsoleColor.Gray,
                _ => ConsoleColor.White
            };
        }

        public static void Info(string message) => Log(message, LogLevel.Info);
        public static void Warning(string message) => Log(message, LogLevel.Warning);
        public static void Error(string message, Exception? ex = null)
        {
            var fullMessage = ex == null ? message : $"{message} - {ex.Message}";
            Log(fullMessage, LogLevel.Error);

            if (ex != null)
            {
                Log($"Stack Trace: {ex.StackTrace}", LogLevel.Error);
            }
        }
        public static void Debug(string message) => Log(message, LogLevel.Debug);
        public static void UserInfo(string message) => Log(message, LogLevel.UserInfo);
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        UserInfo
    }
}
