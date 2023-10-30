using RpiLedMatrix;

namespace RadarLights;

public class AirplaneDataCache
{
    public AirplaneData Data { get; set; } = new AirplaneData();
}

public class AirplaneData
{
    public List<MatrixAircraft> Aircraft { get; set; } = new List<MatrixAircraft>(0);
    public Dictionary<string, List<MatrixAircraft>> History { get; set; } = new Dictionary<string, List<MatrixAircraft>>();
    public List<(int x, int y, Color color)> ExtraPixels { get; set; } = new List<(int x, int y, Color color)>();
    public List<(int x, int y, Color color)> FixedPixels { get; set; } = new List<(int x, int y, Color color)>();
}
