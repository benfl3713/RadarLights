using RadarLights.Services;

namespace RadarLights;

public static class StartupHelper
{
    public static async Task ShowSplashScreen(this WebApplication app)
    {
        var matrix = app.Services.GetRequiredService<ILedMatrix>();
        matrix.Clear();

        using var image = Image.Load<Rgb24>("Assets/logo.png");
        image.Mutate(i => i.Resize(matrix.ColLength, matrix.RowLength));
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgb24> pixelRow = accessor.GetRowSpan(y);

                // pixelRow.Length has the same value as accessor.Width,
                // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                for (int x = 0; x < pixelRow.Length; x++)
                {
                    // Get a reference to the pixel at position x
                    ref Rgb24 pixel = ref pixelRow[x];
                    matrix.SetPixel(x, y, new RpiLedMatrix.Color(pixel.R, pixel.G, pixel.B));
                }
            }
        });
        matrix.Update();
        await Task.Delay(5000);
    }

    public static void SetupRadarSettingsListener(this WebApplication app)
    {
        var renderer = app.Services.GetRequiredService<PlaneRenderService>();

        RadarSettings.SettingsUpdated += async (sender, _) =>
        {
            var settings = (RadarSettings)sender!;
            if (settings.Enabled == RadarSettings.Load().Enabled)
                return;
            if (settings.Enabled == false)
            {
                Console.WriteLine("Stopping renderer");
                await renderer.StopAsync(CancellationToken.None);
            }
            else
            {
                Console.WriteLine("Starting renderer");
                await renderer.StartAsync(CancellationToken.None);
            }
        };
    }
}
