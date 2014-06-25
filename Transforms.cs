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
using System.Linq;
using MathNet.Numerics.LinearAlgebra.Double;

namespace VisionNET
{
    /// <summary>
	/// Enumeration of the various methods that can be used for interpolation when scaling.
    /// </summary>
	public enum InterpolationMethod 
	{ 
		/// <summary>
		/// No interpolation is performed.
		/// </summary>
		None, 
		/// <summary>
		/// Bi-linear interpolation is used.
		/// </summary>
		Linear,
		/// <summary>
		/// Bi-cubic interpolation is used.
		/// </summary>
		Cubic 
	};

    /// <summary>
	/// This class contains routines for image transformation and alteration.
    /// </summary>
	public static class Transforms
	{
        /// <summary>
        /// Method which performs an affine transform on an image.
        /// </summary>
        /// <typeparam name="I">The type of the image</typeparam>
        /// <param name="image">The image to transform</param>
        /// <param name="affineTransform">The matrix representing the affine transform</param>
        /// <param name="border">A border around the transformed image</param>
        /// <param name="background">The background data to use for empty pixels in the transformed image</param>
        /// <param name="method">The interpolation method to use</param>
        /// <returns>A transformed image</returns>
        public static unsafe I Affine<I>(I image, Matrix affineTransform, int border, float[] background, InterpolationMethod method) where I:IMultichannelImage<float>, new()
        {
            int r, c, i;
            float* fillPtr, srcPtr;

            double[] xValues = new double[4];
            double[] yValues = new double[4];

            double x = -image.Width / 2.0;
            double y = -image.Height / 2.0;
            var corner = new DenseVector(new double[] { x, y, 1 });
            var transformedCorner = affineTransform.Multiply(corner);
            xValues[0] = transformedCorner[0];
            yValues[0] = transformedCorner[1];

            corner[0] *= -1;
            transformedCorner = affineTransform.Multiply(corner);
            xValues[1] = transformedCorner[0];
            yValues[1] = transformedCorner[1];

            corner[1] *= -1;
            transformedCorner = affineTransform.Multiply(corner);
            xValues[2] = transformedCorner[0];
            yValues[2] = transformedCorner[1];

            corner[0] *= -1;
            transformedCorner = affineTransform.Multiply(corner);
            xValues[3] = transformedCorner[0];
            yValues[3] = transformedCorner[1];

            double minX = xValues.Min();
            double minY = yValues.Min();
            double maxX = xValues.Max();
            double maxY = yValues.Max();

            I transformed = new I();
            transformed.SetDimensions((int)(maxY - minY) + 1 + border * 2, (int)(maxX - minX) + 1 + border * 2, image.Channels);

            double srcCenterX = image.Width / 2.0;
            double srcCenterY = image.Height / 2.0;
            double dstCenterX = transformed.Width / 2.0;
            double dstCenterY = transformed.Height / 2.0;

            var inverse = affineTransform.Inverse();

            int dstRows = transformed.Rows;
            int dstColumns = transformed.Columns;
            int srcRows = image.Rows;
            int srcColumns = image.Columns;
            int channels = image.Channels;

            fixed (float* srcBuf = image.RawArray, dstBuf = transformed.RawArray, fillBuf = background)
            {
                float* dstPtr = dstBuf;
                for (r = 0; r < dstRows; r++)
                {
                    for (c = 0; c < dstColumns; c++)
                    {
                        x = c + .5 - dstCenterX;
                        y = r + .5 - dstCenterY;

                        var transformPoint = new DenseVector(new double[] { x, y, 1 });
                        var sourcePoint = inverse.Multiply(transformPoint);
                        double srcX = sourcePoint[0];
                        double srcY = sourcePoint[1];

                        double srcC = srcX + srcCenterX;
                        double srcR = srcY + srcCenterY;

                        if ((int)srcC > 0 && (int)srcC < srcColumns && (int)srcR > 0 && (int)srcR < srcRows)
                        {
                            switch (method)
                            {
                                case InterpolationMethod.Cubic:
                                    InterpolateCubic(srcBuf, dstPtr, (float)srcR, (float)srcC, srcRows, srcColumns, channels);
                                    dstPtr += channels;
                                    break;

                                case InterpolationMethod.Linear:
                                    InterpolateLinear(srcBuf, dstPtr, (float)srcR, (float)srcC, srcRows, srcColumns, channels);
                                    dstPtr += channels;
                                    break;

                                case InterpolationMethod.None:
                                    for (srcPtr = srcBuf + ((int)srcR) * srcColumns + (int)srcC, i = 0; i < channels; srcPtr++, dstPtr++, i++)
                                        *dstPtr = *srcPtr;
                                    break;
                            }
                        }
                        else
                        {
                            for (i = 0, fillPtr = fillBuf; i < channels; i++, dstPtr++, fillPtr++)
                                *dstPtr = *fillPtr;
                        }
                    }
                }
            }

            return transformed;
        }

        /// <summary>
        /// Rotates <paramref name="image"/> and returns a new image with a border around it, where unseen regions are filled with a background value.
        /// </summary>
        /// <typeparam name="I">An image type</typeparam>
        /// <param name="image">The image to rotate</param>
        /// <param name="angleInRadians">The angle of rotation</param>
        /// <param name="border">The border to have around the rotated image</param>
        /// <param name="background">The background fill value</param>
        /// <param name="method">The method of interpolation to use</param>
        /// <returns>The rotated image</returns>
        public static unsafe I Rotate<I>(I image, double angleInRadians, int border, float[] background, InterpolationMethod method) where I : IMultichannelImage<float>, new()
        {
            int r,c,i;
            float *fillPtr, srcPtr;
            double A = Math.Cos(angleInRadians);
            double B = Math.Sin(angleInRadians);
            double C = -B;
            double D = A;

            double[] xValues = new double[4];
            double[] yValues = new double[4];

            double x = -image.Width / 2.0;
            double y = -image.Height / 2.0;
            xValues[0] = A * x + B * y;
            yValues[0] = C * x + D * y;

            x *= -1;
            xValues[1] = A * x + B * y;
            yValues[1] = C * x + D * y;

            y *= -1;
            xValues[2] = A * x + B * y;
            yValues[2] = C * x + D * y;

            x *= -1;
            xValues[3] = A * x + B * y;
            yValues[3] = C * x + D * y;

            double minX = xValues.Min();
            double minY = yValues.Min();
            double maxX = xValues.Max();
            double maxY = yValues.Max();

            I rotated = new I();
            rotated.SetDimensions((int)(maxY - minY) + 1 + border * 2, (int)(maxX - minX) + 1 + border * 2, image.Channels);

            double srcCenterX = image.Width / 2.0;
            double srcCenterY = image.Height / 2.0;
            double dstCenterX = rotated.Width / 2.0;
            double dstCenterY = rotated.Height / 2.0;

            D = B;
            B = C;
            C = D;
            D = A;

            int dstRows = rotated.Rows;
            int dstColumns = rotated.Columns;
            int srcRows = image.Rows;
            int srcColumns= image.Columns;
            int channels = image.Channels;

            fixed (float* srcBuf = image.RawArray, dstBuf = rotated.RawArray, fillBuf = background)
            {
                float* dstPtr = dstBuf;
                for (r = 0; r < dstRows; r++)
                {
                    for (c = 0; c < dstColumns; c++)
                    {
                        x = c + .5 - dstCenterX;
                        y = r + .5 - dstCenterY;

                        double srcX = A * x + B * y;
                        double srcY = C * x + D * y;

                        double srcC = srcX + srcCenterX;
                        double srcR = srcY + srcCenterY;

                        if ((int)srcC > 0 && (int)srcC < srcColumns && (int)srcR > 0 && (int)srcR < srcRows)
                        {
                            switch (method)
                            {
                                case InterpolationMethod.Cubic:
                                    InterpolateCubic(srcBuf, dstPtr, (float)srcR, (float)srcC, srcRows, srcColumns, channels);
                                    dstPtr += channels;
                                    break;

                                case InterpolationMethod.Linear:
                                    InterpolateLinear(srcBuf, dstPtr, (float)srcR, (float)srcC, srcRows, srcColumns, channels);
                                    dstPtr += channels;
                                    break;

                                case InterpolationMethod.None:
                                    for (srcPtr = srcBuf + ((int)srcR) * srcColumns + (int)srcC, i = 0; i < channels; srcPtr++, dstPtr++, i++)
                                        *dstPtr = *srcPtr;
                                    break;
                            }
                        }
                        else
                        {
                            for (i = 0, fillPtr = fillBuf; i < channels; i++, dstPtr++, fillPtr++)
                                *dstPtr = *fillPtr;
                        }
                    }
                }
            }

            return rotated;
        }

        /// <summary>
        /// Adds Gaussian noise to an image without alterning the parameter.
        /// </summary>
        /// <typeparam name="I">The image type</typeparam>
        /// <param name="image">The image to use</param>
        /// <param name="sigma">Sigma of the gaussian noise distribution</param>
        /// <returns>A noisier image</returns>
        public static unsafe I AddNoise<I>(I image, float sigma) where I : IMultichannelImage<float>, new()
        {
            Gaussian gauss = new Gaussian(0, sigma);
            int rows = image.Rows;
            int columns = image.Columns;
            int channels = image.Channels;
            float[, ,] data = new float[rows, columns, channels];
            fixed (float* imageSrc = image.RawArray, dataSrc = data)
            {
                float* imagePtr = imageSrc;
                float* dataPtr = dataSrc;
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < columns; c++)
                        for (int i = 0; i < channels; i++)
                            *dataPtr++ = *imagePtr++ + gauss.Sample();
            }
            I result = new I();
            result.SetData(data);
            return result;            
        }

        /// <summary>
        /// Flips an image horizontally on its vertical axis.
        /// </summary>
        /// <typeparam name="I">Image type</typeparam>
        /// <param name="image">Image to flip</param>
        /// <returns>Flipped image</returns>
        public static unsafe I FlipHorizontal<I>(I image) where I : IMultichannelImage<float>, new()
        {
            int rows = image.Rows;
            int columns = image.Columns;
            int channels = image.Channels;
            float[, ,] data = new float[rows, columns, channels];
            int stride = columns * channels;
            fixed (float* src = image.RawArray, dst = data)
            {
                float* srcPtr = src + stride - channels;
                float* dstPtr = dst;
                for (int r = 0; r < rows; r++, srcPtr += stride)
                {
                    float* srcScan = srcPtr;
                    for (int c = 0; c < columns; c++, srcScan -= channels)
                    {
                        for (int j = 0; j < channels; j++)
                            *dstPtr++ = srcScan[j];
                    }
                }
            }
            I result = new I();
            result.SetData(data);
            return result;
        }

        /// <summary>
        /// Flips an image vertically on its horizontal axis.
        /// </summary>
        /// <typeparam name="I">Image type</typeparam>
        /// <param name="image">Image to flip</param>
        /// <returns>Flipped image</returns>
        public static unsafe I FlipVertical<I>(I image) where I : IMultichannelImage<float>, new()
        {
            int rows = image.Rows;
            int columns = image.Columns;
            int channels = image.Channels;
            float[, ,] data = new float[rows, columns, channels];
            int stride = columns * channels;
            fixed (float* src = image.RawArray, dst = data)
            {
                float* srcPtr = src + (rows - 1) * stride;
                float* dstPtr = dst;
                for (int c = 0; c < columns; c++, srcPtr += channels, dstPtr += channels)
                {
                    float* srcScan = srcPtr;
                    float* dstScan = dstPtr;
                    for (int r = 0; r < rows; r++, srcScan -= stride, dstScan += stride)
                    {
                        for (int j = 0; j < channels; j++)
                            dstScan[j] = srcScan[j];
                    }
                }
            }
            I result = new I();
            result.SetData(data);
            return result;
        }
		/// <summary>
		/// Returns a scaled version of an image using bi-linear interpolation.
		/// </summary>
		/// <typeparam name="I">Must inherit from MultichannelImage.</typeparam>
		/// <param name="image">The image to scale</param>
		/// <param name="rows">The desired number of rows</param>
		/// <param name="columns">The desired number of columns</param>
		/// <returns>The scaled image</returns>
        public static I Scale<I>(I image, int rows, int columns) where I : IMultichannelImage<float>, new()
		{
			return Scale<I>(image, rows, columns, InterpolationMethod.Linear);
		}

		/// <summary>
		/// Returns a scaled version of an image using provided method.
		/// </summary>
        /// <typeparam name="I">Must inherit from MultichannelImage.</typeparam>
		/// <param name="image">The image to scale</param>
		/// <param name="rows">The desired number of rows</param>
		/// <param name="columns">The desired number of columns</param>
		/// <param name="method">The method to use</param>
		/// <returns>The scaled image</returns>
        public static I Scale<I>(I image, int rows, int columns, InterpolationMethod method) where I : IMultichannelImage<float>, new()
		{
			switch (method)
			{
				case InterpolationMethod.None:
					return scale<I>(image, rows, columns);

				case InterpolationMethod.Linear:
					return scaleLinear<I>(image, rows, columns);

				case InterpolationMethod.Cubic:
					return scaleCubic<I>(image, rows, columns);

				default:
					throw new Exception("Invalid Interpolation Method");
			}
		}

        /// <summary>
        /// Scale an image through subsampling.
        /// </summary>
        /// <typeparam name="I">Type of the image to subsample</typeparam>
        /// <typeparam name="T">Underlying datatype of the image</typeparam>
        /// <param name="image">Image to subsample</param>
        /// <param name="subsample">Sampling rate</param>
        /// <returns>A scaled version of <paramref name="image"/></returns>
        public static I Scale<I, T>(I image, int subsample) where I : IMultichannelImage<T>,new()
        {
            int rows = image.Rows;
            int columns = image.Columns;
            int channels = image.Channels;
            int newRows = rows / subsample;
            int newColumns = columns / subsample;
            T[,,] src = image.RawArray;
            T[, ,] dst = new T[newRows, newColumns, channels];
            for (int r = 0, srcR = 0; r < newRows; r++, srcR+=subsample)
                for (int c = 0, srcC = 0; c < newColumns; c++, srcC+=subsample)
                    for (int i = 0; i < channels; i++)
                        dst[r, c, i] = src[srcR, srcC, i];
            I result = new I();
            result.SetData(dst);
            return result;
        }

        private static I scale<I>(I image, int rows, int columns) where I:IMultichannelImage<float>,new()
		{
			int orows = image.Rows;
			int ocolumns = image.Columns;
			int channels = image.Channels;
			float cscale = (float)columns / ocolumns;
			float rscale = (float)rows / orows;

            float[, ,] data = new float[rows, columns, channels];
			for (int r = 0; r < rows; r++)
			{
				for (int c = 0; c < columns; c++)
				{
					int i = (int)(r / rscale);
					int j = (int)(c / cscale);
					i = Math.Min(i, orows - 1);
					j = Math.Min(j, ocolumns - 1);
					i = Math.Max(i, 0);
					j = Math.Max(j, 0);
					for (int s = 0; s < channels; s++)
                        data[r, c, s] = image[i, j, s];
				}
			}
            I result = new I();
            result.SetData(data);
			return result;
		}

        private static unsafe I scaleLinear<I>(I image, int rows, int columns) where I : IMultichannelImage<float>, new()
		{
			int orows = image.Rows;
			int ocolumns = image.Columns;
			int channels = image.Channels;
			float cscale = (float)ocolumns/columns;
			float rscale = (float)orows/rows;

            float[, ,] data = new float[rows, columns, channels];
            fixed (float* src = image.RawArray, dst = data)
            {
                float* srcPtr = src;
                float* dstPtr = dst;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        InterpolateLinear(srcPtr, dstPtr, r * rscale, c * cscale, orows, ocolumns, channels);
                        dstPtr += channels;
                        //float a = r / rscale;
                        //float b = c / cscale;
                        //int i = (int)a;
                        //int j = (int)b;
                        //a -= i;
                        //b -= j;

                        //i = Math.Max(0, Math.Min(i, orows - 1));
                        //j = Math.Max(0, Math.Min(j, ocolumns - 1));
                        //Point q11 = new Point(i, j);
                        //Point q12 = i + 1 >= orows ? q11 : new Point(i + 1, j);
                        //Point q21 = j + 1 >= ocolumns ? q12 : new Point(i, j + 1);
                        //Point q22 = i + 1 >= orows || j + 1 >= ocolumns ? q21 : new Point(i + 1, j + 1);

                        //float rema = 1-a;
                        //float remb = 1-b;
                        //for(int s=0; s<channels; s++)
                        //    result[r, c, s] = remb * (rema * image[q11.X, q11.Y, s] + a * image[q12.X, q12.Y, s]) + b * (rema * image[q12.X, q12.Y, s] + a * image[q22.X, q22.Y, s]);
                    }
                }
            }
            I result = new I();
            result.SetData(data);
			return result;
		}

        /// <summary>
        /// Interplates a value from an array using linear interpolation. 
        /// </summary>
        /// <param name="src">The source array</param>
        /// <param name="dst">Pointer to the destination</param>
        /// <param name="srcRow">The row in the source array to interpolate</param>
        /// <param name="srcColumn">The column in the source array to interpolate</param>
        /// <param name="srcRows">The number of rows in the source array</param>
        /// <param name="srcColumns">The number of columns in the source array</param>
        /// <param name="channels">The number of channels to interpolate.  Channels are assumed to be in order starting at the destination pointer.</param>
        public static unsafe void InterpolateLinear(float* src, float* dst, float srcRow, float srcColumn, int srcRows, int srcColumns, int channels)
        {
            int i = (int)srcRow;
            int j = (int)srcColumn;
            float a = srcRow - i;
            float b = srcColumn - j;
            float rema = 1-a;
            float remb = 1-b;
            float[,] multipliers = new float[2,2];
            multipliers[0,0] = rema*remb;
            multipliers[0,1] = rema*b;
            multipliers[1,0] = a*remb;
            multipliers[1,1] = a*b;
            
            i = Math.Max(0, Math.Min(i, srcRows - 2));
            j = Math.Max(0, Math.Min(j, srcColumns - 2));
            int stride = srcColumns * channels;
            float* srcPtr = src + i * stride + j * channels;
            for (int s = 0; s < channels; s++)
            {
                float sum = 0;
                float* srcScan = srcPtr;
                for (int r = 0; r < 2; r++)
                {
                    float* srcScan2 = srcScan;
                    for (int c = 0; c < 2; c++)
                    {
                        sum += multipliers[r, c] * *srcScan2;
                        srcScan2 += channels;
                    }
                    srcScan += stride;
                }
                *dst++ = sum;
                srcPtr++;
            }
        }

        private static float R(float x)
		{
            float Px2 = P(x + 2);
            float Px1 = P(x + 1);
            float Px = P(x);
            float Pxn = P(x - 1);
            float a = Px2 * Px2 * Px2;
            float b = 4 * Px1 * Px1 * Px1;
            float c = 6 * Px * Px * Px;
            float d = 4 * Pxn * Pxn * Pxn;
            return (a - b + c - d) / 6;
            //return (float)(Math.Pow(P(x + 2), 3) - 4 * Math.Pow(P(x + 1), 3) + 6 * Math.Pow(P(x), 3) - 4 * Math.Pow(P(x - 1), 3)) / 6;
		}

		private static float P(float x)
		{
			return x > 0 ? x : 0;
		}

        private static unsafe I scaleCubic<I>(I image, int rows, int columns) where I : IMultichannelImage<float>, new()
		{
			int orows = image.Rows;
			int ocolumns = image.Columns;
			int channels = image.Channels;
			float cscale = (float)ocolumns/columns;
			float rscale = (float)orows/rows;

            float[,,] data = new float[rows,columns,channels];
            fixed (float* src=image.RawArray,dst = data)
            {
                float* srcPtr = src;
                float* dstPtr = dst;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        InterpolateCubic(srcPtr, dstPtr, r * rscale, c * cscale, orows, ocolumns, channels);
                        dstPtr += channels;
                    }
                }
            }
            I result = new I();
            result.SetData(data);
			return result;
		}

        /// <summary>
        /// Interplates a value from an array using cubic interpolation. 
        /// </summary>
        /// <param name="src">The source array</param>
        /// <param name="dst">Pointer to the destination</param>
        /// <param name="srcRow">The row in the source array to interpolate</param>
        /// <param name="srcColumn">The column in the source array to interpolate</param>
        /// <param name="srcRows">The number of rows in the source array</param>
        /// <param name="srcColumns">The number of columns in the source array</param>
        /// <param name="channels">The number of channels to interpolate.  Channels are assumed to be in order starting at the destination pointer.</param>
        public static unsafe void InterpolateCubic(float* src, float*dst, float srcRow, float srcColumn, int srcRows, int srcColumns, int channels)
        {
            int i = (int)srcRow;
            float a = srcRow - i;
            int j = (int)srcColumn;
            float b = srcColumn - j;
            int stride = srcColumns * channels;
            float* srcPtr = src + (i - 1) * stride + (j - 1) * channels;
            if (j - 1 < 0)
                srcPtr += channels;
            if (i - 1 < 0)
                srcPtr += stride;

            for (int s = 0; s < channels; s++)
            {
                float sum = 0;
                float* srcScan = srcPtr;
                for (int m = -1; m <= 2; m++)
                {
                    int test;
                    float* srcScan2 = srcScan;
                    for (int n = -1; n <= 2; n++)
                    {
                        float ra = R(a - m);
                        float rb = R(n - b);
                        sum += *srcScan2 * ra * rb;
                        test = j + n;
                        if (test < srcColumns - 1 && test >= 0)
                            srcScan2 += channels;
                    }
                    test = i + m;
                    if (test < srcRows - 1 && test >= 0)
                        srcScan += stride;
                }
                *dst++ = sum;
                srcPtr++;
            }
        }
    }
}
