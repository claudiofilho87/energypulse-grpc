using EnergyPulse.Client.Sensor.Simulation;
using EnergyPulse.Grpc;
using Grpc.Core;
using Grpc.Net.Client;
using Serilog;

using var channel = GrpcChannel.ForAddress("http://localhost:5210");
var client = new MonitoringService.MonitoringServiceClient(channel);

using var call = client.UploadHistoricalLogs();

var cts = new CancellationTokenSource();

try
{
    for (int i = 1; i <= 100; i++)
    {
        var historicalData = TelemetrySimulator.GenerateHistoricalPoint(-i);
        
        await call.RequestStream.WriteAsync(historicalData);
        
        if (i % 20 == 0) Console.WriteLine($"Sent {i} records so far...");
    }

    await call.RequestStream.CompleteAsync();

    var summary = await call.ResponseAsync;

    Console.WriteLine($"Phase 1 Complete! Server Response: {summary.Status}");
    Console.WriteLine($"Total records processed by server: {summary.RecordsProcessed}");
    
    Console.WriteLine("\n------------------------------------------------------------\n");

    Console.WriteLine("Starting Phase 2: Real-time Frequency Control Loop...");
    
    using var duplexCall = client.FrequencyControlLoop();
    
    var readTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var action in duplexCall.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($">>> SERVER COMMAND: {action.Action} | Adjustment: {action.Value} MW");
            }
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
        {
            Log.Information("Read stream finished.");
        }
    });
    
    var random = new Random();
    while (!cts.Token.IsCancellationRequested)
    {
        double simulatedFreq = 60.0 + (random.NextDouble() - 0.5) * 0.4;

        Console.WriteLine($"Telemetry: Sending Frequency = {simulatedFreq:F2} Hz");

        await duplexCall.RequestStream.WriteAsync(new FrequencyUpdate 
        { 
            Frequency = simulatedFreq 
        }, cts.Token);
        
        await Task.Delay(1000, cts.Token); 
    }
    
    await duplexCall.RequestStream.CompleteAsync();
    Console.WriteLine("Frequency updates completed. Waiting for final responses...");
        
    await readTask;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Sensor encountered a fatal error during operation.");
}
finally
{
    Log.Information(">>> EnergyPulse Sensor Shutting Down <<<");
    Log.CloseAndFlush();
}

Console.WriteLine("Sensor task completed. Press any key to exit.");
Console.ReadKey();
