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

namespace VisionNET
{
    /// <summary>
    /// Class for computing the Discrete Contour descriptor.
    /// </summary>
    public static class DiscreteContours
    {
        private const int SQUARE_SIZE = 28;
        private const int BINS = 24;

        private static readonly int[,] MASK =
           {{24,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1, 27},
            { 2, 24,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1, 27,  7},
            { 2,  2, 24,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1, 27,  7,  7},
            { 2,  2,  2, 24,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1, 27,  7,  7,  7},
            { 2,  2,  2,  2, 24,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,  1, 27,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2, 24,  0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1, 27,  7,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2,  2, 24,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1, 27,  7,  7,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2,  2,  2, 24,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1, 27,  7,  7,  7,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2,  2,  2,  2, 25,  4,  4,  4,  4,  4,  5,  5,  5,  5,  5, 28,  7,  7,  7,  7,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2,  2,  2,  2,  3, 25,  4,  4,  4,  4,  5,  5,  5,  5, 28,  6,  7,  7,  7,  7,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2,  2,  2,  2,  3,  3, 25,  4,  4,  4,  5,  5,  5, 28,  6,  6,  7,  7,  7,  7,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2,  2,  2,  2,  3,  3,  3, 25,  4,  4,  5,  5, 28,  6,  6,  6,  7,  7,  7,  7,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2,  2,  2,  2,  3,  3,  3,  3, 26,  9, 10, 29,  6,  6,  6,  6,  7,  7,  7,  7,  7,  7,  7,  7},
            { 2,  2,  2,  2,  2,  2,  2,  2,  3,  3,  3,  3,  8, 26, 29, 11,  6,  6,  6,  6,  7,  7,  7,  7,  7,  7,  7,  7},
            {12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13, 14, 32, 35, 15, 16, 16, 16, 16, 17, 17, 17, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 13, 32, 18, 19, 35, 16, 16, 16, 16, 17, 17, 17, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13, 31, 20, 20, 21, 21, 34, 16, 16, 16, 17, 17, 17, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 31, 20, 20, 20, 21, 21, 21, 34, 16, 16, 17, 17, 17, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 12, 12, 12, 12, 13, 31, 20, 20, 20, 20, 21, 21, 21, 21, 34, 16, 17, 17, 17, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 12, 12, 12, 12, 31, 20, 20, 20, 20, 20, 21, 21, 21, 21, 21, 34, 17, 17, 17, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 12, 12, 12, 30, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 33, 17, 17, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 12, 12, 30, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 33, 17, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 12, 30, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23, 33, 17, 17, 17, 17, 17},
            {12, 12, 12, 12, 30, 22, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23, 23, 33, 17, 17, 17, 17},
            {12, 12, 12, 30, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 33, 17, 17, 17},
            {12, 12, 30, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 33, 17, 17},
            {12, 30, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 33, 17},
            {30, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 22, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 23, 33}};
        private static readonly int[,] HALF =
           {{0, 2},
            {3, 4},
            {8, 9},
            {1, 7},
            {5, 6},
            {10,11},
            {12,22},
            {13,20},
            {14,18},
            {17,23},
            {16,21},
            {15,19}};
        private static readonly int[] ORIENTATION_BINS = { 8, 8, 8, 4, 4, 4, 4, 8, 1, 1, 1, 1, 8, 4, 1, 1, 4, 8, 1, 1, 4, 4, 8, 8 };

        private static int[] BIN_START;
        private static double[] ORIENTATION_BIN_SIZE;
        private static double[][] ORIENTATION_BIN_START;
        private static double[][] ORIENTATION_BIN_CENTER;

        /// <summary>
        /// Length of the descriptor.
        /// </summary>
        public static int DESCRIPTOR_LENGTH;

        static DiscreteContours()
        {
            BIN_START = new int[BINS];
            ORIENTATION_BIN_SIZE = new double[BINS];
            ORIENTATION_BIN_START = new double[BINS][];
            ORIENTATION_BIN_CENTER = new double[BINS][];
            DESCRIPTOR_LENGTH = 0;
            for (int i = 0; i < BINS; i++)
            {
                BIN_START[i] = DESCRIPTOR_LENGTH;
                DESCRIPTOR_LENGTH += ORIENTATION_BINS[i];
                ORIENTATION_BIN_SIZE[i] = 2 * Math.PI / ORIENTATION_BINS[i];
                ORIENTATION_BIN_START[i] = new double[ORIENTATION_BINS[i]];
                ORIENTATION_BIN_CENTER[i] = new double[ORIENTATION_BINS[i]];
                double pos = 0;
                for (int j = 0; j < ORIENTATION_BINS[i]; j++)
                {
                    ORIENTATION_BIN_START[i][j] = pos;
                    ORIENTATION_BIN_CENTER[i][j] = pos + ORIENTATION_BIN_SIZE[i] / 2;
                    pos += ORIENTATION_BIN_SIZE[i];
                }
            }
        }

        /// <summary>
        /// Required patch size for this descriptor.
        /// </summary>
        public static readonly Rectangle RequiredPatchSize = new Rectangle { Rows = SQUARE_SIZE, Columns = SQUARE_SIZE };

        /// <summary>
        /// Computes DiscreteContours at the samples provided.
        /// </summary>
        /// <param name="samples">Samples to compute contours at</param>
        /// <param name="pyramid">Image pyramid to use for samples</param>
        /// <returns>DiscreteContours</returns>
        public static List<Keypoint> Compute(List<ScaleSpaceSample> samples, ScaleSpacePyramid<GrayscaleImage> pyramid)
        {
            GradientImage[,] grads = new GradientImage[pyramid.Octaves, pyramid.Levels];

            List<Keypoint> points = new List<Keypoint>();
            foreach (ScaleSpaceSample sample in samples)
            {
                if (grads[sample.Octave, sample.Level] == null)
                    grads[sample.Octave, sample.Level] = GradientImage.Compute(pyramid[sample.Octave, sample.Level], false);

                points.Add(Compute(sample, grads[sample.Octave, sample.Level], pyramid.ComputeSigma(sample.Octave, sample.Level)));
            }

            return points;
        }

        /// <summary>
        /// Compute the Discrete Contour descriptor from the provided patch.  This match must be of gradients and
        /// of the size specified by <see cref="RequiredPatchSize"/>.  A raw image patch, or a patch of different
        /// dimensions, will not work.
        /// </summary>
        /// <param name="sample">Sample to compute DiscreteContour at</param>
        /// <param name="gradImage">Gradient image to use</param>
        /// <param name="scale">The scale of the sample (the value returned by ComputeSigma() in ScaleSpacePyramid)</param>
        /// <returns>Discrete Contour descriptor</returns>
        public static unsafe Keypoint Compute(ScaleSpaceSample sample, GradientImage gradImage, float scale)
        {
            Keypoint point = new Keypoint(sample.X, sample.Y, sample.ImageScale, scale, 0);
            float[] desc = new float[DESCRIPTOR_LENGTH];

            int startR = sample.Row - SQUARE_SIZE/2;
            int startC = sample.Column - SQUARE_SIZE/2;
            int rows = gradImage.Rows;
            int columns = gradImage.Columns;
            int channels = gradImage.Channels;
            int stride = columns * channels;

            fixed (int* mask = MASK)
            {
                fixed (float* patch = gradImage.RawArray)
                {
                    int* maskPtr = mask;

                    float* patchPtr;
                    if (startR < 0)
                    {
                        if (startC < 0)
                            patchPtr = patch;
                        else patchPtr = patch + startC * channels;
                    }
                    else if (startC < 0)
                        patchPtr = patch + startR * stride;
                    else patchPtr = patch + startR * stride + startC * channels;
                    for (int r = 0; r < SQUARE_SIZE; r++)
                    {
                        int rr = startR + r;
                        float* patchScan = patchPtr;
                        if (rr >= 0 && rr < rows - 1)
                            patchPtr += stride;
                        for (int c = 0; c < SQUARE_SIZE; c++, maskPtr++)
                        {
                            int bin = *maskPtr;//MASK[rr, cc];

                            float mag = patchScan[0];//gradientPatch[rr, cc, 0];
                            float dir = patchScan[1];//gradientPatch[rr, cc, 1];
                            int cc = startC + c;
                            if (cc >= 0 && cc < columns - 1)
                                patchScan += channels;
                            //if (mag != gradImage[Math.Max(0, Math.Min(rr, rows - 1)), Math.Max(0, Math.Min(cc, columns - 1)), 0])
                            //    throw new Exception("testing");


                            if (bin < BINS)
                            {
                                addValue(bin, dir, mag * 2, desc);
                            }
                            else
                            {
                                bin -= BINS;
                                int bin1 = HALF[bin, 0];
                                int bin2 = HALF[bin, 1];
                                addValue(bin1, dir, mag, desc);
                                addValue(bin2, dir, mag, desc);
                            }
                        }
                    }
                }
            }
            point.Descriptor = desc;
            return point;
        }

        private static void addValue(int bin, float dir, float value, float[] histogram)
        {
            int orientationBins = ORIENTATION_BINS[bin];
            int binStart = BIN_START[bin];

            if (orientationBins == 1)
            {
                histogram[binStart] = value;
                return;
            }
            double obinSize = ORIENTATION_BIN_SIZE[bin];
            int orientationBin = (int)(dir / obinSize);
            if (orientationBin == orientationBins)
                orientationBin = 0;
            double diff = (dir - ORIENTATION_BIN_CENTER[bin][orientationBin]) / obinSize;
            int ob1 = orientationBin;
            int ob2;
            double percent1, percent2;
            if (diff < 0)
            {
                ob2 = ob1 - 1;
                if (ob2 < 0)
                    ob2 = orientationBins - 1;
                percent1 = 1 + diff;
                percent2 = Math.Abs(diff);
            }
            else
            {
                ob2 = ob1 + 1;
                if (ob2 >= orientationBins)
                    ob2 = 0;
                percent1 = 1 - diff;
                percent2 = diff;
            }
            int bin1 = binStart + ob1;
            int bin2 = binStart + ob2;

            histogram[bin1] += (float)(value * percent1);
            histogram[bin2] += (float)(value * percent2);
        }
    }
}
