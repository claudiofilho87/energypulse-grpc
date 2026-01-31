using EnergyPulse.Grpc;
using Google.Protobuf.WellKnownTypes;

namespace EnergyPulse.Client.Sensor.Simulation;

public static class TelemetrySimulator
{
    private static readonly Random Random = new();

    public static TelemetryData GenerateHistoricalPoint(int minutesAgo)
    {
        return new TelemetryData
        {
            Voltage = 220 + (Random.NextDouble() * 4.4 - 2.2),
            Current = 10 + Random.NextDouble() * 5,
            Frequency = 60 + (Random.NextDouble() * 0.2 - 0.1),
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-minutesAgo))
        };
    }
}