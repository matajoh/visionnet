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
using System.Windows.Media.Imaging;

namespace VisionNET
{
    /// <summary>
	/// Image encapsulating the second moment eigensystem information of an image.  This eigensystem contains important information
	/// about the gradient of an image.  The eigenvectors will give the two dominant gradient directions, and their corresponding
	/// eigenvalues give the strength of those gradients.  Thus, at an edge one eigenvalue will be dominant, at a corner both will
	/// be dominant, and at a uniform region both will be near zero.  Each pixel of the image has a dimensionality of 6, encoded as
	/// (eigenvalue1, eigenvalue2, eigenvector1x, eigenvector1y, eigenvector2x, eigenvector2y).  When converted to a Bitmap, the
	/// image will show red at corners, blue at uniform regions, and green at edges.
    /// </summary>
    public sealed class EigensystemImage : IMultichannelImage<float>
    {
        private FloatArrayHandler _handler = new FloatArrayHandler();
        private string _label;

        /// <summary>
        /// Label for the image.
        /// </summary>
        public string ID
        {
            get
            {
                return _label;
            }
            set
            {
                _label = value;
            }
        }

        /// <summary>
        /// The default sensitivity value.
        /// </summary>
		public const float SENSITIVITY = .001f;

        /// <summary>
        /// Constructor.  Creates an empty image.
        /// </summary>
        public EigensystemImage()
        {
        }

        /// <summary>
        /// Computes the eigensystem image from the second moment image provided.  The moments are 
        /// convolved with a default sigma of 1.
        /// </summary>
        /// <param name="moments">Contains the second moments used to compute the eigensystem image.</param>
        /// <returns>The eigensystem image</returns>
        public static EigensystemImage Compute(SecondMomentImage moments)
        {
            return Compute(moments, 1);
        }

        /// <summary>
        /// Computes the eigensystem image from the second moment image provided.
        /// </summary>
        /// <param name="moments">Contains the second moments used to compute the eigensystem image.</param>
        /// <param name="sigma">The sigma to use when convolving the second moment image</param>
        /// <returns>the eigensystem image</returns>
        public static unsafe EigensystemImage Compute(SecondMomentImage moments, float sigma)
        {
            moments = Convolution.ConvolveGaussian<SecondMomentImage>(moments, sigma);
            int rows = moments.Rows;
            int columns = moments.Columns;
            float[, ,] data = new float[rows, columns, 6];
            fixed (float* src = moments.RawArray, dst = data)
            {
                float* srcPtr = src;
                float* dstPtr = dst;

                int length = rows * columns;
                while (length-- > 0)
                {
                    float a11 = *srcPtr++;
                    float a22 = *srcPtr++;
                    float a12 = *srcPtr++;
                    float a21 = a12;
                    float sqrt = (float)Math.Sqrt(4 * a12 * a21 + Math.Pow(a11 - a22, 2));
                    float sum = a11 + a22;

                    float eigen1 = sum + sqrt;
                    float eigen2 = sum - sqrt;

                    float x1, y1;
                    findEigenvector(a11, a12, a21, a22, eigen1, out x1, out y1);

                    float x2, y2;
                    findEigenvector(a11, a12, a21, a22, eigen2, out x2, out y2);
                    *dstPtr++ = eigen1;
                    *dstPtr++ = eigen2;
                    *dstPtr++ = x1;
                    *dstPtr++ = y1;
                    *dstPtr++ = x2;
                    *dstPtr++ = y2;
                }
            }
            EigensystemImage image = new EigensystemImage();
            image.SetData(data);
            return image;
        }

        private static void findEigenvector(float a11, float a12, float a21, float a22, float eigen, out float x1, out float x2)
        {
            if (eigen == 0)
                x1 = x2 = 0;
            else
            {
                float den = a11 - eigen;
                if (den != 0)
                {
                    x1 = -a12 / den;
                    x2 = 1;
                }
                else
                {
                    den = a22 - eigen;
                    x1 = 1;
                    x2 = -a21 / den;
                }
                float sum = (float)Math.Sqrt(x1 * x1 + x2 * x2);
                x1 /= sum;
                x2 /= sum;
            }
        }

		/// <summary>
		/// When converted to a Bitmap, the image will show red at corners, blue at uniform regions, and green at edges.
        /// This is done using a default sensitivity of .001.
		/// </summary>
		/// <returns>A representative Bitmap</returns>
		public BitmapSource ToBitmap()
		{
            return ToBitmap(SENSITIVITY);
		}

        /// <summary>
        /// When converted to a Bitmap, the image will show red at corners, blue at uniform regions, and green at edges.
        /// </summary>
        /// <param name="sensitivity">The sensitivity threshold to use when determining edges and corners.</param>
        /// <returns>A representative Bitmap</returns>
        public unsafe BitmapSource ToBitmap(float sensitivity)
        {
            RGBImage rgb = new RGBImage(Rows, Columns);
            fixed (byte* dst = rgb.RawArray)
            {
                fixed (float* src = RawArray)
                {
                    byte* dstPtr = dst;
                    float* srcPtr = src;
                    int length = Rows * Columns;
                    while (length-- > 0)
                    {
                        float lambda1 = *srcPtr++;
                        float lambda2 = *srcPtr++;
                        srcPtr += 4;
                        if (lambda1 < sensitivity && lambda2 < sensitivity)
                            dstPtr[2] = (byte)255;
                        else if (lambda2 < sensitivity)
                            dstPtr[1] = (byte)255;
                        else *dstPtr = (byte)255;
                        dstPtr += 3;
                    }
                }
            }
            return rgb.ToBitmap();
        }

        /// <summary>
        /// Width of the image (equivalent to <see cref="P:Columns" />)
        /// </summary>
        public int Width
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Height of the image (equivalment to <see cref="P:Rows" />)
        /// </summary>
        public int Height
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Sets whether this array is an integral array.  This property influences how the rectangle sum will be computed.
        /// </summary>
        public bool IsIntegral
        {
            get { return _handler.IsIntegral; }
            set { _handler.IsIntegral = value; }
        }

        /// <summary>
        /// Computes a sum of the values in the array within the rectangle starting at (<paramref name="startRow" />, <paramref name="startColumn"/>) in <paramref name="channel"/>
        /// with a size of <paramref name="rows"/>x<paramref name="columns"/>.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the rectangle</param>
        /// <param name="columns">Number of columns in the rectangle</param>
        /// <param name="channel">Channel to draw values from</param>
        /// <returns>The sum of all values in the rectangle</returns>
        public float ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
        {
            return _handler.ComputeRectangleSum(startRow, startColumn, rows, columns, channel);
        }

        /// <summary>
        /// Computes a sum of the values in the array starting at (<paramref name="row"/>, <paramref name="column"/>) in <paramref name="channel" /> 
        /// in a rectangle described by the offset and size in <paramref name="rect"/>.
        /// </summary>
        /// <param name="row">Reference row</param>
        /// <param name="column">Reference column</param>
        /// <param name="channel">Channel to draw values from</param>
        /// <param name="rect">Offset and size of the rectangle</param>
        /// <returns>The sum of all values in the rectangle</returns>
        public float ComputeRectangleSum(int row, int column, int channel, Rectangle rect)
        {
            return _handler.ComputeRectangleSum(row, column, channel, rect);
        }

        /// <summary>
        /// Number of rows in the array.
        /// </summary>
        public int Rows
        {
            get { return _handler.Rows; }
        }

        /// <summary>
        /// Number of columns in the array.
        /// </summary>
        public int Columns
        {
            get { return _handler.Columns; }
        }

        /// <summary>
        /// Number of channels in the array.
        /// </summary>
        public int Channels
        {
            get { return _handler.Channels; }
        }

        /// <summary>
        /// Sets the data of the array to <paramref name="data"/>.  This new array will replace the current one.  No copy is created.
        /// </summary>
        /// <param name="data">Array to handle</param>
        public void SetData(float[, ,] data)
        {
            _handler.SetData(data);
        }

        /// <summary>
        /// Sets the dimensions of the underlying array.  The resulting new array will replace the old array completely, no data will be copied over.
        /// </summary>
        /// <param name="rows">Number of desired rows in the new array.</param>
        /// <param name="columns">Number of desired columns in the new array.</param>
        /// <param name="channels">Number of desired channels in the new array.</param>
        public void SetDimensions(int rows, int columns, int channels)
        {
            _handler.SetDimensions(rows, columns, channels);
        }

        /// <summary>
        /// Extracts a portion of the array defined by the parameters.
        /// </summary>
        /// <param name="startRow">Starting row</param>
        /// <param name="startColumn">Starting column</param>
        /// <param name="rows">Number of rows in the portion</param>
        /// <param name="columns">Number of columns in the portion</param>
        /// <returns>A portion of the array</returns>
        public float[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            return _handler.ExtractRectangle(startRow, startColumn, rows, columns);
        }

        /// <summary>
        /// Indexes the underlying array.
        /// </summary>
        /// <param name="row">Desired row</param>
        /// <param name="column">Desired column</param>
        /// <param name="channel">Desired column</param>
        /// <returns>Value at (<paramref name="row"/>, <paramref name="column"/>, <paramref name="channel"/>) within the array.</returns>
        public float this[int row, int column, int channel]
        {
            get
            {
                return _handler[row, column, channel];
            }
            set
            {
                _handler[row, column, channel] = value;
            }
        }

        /// <summary>
        /// Extracts an entire channel from the array.
        /// </summary>
        /// <param name="channel">Channel to extract</param>
        /// <returns>Extracted channel</returns>
        public float[,] ExtractChannel(int channel)
        {
            return _handler.ExtractChannel(channel);
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public float[, ,] RawArray
        {
            get { return _handler.RawArray; }
        }

        /// <summary>
        /// Clears all data from the array.
        /// </summary>
        public void Clear()
        {
            _handler.Clear();
        }
    }
}
