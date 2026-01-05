using Aprillz.MewUI.Native;
using Aprillz.MewUI.Native.Constants;
using Aprillz.MewUI.Native.Structs;
using Aprillz.MewUI.Core;
using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Rendering.Gdi;

/// <summary>
/// GDI graphics context implementation.
/// </summary>
internal sealed class GdiGraphicsContext : IGraphicsContext
{
    private readonly nint _hwnd;
    private readonly bool _ownsDc;
    private readonly Stack<int> _savedStates = new();
    private double _translateX;
    private double _translateY;
    private bool _disposed;

    public double DpiScale { get; }

    internal nint Hdc { get; }

    public GdiGraphicsContext(nint hwnd, nint hdc, double dpiScale, bool ownsDc = false)
    {
        _hwnd = hwnd;
        Hdc = hdc;
        _ownsDc = ownsDc;
        DpiScale = dpiScale;

        // Set default modes
        Gdi32.SetBkMode(Hdc, GdiConstants.TRANSPARENT);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ownsDc && Hdc != 0)
            {
                User32.ReleaseDC(_hwnd, Hdc);
            }
            _disposed = true;
        }
    }

    #region State Management

    public void Save()
    {
        int state = Gdi32.SaveDC(Hdc);
        _savedStates.Push(state);
    }

    public void Restore()
    {
        if (_savedStates.Count > 0)
        {
            int state = _savedStates.Pop();
            Gdi32.RestoreDC(Hdc, state);
        }
    }

    public void SetClip(Rect rect)
    {
        var r = ToDeviceRect(rect);
        Gdi32.IntersectClipRect(Hdc, r.left, r.top, r.right, r.bottom);
    }

    public void Translate(double dx, double dy)
    {
        _translateX += dx;
        _translateY += dy;
    }

    #endregion

    #region Drawing Primitives

    public void Clear(Color color)
    {
        var brush = Gdi32.CreateSolidBrush(color.ToCOLORREF());
        try
        {
            User32.GetClientRect(_hwnd, out var rect);
            Gdi32.FillRect(Hdc, ref rect, brush);
        }
        finally
        {
            Gdi32.DeleteObject(brush);
        }
    }

    public void DrawLine(Point start, Point end, Color color, double thickness = 1)
    {
        var pen = Gdi32.CreatePen(GdiConstants.PS_SOLID, (int)(thickness * DpiScale), color.ToCOLORREF());
        var oldPen = Gdi32.SelectObject(Hdc, pen);
        try
        {
            var p1 = ToDevicePoint(start);
            var p2 = ToDevicePoint(end);
            Gdi32.MoveToEx(Hdc, p1.x, p1.y, out _);
            Gdi32.LineTo(Hdc, p2.x, p2.y);
        }
        finally
        {
            Gdi32.SelectObject(Hdc, oldPen);
            Gdi32.DeleteObject(pen);
        }
    }

    public void DrawRectangle(Rect rect, Color color, double thickness = 1)
    {
        var pen = Gdi32.CreatePen(GdiConstants.PS_SOLID, (int)(thickness * DpiScale), color.ToCOLORREF());
        var nullBrush = Gdi32.GetStockObject(GdiConstants.NULL_BRUSH);
        var oldPen = Gdi32.SelectObject(Hdc, pen);
        var oldBrush = Gdi32.SelectObject(Hdc, nullBrush);
        try
        {
            var r = ToDeviceRect(rect);
            Gdi32.Rectangle(Hdc, r.left, r.top, r.right, r.bottom);
        }
        finally
        {
            Gdi32.SelectObject(Hdc, oldPen);
            Gdi32.SelectObject(Hdc, oldBrush);
            Gdi32.DeleteObject(pen);
        }
    }

    public void FillRectangle(Rect rect, Color color)
    {
        var brush = Gdi32.CreateSolidBrush(color.ToCOLORREF());
        try
        {
            var r = ToDeviceRect(rect);
            Gdi32.FillRect(Hdc, ref r, brush);
        }
        finally
        {
            Gdi32.DeleteObject(brush);
        }
    }

    public void DrawRoundedRectangle(Rect rect, double radiusX, double radiusY, Color color, double thickness = 1)
    {
        var pen = Gdi32.CreatePen(GdiConstants.PS_SOLID, (int)(thickness * DpiScale), color.ToCOLORREF());
        var nullBrush = Gdi32.GetStockObject(GdiConstants.NULL_BRUSH);
        var oldPen = Gdi32.SelectObject(Hdc, pen);
        var oldBrush = Gdi32.SelectObject(Hdc, nullBrush);
        try
        {
            var r = ToDeviceRect(rect);
            int rx = (int)(radiusX * DpiScale);
            int ry = (int)(radiusY * DpiScale);
            Gdi32.RoundRect(Hdc, r.left, r.top, r.right, r.bottom, rx * 2, ry * 2);
        }
        finally
        {
            Gdi32.SelectObject(Hdc, oldPen);
            Gdi32.SelectObject(Hdc, oldBrush);
            Gdi32.DeleteObject(pen);
        }
    }

    public void FillRoundedRectangle(Rect rect, double radiusX, double radiusY, Color color)
    {
        var brush = Gdi32.CreateSolidBrush(color.ToCOLORREF());
        var nullPen = Gdi32.GetStockObject(GdiConstants.NULL_PEN);
        var oldBrush = Gdi32.SelectObject(Hdc, brush);
        var oldPen = Gdi32.SelectObject(Hdc, nullPen);
        try
        {
            var r = ToDeviceRect(rect);
            int rx = (int)(radiusX * DpiScale);
            int ry = (int)(radiusY * DpiScale);
            // Add 1 to compensate for NULL_PEN
            Gdi32.RoundRect(Hdc, r.left, r.top, r.right + 1, r.bottom + 1, rx * 2, ry * 2);
        }
        finally
        {
            Gdi32.SelectObject(Hdc, oldPen);
            Gdi32.SelectObject(Hdc, oldBrush);
            Gdi32.DeleteObject(brush);
        }
    }

    public void DrawEllipse(Rect bounds, Color color, double thickness = 1)
    {
        var pen = Gdi32.CreatePen(GdiConstants.PS_SOLID, (int)(thickness * DpiScale), color.ToCOLORREF());
        var nullBrush = Gdi32.GetStockObject(GdiConstants.NULL_BRUSH);
        var oldPen = Gdi32.SelectObject(Hdc, pen);
        var oldBrush = Gdi32.SelectObject(Hdc, nullBrush);
        try
        {
            var r = ToDeviceRect(bounds);
            Gdi32.Ellipse(Hdc, r.left, r.top, r.right, r.bottom);
        }
        finally
        {
            Gdi32.SelectObject(Hdc, oldPen);
            Gdi32.SelectObject(Hdc, oldBrush);
            Gdi32.DeleteObject(pen);
        }
    }

    public void FillEllipse(Rect bounds, Color color)
    {
        var brush = Gdi32.CreateSolidBrush(color.ToCOLORREF());
        var nullPen = Gdi32.GetStockObject(GdiConstants.NULL_PEN);
        var oldBrush = Gdi32.SelectObject(Hdc, brush);
        var oldPen = Gdi32.SelectObject(Hdc, nullPen);
        try
        {
            var r = ToDeviceRect(bounds);
            Gdi32.Ellipse(Hdc, r.left, r.top, r.right + 1, r.bottom + 1);
        }
        finally
        {
            Gdi32.SelectObject(Hdc, oldPen);
            Gdi32.SelectObject(Hdc, oldBrush);
            Gdi32.DeleteObject(brush);
        }
    }

    #endregion

    #region Text Rendering

    public void DrawText(string text, Point location, IFont font, Color color)
    {
        if (font is not GdiFont gdiFont)
            throw new ArgumentException("Font must be a GdiFont", nameof(font));

        var oldFont = Gdi32.SelectObject(Hdc, gdiFont.Handle);
        var oldColor = Gdi32.SetTextColor(Hdc, color.ToCOLORREF());
        try
        {
            var pt = ToDevicePoint(location);
            Gdi32.TextOut(Hdc, pt.x, pt.y, text, text.Length);
        }
        finally
        {
            Gdi32.SetTextColor(Hdc, oldColor);
            Gdi32.SelectObject(Hdc, oldFont);
        }
    }

    public void DrawText(string text, Rect bounds, IFont font, Color color,
        TextAlignment horizontalAlignment = TextAlignment.Left,
        TextAlignment verticalAlignment = TextAlignment.Top,
        TextWrapping wrapping = TextWrapping.NoWrap)
    {
        if (font is not GdiFont gdiFont)
            throw new ArgumentException("Font must be a GdiFont", nameof(font));

        var oldFont = Gdi32.SelectObject(Hdc, gdiFont.Handle);
        var oldColor = Gdi32.SetTextColor(Hdc, color.ToCOLORREF());
        try
        {
            var r = ToDeviceRect(bounds);
            uint format = GdiConstants.DT_NOPREFIX;

            format |= horizontalAlignment switch
            {
                TextAlignment.Left => GdiConstants.DT_LEFT,
                TextAlignment.Center => GdiConstants.DT_CENTER,
                TextAlignment.Right => GdiConstants.DT_RIGHT,
                _ => GdiConstants.DT_LEFT
            };

            if (wrapping == TextWrapping.NoWrap)
            {
                format |= GdiConstants.DT_SINGLELINE;

                format |= verticalAlignment switch
                {
                    TextAlignment.Top => GdiConstants.DT_TOP,
                    TextAlignment.Center => GdiConstants.DT_VCENTER,
                    TextAlignment.Bottom => GdiConstants.DT_BOTTOM,
                    _ => GdiConstants.DT_TOP
                };
            }
            else
            {
                format |= GdiConstants.DT_WORDBREAK;
            }

            Gdi32.DrawText(Hdc, text, text.Length, ref r, format);
        }
        finally
        {
            Gdi32.SetTextColor(Hdc, oldColor);
            Gdi32.SelectObject(Hdc, oldFont);
        }
    }

    public Size MeasureText(string text, IFont font)
    {
        if (font is not GdiFont gdiFont)
            throw new ArgumentException("Font must be a GdiFont", nameof(font));

        var oldFont = Gdi32.SelectObject(Hdc, gdiFont.Handle);
        try
        {
            if (Gdi32.GetTextExtentPoint32(Hdc, text, text.Length, out var size))
            {
                return new Size(size.cx / DpiScale, size.cy / DpiScale);
            }
            return Size.Empty;
        }
        finally
        {
            Gdi32.SelectObject(Hdc, oldFont);
        }
    }

    public Size MeasureText(string text, IFont font, double maxWidth)
    {
        if (font is not GdiFont gdiFont)
            throw new ArgumentException("Font must be a GdiFont", nameof(font));

        var oldFont = Gdi32.SelectObject(Hdc, gdiFont.Handle);
        try
        {
            var rect = new RECT(0, 0, (int)(maxWidth * DpiScale), int.MaxValue);
            Gdi32.DrawText(Hdc, text, text.Length, ref rect, GdiConstants.DT_CALCRECT | GdiConstants.DT_WORDBREAK | GdiConstants.DT_NOPREFIX);
            return new Size(rect.Width / DpiScale, rect.Height / DpiScale);
        }
        finally
        {
            Gdi32.SelectObject(Hdc, oldFont);
        }
    }

    #endregion

    #region Image Rendering

    public void DrawImage(IImage image, Point location)
    {
        if (image is not GdiImage gdiImage)
            throw new ArgumentException("Image must be a GdiImage", nameof(image));

        DrawImage(gdiImage, new Rect(location.X, location.Y, image.PixelWidth, image.PixelHeight));
    }

    public void DrawImage(IImage image, Rect destRect)
    {
        if (image is not GdiImage gdiImage)
            throw new ArgumentException("Image must be a GdiImage", nameof(image));

        DrawImage(gdiImage, destRect, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
    }

    public void DrawImage(IImage image, Rect destRect, Rect sourceRect)
    {
        if (image is not GdiImage gdiImage)
            throw new ArgumentException("Image must be a GdiImage", nameof(image));

        var memDc = Gdi32.CreateCompatibleDC(Hdc);
        var oldBitmap = Gdi32.SelectObject(memDc, gdiImage.Handle);
        try
        {
            var dest = ToDeviceRect(destRect);
            int srcX = (int)sourceRect.X;
            int srcY = (int)sourceRect.Y;
            int srcW = (int)sourceRect.Width;
            int srcH = (int)sourceRect.Height;

            // Use alpha blending for 32-bit images
            var blend = BLENDFUNCTION.SourceOver(255);
            Gdi32.AlphaBlend(
                Hdc, dest.left, dest.top, dest.Width, dest.Height,
                memDc, srcX, srcY, srcW, srcH,
                blend);
        }
        finally
        {
            Gdi32.SelectObject(memDc, oldBitmap);
            Gdi32.DeleteDC(memDc);
        }
    }

    #endregion

    #region Helper Methods

    private POINT ToDevicePoint(Point pt) => new POINT(
            LayoutRounding.RoundToPixelInt(pt.X + _translateX, DpiScale),
            LayoutRounding.RoundToPixelInt(pt.Y + _translateY, DpiScale)
        );

    private RECT ToDeviceRect(Rect rect) => RECT.FromLTRB(
            LayoutRounding.RoundToPixelInt(rect.X + _translateX, DpiScale),
            LayoutRounding.RoundToPixelInt(rect.Y + _translateY, DpiScale),
            LayoutRounding.RoundToPixelInt(rect.Right + _translateX, DpiScale),
            LayoutRounding.RoundToPixelInt(rect.Bottom + _translateY, DpiScale)
        );

    #endregion
}
