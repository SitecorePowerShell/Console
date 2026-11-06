using System;
using System.Diagnostics;
using Spe.Core.Diagnostics;

namespace Spe.Core.Utility
{
    public static class SpeTimer
    {
        public static T Measure<T>(string message, bool log, Func<T> action)
        {
            var stopWatch = new Stopwatch();
            try
            {
                stopWatch.Start();

                var result = action();

                stopWatch.Stop();
                if (log)
                {
                    PowerShellLog.Debug($"[Timer] action=measure operation=\"{message}\" elapsed={stopWatch.ElapsedMilliseconds}ms");
                }
                return result;
            }
            catch (Exception)
            {
                PowerShellLog.Error($"[Timer] action=measure status=failed operation=\"{message}\" elapsed={stopWatch.ElapsedMilliseconds}ms");
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