namespace RadarLights;

public class MatrixAircraft
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Altitude { get; set; }
    public double? Track { get; set; }
    public string? Flight { get; set; }
}

public class MatrixAircraftCoordinateComparer : IEqualityComparer<MatrixAircraft>
{
    public bool Equals(MatrixAircraft? x, MatrixAircraft? y)
    {
        if (x == null || y == null)
            return x == y;
        
        return x.X == y.X && x.Y == y.Y && x.Altitude == y.Altitude;
    }

    public int GetHashCode(MatrixAircraft obj)
    {
        return HashCode.Combine(obj.X, obj.Y, obj.Altitude);
    }
}
