using System;
using PoeStashPricer;
class RetryTest
{
    static void Check(bool value) { if (!value) throw new Exception("Retry timing regression"); }
    static void Main()
    {
        Check(RetryTiming.HoverDelay(40, true) == 40);
        Check(RetryTiming.CopyTimeout(150, 73, true) == 73);
        Check(RetryTiming.HoverDelay(10, true) == 40);
        Check(RetryTiming.HoverDelay(100, true) == 100);
        Check(RetryTiming.CopyTimeout(500, 350, true) == 350);
        Check(RetryTiming.HoverDelay(40, false) == 90);
        Check(RetryTiming.CopyTimeout(150, 73, false) == 250);
        Check(RetryTiming.CopyTimeout(500, 73, false) == 500);
        Check(RetryTiming.CopyTimeout(150, 0, true) == 60);
        Console.WriteLine("PASS: 9 retry checks; fast response, minimum waits, slow response and unmeasured fallback");
    }
}
