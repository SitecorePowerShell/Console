using System;
using System.Diagnostics;
using Cognifide.PowerShell.Core.Diagnostics;
using Microsoft.PowerShell.Commands;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Core.Utility
{
    public static class SpeTimer
    {
        public static T Measure<T>(string message, Func<T> action) where T : class
        {
            var stopWatch = new Stopwatch();
            try
            {
                stopWatch.Start();

                var result = action();

                stopWatch.Stop();
                PowerShellLog.Info($"The {message} completed in {stopWatch.ElapsedMilliseconds} ms.");
                return result;
            }
            catch (Exception)
            {
                PowerShellLog.Error($"Error while performing timed '{message}' operation within {stopWatch.ElapsedMilliseconds} ms. Exception logged at operation origin point.");
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