using RpiLedMatrix;
using Color = RpiLedMatrix.Color;

namespace RadarLights.Services.Renderers;

public class ClockRendererService
{
    private readonly RGBLedFont _font = new RGBLedFont("./Fonts/5x8.bdf");
    
    public void Render(ILedMatrix matrix)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        int startCol = matrix.ColLength / 2 - 20;
        matrix.DrawText(_font, startCol, 6, new Color(150, 150, 150), time);
    }
}