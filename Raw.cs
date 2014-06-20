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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace VisionNET
{
    /// <summary>
    /// Reads in images in the RAW format, where each pixel in the image is written out as 24-bit RGB values in row column order.  Also compatible with
    /// RAW grayscale images, in which each grayscale value is stored as a 24-bit integer.  This system is able to read gzipped RAW images provided they
    /// have the file extension ".gz".
    /// </summary>
    public static class Raw
    {
        /// <summary>
        /// Reads a RAW file as an RGB image.
        /// </summary>
        /// <param name="filename">The filename to read from.  If it ends with ".gz", the file will be treated as a gzip compressed RAW file.</param>
        /// <param name="rows">The number of rows in the image</param>
        /// <param name="columns">The number of columns in the image</param>
        /// <returns>A RAW image</returns>
        public static unsafe RGBImage ReadAsRGB(string filename, int rows, int columns)
        {
            Stream input = new FileStream(filename, FileMode.Open, FileAccess.Read);
            if (filename.EndsWith(".gz"))
                input = new GZipStream(input, CompressionMode.Decompress);
            try
            {
                return ReadAsRGB(input, rows, columns);
            }
            finally
            {
                input.Close();
            }
        }

        /// <summary>
        /// Reads RAW data from the stream as an RGB image.
        /// </summary>
        /// <param name="stream">The stream containing the image data</param>
        /// <param name="rows">The number of rows in the image</param>
        /// <param name="columns">The number of columns in the image</param>
        /// <returns>The RGB image</returns>
        public static unsafe RGBImage ReadAsRGB(Stream stream, int rows, int columns)
        {
            RGBImage image = new RGBImage(rows, columns);
            int scan = columns*3;
            byte[] row = new byte[scan];
            fixed (byte* dst = image.RawArray, src=row)
            {
                byte* dstPtr = dst;
                for (int r = 0; r < rows; r++)
                {
                    stream.Read(row, 0, scan);
                    int count = scan;
                    byte* srcPtr = src;
                    while (count-- != 0)
                        *dstPtr++ = *srcPtr++;
                }
            }
            return image;
        }

        /// <summary>
        /// Reads RAW data as a 24-bit monochrome image.
        /// </summary>
        /// <param name="filename">The location of the RAW file.  If this ends with ".gz", then the file will be treated as a GZip compressed RAW file.</param>
        /// <param name="rows">The number of rows in the image</param>
        /// <param name="columns">The number of columns in the image</param>
        /// <returns>A monochrome image</returns>
        public static unsafe MonochromeImage ReadAsMonochrome(string filename, int rows, int columns)
        {
            Stream input = new FileStream(filename, FileMode.Open, FileAccess.Read);
            if (filename.EndsWith(".gz"))
                input = new GZipStream(input, CompressionMode.Decompress);
            try
            {
                return ReadAsMonochrome(input, rows, columns);
            }
            finally
            {
                input.Close();
            }
        }
        
        /// <summary>
        /// Reads RAW data from the stream as a 24-bit monochrome image.
        /// </summary>
        /// <param name="stream">The stream containing the image data</param>
        /// <param name="rows">The number of rows in the image</param>
        /// <param name="columns">The number of columns in the image</param>
        /// <returns>The monochrome image</returns>
        public static unsafe MonochromeImage ReadAsMonochrome(Stream stream, int rows, int columns)
        {
            MonochromeImage image = new MonochromeImage(rows, columns);
            int scan = columns * 3;
            byte[] row = new byte[scan];
            fixed (byte* src = row)
            {
                fixed (int* dst = image.RawArray)
                {
                    int* dstPtr = dst;
                    for (int r = 0; r < rows; r++)
                    {
                        stream.Read(row, 0, scan);
                        int count = columns;
                        byte* srcPtr = src;
                        while (count-- != 0)
                        {
                            int A = *srcPtr++;
                            int B = *srcPtr++;
                            int C = *srcPtr++;
                            *dstPtr++ = (A << 16) + (B << 8) + C;
                        }
                    }
                }
            }
            return image;
        }

        /// <summary>
        /// Writes an RGBImage to the provided location.
        /// </summary>
        /// <param name="filename">The filename to write to.  If this ends with ".gz", then the data will be compressed with GZip as it is written.</param>
        /// <param name="image">The image to write</param>
        public static unsafe void Write(string filename, RGBImage image)
        {
            Stream output = new FileStream(filename, FileMode.Create, FileAccess.Write);
            if(filename.EndsWith(".gz"))
                output = new GZipStream(output, CompressionMode.Compress);
            Write(output, image);
            output.Close();
        }

        /// <summary>
        /// Writes an RGBImage to the stream.
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="image">The image to write</param>
        public static unsafe void Write(Stream stream, RGBImage image)
        {
            int rows = image.Rows;
            int columns = image.Columns;
            int scan = columns * 3;
            byte[] row = new byte[scan];
            fixed (byte* src = image.RawArray, dst = row)
            {
                byte* srcPtr = src;
                for (int r = 0; r < rows; r++)
                {
                    int count = scan;
                    byte* dstPtr = dst;
                    while (count-- != 0)
                        *dstPtr++ = *srcPtr++;
                    stream.Write(row, 0, scan);
                }
            }
            stream.Flush();
        }
    }
}
