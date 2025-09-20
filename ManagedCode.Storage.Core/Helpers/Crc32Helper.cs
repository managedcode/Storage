using System;
using System.IO;

namespace ManagedCode.Storage.Core.Helpers;

public static class Crc32Helper
{
    private const uint Polynomial = 0xedb88320;
    private static readonly uint[] Crc32Table;

    static Crc32Helper()
    {
        Crc32Table = new uint[256];

        for (var i = 0; i < 256; i++)
        {
            var crc = (uint)i;
            for (var j = 8; j > 0; j--)
                if ((crc & 1) == 1)
                    crc = (crc >> 1) ^ Polynomial;
                else
                    crc >>= 1;

            Crc32Table[i] = crc;
        }
    }

    public static uint Calculate(byte[] bytes)
    {
        var crcValue = UpdateCrc(bytes);
        return Complete(crcValue);
    }

    public static uint CalculateFileCrc(string filePath)
    {
        var crcValue = Begin();

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            var buffer = new byte[4096]; // 4KB buffer
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                crcValue = Update(buffer.AsSpan(0, bytesRead), crcValue);
            }
        }

        return Complete(crcValue); // Return the final CRC value
    }

    public static uint Begin() => 0xffffffff;

    public static uint Update(ReadOnlySpan<byte> bytes, uint crcValue = 0xffffffff)
    {
        foreach (var by in bytes)
        {
            var tableIndex = (byte)((crcValue & 0xff) ^ by);
            crcValue = Crc32Table[tableIndex] ^ (crcValue >> 8);
        }

        return crcValue;
    }

    public static uint Update(uint current, ReadOnlySpan<byte> bytes)
    {
        return Update(bytes, current);
    }

    public static uint Complete(uint crcValue) => ~crcValue;

    private static uint UpdateCrc(ReadOnlySpan<byte> bytes)
    {
        return Update(bytes, Begin());
    }
}
