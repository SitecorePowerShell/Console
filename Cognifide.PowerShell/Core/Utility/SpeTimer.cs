using System;
using System.Diagnostics;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Core.Utility
{
    public static class SpeTimer
    {
        public static T Measure<T>(string message, Func<T> action) where T : class
        {
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var result = action();

                stopWatch.Stop();
                Log.Debug(String.Format("The {0} completed in {1} ms.", message, stopWatch.ElapsedMilliseconds), action);
                return result;
            }
            catch (Exception ex)
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