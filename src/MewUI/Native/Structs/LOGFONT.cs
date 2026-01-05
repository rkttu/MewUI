using System.Runtime.InteropServices;

namespace Aprillz.MewUI.Native.Structs;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct LOGFONT
{
    public int lfHeight;
    public int lfWidth;
    public int lfEscapement;
    public int lfOrientation;
    public int lfWeight;
    public byte lfItalic;
    public byte lfUnderline;
    public byte lfStrikeOut;
    public byte lfCharSet;
    public byte lfOutPrecision;
    public byte lfClipPrecision;
    public byte lfQuality;
    public byte lfPitchAndFamily;
    public fixed char lfFaceName[32];

    public void SetFaceName(string name)
    {
        fixed (char* ptr = lfFaceName)
        {
            int len = Math.Min(name.Length, 31);
            for (int i = 0; i < len; i++)
                ptr[i] = name[i];
            ptr[len] = '\0';
        }
    }
}
