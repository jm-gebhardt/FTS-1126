namespace Gebhardt.Acc.Vda5050Simulator.Configuration;

public sealed class MqttOptions
{
    public string Host { get; init; } = "127.0.0.1";
    
    public int Port { get; init; } = 1883;
    
    public string Username { get; init; } = string.Empty;
    
    public string Password { get; init; } = string.Empty;
    
    public string ClientId { get; init; } = "vda5050-simulator";
    
    public string InterfaceName { get; init; } = "uagv";
    
    public string Version { get; init; } = "v2";
    
    public string ProtocolVersion { get; init; } = "2.1.0";
}

public sealed class SimulationOptions
{
    public double Speedup { get; init; } = 1.0;

    public int TickStepMilliseconds { get; init; } = 100;

    public int StateIntervalSeconds { get; init; } = 30;
    
    public double ActionDurationSeconds { get; init; } = 2.0;
}