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

using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;

namespace VisionNET
{
    /// <summary>
    /// Computes the corners of the image using the Harris algorithm.
    /// </summary>
	public static class Harris
	{
        /// <summary>
        /// Extracts corners from the gradient image using the default threshold.
        /// </summary>
        /// <param name="grad">Gradient image of source</param>
        /// <returns>A list of corners</returns>
        public static Vector[] Extract(GradientImage grad)
		{
			return Extract(grad, EigensystemImage.SENSITIVITY);
		}

        /// <summary>
        /// Extracts corners from the gradient image.  The method used here is one which uses the eigensystem at each
        /// pixel (computed from the smoothed second moments) to determine whether a corner is present.  A corner is
        /// one in which both eigenvalues are above the threshold.
        /// </summary>
        /// <param name="grad">Gradient image of source</param>
        /// <param name="threshold">Threshold used to determine corners</param>
        /// <returns>A list of corners</returns>
        public static unsafe Vector[] Extract(GradientImage grad, float threshold)
		{
			EigensystemImage eigen = EigensystemImage.Compute(SecondMomentImage.Compute(grad));
            List<Vector> points = new List<Vector>();
			int rows = eigen.Rows;
			int columns = eigen.Columns;
            fixed (float* src = eigen.RawArray)
            {
                float* srcPtr = src;
                int channels = eigen.Channels;
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < columns; c++, srcPtr += channels)
                    {
                        float lambda1 = srcPtr[0];
                        float lambda2 = srcPtr[1];
                        if (lambda1 > threshold && lambda2 > threshold)
                            points.Add(new DenseVector(new float[]{c, r}));
                    }
            }
			return points.ToArray();
		}
        /*
        /// <summary>
        /// Extracts
        /// </summary>
        /// <param name="grad"></param>
        /// <param name="k"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static unsafe Point[] Extract(GradientImage grad, float k, float threshold)
		{
            List<Point> result = new List<Point>();

			int rows = grad.Rows;
			int columns = grad.Columns;
            fixed (float* dst = grad._data)
            {
                float* dstPtr = dst;
                int length = rows * columns;
                int channels = grad.Channels;
                for (int i = 0; i < length; i++, dstPtr += channels)
                {
                    dstPtr[2] *= 255;
                    dstPtr[3] *= 255;
                }
            }

			SecondMomentImage moments = SecondMomentImage.Compute(grad);
			moments = Convolution.ConvolveGaussian<SecondMomentImage>(moments, 1);
			float[,] corners = new float[rows, columns];
            fixed (float* src = moments._data, dst = corners)
            {
                int length = rows * columns;
                float* srcPtr = src;
                float* dstPtr = dst;
                while (length-- > 0)
                {
                    float A = *srcPtr++;
                    float B = *srcPtr++;
                    float C = *srcPtr++;
                    *dstPtr++ = (float)((A * B - C * C) - k * Math.Pow(A + B, 2));
                }
            }
			bool[,] max = new bool[rows, columns];
            fixed (float* src = corners)
            {
                fixed (bool* dst = max)
                {
                    float* srcPtrM = src + columns;
                    float* srcPtr = srcPtrM + 1;
                    float* srcPtrP = srcPtr + 1;
                    bool* dstPtr = dst + 1 + columns;
                    for (int r = 1; r < rows - 1; r++, srcPtrM += 3, srcPtr += 3, srcPtrP += 3, dstPtr += 3)
                    {
                        for (int c = 1; c < columns - 1; c++, srcPtrM++, srcPtr++, srcPtrP++, dstPtr++)
                        {
                            float left = *srcPtrM;
                            float current = *srcPtr;
                            float right = *srcPtrP;
                            if (current > left)
                                *dstPtr = current > right;
                        }
                    }

                    float* srcScan = src + 1;
                    bool* dstScan = dst + 1 + columns;
                    for (int c = 1; c < columns - 1; c++, srcScan++, dstScan++)
                    {
                        srcPtrM = srcScan;
                        srcPtr = srcPtrM + columns;
                        srcPtrP = srcPtr + columns;
                        dstPtr = dstScan;

                        for (int r = 1; r < rows - 1; r++, srcPtrM += columns, srcPtr += columns, srcPtrP += columns, dstPtr += columns)
                        {
                            float top = *srcPtrM;
                            float current = *srcPtr;
                            float bottom = *srcPtrP;
                            bool test = *dstPtr;
                            if (test && current > top)
                                *dstPtr = current > bottom;
                        }
                    }


                    srcPtr = src + 1 + columns;
                    dstPtr = dst + 1 + columns;
                    for (int r = 1; r < rows - 1; r++, srcPtr += 3, dstPtr += 3)
                        for (int c = 1; c < columns - 1; c++, srcPtr++, dstPtr++)
                        {
                            if (*dstPtr)
                            {
                                float current = *srcPtr;
                                float tl = srcPtr[-columns - 1];//corners[r - 1, c - 1];
                                float tr = srcPtr[1 - columns];//corners[r - 1, c + 1];
                                float bl = srcPtr[columns - 1];//corners[r + 1, c - 1];
                                float br = srcPtr[columns + 1];//corners[r + 1, c + 1];
                                if (current > tl && current > tr && current > bl && current > br && current > threshold)
                                    result.Add(new Point(c, r));
                            }
                        }
                }
            }

			return result.ToArray();
		}
         */
	}
}
