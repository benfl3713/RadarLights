namespace RpiLedMatrix;

/// <summary>
/// Represents an RGB (red, green, blue) color
/// </summary>
public struct Color
{
    /// <summary>
    /// The red component value of this instance.
    /// </summary>
    public byte R;

    /// <summary>
    /// The green component value of this instance.
    /// </summary>
    public byte G;

    /// <summary>
    /// The blue component value of this instance.
    /// </summary>
    public byte B;

    /// <summary>
    /// Creates a new color from the specified color values (red, green, and blue).
    /// </summary>
    /// <param name="r">The red component value.</param>
    /// <param name="g">The green component value.</param>
    /// <param name="b">The blue component value.</param>
    public Color(int r, int g, int b) : this((byte)r, (byte)g, (byte)b) { }

    /// <summary>
    /// Creates a new color from the specified color values (red, green, and blue).
    /// </summary>
    /// <param name="r">The red component value.</param>
    /// <param name="g">The green component value.</param>
    /// <param name="b">The blue component value.</param>
    public Color(byte r, byte g, byte b) => (R, G, B) = (r, g, b);

    public override bool Equals(object? obj)
    {
        if (obj?.GetType() != typeof(Color))
            return false;

        Color c = (Color)obj;

        return R == c.R && G == c.G && B == c.B;
    }
    
    public override int GetHashCode()
    {
        return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode();
    }

    public override string ToString()
    {
        return $"(R: {R}, G: {G}, B: {B})";
    }
    
    public static Color operator /(Color a, int b)
    {
        return new Color(a.R / b, a.G / b, a.B / b);
    }
    
    public static Color operator *(Color a, int b)
    {
        return new Color(a.R * b, a.G * b, a.B * b);
    }
    
    public static Color operator +(Color a, Color b)
    {
        return new Color(a.R + b.R, a.G + b.G, a.B + b.B);
    }
    
    public static Color operator -(Color a, Color b)
    {
        return new Color(a.R - b.R, a.G - b.G, a.B - b.B);
    }

    public static Color FromString(string colorString)
    {
        string[] parts = colorString.Split(',');
        if (parts.Length != 3)
            throw new ArgumentException("Color string must be in the format 'R,G,B'");
        
        return new Color(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }
}
