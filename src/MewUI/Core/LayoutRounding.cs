using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Core;

internal static class LayoutRounding
{
    public static Rect RoundRectToPixels(Rect rect, double dpiScale)
    {
        if (dpiScale <= 0 || double.IsNaN(dpiScale) || double.IsInfinity(dpiScale))
            return rect;

        if (rect.IsEmpty)
            return rect;

        double left = RoundToPixel(rect.Left, dpiScale);
        double top = RoundToPixel(rect.Top, dpiScale);
        double right = RoundToPixel(rect.Right, dpiScale);
        double bottom = RoundToPixel(rect.Bottom, dpiScale);

        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    public static int RoundToPixelInt(double value, double dpiScale)
    {
        if (dpiScale <= 0 || double.IsNaN(dpiScale) || double.IsInfinity(dpiScale))
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);

        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0;

        return (int)Math.Round(value * dpiScale, MidpointRounding.AwayFromZero);
    }

    private static double RoundToPixel(double value, double dpiScale)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return value;

        // WPF-style: avoid banker's rounding to reduce jitter at .5 boundaries (e.g. 150% DPI).
        return Math.Round(value * dpiScale, MidpointRounding.AwayFromZero) / dpiScale;
    }
}
