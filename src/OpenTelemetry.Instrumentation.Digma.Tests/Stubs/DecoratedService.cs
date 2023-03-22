using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenTelemetry.Instrumentation.Digma.Helpers.Attributes;

namespace OpenTelemetry.Instrumentation.Digma.Tests.Stubs;

public interface IDecoratedService
{
    public void MethodExplicitlyMarkedForTracing(Action stateValidation);

    public Task AsyncMethodExplicitlyMarkedForTracing(Action stateValidation);

    public Task AsyncVoid();
    
    public Task<int> AsyncValue();
    
    public Task AsyncError();

    public void MethodNotExplicitlyMarkedForTracing(Action stateValidation);

    public void MethodWithStrangeParams1(Action stateValidation,
        IList<string>[] arrayOfList, ISet<int[]> setOfArray, IDictionary<int, ICollection<string>> dict,
        ref int intVal);

    public void MethodJaggedAndMultiDimArraysParams(Action stateValidation, out string strVal,
        bool[][][] jaggedArrayOfBools, short[,,,][,][,,] multiDimArrayOfShorts,
        long[,,][][,][] mixMultiDimAndJaggedArraysOfLongs
    );

    void MethodExplicitlyMarkedForTracingWithAttributes(Action action);
}
    
[ActivitiesAttributes("att1:value1")]
public class DecoratedService : IDecoratedService
{
    [TraceActivity()]
    public void MethodExplicitlyMarkedForTracing(Action stateValidation)
    {
        var v = Activity.Current;
        stateValidation();
    }
    
    [TraceActivity()]
    [ActivitiesAttributes("att1:value1")]
    public void MethodExplicitlyMarkedForTracingWithAttributes(Action stateValidation)
    {
        stateValidation();
    }

    [TraceActivity()]
    public async Task AsyncMethodExplicitlyMarkedForTracing(Action stateValidation)
    {
        stateValidation();
    }


    public async Task AsyncVoid()
    {
        await Task.Delay(100);
    }

    public async Task<int> AsyncValue()
    {
        await Task.Delay(100);
        return 123;
    }

    public async Task AsyncError()
    {
        await Task.Delay(100);
        throw new Exception("Bla");
    }
    
    public void MethodNotExplicitlyMarkedForTracing(Action stateValidation)
    {
        stateValidation();
    }

    public void MethodWithStrangeParams1(Action stateValidation,
        IList<string>[] arrayOfList, ISet<int[]> setOfArray, IDictionary<int, ICollection<string>> dict, ref int intVal)
    {
        stateValidation();
    }

    public void MethodJaggedAndMultiDimArraysParams(Action stateValidation, out string strVal,
        bool[][][] jaggedArrayOfBools, short[,,,][,][,,] multiDimArrayOfShorts,
        long[,,][][,][] mixMultiDimAndJaggedArraysOfLongs)
    {
        strVal = "hello";
        stateValidation();
    }
}