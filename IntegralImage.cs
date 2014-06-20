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

namespace VisionNET
{
    /// <summary>
    /// Computes the integral image of a source image in multiple dimensions, and is able to compute
    /// rectangle sums both globally and within a window of interest.
    /// </summary>
    public static class IntegralImage
    {
        /// <summary>
        /// Computes an integral image in one pass from the source image.
        /// </summary>
        /// <param name="input">Source image</param>
        /// <returns>Integral image</returns>
        public static unsafe T ComputeFloat<T>(IArrayHandler<float> input) where T : IArrayHandler<float>, new()
        {
            int rows = input.Rows;
            int columns = input.Columns;
            int channels = input.Channels;

            float[, ,] ii = new float[rows + 1, columns + 1, channels];
            float[, ,] s = new float[rows + 1, columns + 1, channels];
            //float[, ,] s2 = computeStandardDeviation ? new float[rows + 1, columns + 1, channels] : null;
            //float[, ,] ii2 = computeStandardDeviation ? new float[rows + 1, columns + 1, channels] : null;
            int stride = (columns+1)*channels;
            fixed (float* iiScan = ii, sScan = s, /*s2Scan = s2, ii2Scan = ii2,*/ src = input.RawArray)
            {
                float* srcPtr = src;
                float* iiPtrM = iiScan + stride;
                float* iiPtr = iiPtrM + channels;
                float* sPtrM = sScan + channels;
                float* sPtr = sPtrM + stride;

                //float* ii2PtrM = computeStandardDeviation ? ii2Scan + stride : null;
                //float* ii2Ptr = computeStandardDeviation ? ii2PtrM + channels : null;
                //float* s2PtrM = computeStandardDeviation ? s2Scan + channels : null;
                //float* s2Ptr = computeStandardDeviation ? s2PtrM + stride : null;

                for (int r = 1; r < rows + 1; r++)
                {
                    for (int c = 1; c < columns + 1; c++)
                    {
                        for (int i = 0; i < channels; i++)
                        {
                            //float assert = 0;
                            //for (int rr = 0; rr < r; rr++)
                            //    for (int cc = 0; cc < c; cc++)
                            //        assert += input[rr, cc, i];
                            float val = *srcPtr++;
                            //float val = input[r-1, c-1, i];
                            // normal
                            *sPtr = *sPtrM + val;
                            //s[r, c, i] = s[r - 1, c, i] + val;
                            *iiPtr = *iiPtrM + *sPtr;
                            //ii[r, c, i] = ii[r, c - 1, i] + s[r, c, i];
                            // squared
                            //if (computeStandardDeviation)
                            //{
                            //    *s2Ptr = *s2PtrM + val * val;
                            //    //s2[r, c, i] = s2[r - 1, c, i] + val * val;
                            //    *ii2Ptr = *ii2PtrM + *s2Ptr;
                            //    //ii2[r, c, i] = ii2[r, c - 1, i] + s2[r, c, i];
                            //}

                            sPtr++;
                            iiPtr++;
                            sPtrM++;
                            iiPtrM++;
                            //if (computeStandardDeviation)
                            //{
                            //    ii2PtrM++;
                            //    s2Ptr++;
                            //    ii2Ptr++;
                            //    s2PtrM++;
                            //}
                            
                            //if (assert != ii[r, c, i])
                            //    Console.WriteLine("hmmm");

                        }
                    }
                    sPtrM += channels;
                    sPtr += channels;
                    iiPtrM += channels;
                    iiPtr += channels;
                    //if (computeStandardDeviation)
                    //{
                    //    s2PtrM += channels;
                    //    s2Ptr += channels;
                    //    ii2PtrM += channels;
                    //    ii2Ptr += channels;
                    //}
                }
            }
            //float[] stddev = new float[channels];
            //if (computeStandardDeviation)
            //{
            //    for (int i = 0; i < channels; i++)
            //    {
            //        float sum = ii[rows, columns, i];
            //        float squaredSum = ii2[rows, columns, i];
            //        int count = rows * columns;
            //        float mean = sum / count;
            //        float variance = squaredSum / count - mean * mean;
            //        stddev[i] = (float)Math.Sqrt(variance);
            //    }
            //}
            s = null;
            GC.Collect();

            T result = new T();
            result.SetData(ii);
            result.IsIntegral = true;            

            return result;
        }

        /// <summary>
        /// Computes an integral image in one pass from the source image.
        /// </summary>
        /// <param name="input">Source image</param>
        /// <returns>Integral image</returns>
        public static unsafe T ComputeInteger<T>(IArrayHandler<int> input) where T : IArrayHandler<int>, new()
        {
            int rows = input.Rows;
            int columns = input.Columns;
            int channels = input.Channels;

            int[, ,] ii = new int[rows + 1, columns + 1, channels];
            int[, ,] s = new int[rows + 1, columns + 1, channels];
            int stride = (columns + 1) * channels;
            fixed (int* iiScan = ii, sScan = s, src = input.RawArray)
            {
                int* srcPtr = src;
                int* iiPtrM = iiScan + stride;
                int* iiPtr = iiPtrM + channels;
                int* sPtrM = sScan + channels;
                int* sPtr = sPtrM + stride;

                for (int r = 1; r < rows + 1; r++)
                {
                    for (int c = 1; c < columns + 1; c++)
                    {
                        for (int i = 0; i < channels; i++)
                        {
                            int val = *srcPtr++;
                            *sPtr = *sPtrM + val;
                            *iiPtr = *iiPtrM + *sPtr;

                            sPtr++;
                            iiPtr++;
                            sPtrM++;
                            iiPtrM++;
                        }
                    }
                    sPtrM += channels;
                    sPtr += channels;
                    iiPtrM += channels;
                    iiPtr += channels;
                }
            }
            T result = new T();
            result.SetData(ii);
            result.IsIntegral = true;

            return result;
        }
    }
}
