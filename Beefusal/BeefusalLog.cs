using Serilog;
using Serilog.Core;

namespace Beefusal
{
    internal static class BeefusalLog
    {
        public static readonly ILogger ReadableLogger;
        public static ILogger SentryLogger;

        static BeefusalLog()
        {
            ReadableLogger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "{Message:lj}{NewLine}")
                .CreateLogger();
        }

        public static void InitializeSentry(Config config)
        {
            if (config.SentryDsn != null)
            {
                SentryLogger = new LoggerConfiguration()
                    .WriteTo.Sentry(options =>
                    {
                        options.Dsn = new Sentry.Dsn(config.SentryDsn);
                    })
                    .CreateLogger();
            }
            else
            {
                SentryLogger = Logger.None;
            }
        }
    }
}
