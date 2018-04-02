using Microsoft.Extensions.Logging;

namespace SkypeTeamNotificationBot.Utils
{
    public static class LoggerProxy
    {
        private static ILoggerFactory _loggerFactory;

        public static void Init(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public static ILogger<T> Logger<T>()
        {
            return _loggerFactory.CreateLogger<T>();
        }

        public static ILogger Logger(string className)
        {
            return _loggerFactory.CreateLogger(className);
        }
    }
}