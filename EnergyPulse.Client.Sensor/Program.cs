using EnergyPulse.Client.Sensor.Simulation;
using EnergyPulse.Grpc;
using Grpc.Net.Client;
using Serilog;

using var channel = GrpcChannel.ForAddress("http://localhost:5210");
var client = new MonitoringService.MonitoringServiceClient(channel);

using var call = client.UploadHistoricalLogs();

try
{
    for (int i = 1; i <= 100; i++)
    {
        var historicalData = TelemetrySimulator.GenerateHistoricalPoint(-i);
        
        await call.RequestStream.WriteAsync(historicalData);
        
        if (i % 20 == 0) Log.Debug("Sent {Count} records so far...", i);
    }

    await call.RequestStream.CompleteAsync();

    var summary = await call.ResponseAsync;

    Console.WriteLine($"Upload finished! Server response: {summary.Status}");
    Console.WriteLine($"Total records processed by server: {summary.RecordsProcessed}");
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to upload historical logs.");
}

Console.WriteLine("Sensor task completed. Press any key to exit.");
Console.ReadKey();
