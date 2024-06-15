using BdfFontParser;
using RpiLedMatrix;
using Color = RpiLedMatrix.Color;

namespace RadarLights;

public interface ILedMatrix
{
    int RowLength { get; }
    int ColLength { get; }
    string[][] ActivePixels { get; }
    void SetPixel(int x, int y, Color color);
    void DrawText(BdfFont font, int x, int y, Color color, string text);
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
                _buffer = new string[RowLength][].Select(_ => new string[ColLength]).ToArray();
                _activeBuffer = new string[RowLength][].Select(_ => new string[ColLength]).ToArray();
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
    public string[][] ActivePixels => _activeBuffer.ToArray();
    private string[][] _buffer = null!;
    private string[][] _activeBuffer = null!;

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
        _buffer[y][x] = color.ToShortString();
    }

    public void DrawTextNative(RGBLedFont font, int x, int y, Color color, string text)
    {
        Canvas.DrawText(font, x, y, color, text);
    }
    
    public void DrawText(BdfFont font, int x, int y, Color color, string text)
    {
        var map = font.GetMapOfString(text);
        var width = map.GetLength(0);
        var height = map.GetLength(1);
        
        for (int line = 0; line < height; line++)
        {
            // iterate through every bit
            for (int bit = 0; bit < width; bit++)
            {
                var charX = bit + x;
                var charY = line + (y - font.BoundingBox.Y - font.BoundingBox.OffsetY);

                if(map[bit,line] && charX >= 0 && charY >= 0 && charX <= Width-1 && charY <= Height-1)
                {
                    try
                    {
                        SetPixel(charX, charY, color);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }
        }
    }

    public void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        Canvas.DrawLine(x0, y0, x1, y1, color);
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = (dx > dy ? dx : -dy) / 2, e2;
        for (;;)
        {
            _buffer[y0][x0] = color.ToShortString();
            if (x0 == x1 && y0 == y1) break;
            e2 = err;
            if (e2 > -dx) { err -= dy; x0 += sx; }
            if (e2 < dy) { err += dx; y0 += sy; }
        }
    }

    public void DrawCircle(int x0, int y0, int radius, Color color)
    {
        Canvas.DrawCircle(x0, y0, radius, color);
        for (int x = x0 - radius; x <= x0 + radius; x++)
        {
            for (int y = y0 - radius; y <= y0 + radius; y++)
            {
                if (Math.Pow(x - x0, 2) + Math.Pow(y - y0, 2) <= Math.Pow(radius, 2))
                {
                    _buffer[y][x] = color.ToShortString();
                }
            }
        }
    }

    public void Clear()
    {
        Canvas.Clear();
        for (int y = 0; y < RowLength; y++)
        {
            for (int x = 0; x < ColLength; x++)
            {
                _buffer[y][x] = new Color(0, 0, 0).ToShortString();
            }
        }
    }

    public void Fill(Color color)
    {
        Canvas.Fill(color);
        for (int y = 0; y < RowLength; y++)
        {
            for (int x = 0; x < ColLength; x++)
            {
                _buffer[y][x] = color.ToShortString();
            }
        }
    }

    public void Update()
    {
        Matrix.SwapOnVsync(Canvas);
        _activeBuffer = _buffer.Select(t => t.ToArray()).ToArray();
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
