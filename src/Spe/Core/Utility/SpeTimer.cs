using System;
using System.Diagnostics;
using Spe.Core.Diagnostics;

namespace Spe.Core.Utility
{
    public static class SpeTimer
    {
        public static T Measure<T>(string message, bool log, Func<T> action) where T : class
        {
            var stopWatch = new Stopwatch();
            try
            {
                stopWatch.Start();

                var result = action();

                stopWatch.Stop();
                if (log)
                {
                    PowerShellLog.Debug($"The {message} completed in {stopWatch.ElapsedMilliseconds} ms.");
                }
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