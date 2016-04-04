using System;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Core.Utility
{
    public class LogUtils
    {
        private const string MessagePrefix = "[SPE] ";

        public static void Audit(object owner, string format, params string[] parameters)
        {
            Log.Audit(owner, format, parameters);
        }

        public static void Debug(string message, object owner)
        {
            Log.Debug(MessagePrefix + message, owner);
        }

        public static void Info(string message, object owner)
        {
            Log.Info(MessagePrefix + message, owner);
        }

        public static void Warn(string message, object owner)
        {
            Log.Warn(MessagePrefix + message, owner);
        }

        public static void Error(string message, object owner)
        {
            Log.Error(MessagePrefix + message, owner);
        }

        public static void Error(string message, Exception exception, object owner)
        {
            Log.Error(MessagePrefix + message, exception, owner);
        }

        public static void Fatal(string message, object owner)
        {
            Log.Fatal(MessagePrefix + message, owner);
        }
    }
}