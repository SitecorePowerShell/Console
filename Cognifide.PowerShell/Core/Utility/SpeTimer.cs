using System;
using System.Diagnostics;
using Microsoft.PowerShell.Commands;
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
                LogUtils.Info($"The {message} completed in {stopWatch.ElapsedMilliseconds} ms.", typeof(SpeTimer));
                return result;
            }
            catch (Exception ex)
            {
                LogUtils.Error(ex.Message, typeof(SpeTimer));
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