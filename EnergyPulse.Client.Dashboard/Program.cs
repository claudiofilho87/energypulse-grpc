using EnergyPulse.Grpc;
using Grpc.Core;
using Grpc.Net.Client;

using var channel = GrpcChannel.ForAddress("http://localhost:5210");
var client  = new MonitoringService.MonitoringServiceClient(channel);

using var call = client.StreamLiveTelemetry(new MonitorRequest { IntervalMs = 100 });

await foreach (var response in call.ResponseStream.ReadAllAsync())
{
    Console.Clear();
    Console.WriteLine($"=== EnergyPulse Dashboard - Live Telemetry ===");
    Console.WriteLine($"Voltage:   {response.Voltage:F2} V");
    Console.WriteLine($"Current:   {response.Current:F2} A");
    Console.WriteLine($"Frequency: {response.Frequency:F2} Hz");
    Console.WriteLine($"Timestamp: {response.Timestamp.ToDateTime()}");
}