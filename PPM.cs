/*
 * Vision.NET 2.1 Computer Vision Library
 * Copyright (C) 2009 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Text;
using System.IO;

namespace VisionNET
{
    /// <summary>
    /// Utility class with routines for reading the simple PPM image file format.
    /// </summary>
    public static class PPM
    {
        private static char _currentChar;
        private static FileStream _stream;

        private static void init(FileStream stream)
        {
            _stream = stream;
            _currentChar = (char)_stream.ReadByte();
        }

        private static string readString()
        {
            StringBuilder sb = new StringBuilder();
            while (!char.IsWhiteSpace(_currentChar))
            {
                sb.Append(_currentChar);
                _currentChar = (char)_stream.ReadByte();
            }
            return sb.ToString();
        }

        private static int readInt()
        {
            return int.Parse(readString());
        }

        private static void clearWhitespace()
        {
            while (char.IsWhiteSpace(_currentChar))
                _currentChar = (char)_stream.ReadByte();
        }

        /// <summary>
        /// Returns an RGB image version of the PPM file stored at <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">A PPM image file</param>
        /// <returns>An RGB image</returns>
        public static RGBImage Read(string filename)
        {
            FileStream stream = File.OpenRead(filename);
            init(stream);
            if (readString() != "P6")
                return null;
            clearWhitespace();
            int columns = readInt();
            clearWhitespace();
            int rows = readInt();
            clearWhitespace();
            int max = readInt();
            try
            {
                if (max < 256)
                    return readColorByteImage(columns, rows, max, stream);
                else return readColorShortImage(columns, rows, max, stream);
            }
            finally
            {
                stream.Close();
            }
        }

        private static unsafe RGBImage readColorByteImage(int columns, int rows, int max, FileStream stream)
        {
            RGBImage image = new RGBImage(rows, columns);
            double multiplier = 255.0 / max;
            fixed(byte* src=image.RawArray){
                byte* ptr = src;
                byte[] scanline = new byte[columns*3];
                for (int r = 0; r < rows; r++)
                {
                    int count = 0;
                    while (count < scanline.Length)
                        count += stream.Read(scanline, count, scanline.Length - count);
                    for (int c = 0; c < columns; c++)
                    {
                        byte R = scanline[c * 3];
                        byte G = scanline[c * 3 + 1];
                        byte B = scanline[c * 3 + 2];
                        *ptr++ = (byte)(R * multiplier);
                        *ptr++ = (byte)(G * multiplier);
                        *ptr++ = (byte)(B * multiplier);
                    }
                }
            }
            return image;
        }

        private static unsafe RGBImage readColorShortImage(int columns, int rows, int max, FileStream stream)
        {
            RGBImage image = new RGBImage(rows, columns);
            double multiplier = 255.0 / max;
            fixed(byte* src=image.RawArray){
                byte* ptr = src;
                byte[] scanline = new byte[columns * 6];
                for (int r = 0; r < rows; r++)
                {
                    int count = 0;
                    while (count < scanline.Length)
                        count += stream.Read(scanline, count, scanline.Length - count);
                    for (int c = 0; c < columns; c++)
                    {
                        int shortR = scanline[c * 6];
                        shortR = shortR << 8 + scanline[c * 6 + 1];
                        int shortG = scanline[c * 6 + 2];
                        shortG = shortG << 8 + scanline[c * 6 + 3];
                        int shortB = scanline[c * 6 + 4];
                        shortB = shortB << 8 + scanline[c * 6 + 5];
                        *ptr++ = (byte)(multiplier * shortR);
                        *ptr++ = (byte)(multiplier * shortG);
                        *ptr++ = (byte)(multiplier * shortB);
                    }
                }
            }
            return image;
        }
    }
}
