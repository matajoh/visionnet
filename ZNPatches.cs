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
    /// Class to compute zero-normalized patch descriptors.
    /// </summary>
    public static class ZNPatches
    {
        /// <summary>
        /// Computes a zero-normalized patch descriptor from an image patch.
        /// </summary>
        /// <param name="patch">The patch to use for computation</param>
        /// <returns>The descriptor</returns>
        public unsafe static float[] Compute(float[,] patch)
        {
            int rows = patch.GetLength(0);
            int columns = patch.GetLength(1);
            float[] desc = new float[rows * columns];
            int length = rows*columns;
            fixed(float* src=patch,dst=desc){
                float* srcPtr = src;
                float* dstPtr = dst;

                float mean = 0;
                for(int i=0; i<length; i++, dstPtr++, srcPtr++){
                    float val = *srcPtr;
                    mean += val;
                    *dstPtr = val;
                }

                mean /= length;

                float stddev = 0;
                dstPtr = dst;
                for (int i = 0; i < length; i++, dstPtr++)
                {
                    float val = *dstPtr - mean;
                    stddev += val * val;
                    *dstPtr = val;
                }

                stddev /= length;
                stddev = (float)Math.Sqrt(stddev);
                dstPtr = dst;
                for (int i = 0; i < length; i++, dstPtr++)
                {
                    *dstPtr++ = *dstPtr / stddev;
                }
            }
            return desc;
        }
    }
}
