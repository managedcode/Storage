namespace ManagedCode.Storage.IntegrationTests.Helpers;

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
}