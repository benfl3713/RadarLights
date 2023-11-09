using System.Collections.Concurrent;
using Color = RpiLedMatrix.Color;

namespace RadarLights;

public class AirplaneDataCache
{
    public AirplaneData Data { get; set; } = new AirplaneData();
}

public class AirplaneData
{
    public List<MatrixAircraft> Aircraft { get; set; } = new List<MatrixAircraft>(0);
    public ConcurrentDictionary<string, List<MatrixAircraft>> History { get; set; } = new ConcurrentDictionary<string, List<MatrixAircraft>>();
    public List<(int x, int y, Color color)> ExtraPixels { get; set; } = new List<(int x, int y, Color color)>();
    public List<(int x, int y, Color color)> FixedPixels { get; set; } = new List<(int x, int y, Color color)>();
}
