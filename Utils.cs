using System;
using System.IO;

namespace ADX
{
    public static class Utils
    {
        public static int ReadDwordBE(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
        }

        public static int ReadWordBE(byte[] buffer, int offset)
        {
            return (buffer[offset] << 8) | buffer[offset + 1];
        }

        public static void WriteDwordBE(byte[] p, int offset, long d)
        {
            p[offset] = (byte)(d >> 24);
            p[offset + 1] = (byte)(d >> 16);
            p[offset + 2] = (byte)(d >> 8);
            p[offset + 3] = (byte)d;
        }
        public static void WriteWordBE(byte[] p, int offset, short d)
        {
            p[offset] = (byte)(d >> 8);
            p[offset + 1] = (byte)d;
        }
        public static int ReadShorts(BinaryReader stream, short[] buffer, int offset, int count)
        {
            byte[] byteBuffer = new byte[count * sizeof(short)];
            int bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length);
            Buffer.BlockCopy(byteBuffer, 0, buffer, offset * sizeof(short), bytesRead);
            return bytesRead / sizeof(short);
        }
        public static byte[] ShortArrayToByteArray(short[] array, int length)
        {
            byte[] byteArray = new byte[length * 2];
            Buffer.BlockCopy(array, 0, byteArray, 0, length * 2);
            return byteArray;
        }

    }
}
