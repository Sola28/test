using System.Text;
using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Services.Instruments.Drivers;

public class FileDriver : IInstrumentDriver
{
    private readonly Instrument _instrument;
    private readonly IServiceProvider _sp;
    private FileSystemWatcher? _watcher;

    public FileDriver(Instrument instrument, IServiceProvider sp)
    {
        _instrument = instrument;
        _sp = sp;
    }

    public int InstrumentId => _instrument.Id;
    public bool IsRunning => _watcher?.EnableRaisingEvents == true;

    public event Action<string, string>? Log;
    public event Action<string>? RawReceived;

    public Task StartAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_instrument.WatchFolder) ||
            !Directory.Exists(_instrument.WatchFolder))
            throw new InvalidOperationException("Watch folder not configured or missing.");

        _watcher = new FileSystemWatcher(_instrument.WatchFolder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            Filter = "*.*",
            EnableRaisingEvents = true,
            IncludeSubdirectories = false
        };

        _watcher.Created += OnFile;
        _watcher.Changed += OnFile;
        _watcher.Renamed += (s, e) => OnFile(s, e);

        Log?.Invoke("Info", $"Watching {_instrument.WatchFolder}");
        return Task.CompletedTask;
    }

    private void OnFile(object sender, FileSystemEventArgs e)
    {
        // Debounce — wait for the writer to release the handle
        for (int i = 0; i < 5; i++)
        {
            try
            {
                using var fs = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                break;
            }
            catch (IOException) { Thread.Sleep(200); }
            catch (Exception ex) { Log?.Invoke("Error", ex.Message); return; }
        }

        try
        {
            var bytes = File.ReadAllBytes(e.FullPath);
            if (bytes.Length == 0) return;

            var text = Encoding.ASCII.GetString(bytes);
            if (!string.IsNullOrWhiteSpace(text))
                RawReceived?.Invoke(text);

            Log?.Invoke("Info", $"Processed {Path.GetFileName(e.FullPath)}");
        }
        catch (Exception ex)
        {
            Log?.Invoke("Error", $"File read error: {ex.Message}");
        }
    }

    public Task StopAsync()
    {
        if (_watcher is not null) _watcher.EnableRaisingEvents = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _watcher?.Dispose();
        return ValueTask.CompletedTask;
    }
}