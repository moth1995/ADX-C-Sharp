using System;
using System.IO;
using System.Text;
namespace ADX
{
    struct ChannelState
    {
        public int s1, s2;
    }
    public static class ADX
    {
        /*
        Disclaimer: this is not an original code, I just converted the C code made back then and turn it into C# compatible from .net framework 3.5
        the only extra code added was the loop logic when converting from wav to adx, most information taken from wikipedia
        https://en.wikipedia.org/wiki/ADX_(file_format)
        https://en.wikipedia.org/wiki/WAV
        Loop function only works for full file loop
         */

        /*
            adv2wav

            (c)2001 BERO

            http://www.geocities.co.jp/Playtown/2004/
            bero@geocities.co.jp

            adx info from: http://ku-www.ss.titech.ac.jp/~yatsushi/adx.html

        */
        const int BASEVOL = 0x4000;
        const int BLOCK_SIZE = 0x12;
        const int BLOCK_SAMPLES = 32;
        const int WAV_HEADER_SIZE = 44;
        const int ADX_HEADER_SIZE = 44;
        const byte ADX_ENCODING_TYPE = 0x03;
        const byte ADX_SAMPLE_BITDEPTH = 0x04;
        const byte ADX_VERSION = 3;
        const byte ADX_FLAGS = 0;
        const short ADX_HIGHPASS_FREQ = 500;
        const string ADX_COPYRIGHT = "(c)CRI";
        const int LOOP_START_OFFSET = 2048; // most adx files I've seen have their starting sample at offset 2048 always

        /// <summary>
        /// Converts a ADX file into wav and returns an array of bytes.
        /// 
        /// <example>
        /// Example of usage:
        /// <code>
        /// string AdxFilePath = "myADXFilePath.adx";
        /// string WavFilePath = "myWAVFilePath.adx";
        /// byte[] adxData = File.ReadAllBytes(AdxFilePath);
        /// byte[] wavData = ToWav(adxData);
        /// File.WriteAllBytes(WavFilePath, wavData);
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="adxData">The adx file byte array to convert.</param>
        /// <returns>A byte Array containing the wav data</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static byte[] ToWav(byte[] adxData)
        {
            using (MemoryStream adxStream = new MemoryStream(adxData))
            using (BinaryReader adxReader = new BinaryReader(adxStream))
            using (MemoryStream wavStream = new MemoryStream())
            using (BinaryWriter wavWriter = new BinaryWriter(wavStream))
            {
                byte[] buf = new byte[BLOCK_SIZE * 2];
                short[] outbuf = new short[BLOCK_SAMPLES * 2];

                adxReader.Read(buf, 0, 16);

                int channel = buf[7];
                int freq = Utils.ReadDwordBE(buf, 8);
                int size = Utils.ReadDwordBE(buf, 12);

                int offset = Utils.ReadWordBE(buf, 2) - 2;
                adxReader.BaseStream.Seek(offset, SeekOrigin.Begin);
                adxReader.Read(buf, 1, 6);

                if (buf[0] != 0x80 || Encoding.ASCII.GetString(buf, 1, 6) != ADX_COPYRIGHT)
                {
                    throw new InvalidOperationException("Not a valid ADX file.");
                }

                WriteWavHeader(wavWriter, channel, freq, size);

                ChannelState[] channelState = new ChannelState[2];
                channelState[0].s1 = channelState[0].s2 = 0;
                channelState[1].s1 = channelState[1].s2 = 0;

                if (channel == 1)
                {
                    while (size > 0)
                    {
                        adxReader.Read(buf, 0, BLOCK_SIZE);
                        Decode(outbuf, 0, buf, 0, ref channelState[0]);
                        int wsize = size > BLOCK_SAMPLES ? BLOCK_SAMPLES : size;
                        size -= wsize;
                        wavWriter.Write(Utils.ShortArrayToByteArray(outbuf, wsize));
                    }
                }
                else if (channel == 2)
                {
                    while (size > 0)
                    {
                        short[] tmpbuf = new short[BLOCK_SAMPLES * 2];
                        int i;

                        adxReader.Read(buf, 0, BLOCK_SIZE * 2);
                        Decode(tmpbuf, 0, buf, 0, ref channelState[0]);
                        Decode(tmpbuf, BLOCK_SAMPLES, buf, BLOCK_SIZE, ref channelState[1]);
                        for (i = 0; i < BLOCK_SAMPLES; i++)
                        {
                            outbuf[i * 2] = tmpbuf[i];
                            outbuf[i * 2 + 1] = tmpbuf[i + BLOCK_SAMPLES];
                        }
                        int wsize = size > BLOCK_SAMPLES ? BLOCK_SAMPLES : size;
                        size -= wsize;
                        wavWriter.Write(Utils.ShortArrayToByteArray(outbuf, wsize * 2));
                    }
                }

                return wavStream.ToArray();
            }
        }
        /// <summary>
        /// Converts a WAV file into wav and returns an array of bytes.
        /// <example>
        /// Example of usage:
        /// <code>
        /// string WavFilePath = "myWAVFilePath.adx";
        /// string AdxFilePath = "myADXFilePath.adx";
        /// byte[] wavData = File.ReadAllBytes(WavFilePath);
        /// byte[] adxData = FromWav(wavData);
        /// File.WriteAllBytes(AdxFilePath, adxData);
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="wavData">The wav file byte array to convert.</param>
        /// <param name="loop">If you need your whole file to loop.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static byte[] FromWav(byte[] wavData, bool loop)
        {
            using (MemoryStream wavStream = new MemoryStream(wavData))
            using (BinaryReader wavReader = new BinaryReader(wavStream))
            using (MemoryStream adxStream = new MemoryStream())
            using (BinaryWriter adxWriter = new BinaryWriter(adxStream))
            {
                byte[] adxbuf = new byte[BLOCK_SIZE * 2];
                short[] wavbuf = new short[BLOCK_SAMPLES * 2];
                int channel, freq, size, wsize;
                ChannelState[] channelState = new ChannelState[2];

                byte[] wavhdr = new byte[WAV_HEADER_SIZE];
                wavReader.Read(wavhdr, 0, wavhdr.Length);

                if (Encoding.ASCII.GetString(wavhdr, 0, 4) != "RIFF" ||
                    Encoding.ASCII.GetString(wavhdr, 8, 8) != "WAVEfmt " ||
                    BitConverter.ToInt32(wavhdr, 16) != 0x10 ||
                    BitConverter.ToInt16(wavhdr, 20) != 1 ||
                    BitConverter.ToInt16(wavhdr, 34) != 16)
                {
                    throw new InvalidOperationException("Not a valid WAV file");
                }

                channel = BitConverter.ToInt16(wavhdr, 22);
                freq = BitConverter.ToInt32(wavhdr, 24);
                size = BitConverter.ToInt32(wavhdr, 40) / BitConverter.ToInt16(wavhdr, 32);

                WriteAdxHeader(adxWriter, channel, freq, size, loop);

                channelState[0].s1 = 0;
                channelState[0].s2 = 0;
                channelState[1].s1 = 0;
                channelState[1].s2 = 0;

                if (channel == 1)
                {
                    while (size > 0)
                    {
                        int bytesRead = Utils.ReadShorts(wavReader, wavbuf, 0, BLOCK_SAMPLES);
                        if (bytesRead == 0) break;
                        Encode(adxbuf, 0, wavbuf, 0, ref channelState[0]);
                        wsize = size > BLOCK_SAMPLES ? BLOCK_SAMPLES : size;
                        size -= wsize;
                        adxWriter.Write(adxbuf);
                    }
                }
                else if (channel == 2)
                {
                    while (size > 0)
                    {
                        short[] tmpbuf = new short[BLOCK_SAMPLES * 2];
                        int bytesRead = Utils.ReadShorts(wavReader, tmpbuf, 0, BLOCK_SAMPLES * 2);
                        if (bytesRead == 0) break;
                        for (int i = 0; i < BLOCK_SAMPLES; i++)
                        {
                            wavbuf[i] = tmpbuf[i * 2];
                            wavbuf[i + BLOCK_SAMPLES] = tmpbuf[i * 2 + 1];
                        }

                        Encode(adxbuf, 0, wavbuf, 0, ref channelState[0]);
                        Encode(adxbuf, BLOCK_SIZE, wavbuf, BLOCK_SAMPLES, ref channelState[1]);
                        wsize = size > BLOCK_SAMPLES ? BLOCK_SAMPLES : size;
                        size -= wsize;
                        adxWriter.Write(adxbuf);
                    }
                }
                byte[] finalBlock = new byte[BLOCK_SIZE];
                Utils.WriteDwordBE(finalBlock, 0, 0x8001000e);

                adxWriter.Write(finalBlock);
                return adxStream.ToArray();
            }
        }
        private static void WriteWavHeader(BinaryWriter writer, int channels, int sampleRate, int dataSize)
        {
            int byteRate = sampleRate * channels * 2;
            int blockAlign = channels * 2;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize * channels * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize * channels * 2);
        }
        private static void WriteAdxHeader(BinaryWriter adxWriter, int channel, int freq, int size, bool loop)
        {

            byte[] adxhdr = new byte[ADX_HEADER_SIZE];
            int offset = adxhdr.Length + 2;

            adxhdr[4] = ADX_ENCODING_TYPE;
            adxhdr[5] = BLOCK_SIZE;
            adxhdr[6] = ADX_SAMPLE_BITDEPTH;
            adxhdr[7] = (byte)channel;

            Utils.WriteDwordBE(adxhdr, 8, freq);
            Utils.WriteDwordBE(adxhdr, 12, size);
            Utils.WriteWordBE(adxhdr, 16, ADX_HIGHPASS_FREQ);
            adxhdr[18] = ADX_VERSION;
            adxhdr[19] = ADX_FLAGS;
            if (loop)
            {
                offset = LOOP_START_OFFSET - 4;
                Utils.WriteWordBE(adxhdr, 20, 0);
                Utils.WriteWordBE(adxhdr, 22, 1);
                Utils.WriteDwordBE(adxhdr, 24, 1);
                Utils.WriteDwordBE(adxhdr, 28, 0);
                Utils.WriteDwordBE(adxhdr, 32, LOOP_START_OFFSET);
                Utils.WriteDwordBE(adxhdr, 36, size);
                int loopEndOffset = GetLoopEndOffset(LOOP_START_OFFSET, size);
                Utils.WriteDwordBE(adxhdr, 40, loopEndOffset);
            }
            Utils.WriteDwordBE(adxhdr, 0, offset | 0x80000000);

            adxWriter.Write(adxhdr);
            if (loop)
            {
                adxWriter.Write(new byte[LOOP_START_OFFSET - adxhdr.Length + ADX_COPYRIGHT.Length]);
            }
            adxWriter.Write(Encoding.ASCII.GetBytes(ADX_COPYRIGHT));
        }
        private static int GetLoopEndOffset(int firstSampleOffset, int wavFileSize)
        {
            int numBlocks = (int)Math.Ceiling((double)wavFileSize / BLOCK_SAMPLES);

            int offset = firstSampleOffset + (numBlocks * BLOCK_SIZE * 2);

            return offset;
        }
        private static void Decode(short[] outbuf, int outOffset, byte[] inbuf, int inOffset, ref ChannelState channelState)
        {
            int scale = (inbuf[inOffset] << 8) | inbuf[inOffset + 1];
            int s0, s1, s2, d;

            s1 = channelState.s1;
            s2 = channelState.s2;
            for (int i = 0; i < 16; i++)
            {
                d = inbuf[inOffset + i + 2] >> 4;
                if ((d & 8) != 0) d -= 16;
                s0 = (BASEVOL * d * scale + 0x7298 * s1 - 0x3350 * s2) >> 14;
                if (s0 > 32767) s0 = 32767;
                else if (s0 < -32768) s0 = -32768;
                outbuf[outOffset + i * 2] = (short)s0;
                s2 = s1;
                s1 = s0;

                d = inbuf[inOffset + i + 2] & 0x0F;
                if ((d & 8) != 0) d -= 16;
                s0 = (BASEVOL * d * scale + 0x7298 * s1 - 0x3350 * s2) >> 14;
                if (s0 > 32767) s0 = 32767;
                else if (s0 < -32768) s0 = -32768;
                outbuf[outOffset + i * 2 + 1] = (short)s0;
                s2 = s1;
                s1 = s0;
            }

            channelState.s1 = s1;
            channelState.s2 = s2;
        }
        private static void Encode(byte[] outBuffer, int outOffset, short[] inBuffer, int inOffset, ref ChannelState channelState)
        {
            int scale;
            int s0, s1, s2, d;
            int max = 0;
            int min = 0;
            int[] data = new int[32];

            s1 = channelState.s1;
            s2 = channelState.s2;
            for (int i = 0; i < 32; i++)
            {
                s0 = inBuffer[inOffset + i];
                d = ((s0 << 14) - 0x7298 * s1 + 0x3350 * s2) / BASEVOL;
                data[i] = d;
                if (max < d) max = d;
                if (min > d) min = d;
                s2 = s1;
                s1 = s0;
            }
            channelState.s1 = s1;
            channelState.s2 = s2;

            if (max == 0 && min == 0)
            {
                Array.Clear(outBuffer, outOffset, 18);
                return;
            }

            if (max / 7 > -min / 8) scale = max / 7;
            else scale = -min / 8;

            if (scale == 0) scale = 1;

            outBuffer[outOffset] = (byte)(scale >> 8);
            outBuffer[outOffset + 1] = (byte)scale;

            for (int i = 0; i < 16; i++)
            {
                outBuffer[outOffset + i + 2] = (byte)(((data[i * 2] / scale) << 4) | ((data[i * 2 + 1] / scale) & 0xf));
            }
        }
    }
}
