// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using EventPipe.UnitTests.Common;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;
using Xunit.Abstractions;

// Use this test as an example of how to write tests for EventPipe in
// the dotnet/diagnostics repo

namespace EventPipe.UnitTests.ProviderValidation
{
    public sealed class MyEventSource : EventSource
    {
        private MyEventSource() { }
        public static MyEventSource Log = new();
        public void MyEvent() { WriteEvent(1, "MyEvent"); }
    }

    public class ProviderTests
    {
        private readonly ITestOutputHelper output;

        public ProviderTests(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        [Fact]
        public async Task UserDefinedEventSource_ProducesEvents()
        {
            await RemoteTestExecutorHelper.RunTestCaseAsync(() => {
                Dictionary<string, ExpectedEventCount> expectedEventCounts = new()
                {
                    { "MyEventSource", new ExpectedEventCount(100_000, 0.30f) },
                    { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
                    { "Microsoft-DotNETCore-SampleProfiler", -1 }
                };

                List<EventPipeProvider> providers = new()
                {
                    new EventPipeProvider("MyEventSource", EventLevel.Verbose, -1),
                    new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational)
                };

                Action eventGeneratingAction = () => {
                    for (int i = 0; i < 100_000; i++)
                    {
                        if (i % 10_000 == 0)
                        {
                            Logger.logger.Log($"Fired MyEvent {i:N0}/100,000 times...");
                        }

                        MyEventSource.Log.MyEvent();
                    }
                };
                int ret = IpcTraceTest.RunAndValidateEventCounts(expectedEventCounts, eventGeneratingAction, providers, 1024, null);
                Assert.Equal(100, ret);
            }, output);
        }
    }
}
