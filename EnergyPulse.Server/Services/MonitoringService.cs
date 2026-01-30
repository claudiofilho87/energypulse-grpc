using Grpc.Core;
using EnergyPulse.Grpc;

namespace EnergyPulse.Server.Services;

public class MonitoringService : EnergyPulse.Grpc.MonitoringService.MonitoringServiceBase
{
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(ILogger<MonitoringService> logger)
    {
        _logger = logger;
    }
    
    public override Task<SubstationInfo> GetSubstationInfo(InfoRequest infoRequest, ServerCallContext context)
    {
        return Task.FromResult(new SubstationInfo
        {
            Name = "Subestação Central EnergyPulse",
            Location = "Recife, PE",
            MaxCapacityMw = 500.0
        });
    }

    public override async Task StreamLiveTelemetry(MonitorRequest request, IServerStreamWriter<TelemetryData> responseStream, ServerCallContext context)
    {
        var random = new Random();
        double time = 0;

        while (!context.CancellationToken.IsCancellationRequested)
        {
            var data = new TelemetryData
            {
                Voltage = 220 + Math.Sin(time) * 2 + (random.NextDouble() - 0.5),
                Current = 10 + Math.Cos(time) * 1,
                Frequency = 60 + (random.NextDouble() - 0.5) * 0.1,
                Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
            };

            await responseStream.WriteAsync(data);
            
            Console.WriteLine($"[Telemetria] Enviando: {data.Voltage:F2}V | {data.Frequency:F2}Hz");
            
            time += 0.1;

            await Task.Delay(request.IntervalMs, context.CancellationToken);
        }
    }

    public override async Task<UploadSummary> UploadHistoricalLogs(IAsyncStreamReader<TelemetryData> requestStream, ServerCallContext context)
    {
        int totalRecords = 0;
        double sumVoltage = 0;
        
        _logger.LogInformation("Starting to receive historical logs...");

        await foreach (var message in requestStream.ReadAllAsync())
        {
            totalRecords++;
            sumVoltage += message.Voltage;

            var v = message.Voltage;
            var i = message.Current;
            var f = message.Frequency;
            var timestamp = message.Timestamp.ToDateTime();
            
            _logger.LogInformation("Processed: {V}V | {A}A | {Hz}Hz | Date: {T}", v, i, f, timestamp);
        }
        
        _logger.LogInformation("Synchronization complete. {Count} records processed.", totalRecords);

        double averageVoltage = totalRecords > 0 ? sumVoltage / totalRecords : 0;

        return new UploadSummary
        {
            RecordsProcessed = totalRecords,
            Status = $"Success! Average voltage during the period: {averageVoltage:F2}V"
        };
    }
}