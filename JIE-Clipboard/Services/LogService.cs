namespace JIE剪切板.Services;

/// <summary>
/// 日志服务。提供统一的日志记录功能，将错误信息写入文件以便排查问题。
/// 日志文件位于 %AppData%\JIE剪切板\Logs\ 目录，每天一个文件，自动清理 7 天前的日志。
/// </summary>
public static class LogService
{
    /// <summary>日志文件存储目录</summary>
    private static readonly string _logFolder;

    /// <summary>文件写入锁，确保多线程安全</summary>
    private static readonly object _lock = new();

    static LogService()
    {
        _logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "JIE剪切板", "Logs");
    }

    /// <summary>初始化日志服务：创建目录并清理过期日志</summary>
    public static void Initialize()
    {
        try
        {
            if (!Directory.Exists(_logFolder))
                Directory.CreateDirectory(_logFolder);
            CleanOldLogs();
        }
        catch { /* 日志初始化失败不影响程序运行 */ }
    }

    /// <summary>
    /// 记录一条日志。
    /// </summary>
    /// <param name="message">日志消息（会自动脱敏用户路径）</param>
    /// <param name="ex">关联的异常对象（可选）</param>
    public static void Log(string message, Exception? ex = null)
    {
        try
        {
            lock (_lock) // 加锁防止多线程同时写入文件
            {
                var logFile = Path.Combine(_logFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");
                var sb = new StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {SanitizeLogMessage(message)}");
                if (ex != null)
                {
                    sb.AppendLine($"  Type: {ex.GetType().FullName}");      // 异常类型
                    sb.AppendLine($"  Message: {SanitizeLogMessage(ex.Message)}"); // 异常消息
                    sb.AppendLine($"  Stack: {ex.StackTrace}");              // 调用栈
                }
                File.AppendAllText(logFile, sb.ToString());
            }
        }
        catch { /* 日志记录本身不能抛异常，否则可能引起无限循环 */ }
    }

    /// <summary>
    /// 脱敏日志消息：将各类敏感路径替换为环境变量占位符，防止泄露用户名等隐私信息。
    /// 替换顺序：长路径优先（USERPROFILE 包含 APPDATA），避免部分替换。
    /// </summary>
    private static string SanitizeLogMessage(string message)
    {
        // 按路径长度降序替换，避免短路径先匹配导致长路径替换不完整
        ReadOnlySpan<(Environment.SpecialFolder folder, string token)> mappings =
        [
            (Environment.SpecialFolder.LocalApplicationData, "%LOCALAPPDATA%"),
            (Environment.SpecialFolder.ApplicationData, "%APPDATA%"),
            (Environment.SpecialFolder.UserProfile, "%USERPROFILE%"),
        ];
        foreach (var (folder, token) in mappings)
        {
            var path = Environment.GetFolderPath(folder);
            if (!string.IsNullOrEmpty(path))
                message = message.Replace(path, token);
        }

        // 脱敏临时目录
        var temp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        if (!string.IsNullOrEmpty(temp))
            message = message.Replace(temp, "%TEMP%");

        return message;
    }

    /// <summary>清理 7 天前的旧日志文件</summary>
    private static void CleanOldLogs()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_logFolder, "log_*.txt"))
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-7))
                    File.Delete(file);
            }
        }
        catch { /* 日志清理失败不影响程序运行 */ }
    }
}
