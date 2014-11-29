using System;
using System.Diagnostics;
using Microsoft.PowerShell.Commands;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.PowerShellIntegrations
{
    public static class SpeTimer
    {
        public static T Measure<T>(string message, Func<T> action) where T : class
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                T result = action();

                stopWatch.Stop();
                Log.Info(string.Format("Timer {0} finished at {1}ms", message, stopWatch.ElapsedMilliseconds), action);
                return result;
            }
            catch(Exception ex)
            {
                Log.Error(ex.Message, action);
                throw;
            }
        }

        public class ItemEditArgs
        {
            public ItemEditArgs()
            {
                UpdateStatistics = true;
                Save = true;
            }

            /// <summary>
            ///     is set to true this instance will update statistics
            ///     default: true
            /// </summary>
            public bool UpdateStatistics { get; set; }

            /// <summary>
            ///     if set to true this instance is silent
            ///     default: false
            /// </summary>
            public bool Silent { get; set; }

            /// <summary>
            ///     if set to true a succesful operation will result in item being saved
            ///     default: true
            /// </summary>
            public bool Save { get; set; }

            /// <summary>
            ///     if set to true the edited item will get saved despite exceptions in clause code
            ///     default: false
            /// </summary>
            public bool SaveOnError { get; set; }
        }
    }
}