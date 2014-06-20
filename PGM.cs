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
    /// Utility class for reading and writing the PGM grayscale image file format.
    /// </summary>
    public static class PGM
    {
        /// <summary>
        /// Reads a PGM file from <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The location of the PGM file</param>
        /// <returns>An object encapsulating the image data</returns>
        public static unsafe GrayscaleImage Read(string filename)
        {
            FileStream stream = File.OpenRead(filename);
            StreamReader input = new StreamReader(stream);
            string magicNumber = input.ReadLine();
            if (magicNumber != "P5")
                return null;
            string[] dims = input.ReadLine().Split();
            string depth = input.ReadLine();
            if (depth != "255")
                return null;
            int columns = int.Parse(dims[0]);
            int rows = int.Parse(dims[1]);
            GrayscaleImage gray = new GrayscaleImage(rows, columns);
            fixed(float* src=gray.RawArray)
            {
                float* ptr = src;
                byte[] scanline = new byte[columns];
                float norm = 1f / 255;
                for (int r = 0; r < rows; r++)
                {
                    int count = 0;
                    while (count < columns)
                        count += stream.Read(scanline, count, columns - count);
                    for (int c = 0; c < columns; c++)
                    {
                        float val = scanline[c]*norm;
                        *ptr++ = val;
                    }
                }
            }
            return gray;
        }

        /// <summary>
        /// Writes <paramref name="image"/> to <paramref name="filename"/> using the PGM file format.
        /// </summary>
        /// <param name="image">Image to write</param>
        /// <param name="filename">Path to write to</param>
        public static unsafe void Write(GrayscaleImage image, string filename)
        {
            FileStream output = File.OpenWrite(filename);
            int rows = image.Height;
            int columns = image.Width;
            string header = string.Format("P5\n{0} {1}\n255\n", columns, rows);
            byte[] buf = Encoding.ASCII.GetBytes(header);
            output.Write(buf, 0, buf.Length);
            fixed(float* src=image.RawArray)
            {
                float* ptr = src;

                for (int r = 0; r < rows; r++)
                {
                    byte[] row = new byte[columns];
                    for (int c = 0; c < columns; c++)
                    {
                        row[c] = (byte)(*ptr++*255);
                    }
                    output.Write(row, 0, columns);
                }
            }
            output.Close();
        }

    }
}
