﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace OpenTelemetry.AutoInstrumentation.Digma.Utils;

public class ActivitySourceProvider
{
    private readonly ActivitySource _defaultActivitySource = new("OpenTelemetry.AutoInstrumentation.Digma");
    private readonly Dictionary<Type, ActivitySource> _activitySources = new();
    private readonly ReaderWriterLock _lock = new ();

    public ActivitySource GetOrCreate(Type type)
    {
        try
        {
            _lock.AcquireReaderLock(TimeSpan.FromMinutes(1));
            try
            {
                if (_activitySources.TryGetValue(type, out var source))
                {
                    Logger.LogDebug($"ActivitySourceProvider returns {source.Name} (listened: {source.HasListeners()})");
                    return source;
                }

                var cookie = _lock.UpgradeToWriterLock(TimeSpan.FromMinutes(1));
                try
                {
                    source = new ActivitySource(type.Name);
                    _activitySources[type] = source;
                    Logger.LogDebug($"ActivitySourceProvider created+return {source.Name} (listened: {source.HasListeners()})");
                    return source;
                }
                finally
                {
                    _lock.DowngradeFromWriterLock(ref cookie);
                }
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }
        catch
        {
            Logger.LogDebug($"ActivitySourceProvider returns default {_defaultActivitySource.Name} (listened: {_defaultActivitySource.HasListeners()})");
            return _defaultActivitySource;
        }
    }
}