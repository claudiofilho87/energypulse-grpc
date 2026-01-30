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
        _logger.LogInformation("Substation info requested for ID: {Id}", infoRequest.SubstationId);
        
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
        
        _logger.LogInformation("Starting live telemetry stream. Interval: {Interval}ms", request.IntervalMs);

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
            
            _logger.LogInformation("Sending Telemetry: {V}V | {Hz}Hz", data.Voltage.ToString("F2"), data.Frequency.ToString("F2"));
            
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

    public override async Task FrequencyControlLoop(IAsyncStreamReader<FrequencyUpdate> requestStream, IServerStreamWriter<ControlAction> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Bi-directional stream established: Starting frequency stabilization loop.");
        
        await foreach (var message in requestStream.ReadAllAsync())
        {
            var currentFreq = message.Frequency;
            _logger.LogInformation("Received frequency from sensor: {Freq}hz", currentFreq);

            string action = "STABLE";
            double adjustmentValue = 0;

            if (currentFreq < 59.95)
            {
                action = "INCREASE_POWER";
                adjustmentValue = (60.0 - currentFreq) * 12.5;
            }
            else if (currentFreq > 60.05)
            {
                action = "DECREASE_POWER";
                adjustmentValue = (currentFreq - 60.0) * 10.0;
            }

            await responseStream.WriteAsync(new ControlAction
            {
                Action = action,
                Value = adjustmentValue
            });

            if (action != "STABLE")
            {
                _logger.LogWarning("Control action sent: {Action} | Delta: {Value} MW", action, adjustmentValue);
            }
        }
    }
}