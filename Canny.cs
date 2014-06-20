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

namespace VisionNET
{
    /// <summary>
    /// Computes the edges in an image using the Canny algorithm.
    /// </summary>
    public static class Canny
    {
        private const byte NOEDGE = (byte)255;
        private const byte POSSIBLE_EDGE = (byte)128;
        private const byte EDGE = (byte)0;
        /// <summary>
        /// Default lower threshold for hysteresis.
        /// </summary>
        public const float LOWER_THRESHOLD = .33f;
        /// <summary>
        /// Default higher threshold for hysteresis.
        /// </summary>
        public const float HIGHER_THRESHOLD = .8f;

        /// <summary>
        /// Computes an edge image using the Canny algorithm from the provided gradient image.  Uses the
        /// default lower threshold and higher threshold.
        /// </summary>
        /// <param name="image">The image to use for edge-seeking</param>
        /// <returns>an edge image</returns>
        public static BinaryImage 
            Compute(GradientImage image)
        {
            return Compute(image, LOWER_THRESHOLD, HIGHER_THRESHOLD);
        }

        /// <summary>
        /// Computes an edge image using the provided image.  The lower threshold and higher threshold
        /// provided are those used for hysteresis, on a scale from 0 to 1.
        /// </summary>
        /// <param name="image">The image to use for edge-seeking</param>
        /// <param name="lowThreshold">The lower threshold for hysteresis, from 0 to 1</param>
        /// <param name="highThreshold">The higher threshold for hysteresis, from 0 to 1</param>
        /// <returns></returns>
        public static BinaryImage Compute(
            GradientImage image,
            float lowThreshold,
            float highThreshold
            )
        {
            IArrayHandler<byte> nms = NonMaximalSuppression(image);

            return Hysteresis(image, nms, lowThreshold, highThreshold);
        }

        private static unsafe BinaryImage Hysteresis(GradientImage grad, IArrayHandler<byte> nms, float tlow, float thigh)
        {
            int r, c, pos, numedges, highcount;
            int[] hist = new int[short.MaxValue];
            float maximum_mag, lowthreshold, highthreshold;
            maximum_mag = 0;
            float[,] magChannel = grad.ExtractChannel(0);
            short[] mag = new short[magChannel.Length];
            fixed (short* dst = mag)
            {
                fixed (float* src = magChannel)
                {
                    float* srcPtr = src;
                    short* dstPtr = dst;
                    for (int i = 0; i < mag.Length; i++, srcPtr++, dstPtr++)
                        *dstPtr = (short)(*srcPtr * 255);
                }
            }

            int rows = grad.Rows;
            int cols = grad.Columns;

            byte[] edge = new byte[rows * cols];
            fixed (byte* src = nms.RawArray)
            {
                byte* srcPtr = src;
                int length = rows * cols;
                for (pos = 0; pos < length; pos++)
                {
                    if (*srcPtr++ == POSSIBLE_EDGE) 
                        edge[pos] = POSSIBLE_EDGE;
                    else edge[pos] = NOEDGE;
                }
            }

            for (r = 0, pos = 0; r < rows; r++, pos += cols)
            {
                edge[pos] = NOEDGE;
                edge[pos + cols - 1] = NOEDGE;
            }
            pos = (rows - 1) * cols;
            for (c = 0; c < cols; c++, pos++)
            {
                edge[c] = NOEDGE;
                edge[pos] = NOEDGE;
            }

            for (r = 0; r < short.MaxValue; r++) hist[r] = 0;
            for (r = 0, pos = 0; r < rows; r++)
            {
                for (c = 0; c < cols; c++, pos++)
                {
                    if (edge[pos] == POSSIBLE_EDGE) hist[mag[pos]]++;
                }
            }

            for (r = 1, numedges = 0; r < short.MaxValue; r++)
            {
                if (hist[r] != 0)
                    maximum_mag = (short)r;
                numedges += hist[r];
            }

            highcount = (int)(numedges * thigh + 0.5);

            r = 1;
            numedges = hist[1];
            while ((r < (maximum_mag - 1)) && (numedges < highcount))
            {
                r++;
                numedges += hist[r];
            }
            highthreshold = (short)r;
            lowthreshold = (short)(highthreshold * tlow + 0.5);

            for (r = 0, pos = 0; r < rows; r++)
            {
                for (c = 0; c < cols; c++, pos++)
                {
                    if ((edge[pos] == POSSIBLE_EDGE) && (mag[pos] >= highthreshold))
                    {
                        edge[pos] = EDGE;
                        follow_edges(edge, mag, pos, lowthreshold, cols);
                    }
                }
            }

            for (r = 0, pos = 0; r < rows; r++)
            {
                for (c = 0; c < cols; c++, pos++) if (edge[pos] != EDGE) edge[pos] = NOEDGE;
            }
            BinaryImage edgeImage = new BinaryImage(rows, cols);
            fixed (bool* dst = edgeImage.RawArray)
            {
                bool* dstPtr = dst;
                int length = rows * cols;
                for (pos = 0; pos < length; pos++)
                    *dstPtr++ = edge[pos] == EDGE;
            }

            return edgeImage;
        }

        private static void follow_edges(byte[] map, short[] mag, int pos, float lowval, int cols)
        {
            int i;
            int[] x = new int[] { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] y = new int[] { 0, 1, 1, 1, 0, -1, -1, -1 };

            for (i = 0; i < 8; i++)
            {
                int tempPos = pos - y[i] * cols + x[i];

                if ((map[pos] == POSSIBLE_EDGE) && (mag[pos] > lowval))
                {
                    map[pos] = EDGE;
                    follow_edges(map, mag, tempPos, lowval, cols);
                }
            }
        }
        
        private static unsafe IArrayHandler<byte> NonMaximalSuppression(GradientImage magImage)
        {
            int nrows = magImage.Rows;
            int ncols = magImage.Columns;
            int rowcount, colcount, count;

            float[,] magVals = magImage.ExtractChannel(0);
            float[,] gradxVals = magImage.ExtractChannel(2);
            float[,] gradyVals = magImage.ExtractChannel(3);

            byte[] data = new byte[nrows*ncols];
            fixed (byte* result = data)
            {
                fixed (float* mag = magVals, gradx = gradxVals, grady = gradyVals)
                {
                    float* magrowptr, magptr;
                    float* gxrowptr, gxptr;
                    float* gyrowptr, gyptr;
                    float z1, z2;
                    float m00, gx, gy;
                    float mag1, mag2, xperp, yperp;
                    byte* resultrowptr, resultptr;


                    /****************************************************************************
                    * Zero the edges of the result image.
                    ****************************************************************************/
                    for (count = 0, resultrowptr = result, resultptr = result + ncols * (nrows - 1);
                        count < ncols; resultptr++, resultrowptr++, count++)
                    {
                        *resultrowptr = *resultptr = 0;
                    }

                    for (count = 0, resultptr = result, resultrowptr = result + ncols - 1;
                        count < nrows; count++, resultptr += ncols, resultrowptr += ncols)
                    {
                        *resultptr = *resultrowptr = 0;
                    }

                    /****************************************************************************
                    * Suppress non-maximum points.
                    ****************************************************************************/
                    for (rowcount = 1, magrowptr = mag + ncols + 1, gxrowptr = gradx + ncols + 1,
                       gyrowptr = grady + ncols + 1, resultrowptr = result + ncols + 1;
                       rowcount < nrows - 2;
                       rowcount++, magrowptr += ncols, gyrowptr += ncols, gxrowptr += ncols,
                       resultrowptr += ncols)
                    {
                        for (colcount = 1, magptr = magrowptr, gxptr = gxrowptr, gyptr = gyrowptr,
                           resultptr = resultrowptr; colcount < ncols - 2;
                           colcount++, magptr++, gxptr++, gyptr++, resultptr++)
                        {
                            m00 = *magptr;
                            gx = *gxptr;
                            gy = *gyptr;
                            if (m00 == 0)
                            {
                                *resultptr = NOEDGE;
                                continue;
                            }
                            else
                            {
                                xperp = -(gx = *gxptr) / ((float)m00);
                                yperp = (gy = *gyptr) / ((float)m00);
                            }

                            if (gx >= 0)
                            {
                                if (gy >= 0)
                                {
                                    if (gx >= gy)
                                    {
                                        /* 111 */
                                        /* Left point */
                                        z1 = *(magptr - 1);
                                        z2 = *(magptr - ncols - 1);

                                        mag1 = (m00 - z1) * xperp + (z2 - z1) * yperp;

                                        /* Right point */
                                        z1 = *(magptr + 1);
                                        z2 = *(magptr + ncols + 1);

                                        mag2 = (m00 - z1) * xperp + (z2 - z1) * yperp;
                                    }
                                    else
                                    {
                                        /* 110 */
                                        /* Left point */
                                        z1 = *(magptr - ncols);
                                        z2 = *(magptr - ncols - 1);

                                        mag1 = (z1 - z2) * xperp + (z1 - m00) * yperp;

                                        /* Right point */
                                        z1 = *(magptr + ncols);
                                        z2 = *(magptr + ncols + 1);

                                        mag2 = (z1 - z2) * xperp + (z1 - m00) * yperp;
                                    }
                                }
                                else
                                {
                                    if (gx >= -gy)
                                    {
                                        /* 101 */
                                        /* Left point */
                                        z1 = *(magptr - 1);
                                        z2 = *(magptr + ncols - 1);

                                        mag1 = (m00 - z1) * xperp + (z1 - z2) * yperp;

                                        /* Right point */
                                        z1 = *(magptr + 1);
                                        z2 = *(magptr - ncols + 1);

                                        mag2 = (m00 - z1) * xperp + (z1 - z2) * yperp;
                                    }
                                    else
                                    {
                                        /* 100 */
                                        /* Left point */
                                        z1 = *(magptr + ncols);
                                        z2 = *(magptr + ncols - 1);

                                        mag1 = (z1 - z2) * xperp + (m00 - z1) * yperp;

                                        /* Right point */
                                        z1 = *(magptr - ncols);
                                        z2 = *(magptr - ncols + 1);

                                        mag2 = (z1 - z2) * xperp + (m00 - z1) * yperp;
                                    }
                                }
                            }
                            else
                            {
                                if ((gy = *gyptr) >= 0)
                                {
                                    if (-gx >= gy)
                                    {
                                        /* 011 */
                                        /* Left point */
                                        z1 = *(magptr + 1);
                                        z2 = *(magptr - ncols + 1);

                                        mag1 = (z1 - m00) * xperp + (z2 - z1) * yperp;

                                        /* Right point */
                                        z1 = *(magptr - 1);
                                        z2 = *(magptr + ncols - 1);

                                        mag2 = (z1 - m00) * xperp + (z2 - z1) * yperp;
                                    }
                                    else
                                    {
                                        /* 010 */
                                        /* Left point */
                                        z1 = *(magptr - ncols);
                                        z2 = *(magptr - ncols + 1);

                                        mag1 = (z2 - z1) * xperp + (z1 - m00) * yperp;

                                        /* Right point */
                                        z1 = *(magptr + ncols);
                                        z2 = *(magptr + ncols - 1);

                                        mag2 = (z2 - z1) * xperp + (z1 - m00) * yperp;
                                    }
                                }
                                else
                                {
                                    if (-gx > -gy)
                                    {
                                        /* 001 */
                                        /* Left point */
                                        z1 = *(magptr + 1);
                                        z2 = *(magptr + ncols + 1);

                                        mag1 = (z1 - m00) * xperp + (z1 - z2) * yperp;

                                        /* Right point */
                                        z1 = *(magptr - 1);
                                        z2 = *(magptr - ncols - 1);

                                        mag2 = (z1 - m00) * xperp + (z1 - z2) * yperp;
                                    }
                                    else
                                    {
                                        /* 000 */
                                        /* Left point */
                                        z1 = *(magptr + ncols);
                                        z2 = *(magptr + ncols + 1);

                                        mag1 = (z2 - z1) * xperp + (m00 - z1) * yperp;

                                        /* Right point */
                                        z1 = *(magptr - ncols);
                                        z2 = *(magptr - ncols - 1);

                                        mag2 = (z2 - z1) * xperp + (m00 - z1) * yperp;
                                    }
                                }
                            }

                            /* Now determine if the current point is a maximum point */

                            if ((mag1 > 0.0) || (mag2 > 0.0))
                            {
                                *resultptr = NOEDGE;
                            }
                            else
                            {
                                if (mag2 == 0.0)
                                    *resultptr = NOEDGE;
                                else
                                    *resultptr = POSSIBLE_EDGE;
                            }
                        }
                    }
                }
            }
            ByteArrayHandler handler = new ByteArrayHandler(nrows, ncols,1);
            fixed (byte* dst = handler.RawArray)
            {
                byte* dstPtr = dst;
                int length = nrows * ncols;
                for (int pos = 0; pos < length; pos++)
                    *dstPtr++ = data[pos];
            }
            //MonochromeImage mono = new MonochromeImage(nrows, ncols);
            //for (int r = 0; r < nrows; r++)
            //    for (int c = 0; c < ncols; c++)
            //        mono[r, c] = handler[r, c, 0];
            //mono.ToRGB().Save("cannyNMS.bmp");
            return handler;
        }
    }
}
