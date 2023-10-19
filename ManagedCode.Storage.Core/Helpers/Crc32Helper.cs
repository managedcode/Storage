using System.IO;

namespace ManagedCode.Storage.Core.Helpers;

public static class Crc32Helper
{
    private static readonly uint[] Crc32Table;
    private const uint polynomial = 0xedb88320;

    static Crc32Helper()
    {
        Crc32Table = new uint[256];
        
        for (int i = 0; i < 256; i++)
        {
            uint crc = (uint)i;
            for (int j = 8; j > 0; j--)
            {
                if ((crc & 1) == 1)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
            Crc32Table[i] = crc;
        }
    }

    public static uint Calculate(byte[] bytes)
    {
        uint crcValue = 0xffffffff;

        foreach (byte by in bytes)
        {
            byte tableIndex = (byte)(((crcValue) & 0xff) ^ by);
            crcValue = Crc32Table[tableIndex] ^ (crcValue >> 8);
        }
        return ~crcValue;
    }
    
    public static uint Calculate(Stream stream)
    {
        var bytes = StreamToByteArray(stream);
        uint crcValue = 0xffffffff;

        foreach (byte by in bytes)
        {
            byte tableIndex = (byte)(((crcValue) & 0xff) ^ by);
            crcValue = Crc32Table[tableIndex] ^ (crcValue >> 8);
        }
        return ~crcValue;
    }
    
    public static uint CalculateFileCRC(string filePath)
    {
        uint crcValue = 0xffffffff;
        
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            byte[] buffer = new byte[4096]; // 4KB buffer
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                crcValue = Calculate(buffer, crcValue);
            }
        }

        return ~crcValue;  // Return the final CRC value
    }
    
    public static uint Calculate(byte[] bytes, uint crcValue = 0xffffffff)
    {
        foreach (byte by in bytes)
        {
            byte tableIndex = (byte)(((crcValue) & 0xff) ^ by);
            crcValue = Crc32Table[tableIndex] ^ (crcValue >> 8);
        }
        return crcValue;
    }
    
    private static byte[] StreamToByteArray(Stream stream)
    {
        // Create a memory stream to store the data from the input stream
        using (MemoryStream memoryStream = new MemoryStream())
        {
            int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            // Read from the input stream and write to the memory stream
            while ((bytesRead = stream.Read(buffer, 0, bufferSize)) > 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
            }

            // Convert the memory stream to a byte array
            return memoryStream.ToArray();
        }
    }
}