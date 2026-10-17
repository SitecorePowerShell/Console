using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using log4net;
using Sitecore.Configuration;
using Sitecore.Diagnostics;

namespace Spe.Core.Diagnostics
{
    public class PowerShellLog
    {
        /*

        Loggers may be assigned levels.
        Levels are instances of the log4net.Core.Level class.
        The following levels are defined in order of increasing priority:

        ALL
        DEBUG
        INFO
        WARN
        ERROR
        FATAL
        OFF

        */

        private static readonly string messagePrefix = string.Empty;
        private static readonly ILog Log;
        private static readonly bool UseJsonFormat;
        private static readonly Regex CategoryPattern = new Regex(@"^\[([^\]]+)\]\s*(.*)$", RegexOptions.Compiled);
        private static readonly Regex KeyValuePattern = new Regex(@"(\w+)=(\S+)", RegexOptions.Compiled);

        static PowerShellLog()
        {
            Log = LogManager.GetLogger("Spe.Diagnostics");

            if (Factory.GetConfigNode("log4net/appender[@name='PowerShellExtensionsFileAppender']") == null)
            {
                // no own appender - prefix with [SPE]
                messagePrefix = "[SPE] ";
            }

            if (Log == null)
            {
                Log = LoggerFactory.GetLogger(typeof(PowerShellLog));
            }

            var logFormat = Sitecore.Configuration.Settings.GetSetting("Spe.LogFormat", "keyvalue");
            UseJsonFormat = string.Equals(logFormat, "json", StringComparison.OrdinalIgnoreCase);
        }

        public static void Audit(string message, params object[] parameters)
        {
            Assert.ArgumentNotNull(message, "message");
            if (parameters?.Length > 0)
            {
                message = string.Format(message, parameters);
            }

            var user = Sitecore.Context.User?.Name ?? "unknown user";

            if (UseJsonFormat)
            {
                var json = ToJson(message);
                json["level"] = "audit";
                if (!json.ContainsKey("user"))
                {
                    json["user"] = user;
                }
                Log.Info(new JavaScriptSerializer().Serialize(json));
            }
            else
            {
                Log.Info($"{messagePrefix}AUDIT ({user}) {message}");
            }
        }

        public static void Debug(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            Assert.ArgumentNotNull(message, "message");
            if (exception == null)
            {
                Log.Debug(FormatMessage(message, "debug"));
            }
            else
            {
                Log.Debug(FormatMessage(message, "debug"), exception);
            }
        }

        private static string FormatMessage(string message, string level = null)
        {
            if (UseJsonFormat)
            {
                var json = ToJson(message);
                if (level != null)
                {
                    json["level"] = level;
                }
                return new JavaScriptSerializer().Serialize(json);
            }

            return messagePrefix != string.Empty ? messagePrefix + message : message;
        }

        internal static Dictionary<string, object> ToJson(string message)
        {
            var json = new Dictionary<string, object>();
            var categoryMatch = CategoryPattern.Match(message);

            if (categoryMatch.Success)
            {
                json["type"] = categoryMatch.Groups[1].Value;
                var remainder = categoryMatch.Groups[2].Value;
                var kvMatches = KeyValuePattern.Matches(remainder);
                foreach (Match kv in kvMatches)
                {
                    var key = kv.Groups[1].Value;
                    if (!json.ContainsKey(key))
                    {
                        json[key] = kv.Groups[2].Value;
                    }
                }
            }
            else
            {
                json["message"] = message;
            }

            return json;
        }

        public static void Error(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            if (exception == null)
            {
                Log.Error(FormatMessage(message, "error"));
            }
            else
            {
                Log.Error(FormatMessage(message, "error"), exception);
            }
        }

        public static void Fatal(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            if (exception == null)
            {
                Log.Fatal(FormatMessage(message, "fatal"));
            }
            else
            {
                Log.Fatal(FormatMessage(message, "fatal"), exception);
            }
        }

        public static void Info(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            if (exception == null)
            {
                Log.Info(FormatMessage(message, "info"));
            }
            else
            {
                Log.Info(FormatMessage(message, "info"), exception);
            }
        }

        public static void Warn(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            if (exception == null)
            {
                Log.Warn(FormatMessage(message, "warn"));
            }
            else
            {
                Log.Warn(FormatMessage(message, "warn"), exception);
            }
        }
    }
}
