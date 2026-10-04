using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace PartyGame.Server.Tests;

/// <summary>
/// Runs the real server executable, with Kestrel, in its own process. Unlike WebApplicationFactory and its
/// in-memory TestServer, this shows which addresses the server actually listens on and how the process exits.
/// </summary>
internal sealed class ServerProcess : IDisposable
{
    private readonly Process _process;
    private readonly StringBuilder _output = new();

    private ServerProcess(Process process)
    {
        _process = process;
        _process.OutputDataReceived += (_, e) => Append(e.Data);
        _process.ErrorDataReceived += (_, e) => Append(e.Data);
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    public string Output
    {
        get
        {
            lock (_output)
            {
                return _output.ToString();
            }
        }
    }

    public static ServerProcess Start(IReadOnlyDictionary<string, string> environment)
    {
        var startInfo = new ProcessStartInfo
        {
            // The test host is not 'dotnet' itself: dotnet test exposes the muxer it runs with.
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            ArgumentList = { Path.Combine(AppContext.BaseDirectory, "PartyGame.Server.dll") },
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // Listening URLs inherited from the developer's environment must not hide the server's own behaviour.
        startInfo.Environment.Remove("ASPNETCORE_URLS");
        startInfo.Environment.Remove("DOTNET_URLS");
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        foreach (var (name, value) in environment)
        {
            startInfo.Environment[name] = value;
        }

        return new ServerProcess(Process.Start(startInfo) ?? throw new InvalidOperationException("Server process did not start"));
    }

    public static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public static IPAddress? FindNonLoopbackIPv4Address() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Select(unicast => unicast.Address)
            .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address));

    public async Task<int> WaitForExitAsync(TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout);
        await _process.WaitForExitAsync(cts.Token);
        return _process.ExitCode;
    }

    /// <summary>Polls <paramref name="url"/> until the server answers, or fails if it exits or never answers.</summary>
    public async Task<HttpResponseMessage> WaitForResponseAsync(HttpClient client, Uri url, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                return await client.GetAsync(url, TestContext.Current.CancellationToken);
            }
            catch (HttpRequestException) when (!_process.HasExited && DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
            }
        }
    }

    /// <summary>Waits until the output contains <paramref name="text"/>, which the process may write after it answers requests.</summary>
    public async Task<string> WaitForOutputAsync(string text, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!Output.Contains(text, StringComparison.Ordinal) && !_process.HasExited && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        }

        return Output;
    }

    /// <summary>Kills the process, as a crash would: nothing is saved nor closed.</summary>
    public void Kill()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit();
        }
    }

    public void Dispose()
    {
        Kill();
        _process.Dispose();
    }

    private void Append(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_output)
        {
            _output.AppendLine(line);
        }
    }
}
