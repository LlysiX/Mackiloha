﻿namespace Mackiloha;

public class HMXBitmap : ISerializable
{
    public int Bpp { get; set; }
    public int Encoding { get; set; }
    public int MipMaps { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }
    public int BPL { get; set; }
    public int WiiAlphaNumber { get; set; }

    public byte[] RawData { get; set; }
}
