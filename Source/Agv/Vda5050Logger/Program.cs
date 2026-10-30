using Microsoft.Extensions.Configuration;
using MQTTnet;

namespace Gebhardt.Acc.Vda5050Logger;

internal sealed class MqttSettings
{
    public string Host { get; init; } = "127.0.0.1";
    
    public int Port { get; init; } = 1883;

    public bool UseTls { get; init; }

    public bool AllowUntrustedCertificates { get; init; }

    public string CaFile { get; init; } = string.Empty;

    public string ClientCertFile { get; init; } = string.Empty;

    public string ClientKeyFile { get; init; } = string.Empty;
    
    public string Username { get; init; } = string.Empty;
    
    public string Password { get; init; } = string.Empty;
    
    public string ClientId { get; init; } = "vda5050-logger";
    
    public string Topic { get; init; } = "#";
}

internal sealed class OutputSettings
{
    public string Directory { get; init; } = "vda5050-messages";
}

internal static class Program
{
    private static async Task<int> Main()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var mqtt = configuration.GetSection("Mqtt").Get<MqttSettings>() ?? new MqttSettings();
        var output = configuration.GetSection("Output").Get<OutputSettings>() ?? new OutputSettings();

        //if (string.IsNullOrWhiteSpace(mqtt.Username) || string.IsNullOrWhiteSpace(mqtt.Password))
        //{
        //    Console.Error.WriteLine("Mqtt:Username and Mqtt:Password must be set in appsettings.json.");
        //    return 1;
        //}

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        string outputDirectory = Path.GetFullPath(output.Directory);
        if (Directory.Exists(outputDirectory))
        {
            foreach (var file in Directory.GetFiles(outputDirectory))
            {
                File.Delete(file);
            }
        }

        Directory.CreateDirectory(outputDirectory);
        var writers = new Dictionary<string, StreamWriter>(StringComparer.Ordinal);
        object writeLock = new object();

        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(mqtt.Host, mqtt.Port)
            .WithCredentials(mqtt.Username, mqtt.Password)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
            .WithClientId(mqtt.ClientId)
            .WithCleanSession();

        if (mqtt.UseTls)
        {
            var certificates = new List<System.Security.Cryptography.X509Certificates.X509Certificate2>();
            if (!string.IsNullOrWhiteSpace(mqtt.ClientCertFile))
            {
                var clientCert = string.IsNullOrWhiteSpace(mqtt.ClientKeyFile)
                    ? System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPemFile(mqtt.ClientCertFile)
                    : System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPemFile(mqtt.ClientCertFile, mqtt.ClientKeyFile);
                // re-import so the private key is usable by SChannel
                certificates.Add(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12(
                    clientCert.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pfx), null));
            }

            System.Security.Cryptography.X509Certificates.X509Certificate2? ca = string.IsNullOrWhiteSpace(mqtt.CaFile)
                ? null
                : System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificateFromFile(mqtt.CaFile);

            optionsBuilder.WithTlsOptions(o =>
            {
                o.UseTls();
                if (certificates.Count > 0)
                {
                    o.WithClientCertificates(certificates);
                }

                if (mqtt.AllowUntrustedCertificates)
                {
                    o.WithCertificateValidationHandler(_ => true);
                }
                else if (ca != null)
                {
                    o.WithTrustChain(new System.Security.Cryptography.X509Certificates.X509Certificate2Collection(ca));
                }
            });
        }

        var options = optionsBuilder.Build();

        var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(mqtt.Topic)
            .Build();

        client.ApplicationMessageReceivedAsync += e =>
        {
            string payload = e.ApplicationMessage.ConvertPayloadToString() ?? string.Empty;
            // one message per line
            string line = $"{e.ApplicationMessage.Topic} {payload.Replace("\r", string.Empty).Replace("\n", string.Empty)}";
            lock (writeLock)
            {
                string topic = e.ApplicationMessage.Topic;
                if (!writers.TryGetValue(topic, out var writer))
                {
                    string filePath = Path.Combine(outputDirectory, ToFileName(topic));
                    writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
                    writers[topic] = writer;
                }

                Console.WriteLine($"{topic}: {line}");
                writer.WriteLine(line);
            }
            return Task.CompletedTask;
        };

        client.DisconnectedAsync += async e =>
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }
            Console.WriteLine($"Disconnected from broker ({e.Reason}), reconnecting in 5s...");
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                await client.ConnectAsync(options, cts.Token);
                await client.SubscribeAsync(subscribeOptions, cts.Token);
                Console.WriteLine("Reconnected and resubscribed.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Reconnect failed: {ex.Message}");
            }
        };

        try
        {
            await client.ConnectAsync(options, cts.Token);
            Console.WriteLine($"Connected to {mqtt.Host}:{mqtt.Port} as {mqtt.Username}.");

            await client.SubscribeAsync(subscribeOptions, cts.Token);
            Console.WriteLine($"Subscribed to '{mqtt.Topic}', writing one file per topic to '{outputDirectory}'. Press Ctrl+C to exit.");

            await Task.Delay(Timeout.Infinite, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync();
            }

            lock (writeLock)
            {
                foreach (var writer in writers.Values)
                {
                    writer.Dispose();
                }
                writers.Clear();
            }
        }

        return 0;
    }

    private static string ToFileName(string topic)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = topic.Select(c => c == '/' || invalid.Contains(c) ? '_' : c).ToArray();
        string name = new string(chars).Trim('.', ' ');
        return (name.Length == 0 ? "_" : name) + ".json";
    }
}
