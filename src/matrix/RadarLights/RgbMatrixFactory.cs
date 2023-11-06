using RpiLedMatrix;

namespace RadarLights;

public class RgbMatrixFactory
{
    private readonly AppConfig _appConfig;

    public RgbMatrixFactory(AppConfig appConfig)
    {
        _appConfig = appConfig;
    }
    
    public RGBLedMatrix CreateLedMatrix()
    {
        return new RGBLedMatrix(_appConfig.Matrix);
    }
}
