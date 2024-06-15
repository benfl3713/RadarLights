using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RadarLights.Services;

namespace RadarLights;

public class MatrixHub : Hub
{
    private readonly ILedMatrix _matrix;
    private readonly AppConfig _config;

    public MatrixHub(ILedMatrix matrix, AppConfig config)
    {
        _matrix = matrix;
        _config = config;
    }
    
    public async Task GetFullMatrix()
    {
        var spinnerColour = _config.RadarSpinnerColour.Replace(" ", "");
        Console.WriteLine("Full matrix requested");
        var buffer = _matrix.ActivePixels;
        List<PointUpdate> toSend = new List<PointUpdate>();
        for (int y = 0; y < buffer.Length; y++)
        {
            for (int x = 0; x < buffer[y].Length; x++)
            {
                string colour = buffer[y][x];
                if (buffer[y][x] == spinnerColour)
                    colour = "0, 0, 0";
                
                toSend.Add(new PointUpdate(y, x, colour));
            }
        }
        
        var bufferString = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(toSend)));      
        _ = Clients.Caller.SendAsync("ReceiveMatrix", bufferString);
    }
}