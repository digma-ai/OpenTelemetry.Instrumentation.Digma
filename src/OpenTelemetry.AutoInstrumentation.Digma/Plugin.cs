﻿using System;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.Digma.Utils;


namespace OpenTelemetry.AutoInstrumentation.Digma;

public class Plugin
{
    private readonly AutoInstrumentor _autoInstrumentor = new();
    
    public void Initializing()
    {
        Logger.LogInfo("Initialization started");
        
        new Thread(o =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            _autoInstrumentor.Instrument();
        }).Start();
    }

    [Obsolete("Please use AutoInstrumentor.Instrument()")]
    public void SyncInitializing()
    {
        _autoInstrumentor.Instrument();
    }
}