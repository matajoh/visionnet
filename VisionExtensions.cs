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
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using VisionNET.Learning;
using MathNet.Numerics.LinearAlgebra;

namespace VisionNET
{
    /// <summary>
    /// This class contains a series of useful extensions for various interfaces within the Vision.NET library.
    /// </summary>
    public static class VisionExtensions
    {
        private static List<T> permute<T>(List<T> current, List<T> remaining, int size)
        {
            while (current.Count < size && remaining.Count > 0)
            {
                int index = ThreadsafeRandom.Next(remaining.Count);
                current.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
            return current;
        }

        /// <summary>
        /// Compute the intersection over the union of the areas of the two rectangles.
        /// </summary>
        /// <param name="lhs">The first rectangle</param>
        /// <param name="rhs">The second rectangle</param>
        /// <returns>Intersection of the area over the union of the area</returns>
        public static float IntersectionOverUnion(this RectangleF lhs, RectangleF rhs)
        {
            RectangleF intersection = RectangleF.Intersect(lhs, rhs);

            float unionArea = lhs.Width * lhs.Height + rhs.Width * rhs.Height;
            float intersectArea = intersection.Width * intersection.Height;

            return intersectArea / (unionArea - intersectArea);
        }

        /// <summary>
        /// Computes a sum of the values in the array starting at (<paramref name="row"/>, <paramref name="column"/>) in <paramref name="channel" /> 
        /// in a rectangle described by the offset and size in <paramref name="rect"/>.
        /// </summary>
        /// <param name="handler">The handler used to perform the operation</param>
        /// <param name="row">Reference row</param>
        /// <param name="column">Reference column</param>
        /// <param name="channel">Channel to draw values from</param>
        /// <param name="rect">Offset and size of the rectangle</param>
        /// <returns>The sum of all values in the rectangle</returns>
        public static T ComputeRectangleSum<T>(this IArrayHandler<T> handler, int row, int column, int channel, Rectangle rect)
        {
            int startRow = row + rect.Top;
            int startColumn = column + rect.Left;
            int rows = rect.Height;
            int columns = rect.Width;
            return handler.ComputeRectangleSum(startRow, startColumn, rows, columns, channel);
        }

        /// <summary>
        /// Interpolates the value in-between cells in an array using bi-linear interpolation.
        /// </summary>
        /// <param name="handler">The array to use</param>
        /// <param name="row">The real-valued row</param>
        /// <param name="column">The real-valued column</param>
        /// <param name="channel">The image channel</param>
        /// <returns>An interpolated value</returns>
        public static float InterploateLinear(this IArrayHandler<float> handler, float row, float column, int channel)
        {
            row = row < 0 ? 0 : row;
            column = column < 0 ? 0 : column;
            int i0 = (int)row;
            int j0 = (int)column;
            int i1 = i0 + 1;
            i1 = i1 < handler.Rows ? i1 : handler.Rows - 1;
            int j1 = j0 + 1;
            j1 = j1 < handler.Columns ? j1 : handler.Columns - 1;

            float di = row - i0;
            float dj = column - j0;

            return handler[i0, j0, channel] * (1 - di) * (1 - dj) +
                handler[i0, j1, channel] * (1 - di) * dj +
                handler[i1, j0, channel] * di * (1 - dj) +
                handler[i1, j1, channel] * di * dj;
        }

        /// <summary>
        /// Extracts a rectangle from a handler.
        /// </summary>
        /// <typeparam name="T">Underlying type of the handler</typeparam>
        /// <param name="handler">The handler used in extraction</param>
        /// <param name="rect">The rectangle to extract</param>
        /// <returns>The extracted rectangle</returns>
        public static T[, ,] ExtractRectangle<T>(this IArrayHandler<T> handler, Rectangle rect)
        {
            return handler.ExtractRectangle(rect.R, rect.C, rect.Rows, rect.Columns);
        }

        /// <summary>
        /// Creates a histogram from the list of values containing the provided number of bins.
        /// </summary>
        /// <param name="values">The values to turn into a histogram</param>
        /// <param name="numBins">The number of bins</param>
        /// <returns>A histogram</returns>
        public static int[] ToHistogram(this IEnumerable<float> values, int numBins)
        {
            return values.ToHistogram(numBins, values.Min(), values.Max());
        }

        /// <summary>
        /// Creates a histogram from the list of values containing the provided number of bins, scaling from the provided minimum to maximum values.
        /// </summary>
        /// <param name="values">The values to quantize</param>
        /// <param name="numBins">The number of bins</param>
        /// <param name="min">The minimum value</param>
        /// <param name="max">The maximum value</param>
        /// <returns>A histogram</returns>
        public static int[] ToHistogram(this IEnumerable<float> values, int numBins, float min, float max)
        {
            float[] data = values.ToArray();
            Array.Sort<float>(data);

            float binSize = (max - min) / numBins;
            int[] histogram = new int[numBins];
            float thresh = min+binSize;
            int j = 0;
            for (int i = 0; i < numBins; i++)
            {
                int count = 0;
                for (; j < data.Length && data[j] < thresh; j++, count++) ;
                histogram[i] = count;
                thresh += binSize;
            }
            return histogram;
        }

        /// <summary>
        /// Creates a histogram image of the provided width and height, with bins shown as vertical black bars on a white background.
        /// </summary>
        /// <param name="histogramValues">The list of bin counts</param>
        /// <param name="width">Width of the output image</param>
        /// <param name="height">Height of the output image</param>
        /// <returns>A histogram image</returns>
        public static Bitmap CreateHistogramImage(this IEnumerable<int> histogramValues, int width, int height)
        {
            Bitmap image = new Bitmap(width, height);
            int[] histogram = histogramValues.ToArray();
            var brushes = Enumerable.Range(1, histogram.Length+1).Select(o =>
            {
                var color = LabelDictionary.LabelToColor((short)o);
                return new SolidBrush(System.Drawing.Color.FromArgb(color.R, color.G, color.B));
            }).ToArray();

            Graphics g = Graphics.FromImage(image);
            g.FillRectangle(System.Drawing.Brushes.White, 0, 0, width, height);

            float binWidth = (float)width / histogram.Length;
            float max = histogram.Max();

            float x = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                float binHeight = histogram[i] * height / max;
                float y = height - binHeight;
                g.FillRectangle(brushes[i], x, y, binWidth, binHeight);
                x += binWidth;
            }

            return image;
        }

        /// <summary>
        /// Saves <paramref name="image"/> to the provided location on the disk, using an encoder determined by the extension of the filename.
        /// </summary>
        /// <param name="image">The image to save</param>
        /// <param name="filename">The location on disk.  The extension determines the encoder used.</param>
        public static void Save(this BitmapSource image, string filename)
        {
            BitmapEncoder encode = IO.GetEncoder(filename);
            encode.Frames.Add(BitmapFrame.Create(image));
            encode.Save(filename);
        }

        /// <summary>
        /// Thresholds the provided image.  The <paramref name="lessThan"/> parameter will determine whether the pixels that are less than the
        /// threshold are set to true and the others "false" or vice versa.  The thresholding will be performed on the provided channel.
        /// channel.
        /// </summary>
        /// <typeparam name="T">The underlying type of the input image.  Must be of interface IComparable.</typeparam>
        /// <param name="image">The input image</param>
        /// <param name="threshold">The threshold to use</param>
        /// <param name="lessThan">Whether pixels less than the threshold are set to "true" and all others to "false", or vice versa</param>
        /// <param name="channel">The channel on which to perform thresholding</param>
        /// <returns>The thresholded image</returns>
        public static BinaryImage Threshold<T>(this IMultichannelImage<T> image, T threshold, bool lessThan, int channel) where T : IComparable<T>
        {
            T[, ,] data = image.RawArray;
            int rows = image.Rows;
            int columns = image.Columns;
            BinaryImage resultImage = new BinaryImage(rows, columns);
            bool[, ,] result = resultImage.RawArray;

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    result[r, c, 0] = data[r, c, channel].CompareTo(threshold) <= 0 ? lessThan : !lessThan;

            return resultImage;
        }

        /// <summary>
        /// Fills a handler with the provided value.
        /// </summary>
        /// <typeparam name="T">The underlying type of the handler</typeparam>
        /// <param name="handler">The handler to fill</param>
        /// <param name="value">The value to fill with</param>
        public static void Fill<T>(this IArrayHandler<T> handler, T value)
        {
            T[, ,] data = handler.RawArray;
            int rows = handler.Rows;
            int columns = handler.Columns;
            int channels = handler.Channels;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    for (int i = 0; i < channels; i++)
                        data[r, c, i] = value;
        }

        private const float DIRICHLET = .0001f;

        /// <summary>
        /// Returns a normalized version of <paramref name="distribution"/>.
        /// </summary>
        /// <param name="distribution">Values to normalize</param>
        /// <returns>A normalized version of <paramref name="distribution"/></returns>
        public static float[] Normalize(this float[] distribution)
        {
            float dirichlet = DIRICHLET / distribution.Length;
            float[] result = new float[distribution.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = distribution[i] + dirichlet;
            float sum = result.Sum();
            float norm = 1f / sum;
            for (int i = 0; i < result.Length; i++)
                result[i] *= norm;

            return result;
        }

        /// <summary>
        /// Normalizes a distribution using the L1 norm.
        /// </summary>
        /// <param name="distribution">The distribution to normalize</param>
        /// <returns>A normalized distribution</returns>
        public static IEnumerable<double> Normalize(this IEnumerable<double> distribution)
        {
            double dirichlet = DIRICHLET / distribution.Count();
            var result = from val in distribution
                         select val + dirichlet;
            double sum = result.Sum();
            double norm = 1 / sum;
            result = from val in result
                     select val * norm;
            return result;
        }

        /// <summary>
        /// Extracts a random number of points, such that the probability of a point being extracted is equal
        /// to <paramref name="percentage"/>
        /// </summary>
        /// <typeparam name="T">Underlying type of the image</typeparam>
        /// <param name="image">The image</param>
        /// <param name="percentage">The probability that a point will be extracted</param>
        /// <param name="border">Border around the edge of the image to exclude</param>
        /// <returns>A list of image points</returns>
        public static IEnumerable<ImageDataPoint<T>> ExtractPoints<T>(this IMultichannelImage<T> image, double percentage, short border = 0)
        {
            int rows = image.Rows;
            int columns = image.Columns;
            int total = (int)((rows - 2*border) * (columns - 2*border) * percentage);

            if (percentage <= .1)
            {
                for (int i = 0; i < total; i++)
                {
                    short row = (short)ThreadsafeRandom.Next(border, rows - border);
                    short column = (short)ThreadsafeRandom.Next(border, rows - border);
                    yield return new ImageDataPoint<T>(image, row, column, 0);
                }
            }
            else
            {
                for (short r = border; r < rows - border; r++)
                    for (short c = border; c < columns - border; c++)
                        if (ThreadsafeRandom.NextDouble() < percentage)
                            yield return new ImageDataPoint<T>(image, r, c, 0);
            }
        }

        /// <summary>
        /// Uses the string as a filename, and reads the lines of the file in order, returning them as a collection.
        /// </summary>
        /// <param name="filename">The location of the file</param>
        /// <returns>The lines of the file</returns>
        public static IEnumerable<string> Lines(this string filename)
        {
            StreamReader input = new StreamReader(filename);
            while (!input.EndOfStream)
                yield return input.ReadLine();
            input.Close();
        }

        /// <summary>
        /// Computes the Shannon entropy of a distribution.
        /// </summary>
        /// <param name="distribution">The distribution to analyze</param>
        /// <returns>The Shannon entropy</returns>
        public static double ShannonEntropy(this IEnumerable<float> distribution)
        {
            float sum = distribution.Sum();
            double entropy = 0;
            foreach (float value in distribution)
            {
                float p = value / sum;
                entropy -= p * Math.Log(p, 2);
            }
            return entropy;
        }

        /// <summary>
        /// Samples an index from <paramref name="distribution"/>.
        /// </summary>
        /// <param name="distribution">A collection of values which sum to 1</param>
        /// <returns>The sampled index</returns>
        public static int Sample(this IEnumerable<float> distribution)
        {
            float sample = ThreadsafeRandom.NextFloat() * distribution.Sum();
            Queue<float> values = new Queue<float>(distribution);
            int index = 0;
            while (sample > 0 && values.Count > 0)
            {
                sample -= values.Dequeue();
                index++;
            }
            return index - 1;
        }

        /// <summary>
        /// Samples an index from <paramref name="list"/> using <paramref name="selector"/> to transform <typeparamref name="T"/> to a float.
        /// </summary>
        /// <typeparam name="T">Type of the list</typeparam>
        /// <param name="list">The list to sample</param>
        /// <param name="selector">Function used to create a normalized list</param>
        /// <returns>The sampled index</returns>
        public static int Sample<T>(this IEnumerable<T> list, Func<T,float> selector)
        {
            float sample = ThreadsafeRandom.NextFloat() * list.Sum(selector);
            Queue<float> values = new Queue<float>(from item in list
                                                   select selector(item));
            int index = 0;
            while (sample > 0 && values.Count > 0)
            {
                sample -= values.Dequeue();
                index++;
            }
            return index - 1;
        }

        /// <summary>
        /// Returns a permutated version of <paramref name="list"/> of size <paramref name="size"/>.
        /// </summary>
        /// <typeparam name="T">The type of the list</typeparam>
        /// <param name="list">The list to permute</param>
        /// <param name="size">The size of the list to return</param>
        /// <returns>A permutated list</returns>
        public static IEnumerable<T> Permute<T>(this IEnumerable<T> list, int size)
        {
            List<T> remaining = new List<T>(list);
            return permute(new List<T>(), remaining, size);
        }

        private static short findMiddle(short[] values)
        {
            // find edges, if any, and put middle in the midst of the largest continuous segment
            short[] leftEdges = (from v in values
                                 where Array.BinarySearch<short>(values, (short)(v - 1)) < 0
                               select v).ToArray();
            short[] rightEdges = (from v in values
                                  where Array.BinarySearch<short>(values, (short)(v + 1)) < 0
                                select v).ToArray();
            var spans = from index in Enumerable.Range(0, leftEdges.Length)
                        select new
                        {
                            Middle = (short)((leftEdges[index] + rightEdges[index]) >> 1),
                            Width = (rightEdges[index] - leftEdges[index])
                        };

            return spans.OrderByDescending(o => o.Width).First().Middle;
        }

        /// <summary>
        /// Finds a point which lies inside the segment defined by <paramref name="points"/>.
        /// </summary>
        /// <typeparam name="T">The underlying type of the data points</typeparam>
        /// <param name="points">The points defining the segment</param>
        /// <returns>A point on the inside of the segment</returns>
        public static ImageDataPoint<T> FindInside<T>(this IEnumerable<ImageDataPoint<T>> points)
        {
            short[] columns = (from point in points
                               select point.Column).ToArray();
            short column = columns[columns.Length / 2];

            // find middle row, weighted by width
            short[] rows = (from point in points
                            where point.Column == column
                            select point.Row).ToArray();
            int row = findMiddle(rows);

            columns = (from point in points
                       where point.Row == row
                       select point.Column).ToArray();
            column = findMiddle(columns);

            return (from point in points
                    where point.Row == row && point.Column == column
                    select point).First();
        }

        /// <summary>
        /// Creates a list of items with spaces in between and default formatting.
        /// </summary>
        /// <typeparam name="T">The type of the list's items</typeparam>
        /// <param name="list">The list to display</param>
        /// <returns>A string representation of the list with no delimiter</returns>
        public static string ToDisplayString<T>(this IEnumerable<T> list)
        {
            return list.ToDisplayString("{0}", " ");
        }

        /// <summary>
        /// Computes and returns the set product (product of all numbers in <paramref name="list"/>).
        /// </summary>
        /// <param name="list">The list to multiply</param>
        /// <returns>The set product</returns>
        public static float Product(this IEnumerable<float> list)
        {
            float product = 1;
            foreach (float val in list)
                product *= val;
            return product;
        }

        /// <summary>
        /// Computes and returns the set product (product of all numbers in <paramref name="list"/>), using <paramref name="selector"/> to translate
        /// the list members into float values.
        /// </summary>
        /// <param name="list">The list to multiply</param>
        /// <param name="selector">The function to use to translate the list members into float values</param>
        /// <returns>The set product</returns>
        public static float Product<T>(this IEnumerable<T> list, Func<T, float> selector)
        {
            float product = 1;
            foreach (T val in list)
                product *= selector(val);
            return product;
        }

        /// <summary>
        /// Constructs a string representation of <paramref name="list"/> using <paramref name="formatString"/> to format all items in the list, separating them using <paramref name="delimiter"/>.
        /// </summary>
        /// <typeparam name="T">Type of the list's items</typeparam>
        /// <param name="list">The list to display</param>
        /// <param name="formatString">Used for formatting the items in the list</param>
        /// <param name="delimiter">Used to separate items in the list</param>
        /// <returns>A string representation</returns>
        public static string ToDisplayString<T>(this IEnumerable<T> list, string formatString, string delimiter)
        {
            if (list.Count() == 0)
                return "[]";
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.AppendFormat(formatString, list.First());
            foreach(T val in list.Skip(1))
                sb.AppendFormat(delimiter+formatString, val);
            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the index of the maximum value of <paramref name="list"/>.
        /// </summary>
        /// <typeparam name="T">The underlying type of the list</typeparam>
        /// <param name="list">The list to examine</param>
        /// <returns>The index of the maximum value</returns>
        public static int MaxIndex<T>(this IEnumerable<T> list) where T : IComparable<T>
        {
            int maxIndex = 0;
            T max = list.First();
            int index = 0;
            foreach (T value in list)
            {
                if (value.CompareTo(max) > 0)
                {
                    max = value;
                    maxIndex = index;
                }
                index++;
            }
            return maxIndex;
        }

        /// <summary>
        /// Returns the index of the minimum value of <paramref name="list"/>
        /// </summary>
        /// <typeparam name="T">The underlying type of the list, must implement IComparable</typeparam>
        /// <param name="list">The list to analyze</param>
        /// <returns>The index of the minimum value of the list</returns>
        public static int MinIndex<T>(this IEnumerable<T> list) where T : IComparable<T>
        {
            int minIndex = 0;
            T min = list.First();
            int index = 0;
            foreach (T value in list)
            {
                if (value.CompareTo(min) < 0)
                {
                    min = value;
                    minIndex = index;
                }
                index++;
            }
            return minIndex;
        }

        /// <summary>
        /// Converts a GDI+ <see cref="Bitmap"/> object into a <see cref="BitmapSource"/> object.
        /// </summary>
        /// <param name="bitmap">The bitmap to convert</param>
        /// <returns>The converted object</returns>
        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
        }

        /// <summary>
        /// Converts a <see cref="BitmapSource"/> object into a GDI+ <see cref="Bitmap"/> object.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static unsafe Bitmap ToBitmap(this BitmapSource source)
        {
            FormatConvertedBitmap bmp = new FormatConvertedBitmap();
            bmp.BeginInit();
            bmp.Source = source;
            bmp.DestinationFormat = PixelFormats.Bgra32;
            bmp.EndInit();
            int width = bmp.PixelWidth;
            int height = bmp.PixelHeight;
            int stride = width * ((bmp.Format.BitsPerPixel + 7) / 8);

            byte[] bits = new byte[height * stride];

            bmp.CopyPixels(bits, stride, 0);

            Bitmap bitmap = null;
            fixed (byte* pBits = bits)
            {
                IntPtr ptr = new IntPtr(pBits);

                bitmap = new Bitmap(
                    width,
                    height,
                    stride,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                    ptr);

            }
            bits = null;
            bmp = null;
            return bitmap;
        }

        /// <summary>
        /// Saves a bitmap to a file.
        /// </summary>
        /// <param name="encoder">The encoder to use when encoding the bitmap</param>
        /// <param name="filename">The filename to write to</param>
        public static void Save(this BitmapEncoder encoder, string filename)
        {
            FileStream stream = File.OpenWrite(filename);
            encoder.Save(stream);
            stream.Close();
        }

        /// <summary>
        /// Extracts a channel as a Grayscale image.  Uses the <see cref="M:IArrayHandler.ExtractChannel"/> method.
        /// </summary>
        /// <param name="handler">The image upon which to operate</param>
        /// <param name="channel">The channel to extract</param>
        /// <returns>An image representation of the channel</returns>
        public static unsafe GrayscaleImage ExtractChannelAsImage(this IArrayHandler<float> handler, int channel)
        {
            float[,] buffer = handler.ExtractChannel(channel);
            int rows = handler.Rows;
            int columns = handler.Columns;
            float[, ,] data = new float[rows, columns, 1];
            fixed (float* channelSrc = buffer, dataSrc = data)
            {
                float* channelPtr = channelSrc;
                float* dataPtr = dataSrc;
                int count = rows * columns;
                while (count-- > 0)
                    *dataPtr++ = *channelPtr++;
            }
            GrayscaleImage gray = new GrayscaleImage();
            gray.SetData(data);
            return gray;
        }

        /// <summary>
        /// Read values into the buffer using the provided stream, and reversing the order if necessary.
        /// </summary>
        /// <param name="stream">Stream to use when reading bytes</param>
        /// <param name="buff">The buffer to read into</param>
        /// <param name="reverse">Whether to reverse the bytes once read</param>
        public static void Read(this Stream stream, byte[] buff, bool reverse)
        {
            stream.Read(buff, 0, buff.Length);
            if (reverse)
            {
                for (int i = 0; i < buff.Length >> 1; i++)
                {
                    byte tmp = buff[i];
                    buff[i] = buff[buff.Length - i - 1];
                    buff[buff.Length - i - 1] = tmp;
                }
            }
        }

        /// <summary>
        /// Add one image to the right of another to create a new, wider image. 
        /// </summary>
        /// <typeparam name="T">The image type</typeparam>
        /// <typeparam name="D">The underlying pixel data type</typeparam>
        /// <param name="lhs">The left hand image</param>
        /// <param name="rhs">The right hand image</param>
        /// <returns>The combined image</returns>
        public static T AppendRight<T, D>(this T lhs, T rhs) where T : IMultichannelImage<D>, new()
        {
            int newRows = Math.Max(lhs.Rows, rhs.Rows);
            int newColumns = lhs.Columns + rhs.Columns;

            int lStartRow = (newRows - lhs.Rows) / 2;
            int rStartRow = (newRows - rhs.Rows) / 2;

            D[, ,] values = new D[newRows, newColumns, lhs.Channels];
            D[, ,] lhsValues = lhs.RawArray;
            D[, ,] rhsValues = rhs.RawArray;
            for (int r = 0; r < lhs.Rows; r++)
                for (int c = 0; c < lhs.Columns; c++)
                    for (int i = 0; i < lhs.Channels; i++)
                        values[r + lStartRow, c, i] = lhsValues[r, c, i];
            for (int r = 0; r < rhs.Rows; r++)
                for (int c = 0; c < rhs.Columns; c++)
                    for (int i = 0; i < rhs.Channels; i++)
                        values[r + rStartRow, c + lhs.Columns, i] = rhsValues[r, c, i];
            T result = new T();
            result.SetData(values);
            return result;
        }

        /// <summary>
        /// Add one image to the bottom of another to create a new, taller image. 
        /// </summary>
        /// <typeparam name="T">The image type</typeparam>
        /// <typeparam name="D">The underlying pixel data type</typeparam>
        /// <param name="lhs">The top image</param>
        /// <param name="rhs">The bottom image</param>
        /// <returns>The combined image</returns>
        public static T AppendBottom<T, D>(this T lhs, T rhs) where T : IMultichannelImage<D>, new()
        {
            int newRows = lhs.Rows + rhs.Rows;
            int newColumns = Math.Max(lhs.Columns, rhs.Columns);

            int lStartColumn = (newColumns - lhs.Columns) / 2;
            int rStartColumn = (newColumns - rhs.Columns) / 2;

            D[, ,] values = new D[newRows, newColumns, lhs.Channels];
            D[, ,] lhsValues = lhs.RawArray;
            D[, ,] rhsValues = rhs.RawArray;
            for (int r = 0; r < lhs.Rows; r++)
                for (int c = 0; c < lhs.Columns; c++)
                    for (int i = 0; i < lhs.Channels; i++)
                        values[r, c + lStartColumn, i] = lhsValues[r, c, i];
            for (int r = 0; r < rhs.Rows; r++)
                for (int c = 0; c < rhs.Columns; c++)
                    for (int i = 0; i < rhs.Channels; i++)
                        values[r + lhs.Rows, c + rStartColumn, i] = rhsValues[r, c, i];
            T result = new T();
            result.SetData(values);
            return result;
        }

        /// <summary>
        /// Add the values of the two arrays together.
        /// </summary>
        /// <param name="lhs">The first array</param>
        /// <param name="rhs">The second array</param>
        /// <returns>A new array with the sums of the values</returns>
        public static float[] Add(this float[] lhs, float[] rhs)
        {
            if (lhs.Length != rhs.Length)
                throw new ArgumentException("Array lengths must be equal");

            float[] result = new float[lhs.Length];
            for (int i = 0; i < lhs.Length; i++)
                result[i] = lhs[i] + rhs[i];

            return result;
        }

        /// <summary>
        /// Multiply the elements of the array by the provided value.
        /// </summary>
        /// <param name="lhs">The array to use</param>
        /// <param name="rhs">The value to scale by</param>
        /// <returns>A new array with the multiplied values</returns>
        public static float[] Multiply(this float[] lhs, float rhs)
        {
            float[] result = new float[lhs.Length];
            for (int i = 0; i < lhs.Length; i++)
                result[i] = lhs[i] * rhs;
            return result;
        }

        /// <summary>
        /// Returns a random subset of a list of a given size.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list</typeparam>
        /// <param name="values">The set of values</param>
        /// <param name="size">The size of the desired subset</param>
        /// <returns>A random subset of values</returns>
        public static List<T> RandomSubset<T>(this IEnumerable<T> values, int size)
        {
            Random rand = new Random();
            List<T> source = new List<T>(values);
            size = Math.Min(source.Count, size);

            List<T> result = new List<T>();
            for (int i = 0; i < size; i++)
            {
                int index = rand.Next(source.Count);
                result.Add(source[index]);
                source.RemoveAt(index);
            }
            return result;
        }

        /// <summary>
        /// Computes the squared L2 difference between two vectors with a value to use to allow early stopping.
        /// </summary>
        /// <param name="lhs">The left hand vector</param>
        /// <param name="rhs">The right hand vector</param>
        /// <param name="minDist">If the distance being computed exceeds this value, the function returns</param>
        /// <returns>The distance</returns>
        public static double SquaredDistance(this Vector<double> lhs, Vector<double> rhs, double minDist = double.MaxValue)
        {
            double dist = 0;
            if (lhs.Count != rhs.Count)
                throw new ArgumentException("Invalid arguments, vector sizes must be equal.");
            for (int i = 0; i < lhs.Count; i++)
            {
                double dx = lhs[i] - rhs[i];
                dist += dx * dx;
                if (dist > minDist)
                    break;
            }
            return dist;
        }

        /// <summary>
        /// Calculates the Shannon entropy for a distribution.
        /// </summary>
        /// <param name="dist"></param>
        /// <returns>The Shannon entropy</returns>
        public static float CalculateEntropy(this float[] dist)
        {
            dist = dist.Normalize();
            double count = 0;
            double entropy = 0;
            int length = dist.Length;
            for (int i = 0; i < length; i++)
                count += dist[i];
            double norm = 1 / (count + length * DIRICHLET);
            for (int c = 0; c < length; c++)
            {
                double classProb = (dist[c] + DIRICHLET) * norm;
                entropy -= classProb * Math.Log(classProb, 2);
            }
            return (float)entropy;
        }     
   
        /// <summary>
        /// Computes a distribution over labels based upon the labels of the data points provided.
        /// </summary>
        /// <typeparam name="T">The data point type</typeparam>
        /// <typeparam name="D">The underlying data type of the data point</typeparam>
        /// <param name="data">The data points</param>
        /// <param name="numLabels">The total number of expected labels in the data set</param>
        /// <returns>An un-normalized distribution of data points per label</returns>
        public static float[] ComputeDistribution<T,D>(this IEnumerable<T> data, int numLabels) where T:IDataPoint<D>
        {
            float[] result = new float[numLabels];
            foreach (var point in data)
                result[point.Label] += 1;
            return result;
        }

        /// <summary>
        /// Perform a pooling action on an image.
        /// </summary>
        /// <typeparam name="T">The type of the image</typeparam>
        /// <typeparam name="D">The underlying pixel data type of the image</typeparam>
        /// <param name="image">The image to pool</param>
        /// <param name="frequency">The frequency with which to perform pooling (in pixels)</param>
        /// <param name="stride">The size of the square pooling filter (in pixels)</param>
        /// <param name="reduce">Reduction function for pooling the values under the filter</param>
        /// <param name="init">Initialization value for the reduction</param>
        /// <returns></returns>
        public static T Pool<T, D>(this T image, int frequency, int stride, Func<D,D,D> reduce, Func<D> init) where T:IMultichannelImage<D>, new()
        {
            int border = stride / 2;
            int rows = (image.Rows - border) / frequency;
            int columns = (image.Columns - border) / frequency;
            int channels = image.Channels;
            D[,,] input = image.RawArray;
            D[, ,] output = new D[rows, columns, channels];
            for (int r = 0, rr = border; r < rows; r++, rr += frequency)
            {
                for (int c = 0, cc = border; c < columns; c++, cc += frequency)
                {
                    for (int i = 0; i < channels; i++)
                    {
                        D value = init();
                        for (int dr = -border; dr < border; dr++)
                        {
                            for (int dc = -border; dc < border; dc++)
                            {
                                value = reduce(value, input[rr + dr, cc + dc, i]);
                            }
                        }
                        output[r, c, i] = value;
                    }
                }
            }
            T result = new T();
            result.SetData(output);
            return result;
        }
    }
}
