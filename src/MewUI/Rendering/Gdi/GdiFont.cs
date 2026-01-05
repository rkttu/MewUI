using Aprillz.MewUI.Native;
using Aprillz.MewUI.Native.Constants;

namespace Aprillz.MewUI.Rendering.Gdi;

/// <summary>
/// GDI font implementation.
/// </summary>
internal sealed class GdiFont : IFont
{
    private bool _disposed;

    public string Family { get; }
    public double Size { get; }
    public FontWeight Weight { get; }
    public bool IsItalic { get; }
    public bool IsUnderline { get; }
    public bool IsStrikethrough { get; }

    internal nint Handle { get; private set; }

    public GdiFont(string family, double size, FontWeight weight, bool italic, bool underline, bool strikethrough, uint dpi)
    {
        Family = family;
        Size = size;
        Weight = weight;
        IsItalic = italic;
        IsUnderline = underline;
        IsStrikethrough = strikethrough;

        // Font size in this framework is in DIPs (1/96 inch). Convert to pixels for GDI.
        // Negative height means use character height, not cell height.
        int height = -(int)Math.Round(size * dpi / 96.0, MidpointRounding.AwayFromZero);

        Handle = Gdi32.CreateFont(
            height,
            0, 0, 0,
            (int)weight,
            italic ? 1u : 0u,
            underline ? 1u : 0u,
            strikethrough ? 1u : 0u,
            GdiConstants.DEFAULT_CHARSET,
            GdiConstants.OUT_TT_PRECIS,
            GdiConstants.CLIP_DEFAULT_PRECIS,
            GdiConstants.CLEARTYPE_QUALITY,
            GdiConstants.DEFAULT_PITCH | GdiConstants.FF_DONTCARE,
            family
        );

        if (Handle == 0)
        {
            throw new InvalidOperationException($"Failed to create font: {family}");
        }
    }

    public void Dispose()
    {
        if (!_disposed && Handle != 0)
        {
            Gdi32.DeleteObject(Handle);
            Handle = 0;
            _disposed = true;
        }
    }
}
