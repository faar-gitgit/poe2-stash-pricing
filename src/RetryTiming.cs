using System;

namespace PoeStashPricer
{
    public static class RetryTiming
    {
        public static int HoverDelay(int configured, bool measured)
        {
            return measured ? Math.Max(40, configured) : Math.Max(90, configured * 2);
        }

        public static int CopyTimeout(int configured, int measuredTimeout, bool measured)
        {
            return measured ? Math.Max(60, measuredTimeout) : Math.Max(250, configured);
        }
    }
}
