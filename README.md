# EnergyPulse gRPC

A real-time electrical substation monitoring system built with gRPC and .NET 9, demonstrating all four gRPC communication patterns through telemetry data streaming and frequency control.

## Overview

EnergyPulse simulates a complete electrical monitoring infrastructure for substations, featuring:

- **Real-time telemetry streaming** (voltage, current, frequency)
- **Historical data synchronization** via client streaming
- **Bidirectional frequency control loop** for power management
- **Unary RPC** for substation information retrieval

## Architecture

The solution consists of four projects:

### 1. EnergyPulse.Proto
Protocol Buffers definitions and shared contracts.

**Service Definition:**
- `GetSubstationInfo` - Unary RPC for station metadata
- `StreamLiveTelemetry` - Server streaming for real-time data
- `UploadHistoricalLogs` - Client streaming for batch uploads
- `FrequencyControlLoop` - Bidirectional streaming for control systems

### 2. EnergyPulse.Server
gRPC server implementation with ASP.NET Core.

**Features:**
- HTTP/2 protocol support via Kestrel
- Structured logging with Serilog (console + file)
- Simulated telemetry generation with realistic waveforms
- Frequency stabilization logic (60Hz ±0.05Hz tolerance)

### 3. EnergyPulse.Client.Dashboard
Console dashboard for monitoring live telemetry.

**Capabilities:**
- Connects to server streaming endpoint
- Displays voltage, current, and frequency in real-time
- Updates console display every 100ms

### 4. EnergyPulse.Client.Sensor
Sensor simulation client with dual-phase operation.

**Phase 1:** Upload 100 historical telemetry records via client streaming
**Phase 2:** Establish bidirectional frequency control loop

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- IDE with C# support (Visual Studio, Rider, or VS Code)

### Running the Application

1. **Start the Server**
   ```bash
   cd EnergyPulse.Server
   dotnet run
   ```
   Server will listen on `http://localhost:5210`

2. **Run the Dashboard (Optional)**
   ```bash
   cd EnergyPulse.Client.Dashboard
   dotnet run
   ```

3. **Run the Sensor Client**
   ```bash
   cd EnergyPulse.Client.Sensor
   dotnet run
   ```

## gRPC Communication Patterns

### Unary RPC
```csharp
rpc GetSubstationInfo (InfoRequest) returns (SubstationInfo);
```
Simple request-response pattern for retrieving substation metadata.

### Server Streaming
```csharp
rpc StreamLiveTelemetry (MonitorRequest) returns (stream TelemetryData);
```
Server continuously streams telemetry data to clients at specified intervals.

### Client Streaming
```csharp
rpc UploadHistoricalLogs (stream TelemetryData) returns (UploadSummary);
```
Client uploads multiple historical records in a single stream, server responds with summary.

### Bidirectional Streaming
```csharp
rpc FrequencyControlLoop (stream FrequencyUpdate) returns (stream ControlAction);
```
Full-duplex communication for real-time frequency monitoring and control actions.

**Control Logic:**
- Frequency < 59.95Hz → `INCREASE_POWER` (12.5 MW per Hz below target)
- Frequency > 60.05Hz → `DECREASE_POWER` (10.0 MW per Hz above target)
- Within tolerance → `STABLE`

## Project Structure

```
energypulse-grpc/
├── EnergyPulse.Proto/
│   └── Protos/
│       └── monitoring.proto
├── EnergyPulse.Server/
│   ├── Services/
│   │   └── MonitoringService.cs
│   ├── Program.cs
│   └── appsettings.json
├── EnergyPulse.Client.Dashboard/
│   └── Program.cs
├── EnergyPulse.Client.Sensor/
│   ├── Program.cs
│   └── Simulation/
│       └── TelemetrySimulator.cs
└── EnergyPulse.sln
```

## Technologies

- **.NET 9.0** - Runtime and framework
- **gRPC** - Communication protocol
- **Protocol Buffers** - Serialization
- **Serilog** - Structured logging
- **ASP.NET Core** - Web hosting

## Simulated Data

**Substation:** Subestação Central EnergyPulse
**Location:** Recife, PE
**Capacity:** 500 MW

**Telemetry Ranges:**
- Voltage: 220V ±2.2V
- Current: 10-15A
- Frequency: 60Hz ±0.2Hz

## Logging

Server logs are written to:
- **Console** - Real-time output
- **File** - `EnergyPulse.Server/Logs/energy-pulse-{date}.log`
- **Retention** - 7 days

## License

This project is a demonstration of gRPC patterns in .NET for educational purposes.

## Author

Claudio Filho
