using System.Diagnostics;
using AutoInstrumentation.ZeroCodeTests.OtelCollector;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace AutoInstrumentation.ZeroCodeTests.Utils;

public static class Retry
{
    public static void Do(Action action)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                Thread.Sleep(1.Seconds());
                action();
                return;
            }
            catch
            {
                if (sw.Elapsed > 10.Seconds())
                    throw;
            }
           
        }
    }
}