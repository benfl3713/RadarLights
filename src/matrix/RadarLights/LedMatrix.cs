using RpiLedMatrix;

namespace RadarLights;

public class LedMatrix : IDisposable
{
    private readonly RGBLedMatrix _matrix;
    private readonly RGBLedCanvas _canvas;

    public int RowLength => _canvas.Height;
    public int ColLength => _canvas.Width;
    public int Width { get; }
    public int Height { get; }

    public LedMatrix(RGBLedMatrix matrix)
    {
        _matrix = matrix;
        _canvas = _matrix.CreateOffscreenCanvas();
        Width = _canvas.Width;
        Height = _canvas.Height;
        Clear();
        Update();
    }

    public void SetPixel(int x, int y, Color color)
    {
        _canvas.SetPixel(x, y, color);
    }

    public void DrawText(RGBLedFont font, int x, int y, Color color, string text)
    {
        _canvas.DrawText(font, x, y, color, text);
    }

    public void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        _canvas.DrawLine(x0, y0, x1, y1, color);
    }

    public void DrawCircle(int x0, int y0, int radius, Color color) => _canvas.DrawCircle(x0, y0, radius, color);

    public void Clear()
    {
        _canvas.Clear();
    }

    public void Fill(Color color)
    {
        _canvas.Fill(color);
    }

    public void Update()
    {
        _matrix.SwapOnVsync(_canvas);
    }

    public void Dispose()
    {
        _canvas.Clear();
        _matrix.SwapOnVsync(_canvas);
        Thread.Sleep(500); // Wait for Vsync to actually happen
        _matrix.Dispose();
    }
}
