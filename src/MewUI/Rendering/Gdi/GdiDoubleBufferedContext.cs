using Aprillz.MewUI.Native;
using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Rendering.Gdi;

/// <summary>
/// A double-buffered GDI graphics context that renders to an off-screen buffer
/// and blits to the screen on dispose to reduce flickering.
/// </summary>
internal sealed class GdiDoubleBufferedContext : IGraphicsContext
{
    private readonly nint _hwnd;
    private readonly nint _screenDc;
    private readonly nint _memDc;
    private readonly nint _bitmap;
    private readonly nint _oldBitmap;
    private readonly GdiGraphicsContext _context;
    private readonly int _width;
    private readonly int _height;
    private bool _disposed;

    public double DpiScale => _context.DpiScale;

    public GdiDoubleBufferedContext(nint hwnd, nint screenDc, double dpiScale)
    {
        _hwnd = hwnd;
        _screenDc = screenDc;

        // Get client area size
        User32.GetClientRect(hwnd, out var clientRect);
        _width = clientRect.Width;
        _height = clientRect.Height;

        if (_width <= 0) _width = 1;
        if (_height <= 0) _height = 1;

        // Create memory DC and bitmap
        _memDc = Gdi32.CreateCompatibleDC(screenDc);
        _bitmap = Gdi32.CreateCompatibleBitmap(screenDc, _width, _height);
        _oldBitmap = Gdi32.SelectObject(_memDc, _bitmap);

        // Create the inner context that renders to the memory DC
        _context = new GdiGraphicsContext(hwnd, _memDc, dpiScale, false);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Blit from back buffer to screen
            Gdi32.BitBlt(_screenDc, 0, 0, _width, _height, _memDc, 0, 0, 0x00CC0020); // SRCCOPY

            // Clean up
            _context.Dispose();
            Gdi32.SelectObject(_memDc, _oldBitmap);
            Gdi32.DeleteObject(_bitmap);
            Gdi32.DeleteDC(_memDc);

            _disposed = true;
        }
    }

    // Delegate all methods to the inner context
    public void Save() => _context.Save();
    public void Restore() => _context.Restore();
    public void SetClip(Rect rect) => _context.SetClip(rect);
    public void Translate(double dx, double dy) => _context.Translate(dx, dy);
    public void Clear(Color color) => _context.Clear(color);
    public void DrawLine(Point start, Point end, Color color, double thickness = 1) => _context.DrawLine(start, end, color, thickness);
    public void DrawRectangle(Rect rect, Color color, double thickness = 1) => _context.DrawRectangle(rect, color, thickness);
    public void FillRectangle(Rect rect, Color color) => _context.FillRectangle(rect, color);
    public void DrawRoundedRectangle(Rect rect, double radiusX, double radiusY, Color color, double thickness = 1) => _context.DrawRoundedRectangle(rect, radiusX, radiusY, color, thickness);
    public void FillRoundedRectangle(Rect rect, double radiusX, double radiusY, Color color) => _context.FillRoundedRectangle(rect, radiusX, radiusY, color);
    public void DrawEllipse(Rect bounds, Color color, double thickness = 1) => _context.DrawEllipse(bounds, color, thickness);
    public void FillEllipse(Rect bounds, Color color) => _context.FillEllipse(bounds, color);
    public void DrawText(string text, Point location, IFont font, Color color) => _context.DrawText(text, location, font, color);
    public void DrawText(string text, Rect bounds, IFont font, Color color, TextAlignment horizontalAlignment = TextAlignment.Left, TextAlignment verticalAlignment = TextAlignment.Top, TextWrapping wrapping = TextWrapping.NoWrap) => _context.DrawText(text, bounds, font, color, horizontalAlignment, verticalAlignment, wrapping);
    public Size MeasureText(string text, IFont font) => _context.MeasureText(text, font);
    public Size MeasureText(string text, IFont font, double maxWidth) => _context.MeasureText(text, font, maxWidth);
    public void DrawImage(IImage image, Point location) => _context.DrawImage(image, location);
    public void DrawImage(IImage image, Rect destRect) => _context.DrawImage(image, destRect);
    public void DrawImage(IImage image, Rect destRect, Rect sourceRect) => _context.DrawImage(image, destRect, sourceRect);
}
