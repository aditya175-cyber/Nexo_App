using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WinIsland.UI.Services;

public class SystemData
{
    public double Cpu { get; set; }
    public double Ram { get; set; }
    public double RamUsed { get; set; }
    public double RamTotal { get; set; }
}

public class WebSocketClient : IDisposable
{
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private readonly string _uri;

    public event Action<SystemData>? OnSystemDataReceived;
    public event Action<bool>? OnConnectionChanged;

    public WebSocketClient(string uri = "ws://localhost:9123")
    {
        _uri = uri;
    }

    public async Task ConnectAsync()
    {
        _cts = new CancellationTokenSource();

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                _ws = new ClientWebSocket();
                await _ws.ConnectAsync(new Uri(_uri), _cts.Token);
                OnConnectionChanged?.Invoke(true);
                await ReceiveLoop();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                OnConnectionChanged?.Invoke(false);
                try { await Task.Delay(2000, _cts.Token); }
                catch { break; }
            }
            finally
            {
                if (_ws?.State == WebSocketState.Open)
                {
                    try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); }
                    catch { /* ignore */ }
                }
                _ws?.Dispose();
                _ws = null;
            }
        }
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[4096];

        while (_ws?.State == WebSocketState.Open && !_cts!.Token.IsCancellationRequested)
        {
            var result = await _ws.ReceiveAsync(buffer, _cts.Token);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();

                if (type == "system")
                {
                    var data = new SystemData
                    {
                        Cpu = root.GetProperty("cpu").GetDouble(),
                        Ram = root.GetProperty("ram").GetDouble(),
                        RamUsed = root.GetProperty("ram_used").GetDouble(),
                        RamTotal = root.GetProperty("ram_total").GetDouble(),
                    };
                    OnSystemDataReceived?.Invoke(data);
                }
            }
            catch
            {
                // Ignore malformed messages
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _ws?.Dispose();
        _cts?.Dispose();
    }
}

