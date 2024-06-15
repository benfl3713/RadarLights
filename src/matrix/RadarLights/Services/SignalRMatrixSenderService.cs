using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace RadarLights.Services;

public class SignalRMatrixSenderService : BackgroundService
{
    private readonly ILogger<SignalRMatrixSenderService> _logger;
    private readonly IHubContext<MatrixHub> _matrixHub;
    private readonly ILedMatrix _matrix;
    private string[][]? _previousBuffer;

    public SignalRMatrixSenderService(ILogger<SignalRMatrixSenderService> logger, IHubContext<MatrixHub> matrixHub, ILedMatrix matrix)
    {
        _logger = logger;
        _matrixHub = matrixHub;
        _matrix = matrix;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run background task to periodically send the full matrix
        _ = Task.Run(async () =>
        {
            await Task.Delay(10000, stoppingToken);

            var toSend = GetFullUpdate(_matrix.ActivePixels);
            var bufferString = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(toSend)));      
            await _matrixHub.Clients.All.SendAsync("ReceiveMatrix", bufferString, cancellationToken: stoppingToken);
            
        }, stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(100, stoppingToken);
            try
            {
                await SendMatrix();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending matrix");
            }
        }
    }
    
    private async Task SendMatrix()
    {
        var toSend = GetDeltaToSend(_matrix.ActivePixels);
        var bufferString = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(toSend)));      
        _ = _matrixHub.Clients.All.SendAsync("ReceiveMatrix", bufferString);
    }
    
    private List<PointUpdate> GetDeltaToSend(string[][] buffer)
    {
        if (_previousBuffer == null)
        {
            return GetFullUpdate(buffer);
        }
        
        List<PointUpdate> toSend = new List<PointUpdate>();
        var delta = buffer.Select((row, y) => row.Select((color, x) => !color.Equals(_previousBuffer[y][x])).ToArray()).ToArray();
        _previousBuffer = buffer;
        
        for (int y = 0; y < delta.Length; y++)
        {
            for (int x = 0; x < delta[y].Length; x++)
            {
                if (delta[y][x])
                {
                    toSend.Add(new PointUpdate(y, x, buffer[y][x]));
                }
            }
        }

        return toSend;
    }

    private List<PointUpdate> GetFullUpdate(string[][] buffer)
    {
        List<PointUpdate> toSend = new List<PointUpdate>();
        for (int y = 0; y < buffer.Length; y++)
        {
            for (int x = 0; x < buffer[y].Length; x++)
            {
                toSend.Add(new PointUpdate(y, x, buffer[y][x]));
            }
        }

        _previousBuffer = buffer;
        return toSend;
    }
}

public struct PointUpdate
{
    public PointUpdate(int x, int y, string color)
    {
        X = x;
        Y = y;
        Color = color;
    }

    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; }
}