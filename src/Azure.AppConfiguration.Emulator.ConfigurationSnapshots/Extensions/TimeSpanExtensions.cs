using System;

namespace Azure.AppConfiguration.Emulator.ConfigurationSnapshots
{
    internal static class TimeSpanExtensions
    {
        private const int MaxAttempts = sizeof(long) * 8 - 2;

        /// <summary>
        /// Exponential randomized backoff
        /// </summary>
        /// <param name="min">Minimum delay</param>
        /// <param name="max">Maximum delay</param>
        /// <param name="attempts">Total attempts made</param>
        /// <returns></returns>
        public static TimeSpan BackOff(this TimeSpan min, TimeSpan max, int attempts)
        {
            if (min < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(min));
            }

            if (max < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(max));
            }

            if (attempts < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attempts));
            }

            if (attempts <= 1 || max <= min)
            {
                return min;
            }

            //
            // IMPORTANT: This can overflow
            double maxMilliseconds = Math.Max(1, min.TotalMilliseconds) * ((long)1 << Math.Min(attempts, MaxAttempts));

            if (maxMilliseconds > max.TotalMilliseconds ||
                maxMilliseconds <= 0 /*overflow*/)
            {
                maxMilliseconds = max.TotalMilliseconds;
            }

            return TimeSpan.FromMilliseconds(min.TotalMilliseconds + new Random().NextDouble() * (maxMilliseconds - min.TotalMilliseconds));
        }
    }
}
