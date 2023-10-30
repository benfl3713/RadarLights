using System.Diagnostics;
using RpiLedMatrix;

namespace RadarLights;

public class Animations
{
    private RGBLedCanvas _canvas;
    private readonly RGBLedMatrix _matrix;

    public Animations(RGBLedCanvas canvas, RGBLedMatrix matrix)
    {
        _canvas = canvas;
        _matrix = matrix;
    }
    
    public void Rain()
    {
        const int MAX_HEIGHT = 32;
        const int COLOR_STEP = 15;
        const int FRAME_STEP = 1;

        var rnd = new Random();
        var points = new List<Point>();
        var recycled = new Stack<Point>();
        int frame = 0;
        var stopwatch = new Stopwatch();

        while (!Console.KeyAvailable)
        {
            stopwatch.Restart();

            frame++;

            if (frame % FRAME_STEP == 0)
            {
                if (recycled.Count == 0)
                    points.Add(new Point(rnd.Next(0, _canvas.Width - 1), 0));
                else
                {
                    var point = recycled.Pop();
                    point.x = rnd.Next(0, _canvas.Width - 1);
                    point.y = 0;
                    point.recycled = false;
                }
            }

            _canvas.Clear();

            foreach (var point in points)
            {
                if (!point.recycled)
                {
                    point.y++;

                    if (point.y - MAX_HEIGHT > _canvas.Height)
                    {
                        point.recycled = true;
                        recycled.Push(point);
                    }

                    for (var i = 0; i < MAX_HEIGHT; i++)
                    {
                        _canvas.SetPixel(point.x, point.y - i, new Color(0, 255 - i * COLOR_STEP, 0));
                    }
                }
            }

            _matrix.SwapOnVsync(_canvas);

            // force 30 FPS
            var elapsed = stopwatch.ElapsedMilliseconds;
            if (elapsed < 33)
            {
                Thread.Sleep(33 - (int)elapsed);
            }
        }
    }
}

public class Point
{
    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public int x;
    public int y;
    public bool recycled;
}
