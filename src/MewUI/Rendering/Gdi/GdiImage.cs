using Aprillz.MewUI.Native;
using Aprillz.MewUI.Native.Structs;

namespace Aprillz.MewUI.Rendering.Gdi;

/// <summary>
/// GDI bitmap image implementation.
/// </summary>
internal sealed class GdiImage : IImage
{
    private nint _bits;
    private bool _disposed;

    public int PixelWidth { get; }
    public int PixelHeight { get; }

    internal nint Handle { get; private set; }
    internal nint Bits => _bits;

    /// <summary>
    /// Creates a 32-bit ARGB bitmap.
    /// </summary>
    public GdiImage(int width, int height)
    {
        PixelWidth = width;
        PixelHeight = height;

        var bmi = BITMAPINFO.Create32bpp(width, height);

        nint screenDc = User32.GetDC(0);
        try
        {
            Handle = Gdi32.CreateDIBSection(screenDc, ref bmi, 0, out _bits, 0, 0);
            if (Handle == 0)
            {
                throw new InvalidOperationException("Failed to create DIB section");
            }
        }
        finally
        {
            User32.ReleaseDC(0, screenDc);
        }
    }

    /// <summary>
    /// Creates an image from raw pixel data (BGRA format).
    /// </summary>
    public GdiImage(int width, int height, byte[] pixelData) : this(width, height)
    {
        if (pixelData.Length != width * height * 4)
        {
            throw new ArgumentException("Invalid pixel data size", nameof(pixelData));
        }

        unsafe
        {
            fixed (byte* src = pixelData)
            {
                Buffer.MemoryCopy(src, (void*)_bits, pixelData.Length, pixelData.Length);
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed && Handle != 0)
        {
            Gdi32.DeleteObject(Handle);
            Handle = 0;
            _bits = 0;
            _disposed = true;
        }
    }
}
