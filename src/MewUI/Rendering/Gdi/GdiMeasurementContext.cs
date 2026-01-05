using Aprillz.MewUI.Core;
using Aprillz.MewUI.Native;
using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Rendering.Gdi;

/// <summary>
/// A lightweight graphics context for text measurement only.
/// </summary>
internal sealed class GdiMeasurementContext : IGraphicsContext
{
    private readonly nint _hdc;
    private readonly double _dpiScale;
    private bool _disposed;

    public double DpiScale => _dpiScale;

    public GdiMeasurementContext(nint hdc, uint dpi)
    {
        _hdc = hdc;
        _dpiScale = dpi <= 0 ? 1.0 : dpi / 96.0;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            User32.ReleaseDC(0, _hdc);
            _disposed = true;
        }
    }

    public Size MeasureText(string text, IFont font)
    {
        if (string.IsNullOrEmpty(text) || font is not GdiFont gdiFont)
            return Size.Empty;

        var oldFont = Gdi32.SelectObject(_hdc, gdiFont.Handle);
        try
        {
            if (Gdi32.GetTextExtentPoint32(_hdc, text, text.Length, out var size))
            {
                return new Size(size.cx / _dpiScale, size.cy / _dpiScale);
            }
            return Size.Empty;
        }
        finally
        {
            Gdi32.SelectObject(_hdc, oldFont);
        }
    }

    public Size MeasureText(string text, IFont font, double maxWidth)
    {
        // Simple approximation - for accurate wrapping measurement, we'd need more complex logic
        var singleLineSize = MeasureText(text, font);
        if (singleLineSize.Width <= maxWidth)
            return singleLineSize;

        // Estimate wrapped text height
        double estimatedLines = Math.Ceiling(singleLineSize.Width / maxWidth);
        return new Size(maxWidth, singleLineSize.Height * estimatedLines);
    }

    // Below methods are not used for measurement but required by interface
    public void Save() { }
    public void Restore() { }
    public void SetClip(Rect rect) { }
    public void Translate(double dx, double dy) { }
    public void Clear(Color color) { }
    public void DrawLine(Point start, Point end, Color color, double thickness = 1) { }
    public void DrawRectangle(Rect rect, Color color, double thickness = 1) { }
    public void FillRectangle(Rect rect, Color color) { }
    public void DrawRoundedRectangle(Rect rect, double radiusX, double radiusY, Color color, double thickness = 1) { }
    public void FillRoundedRectangle(Rect rect, double radiusX, double radiusY, Color color) { }
    public void DrawEllipse(Rect bounds, Color color, double thickness = 1) { }
    public void FillEllipse(Rect bounds, Color color) { }
    public void DrawText(string text, Point location, IFont font, Color color) { }
    public void DrawText(string text, Rect bounds, IFont font, Color color, TextAlignment horizontalAlignment = TextAlignment.Left, TextAlignment verticalAlignment = TextAlignment.Top, TextWrapping wrapping = TextWrapping.NoWrap) { }
    public void DrawImage(IImage image, Point location) { }
    public void DrawImage(IImage image, Rect destRect) { }
    public void DrawImage(IImage image, Rect destRect, Rect sourceRect) { }
}
