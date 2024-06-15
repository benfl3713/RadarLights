using System.Text.Json;
using BdfFontParser;
using RadarLights.Services.Renderers;
using RpiLedMatrix;
using Color = RpiLedMatrix.Color;

namespace RadarLights.Services;

public class PlaneRenderService : BackgroundService
{
    private readonly ILogger<PlaneRenderService> _logger;
    private readonly ILedMatrix _matrix;
    private readonly AirplaneDataCache _airplaneDataCache;
    private readonly AirplaneColourService _airplaneColourService;
    private readonly ClockRendererService _clockRendererService;
    private RadarSettings _radarSettings;
    private bool _paused;
    private Dictionary<string,List<MatrixAircraft>> _relevantHistory = new Dictionary<string, List<MatrixAircraft>>();
    private readonly BdfFont _font = new BdfFont("./Fonts/4x6.bdf");
    private readonly Color _radarSpinnerColour;

    public PlaneRenderService(ILogger<PlaneRenderService> logger, AppConfig config,  ILedMatrix matrix, AirplaneDataCache airplaneDataCache, AirplaneColourService airplaneColourService, ClockRendererService clockRendererService)
    {
        _logger = logger;
        _matrix = matrix;
        _airplaneDataCache = airplaneDataCache;
        _airplaneColourService = airplaneColourService;
        _clockRendererService = clockRendererService;
        _radarSettings = RadarSettings.Load();
        _radarSpinnerColour = Color.FromString(config.RadarSpinnerColour);

        RadarSettings.SettingsUpdated += OnRadarSettingsUpdate;
    }

    private void OnRadarSettingsUpdate(object? sender, EventArgs _)
    {
        RadarSettings newConfig = (RadarSettings)sender!;
        if (newConfig.Enabled != _radarSettings.Enabled
            || newConfig.Range != _radarSettings.Range
            || newConfig.Trails != _radarSettings.Trails)
        {
            _relevantHistory = new Dictionary<string, List<MatrixAircraft>>();
        }

        _radarSettings = newConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_radarSettings.Enabled)
        {
            await StopAsync(stoppingToken);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _relevantHistory = _airplaneDataCache.Data.History.Where(h => _airplaneDataCache.Data.Aircraft.Any(a => a.Flight == h.Key)).ToDictionary(d => d.Key, d => d.Value);
            for (int i = 0; i < 360; i += 2)
            {
                if (stoppingToken.IsCancellationRequested || _paused)
                    break;

                try
                {
                    await DrawFrame(stoppingToken, i);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error drawing frame");
                }
            }
        }
    }

    private async Task DrawFrame(CancellationToken stoppingToken, int angle)
    {
        foreach ((int x, int y, Color color) in _airplaneDataCache.Data.FixedPixels)
        {
            SafeSetPixel(x, y, color);
        }
        
        DrawTrails();
        DrawAltitudeKey();
        //_matrix.DrawText(_font, _matrix.ColLength / 2 - 4, 6, new Color(0, 70, 0), _radarSettings.Range.ToString());
        
        _matrix.SetPixel(_matrix.ColLength / 2, _matrix.RowLength / 2, _radarSpinnerColour);
        DrawLine(angle, (_matrix.ColLength / 2, _matrix.RowLength / 2), _radarSpinnerColour);

        foreach ((int x, int y, Color color) in _airplaneDataCache.Data.ExtraPixels)
        {
            SafeSetPixel(x, y, color);
        }

        foreach (MatrixAircraft aircraft in _airplaneDataCache.Data.Aircraft)
        {
            DrawPlane(aircraft);
        }
        
        if (_radarSettings.ShowClock)
            _clockRendererService.Render(_matrix);

        _matrix.Update();

        await Task.Delay(10, stoppingToken);
        _matrix.Clear();
    }

    private void DrawAltitudeKey()
    {
        if (_radarSettings.ColourMode != RadarSettings.ColourModes.Altitude || !_radarSettings.ShowAltitudeKey)
            return;
        
        int interval = _matrix.ColLength / 6;
        _matrix.DrawLine(0, _matrix.RowLength - 1, interval, _matrix.RowLength - 1, new Color(150, 0, 0) / 3);
        _matrix.DrawLine(interval, _matrix.RowLength - 1, interval * 2, _matrix.RowLength - 1, new Color(130, 130, 0) / 3);
        _matrix.DrawLine(interval * 2, _matrix.RowLength - 1, interval * 3, _matrix.RowLength - 1, new Color(0, 200, 0) / 3);
        _matrix.DrawLine(interval * 3, _matrix.RowLength - 1, interval * 4, _matrix.RowLength - 1, new Color(0, 120, 180) / 3);
        _matrix.DrawLine(interval * 4, _matrix.RowLength - 1, interval * 5, _matrix.RowLength - 1, new Color(0, 0, 200) / 3);
        _matrix.DrawLine(interval * 5, _matrix.RowLength - 1, _matrix.ColLength - 1, _matrix.RowLength - 1, new Color(120, 0, 170) / 3);

        _matrix.DrawText(_font, 0, _matrix.RowLength - 2, new Color(150, 0, 0) / 3, "000");
        _matrix.DrawText(_font, interval, _matrix.RowLength - 2, new Color(130, 130, 0) / 3, "030");
        _matrix.DrawText(_font, interval * 2, _matrix.RowLength - 2, new Color(0, 200, 0) / 3, "060");
        _matrix.DrawText(_font, interval * 3, _matrix.RowLength - 2, new Color(0, 120, 180) / 3, "100");
        _matrix.DrawText(_font, interval * 4, _matrix.RowLength - 2, new Color(0, 0, 200) / 3, "200");
        _matrix.DrawText(_font, interval * 5, _matrix.RowLength - 2, new Color(120, 0, 170) / 3, "300");
    }

    private void DrawTrails()
    {
        if (!_radarSettings.Trails)
            return;
        
        foreach (var aircraftHistory in _relevantHistory)
        {
            if (_airplaneDataCache.Data.Aircraft.All(p => p.Flight != aircraftHistory.Key))
                continue;

            MatrixAircraft? previous = null;

            var latest = aircraftHistory.Value.ToList();
            latest.Reverse();
            latest = latest.Distinct(new MatrixAircraftCoordinateComparer()).Take(10).ToList();

            for (int index = 0; index < latest.Count; index++)
            {
                MatrixAircraft trace = latest[index];
                // if (trace.Altitude >= 20_000)
                //     continue;

                Color color = _airplaneColourService.GetColour(trace) / 4;

                int brightnessDivider = index / 3;
                if (brightnessDivider > 0)
                    color /= brightnessDivider;

                if (previous == null)
                    SafeSetPixel(trace.X, trace.Y, color);
                else
                {
                    _matrix.DrawLine(previous.X, previous.Y, trace.X, trace.Y, color);
                }

                previous = trace;
            }
        }
    }

    public void Pause()
    {
        _paused = true;
    }

    public void Unpause()
    {
        _paused = false;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        _matrix.Clear();
        _matrix.Update();
        _matrix.Reset();
    }

    private void DrawPlaneDot(MatrixAircraft aircraft)
    {
        SafeSetPixel(aircraft.X, aircraft.Y, _airplaneColourService.GetColour(aircraft));
    }

    private void DrawPlane(MatrixAircraft aircraft)
    {
        switch (_radarSettings.AirplaneIcon)
        {
            case RadarSettings.AirplaneIcons.Dot:
                DrawPlaneDot(aircraft);
                break;
            case RadarSettings.AirplaneIcons.Diamond:
                DrawPlaneDiamond(aircraft);
                break;
            case RadarSettings.AirplaneIcons.Arrow:
                DrawPlaneArrow(aircraft);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DrawPlaneDiamond(MatrixAircraft aircraft)
    {
        SafeSetPixel(aircraft.X, aircraft.Y, _airplaneColourService.GetColour(aircraft));
        SafeSetPixel(aircraft.X - 1, aircraft.Y, _airplaneColourService.GetColour(aircraft));
        SafeSetPixel(aircraft.X + 1, aircraft.Y, _airplaneColourService.GetColour(aircraft));
        SafeSetPixel(aircraft.X, aircraft.Y - 1, _airplaneColourService.GetColour(aircraft));
        SafeSetPixel(aircraft.X, aircraft.Y + 1, _airplaneColourService.GetColour(aircraft));
    }

    private void DrawPlaneArrow(MatrixAircraft aircraft)
    {
        int d = -1;
        double alpha = aircraft.Track ?? 0 * Math.PI / 180;
        double x = aircraft.X + d * Math.Cos(alpha);
        double y = aircraft.Y + d * Math.Sin(alpha);

        SafeSetPixel(aircraft.X, aircraft.Y, _airplaneColourService.GetColour(aircraft));
        SafeSetPixel((int)Math.Round(x), (int)Math.Round(y), _airplaneColourService.GetColour(aircraft));
        
        alpha = SubAngle(Convert.ToInt32(aircraft.Track ?? 0), 90) * Math.PI / 180;
        double lx = aircraft.X + d * Math.Cos(alpha);
        double ly = aircraft.Y + d * Math.Sin(alpha);

        alpha = AddAngle(Convert.ToInt32(aircraft.Track ?? 0), 90) * Math.PI / 180;
        double rx = aircraft.X + d * Math.Cos(alpha);
        double ry = aircraft.Y + d * Math.Sin(alpha);

        SafeSetPixel((int)Math.Round(lx), (int)Math.Round(ly), _airplaneColourService.GetColour(aircraft));
        SafeSetPixel((int)Math.Round(rx), (int)Math.Round(ry), _airplaneColourService.GetColour(aircraft));
        
        // if (aircraft.Flight?.Trim() == "ICE454")
        // {
        //     _matrix.DrawCircle(aircraft.X, aircraft.Y, 4, new Color(255, 255, 255));
        // }
    }
    
    private int AddAngle(int angle, int add)
    {
        int result = angle + add;
        if (result > 360)
            result -= 360;
        else if (result < 0)
            result += 360;
        return result;
    }
    
    private int SubAngle(int angle, int sub)
    {
        int result = angle - sub;
        if (result > 360)
            result -= 360;
        else if (result < 0)
            result += 360;
        return result;
    }

    private void SafeSetPixel(int x, int y, Color color)
    {
        if (x < 0 || x >= _matrix.ColLength || y < 0 || y >= _matrix.RowLength)
            return;
        
        _matrix.SetPixel(x, y, color);
    }

    private void DrawLine(int degrees, (double x, double y) center, Color color)
    {
        (double x, double y) current = center;
        int d = -1;
        double alpha = degrees * Math.PI / 180;

        while (true)
        {
            double x = current.x + d * Math.Cos(alpha);
            double y = current.y + d * Math.Sin(alpha);
            current = (x, y);

            if (current.x < 0 || current.y < 0 || current.x > _matrix.RowLength - 1 || current.y > _matrix.ColLength - 1)
                break;

            int ix = (int)Math.Round(current.x);
            int iy = (int)Math.Round(current.y);
            _matrix.SetPixel(ix, iy, color);
        }
    }
}
