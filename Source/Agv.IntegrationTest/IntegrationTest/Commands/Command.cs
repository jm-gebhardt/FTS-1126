using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.IntegrationTest.Commands;

public class Command
{
    private readonly string _cmd;
    private readonly ILogger _logger;
    private readonly string? _stdin;

    private int _exitCode = -1;
    private readonly bool _writeStdin;

    public string Stdout { get; private set; } = string.Empty;
    public string Stderr { get; private set; } = string.Empty;

    public Command(string cmd, ILogger logger, string? stdin = null)
    {
        _cmd = cmd;
        _logger = logger;
        _stdin = stdin;
        _writeStdin = !string.IsNullOrEmpty(_stdin);
    }

    public async Task<bool> ExecuteAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        _logger.LogInformation("Executing \"{Command}\"", _cmd);

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "bash",
            ArgumentList = { "-c", _cmd.Trim() },
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = _writeStdin
        };
        process.EnableRaisingEvents = true;

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                _logger.LogInformation("{Output}", e.Data);
                stdoutBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                _logger.LogWarning("{Error}", e.Data);
                stderrBuilder.AppendLine(e.Data);
            }
        };

        process.Exited += (sender, _) => tcs.TrySetResult((sender as Process)!.ExitCode);

        if (!process.Start())
        {
            _logger.LogError("Failed to start process \"{Command}\"", _cmd);
            _exitCode = -1;
            return false;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (_writeStdin)
        {
            await process.StandardInput.WriteAsync(_stdin);
            process.StandardInput.Close();
        }

        if (timeout.HasValue)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout.Value);
            cancellationToken = timeoutCts.Token;
        }

        try
        {
            _exitCode = await tcs.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // ignored
            }

            _logger.LogError("Command timeout/cancel: {Command}", _cmd);
            _exitCode = -1;
        }

        Stdout = stdoutBuilder.ToString();
        Stderr = stderrBuilder.ToString();
        return _exitCode == 0;
    }
}