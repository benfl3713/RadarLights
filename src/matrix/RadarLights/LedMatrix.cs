using RpiLedMatrix;

namespace RadarLights;

public interface ILedMatrix
{
    int RowLength { get; }
    int ColLength { get; }
    void SetPixel(int x, int y, Color color);
    void DrawText(RGBLedFont font, int x, int y, Color color, string text);
    void DrawLine(int x0, int y0, int x1, int y1, Color color);
    void DrawCircle(int x0, int y0, int radius, Color color);
    void Clear();
    void Update();
    void Reset();
}

public class LedMatrix : ILedMatrix, IDisposable
{
    private readonly RgbMatrixFactory _matrixFactory;
    private RGBLedMatrix? _matrix;

    private RGBLedMatrix Matrix
    {
        get
        {
            if (_matrix == null)
            {
                Console.WriteLine("Creating new matrix");
                _matrix = _matrixFactory.CreateLedMatrix();
            }

            return _matrix;
        }
        set => _matrix = value;
    }

    private RGBLedCanvas? _canvas;

    public int RowLength => Canvas.Height;
    public int ColLength => Canvas.Width;
    public int Width { get; }
    public int Height { get; }

    private RGBLedCanvas Canvas
    {
        get
        {
            if (_canvas == null)
            {
                Console.WriteLine("Creating new canvas");
                _canvas = Matrix.CreateOffscreenCanvas();
            }

            return _canvas;
        }
        set => _canvas = value;
    }

    public LedMatrix(RgbMatrixFactory matrixFactory)
    {
        _matrixFactory = matrixFactory;
        Width = Canvas.Width;
        Height = Canvas.Height;
        Clear();
        Update();
    }

    public void SetPixel(int x, int y, Color color)
    {
        Canvas.SetPixel(x, y, color);
    }

    public void DrawText(RGBLedFont font, int x, int y, Color color, string text)
    {
        Canvas.DrawText(font, x, y, color, text);
    }

    public void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        Canvas.DrawLine(x0, y0, x1, y1, color);
    }

    public void DrawCircle(int x0, int y0, int radius, Color color) => Canvas.DrawCircle(x0, y0, radius, color);

    public void Clear()
    {
        Canvas.Clear();
    }

    public void Fill(Color color)
    {
        Canvas.Fill(color);
    }

    public void Update()
    {
        Matrix.SwapOnVsync(Canvas);
    }

    public void Dispose()
    {
        Canvas.Clear();
        Matrix.SwapOnVsync(Canvas);
        Thread.Sleep(500); // Wait for Vsync to actually happen
        Matrix.Dispose();
    }

    public void Reset()
    {
        Canvas.Clear();
        Matrix.SwapOnVsync(Canvas);
        Matrix.Dispose();
        Canvas = null!;
        Matrix = null!;
        GC.Collect();
    }
}
