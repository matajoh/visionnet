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
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra.Single;

namespace VisionNET
{
    /// <summary>
    /// Performs an Euclidean distance transform on an input binary image.
    /// </summary>
    public class DistanceTransformImage : IMultichannelImage<int>
    {
        private IntegerArrayHandler _handler;
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
        /// Constructor.
        /// </summary>
        public DistanceTransformImage()
        {
            _handler = new IntegerArrayHandler();
        }

        /// <summary>
        /// Returns a Bitmap version of this image using the computed minimum and maximum values.
        /// </summary>
        /// <returns>A Bitmap representing this image</returns>
        public unsafe BitmapSource ToBitmap()
        {
            return ToGrayscale().ToBitmap();
        }

        /// <summary>
        /// Converts the distance transform image to a grayscale representation.
        /// </summary>
        /// <returns>A grayscale representation of the distance transform</returns>
        public unsafe GrayscaleImage ToGrayscale()
        {
            GrayscaleImage gray = new GrayscaleImage(Rows, Columns);
            fixed (float* dst = gray.RawArray)
            {
                fixed (int* src = RawArray)
                {
                    int* srcPtr = src;
                    float* dstPtr = dst;
                    for (int length = Rows * Columns; length > 0; length--, dstPtr++, srcPtr += Channels)
                        *dstPtr = *srcPtr;
                }
            }
            return gray;
        }

        private const int INFINITY = int.MaxValue;

        private struct Curve
        {
            public int Row;
            public int End;
            public int B;
            public int Column;

            public override string ToString()
            {
                return string.Format("Start={0} End={1} B={2} Row={3} Column={4}", Row, End, B, Row, Column);
            }
        }

        private static int[] _squareLookup;

        private static void initLookup(int size)
        {
            _squareLookup = new int[size];
            int x_last = 0;
            for (int i = 1; i < size; i++)
            {
                int x = x_last + (i << 1) - 1;
                _squareLookup[i] = x;
                x_last = x;
            }
        }

        /// <summary>
        /// Finds the nearest edgels to list of locations.
        /// </summary>
        /// <param name="locations">The locations to test</param>
        /// <returns>The nearest edgels</returns>
        public Dictionary<Vector,Vector> FindNearestEdges(List<Vector> locations)
        {
            int[,,] data = RawArray;
            Dictionary<Vector, Vector> nearest = new Dictionary<Vector, Vector>();
            foreach (Vector loc in locations)
            {
                int r = (int)loc[1];
                int c = (int)loc[0];
                nearest[new DenseVector(new float[]{c,r})] = new DenseVector(new float[]{data[r, c, 2], data[r, c, 1]});
            }
            return nearest;
        }

        internal static unsafe DistanceTransformImage ComputeNaive(BinaryImage edges, bool computeSquareRoot)
        {
            int rows = edges.Rows;
            int columns = edges.Columns;
            List<ImageCell> cells = edges.GetCells();
            DistanceTransformImage dt = new DistanceTransformImage();
            dt.SetDimensions(edges.Rows, edges.Columns, 3);
            for(int r=0; r<rows; r++)
                for (int c = 0; c < columns; c++)
                {
                    var distances = from index in cells
                                    select new
                                    {
                                        Distance = distance(index.Row, index.Column, r, c),
                                        Index = index
                                    };
                    var nearest = distances.OrderBy(o => o.Distance).First();
                    if (computeSquareRoot)
                        dt[r, c, 0] = (int)Math.Sqrt(nearest.Distance);
                    else dt[r, c, 0] = nearest.Distance;
                    dt[r, c, 1] = nearest.Index.Row;
                    dt[r, c, 2] = nearest.Index.Column;
                }
            return dt;
        }

        /// <summary>
        /// Computes the Chamfer distance match between the provided template and this distance transform at the provided index.
        /// </summary>
        /// <param name="row">Row to compare at</param>
        /// <param name="column">Column to compare at</param>
        /// <param name="template">The template to compare</param>
        /// <param name="maxDistance">Maximum distance allowed</param>
        /// <returns>Chamfer match value</returns>
        public float ComputeChamferMatch(int row, int column, List<Vector> template, int maxDistance)
        {
            long sum = 0;
            int[,,] data = RawArray;
            int count = 0;
            int rows = Rows;
            int columns = Columns;
            foreach (var point in template)
            {
                int r = (int)(point[1] + row);
                int c = (int)(point[0] + column);
                if (r >= 0 && r < rows && c >= 0 && c < columns)
                {
                    int dist = data[r, c, 0];
                    dist = dist < maxDistance ? dist : maxDistance;
                    sum += dist;
                    count++;
                }
            }
            return (float)sum / (count*maxDistance);
        }

        /// <summary>
        /// Computes the Chamfer distance match between the provided template and this distance transform at the provided index.
        /// </summary>
        /// <param name="template">The template to compare</param>
        /// <param name="maxDistance">Maximum distance allowed</param>
        /// <returns>Chamfer match value</returns>
        public float ComputeChamferMatch(List<Vector> template, int maxDistance)
        {
            long sum = 0;
            int[, ,] data = RawArray;
            int count = 0;
            int rows = Rows;
            int columns = Columns;
            foreach (var point in template)
            {
                int r = (int)point[1];
                int c = (int)point[0];
                if (r >= 0 && r < rows && c >= 0 && c < columns)
                {
                    int dist = data[r, c, 0];
                    dist = dist < maxDistance ? dist : maxDistance;
                    sum += dist;
                    count++;
                }
            }
            return (float)sum / (count * maxDistance);
        }

        /// <summary>
        /// Computes the Chamfer distance match between the provided template and this distance transform at the provided index.
        /// </summary>
        /// <param name="row">Row to compare at</param>
        /// <param name="column">Column to compare at</param>
        /// <param name="template">The template to compare</param>
        /// <param name="maxDistance">Maximum distance allowed</param>
        /// <param name="scale">Scaling factor for the match</param>
        /// <returns>Chamfer match value</returns>
        public float ComputeChamferMatch(int row, int column, List<Vector> template, int maxDistance, Vector scale)
        {
            long sum = 0;
            int[, ,] data = RawArray;
            int count = 0;
            int rows = Rows;
            int columns = Columns;
            float scaleX = scale[0];
            float scaleY = scale[1];
            foreach (var point in template)
            {
                int r = (int)(point[1]*scaleY + row);
                int c = (int)(point[0]*scaleX + column);
                if (r >= 0 && r < rows && c >= 0 && c < columns)
                {
                    int dist = data[r, c, 0];
                    dist = dist < maxDistance ? dist : maxDistance;
                    sum += dist;
                    count++;
                }
            }
            return (float)sum / (count*maxDistance);
        }

        /// <summary>
        /// Computes the match between the template orientations and the orientations of the nearest edges in the image at the provided offset index.
        /// The Z value of each feature in the template should be the orientation of the template edge at that template index, and all orientations should
        /// be in the range from 0 to Pi.
        /// </summary>
        /// <param name="row">The row to compare at</param>
        /// <param name="column">The column to compare at</param>
        /// <param name="template">The template to compare</param>
        /// <param name="orientations">Orientations from 0 to Pi for each index in the image</param>
        /// <returns>Orientation match value</returns>
        public float ComputeOrientationMatch(int row, int column, List<Vector> template, float[,] orientations)
        {
            int rows = Rows;
            int columns = Columns;
            if (orientations.GetLength(0) != rows || orientations.GetLength(1) != columns)
                throw new ArgumentException("orientations must have the same dimensionality as the image");
            double halfPI = Math.PI / 2;
            double sum = 0;
            int count = 0;
            int[,,] data = RawArray;
            foreach (Vector point in template)
            {
                int r = (int)(point[1] + row);
                int c = (int)(point[0] + column);
                if (r >= 0 && r < rows && c >= 0 && c < columns)
                {
                    int rr = data[r, c, 1];
                    int cc = data[r, c, 2];
                    double diff = Math.Abs(orientations[rr, cc] - point[2]);
                    if (diff > Math.PI)
                        diff -= Math.PI;
                    if (diff > halfPI)
                        diff = Math.PI - diff;
                    sum += diff;
                    count++;
                }
            }
            return (float)(sum / (halfPI*count));
        }

        /// <summary>
        /// Computes the match between the template orientations and the orientations of the nearest edges in the image at the provided offset index.
        /// The Z value of each feature in the template should be the orientation of the template edge at that template index, and all orientations should
        /// be in the range from 0 to Pi.
        /// </summary>
        /// <param name="template">The template to compare</param>
        /// <param name="orientations">Orientations from 0 to Pi for each index in the image</param>
        /// <returns>Orientation match value</returns>
        public float ComputeOrientationMatch(List<Vector> template, float[,] orientations)
        {
            int rows = Rows;
            int columns = Columns;
            if (orientations.GetLength(0) != rows || orientations.GetLength(1) != columns)
                throw new ArgumentException("orientations must have the same dimensionality as the image");
            double halfPI = Math.PI / 2;
            double sum = 0;
            int count = 0;
            int[, ,] data = RawArray;
            foreach (Vector point in template)
            {
                int r = (int)point[1];
                int c = (int)point[0];
                if (r >= 0 && r < rows && c >= 0 && c < columns)
                {
                    int rr = data[r, c, 1];
                    int cc = data[r, c, 2];
                    double diff = Math.Abs(orientations[rr, cc] - point[2]);
                    if (diff > Math.PI)
                        diff -= Math.PI;
                    if (diff > halfPI)
                        diff = Math.PI - diff;
                    sum += diff;
                    count++;
                }
            }
            return (float)(sum / (halfPI * count));
        }

        /// <summary>
        /// Computes the match between the template orientations and the orientations of the nearest edges in the image at the provided offset index.
        /// The Z value of each feature in the template should be the orientation of the template edge at that template index, and all orientations should
        /// be in the range from 0 to Pi.
        /// </summary>
        /// <param name="row">The row to compare at</param>
        /// <param name="column">The column to compare at</param>
        /// <param name="template">The template to compare</param>
        /// <param name="orientations">Orientations from 0 to Pi for each index in the image</param>
        /// <param name="scale">Scaling factor for the match</param>
        /// <returns>Orientation match value</returns>
        public float ComputeOrientationMatch(int row, int column, List<Vector> template, float[,] orientations, Vector scale)
        {
            int[, ,] data = RawArray;
            int rows = Rows;
            int columns = Columns;
            if (orientations.GetLength(0) != rows || orientations.GetLength(1) != columns)
                throw new ArgumentException("orientations must have the same dimensionality as the image");
            double halfPI = Math.PI / 2;
            double sum = 0;
            int count = 0;
            float scaleX = scale[0];
            float scaleY = scale[1];
            foreach (Vector point in template)
            {
                int r = (int)(point[1]*scaleY + row);
                int c = (int)(point[0]*scaleX + column);
                if (r >= 0 && r < rows && c >= 0 && c < columns)
                {
                    int rr = data[r, c, 1];
                    int cc = data[r, c, 2];
                    double diff = Math.Abs(orientations[rr, cc] - point[2]);
                    if (diff > Math.PI)
                        diff -= Math.PI;
                    if (diff > halfPI)
                        diff = Math.PI - diff;
                    sum += diff;
                    count++;
                }
            }
            return (float)(sum / (halfPI*count));
        }

        private static int distance(int row1, int column1, int row2, int column2)
        {
            int dr = row1 - row2;
            int dc = column1 - column2;
            return dr * dr + dc * dc;
        }

        /// <summary>
        /// Computes the distance transform using an efficient Euclidean distance transform.  If <paramref name="computeSquareRoot"/>
        /// is not set, the squared distance will be stored.
        /// </summary>
        /// <param name="edges">Edge image</param>
        /// <param name="computeSquareRoot">Whether to compute the actual distance from the squared distance</param>
        /// <returns>Distance transform</returns>
        public static unsafe DistanceTransformImage Compute(BinaryImage edges, bool computeSquareRoot)
        {
            int i, j, k;
            int r, c;
            int I_1, I_2, r_1, r_2, dr, intersection;
            int I2new, I2val, I2column, dtVal;
            int rows = edges.Rows;
            int columns = edges.Columns;
            int channels = 3;
            int stride = columns * channels;
            Curve[] curves = new Curve[256];
            Curve* bottomPtr, curve0Ptr, curve1Ptr;
            int curvesCount = 0;

            int[, ,] I2 = new int[rows, columns, channels];
            DistanceTransformImage dt = new DistanceTransformImage();
            dt.SetDimensions(rows, columns, channels);

            initLookup(Math.Max(rows, columns));

            fixed (bool* edgesBuf = edges.RawArray)
            {
                fixed (int* I2Buf = I2, squareBuf = _squareLookup, dtBuf = dt.RawArray)
                {
                    fixed (Curve* curvesBuf = curves)
                    {
                        bool* edgesPtr = edgesBuf;
                        int* I2Ptr = I2Buf;
                        int* squarePtr = squareBuf;
                        int lastEdge, width;
                        bool* edgesScan;
                        int* I2RevPtr, I2LagPtr, dtPtr, dtScan, I2Scan;

                        for (r = 0, i = rows; i != 0; i--, r++)
                        {
                            lastEdge = INFINITY;

                            if (*edgesPtr)
                            {
                                I2Ptr[0] = 0;
                                I2Ptr[1] = r;
                                I2Ptr[2] = 0;
                                lastEdge = 0;
                                squarePtr = squareBuf;
                            }
                            else *I2Ptr = INFINITY;

                            I2LagPtr = I2Ptr;
                            edgesPtr++;
                            I2Ptr += channels;

                            for (j = columns - 1, c = 1; j != 0; j--, c++, edgesPtr++, I2LagPtr += channels, I2Ptr += channels)
                            {
                                if (*edgesPtr)
                                {
                                    if (lastEdge == INFINITY)
                                    {
                                        for (k = c + 1, I2RevPtr = I2Ptr, squarePtr = squareBuf; k != 0; k--, I2RevPtr -= channels, squarePtr++)
                                        {
                                            I2RevPtr[0] = *squarePtr;
                                            I2RevPtr[1] = r;
                                            I2RevPtr[2] = c;
                                        }
                                    }
                                    else
                                    {
                                        width = c - lastEdge;
                                        width = width >> 1;
                                        for (k = width + 1, I2RevPtr = I2Ptr, squarePtr = squareBuf; k != 0; k--, I2RevPtr -= channels, squarePtr++)
                                        {
                                            I2RevPtr[0] = *squarePtr;
                                            I2RevPtr[1] = r;
                                            I2RevPtr[2] = c;
                                        }
                                    }
                                    lastEdge = c;
                                    squarePtr = squareBuf;
                                }
                                else
                                {
                                    if (*I2LagPtr == INFINITY)
                                        *I2Ptr = INFINITY;
                                    else
                                    {
                                        I2Ptr[0] = *(++squarePtr);
                                        I2Ptr[1] = r;
                                        I2Ptr[2] = lastEdge;
                                    }
                                }
                            }
                        }


                        for (
                            edgesPtr = edgesBuf,
                            dtPtr = dtBuf,
                            I2Ptr = I2Buf,
                            c = 0,
                            i = columns;

                            i != 0;

                            I2Ptr += channels,
                            edgesPtr++,
                            dtPtr += channels,
                            c++,
                            i--)
                        {
                            curvesCount = 0;
                            bottomPtr = null;
                            curve0Ptr = null;
                            curve1Ptr = null;

                            for (
                                I2Scan = I2Ptr,
                                edgesScan = edgesPtr,
                                dtScan = dtPtr,
                                j = rows,
                                r = 0;

                                j != 0;

                                I2Scan += stride,
                                edgesScan += columns,
                                dtScan += stride,
                                r++,
                                j--)
                            {
                                if (*edgesScan)
                                {
                                    curvesCount = 1;
                                    bottomPtr = curvesBuf;
                                    curve1Ptr = curvesBuf;
                                    curve0Ptr = null;
                                    bottomPtr->Row = r;
                                    bottomPtr->End = rows;
                                    bottomPtr->B = 0;
                                    bottomPtr->Column = c;

                                    dtScan[1] = r;
                                    dtScan[2] = c;

                                    continue;
                                }

                                I2new = INFINITY;
                                I2val = I2Scan[0];
                                I2column = I2Scan[2];
                                if (I2val != INFINITY)
                                {
                                    if (curvesCount > 0)
                                    {
                                        dr = r - bottomPtr->Row;
                                        I2new = _squareLookup[dr] + bottomPtr->B;
                                        if (I2val < I2new)
                                        {
                                            curvesCount = 1;
                                            bottomPtr = curvesBuf;
                                            curve1Ptr = curvesBuf;
                                            curve0Ptr = null;
                                            bottomPtr->Row = r;
                                            bottomPtr->End = rows;
                                            bottomPtr->B = I2val;
                                            bottomPtr->Column = I2column;

                                            dtScan[0] = I2val;
                                            dtScan[1] = r;
                                            dtScan[2] = I2column;
                                            Debug.Assert(dtScan[0] == distance(r, c, dtScan[1], dtScan[2]));

                                            continue;
                                        }
                                    }

                                    for (; ; )
                                    {
                                        if (curvesCount == 0)
                                        {
                                            curvesCount = 1;
                                            bottomPtr = curvesBuf;
                                            curve1Ptr = curvesBuf;
                                            curve0Ptr = null;
                                            bottomPtr->Row = r;
                                            bottomPtr->End = rows;
                                            bottomPtr->B = I2val;
                                            bottomPtr->Column = I2column;

                                            break;
                                        }


                                        if (I2val < curve1Ptr->B)
                                        {
                                            curvesCount--;
                                            if (curvesCount > 0)
                                            {
                                                curve1Ptr--;
                                                if (curvesCount > 1)
                                                    curve0Ptr--;
                                                else curve0Ptr = null;
                                            }
                                            else
                                            {
                                                curve1Ptr = null;
                                                bottomPtr = null;
                                                curve0Ptr = null;
                                            }

                                            continue;
                                        }

                                        I_1 = curve1Ptr->B;
                                        I_2 = I2val;
                                        r_1 = curve1Ptr->Row;
                                        r_2 = r;
                                        dr = r_2 - r_1;
                                        intersection = r_2 + ((I_2 - I_1 - _squareLookup[dr]) / (dr << 1));

                                        if (intersection >= rows)
                                        {
                                            *I2Scan = INFINITY;
                                            break;
                                        }

                                        if (curve0Ptr == null || intersection > curve0Ptr->End)
                                        {
                                            curvesCount++;
                                            curve1Ptr->End = intersection;
                                            curve0Ptr = curve1Ptr;
                                            curve1Ptr++;
                                            curve1Ptr->Row = r;
                                            curve1Ptr->End = rows;
                                            curve1Ptr->B = I_2;
                                            curve1Ptr->Column = I2column;

                                            break;
                                        }

                                        curvesCount--;
                                        if (curvesCount > 0)
                                        {
                                            curve1Ptr--;
                                            if (curvesCount > 1)
                                                curve0Ptr--;
                                            else curve0Ptr = null;
                                        }
                                        else
                                        {
                                            curve1Ptr = null;
                                            bottomPtr = null;
                                            curve0Ptr = null;
                                        }
                                    }
                                }

                                if (curvesCount == 0)
                                    continue;
                                if (I2new == INFINITY)
                                {
                                    dr = r - bottomPtr->Row;
                                    I2new = _squareLookup[dr] + bottomPtr->B;
                                }
                                dtScan[0] = I2new;
                                dtScan[1] = bottomPtr->Row;
                                dtScan[2] = bottomPtr->Column;
                                Debug.Assert(dtScan[0] == distance(r, c, dtScan[1], dtScan[2]));
                                if (I2new < I2val)
                                    *I2Scan = INFINITY;

                                if (bottomPtr->End == r)
                                {
                                    curvesCount--;
                                    bottomPtr++;
                                }

                            }

                            //continue;

                            curvesCount = 0;
                            bottomPtr = null;
                            curve0Ptr = null;
                            curve1Ptr = null;
                            I2Scan -= stride;
                            dtScan -= stride;
                            edgesScan -= columns;

                            for (
                                j = rows,
                                r = rows - 1;

                                j != 0;

                                I2Scan -= stride,
                                dtScan -= stride,
                                edgesScan -= columns,
                                r--,
                                j--)
                            {
                                if (*edgesScan)
                                {
                                    curvesCount = 1;
                                    bottomPtr = curvesBuf;
                                    curve1Ptr = curvesBuf;
                                    curve0Ptr = null;
                                    bottomPtr->Row = r;
                                    bottomPtr->End = -1;
                                    bottomPtr->B = 0;
                                    bottomPtr->Column = c;

                                    continue;
                                }

                                I2new = INFINITY;
                                I2val = I2Scan[0];
                                I2column = I2Scan[2];
                                dtVal = *dtScan;
                                if (I2val != INFINITY)
                                {
                                    if (curvesCount > 0)
                                    {
                                        dr = bottomPtr->Row - r;
                                        I2new = _squareLookup[dr] + bottomPtr->B;
                                        if (dtVal < I2new && dtVal < I2val)
                                        {
                                            curvesCount = 0;
                                            curve1Ptr = null;
                                            bottomPtr = null;
                                            curve0Ptr = null;

                                            continue;
                                        }
                                        if (I2val < I2new)
                                        {
                                            curvesCount = 1;
                                            bottomPtr = curvesBuf;
                                            curve1Ptr = curvesBuf;
                                            curve0Ptr = null;
                                            bottomPtr->Row = r;
                                            bottomPtr->End = -1;
                                            bottomPtr->B = I2val;
                                            bottomPtr->Column = I2column;

                                            dtScan[0] = I2val;
                                            dtScan[1] = r;
                                            dtScan[2] = I2column;
                                            Debug.Assert(dtScan[0] == distance(r, c, dtScan[1], dtScan[2]));
                                            continue;
                                        }
                                    }

                                    for (; ; )
                                    {
                                        if (curvesCount == 0)
                                        {
                                            curvesCount = 1;
                                            bottomPtr = curvesBuf;
                                            curve1Ptr = curvesBuf;
                                            curve0Ptr = null;
                                            bottomPtr->Row = r;
                                            bottomPtr->End = -1;
                                            bottomPtr->B = I2val;
                                            bottomPtr->Column = I2column;

                                            break;
                                        }

                                        if (I2val < curve1Ptr->B)
                                        {
                                            curvesCount--;
                                            if (curvesCount > 0)
                                            {
                                                curve1Ptr--;
                                                if (curvesCount > 1)
                                                    curve0Ptr--;
                                                else curve0Ptr = null;
                                            }
                                            else
                                            {
                                                curve1Ptr = null;
                                                bottomPtr = null;
                                                curve0Ptr = null;
                                            }

                                            continue;
                                        }

                                        I_1 = curve1Ptr->B;
                                        I_2 = I2val;
                                        r_1 = curve1Ptr->Row;
                                        r_2 = r;
                                        dr = r_1 - r_2;
                                        intersection = r_2 - ((I_2 - I_1 - _squareLookup[dr]) / (dr << 1));

                                        if (intersection < 0)
                                        {
                                            break;
                                        }

                                        if (curve0Ptr == null || intersection < curve0Ptr->End)
                                        {
                                            curvesCount++;
                                            curve1Ptr->End = intersection;
                                            curve0Ptr = curve1Ptr;
                                            curve1Ptr++;
                                            curve1Ptr->Row = r;
                                            curve1Ptr->End = -1;
                                            curve1Ptr->B = I_2;
                                            curve1Ptr->Column = I2column;

                                            break;
                                        }

                                        curvesCount--;
                                        if (curvesCount > 0)
                                        {
                                            curve1Ptr--;
                                            if (curvesCount > 1)
                                                curve0Ptr--;
                                            else curve0Ptr = null;
                                        }
                                        else
                                        {
                                            curve1Ptr = null;
                                            bottomPtr = null;
                                            curve0Ptr = null;
                                        }
                                    }
                                }

                                if (curvesCount == 0)
                                    continue;
                                if (I2new == INFINITY)
                                {
                                    dr = bottomPtr->Row - r;
                                    I2new = _squareLookup[dr] + bottomPtr->B;
                                }
                                if ((I2val == INFINITY && dtVal == 0) || I2new < dtVal)
                                {
                                    dtScan[0] = I2new;
                                    dtScan[1] = bottomPtr->Row;
                                    dtScan[2] = bottomPtr->Column;
                                    Debug.Assert(dtScan[0] == distance(r, c, dtScan[1], dtScan[2]));
                                }

                                if (bottomPtr->End == r)
                                {
                                    curvesCount--;
                                    bottomPtr++;
                                }

                            }
                        }
                        if (computeSquareRoot)
                        {
                            dtPtr = dtBuf;
                            for (r = rows; r != 0; r--)
                                for (c = columns; c != 0; c--, dtPtr += channels)
                                {
                                    *dtPtr = (int)Math.Sqrt(*dtPtr);
                                }
                        }
                    }            
                }
            }

            return dt;
        }

        /// <summary>
        /// Width of the image (equivalent to <see cref="P:Columns" />)
        /// </summary>
        public int Width
        {
            get { return _handler.Columns; }
        }

        /// <summary>
        /// Height of the image (equivalment to <see cref="P:Rows" />)
        /// </summary>
        public int Height
        {
            get { return _handler.Rows; }
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
        public int ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
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
        public int ComputeRectangleSum(int row, int column, int channel, Rectangle rect)
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
        public void SetData(int[, ,] data)
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
        public int[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
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
        public int this[int row, int column, int channel]
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
        public int[,] ExtractChannel(int channel)
        {
            return _handler.ExtractChannel(channel);
        }

        /// <summary>
        /// The underlying array.  Breaks capsulation to allow operations using pointer arithmetic.
        /// </summary>
        public int[, ,] RawArray
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
