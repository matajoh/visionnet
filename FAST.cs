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
using MathNet.Numerics.LinearAlgebra.Single;

namespace VisionNET
{
    /// <summary>
    /// Class which implements the FAST interest point detector.
    /// </summary>
    public static class FAST
    {
        private struct xy
        {
            public xy(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
            public int x;
            public int y;
        }

        /// <summary>
        /// Extracts 2D interest points from the provided image using the FAST algorithm.
        /// </summary>
        /// <param name="image">The image to process</param>
        /// <param name="threshold">How much of a difference is required between the "outside" of the corner and the "inside" of the corner</param>
        /// <param name="segmentLength">The arc length of the corner</param>
        /// <param name="nonMaximumSuppression">Whether to perform non-maximum suppression on the results</param>
        /// <returns>A list of 2D interest points detected in this image</returns>
        public static unsafe Vector[] Extract(MonochromeImage image, int threshold, int segmentLength, bool nonMaximumSuppression)
        {
            xy[] result = null;
            fixed (int* im = image.RawArray)
            {
                switch (segmentLength)
                {
                    case 9:
                        result = fast_corner_detect_9(im, image.Columns, image.Rows, threshold);
                        break;

                    case 10:
                        result = fast_corner_detect_10(im, image.Columns, image.Rows, threshold);
                        break;

                    case 11:
                        result = fast_corner_detect_11(im, image.Columns, image.Rows, threshold);
                        break;

                    case 12:
                        result = fast_corner_detect_12(im, image.Columns, image.Rows, threshold);
                        break;

                    default:
                        throw new ArgumentException("Segment Length must be between 9 and 12");
                }
                if(nonMaximumSuppression)
                    result = fast_nonmax(im, image.Columns, image.Rows, result, threshold);
            }
            return (from corner in result
                    select new DenseVector(new float[]{corner.x, corner.y})).ToArray();
        }

        private static unsafe int corner_score(int* imp, int[] pointer_dir, int barrier)
        {
            /*The score for a positive feature is sum of the difference between the pixels
              and the barrier if the difference is positive. Negative is similar.
              The score is the max of those two.
	  
               B = {x | x = points on the Bresenham circle around c}
               Sp = { I(x) - t | x E B , I(x) - t > 0 }
               Sn = { t - I(x) | x E B, t - I(x) > 0}
	  
               Score = max sum(Sp), sum(Sn)*/

            int cb = *imp + barrier;
            int c_b = *imp - barrier;
            int sp = 0, sn = 0;

            int i = 0;

            for (i = 0; i < 16; i++)
            {
                int p = imp[pointer_dir[i]];

                if (p > cb)
                    sp += p - cb;
                else if (p < c_b)
                    sn += c_b - p;
            }

            if (sp > sn)
                return sp;
            else
                return sn;
        }

        //private static unsafe xy[] xfast_nonmax(byte* im, int xsize, int ysize, xy[] corners, int barrier)
        //{

        //    /*Create a list of integer pointer offstes, corresponding to the */
        //    /*direction offsets in dir[]*/
        //    int[] pointer_dir = new int[16];
        //    int[] row_start = new int[ysize];
        //    int[] scores = new int[corners.Length];
        //    int numcorners = corners.Length;
        //    List<xy> nonmax_corners = new List<xy>();
        //    int prev_row = -1;
        //    int i, j;
        //    int point_above = 0;
        //    int point_below = 0;

        //    pointer_dir[0] = 0 + 3 * xsize;
        //    pointer_dir[1] = 1 + 3 * xsize;
        //    pointer_dir[2] = 2 + 2 * xsize;
        //    pointer_dir[3] = 3 + 1 * xsize;
        //    pointer_dir[4] = 3 + 0 * xsize;
        //    pointer_dir[5] = 3 + -1 * xsize;
        //    pointer_dir[6] = 2 + -2 * xsize;
        //    pointer_dir[7] = 1 + -3 * xsize;
        //    pointer_dir[8] = 0 + -3 * xsize;
        //    pointer_dir[9] = -1 + -3 * xsize;
        //    pointer_dir[10] = -2 + -2 * xsize;
        //    pointer_dir[11] = -3 + -1 * xsize;
        //    pointer_dir[12] = -3 + 0 * xsize;
        //    pointer_dir[13] = -3 + 1 * xsize;
        //    pointer_dir[14] = -2 + 2 * xsize;
        //    pointer_dir[15] = -1 + 3 * xsize;

        //    if (corners.Length < 5)
        //    {
        //        return null;
        //    }

        //    /*Compute the score for each detected corner, and find where each row begins*/
        //    /* (the corners are output in raster scan order). A beginning of -1 signifies*/
        //    /* that there are no corners on that row.*/


        //    for (i = 0; i < ysize; i++)
        //        row_start[i] = -1;


        //    for (i = 0; i < numcorners; i++)
        //    {
        //        if (corners[i].y != prev_row)
        //        {
        //            row_start[corners[i].y] = i;
        //            prev_row = corners[i].y;
        //        }

        //        scores[i] = corner_score(im + corners[i].x + corners[i].y * xsize, pointer_dir, barrier);
        //    }


        //    /*Point above points (roughly) to the pixel above the one of interest, if there*/
        //    /*is a feature there.*/


        //    for (i = 1; i < numcorners - 1; i++)
        //    {
        //        int score = scores[i];
        //        xy pos = corners[i];

        //        /*Check left*/
        //        /*if(corners[i-1] == pos-ImageRef(1,0) && scores[i-1] > score)*/
        //        if (corners[i - 1].x == pos.x - 1 && corners[i - 1].y == pos.y && scores[i - 1] > score)
        //            continue;

        //        /*Check right*/
        //        /*if(corners[i+1] == pos+ImageRef(1,0) && scores[i+1] > score)*/
        //        if (corners[i + 1].x == pos.x + 1 && corners[i + 1].y == pos.y && scores[i - 1] > score)
        //            continue;

        //        /*Check above*/
        //        if (pos.y != 0 && row_start[pos.y - 1] != -1)
        //        {
        //            if (corners[point_above].y < pos.y - 1)
        //                point_above = row_start[pos.y - 1];

        //            /*Make point above point to the first of the pixels above the current point,*/
        //            /*if it exists.*/
        //            for (; corners[point_above].y < pos.y && corners[point_above].x < pos.x - 1; point_above++) ;


        //            for (j = point_above; corners[j].y < pos.y && corners[j].x <= pos.x + 1; j++)
        //            {
        //                int x = corners[j].x;
        //                if ((x == pos.x - 1 || x == pos.x || x == pos.x + 1) && scores[j] > score)
        //                {
        //                    goto cont;
        //                }
        //            }

        //        }

        //        /*Check below*/
        //        if (pos.y != ysize - 1 && row_start[pos.y + 1] != -1) /*Nothing below*/
        //        {
        //            if (corners[point_below].y < pos.y + 1)
        //                point_below = row_start[pos.y + 1];

        //            /* Make point below point to one of the pixels belowthe current point, if it*/
        //            /* exists.*/
        //            for (; corners[point_below].y == pos.y + 1 && corners[point_below].x < pos.x - 1; point_below++) ;

        //            for (j = point_below; corners[j].y == pos.y + 1 && corners[j].x <= pos.x + 1; j++)
        //            {
        //                int x = corners[j].x;
        //                if ((x == pos.x - 1 || x == pos.x || x == pos.x + 1) && scores[j] > score)
        //                {
        //                    goto cont;
        //                }
        //            }
        //        }


        //        nonmax_corners.Add(corners[i]);

        //    cont:
        //        ;
        //    }

        //    return nonmax_corners.ToArray();
        //}

        /*void fast_nonmax(const BasicImage<byte>& im, const vector<ImageRef>& corners, int barrier, vector<ReturnType>& nonmax_corners)*/
        private static unsafe xy[] fast_nonmax(int* im, int xsize, int ysize, xy[] corners, int barrier)
        {
            /*Create a list of integer pointer offstes, corresponding to the */
            /*direction offsets in dir[]*/
            int[] pointer_dir = new int[16];
            int[] row_start = new int[ysize];
            int numcorners = corners.Length;
            int[] scores = new int[numcorners];
            List<xy> nonmax_corners = new List<xy>();
            int prev_row = -1;
            int i, j;
            int point_above = 0;
            int point_below = 0;


            pointer_dir[0] = 0 + 3 * xsize;
            pointer_dir[1] = 1 + 3 * xsize;
            pointer_dir[2] = 2 + 2 * xsize;
            pointer_dir[3] = 3 + 1 * xsize;
            pointer_dir[4] = 3 + 0 * xsize;
            pointer_dir[5] = 3 + -1 * xsize;
            pointer_dir[6] = 2 + -2 * xsize;
            pointer_dir[7] = 1 + -3 * xsize;
            pointer_dir[8] = 0 + -3 * xsize;
            pointer_dir[9] = -1 + -3 * xsize;
            pointer_dir[10] = -2 + -2 * xsize;
            pointer_dir[11] = -3 + -1 * xsize;
            pointer_dir[12] = -3 + 0 * xsize;
            pointer_dir[13] = -3 + 1 * xsize;
            pointer_dir[14] = -2 + 2 * xsize;
            pointer_dir[15] = -1 + 3 * xsize;

            if (numcorners < 5)
            {
                return null;
            }

            /*xsize ysize numcorners corners*/

            /*Compute the score for each detected corner, and find where each row begins*/
            /* (the corners are output in raster scan order). A beginning of -1 signifies*/
            /* that there are no corners on that row.*/


            for (i = 0; i < ysize; i++)
                row_start[i] = -1;


            for (i = 0; i < numcorners; i++)
            {
                if (corners[i].y != prev_row)
                {
                    row_start[corners[i].y] = i;
                    prev_row = corners[i].y;
                }

                scores[i] = corner_score(im + corners[i].x + corners[i].y * xsize, pointer_dir, barrier);
            }


            /*Point above points (roughly) to the pixel above the one of interest, if there*/
            /*is a feature there.*/

            for (i = 0; i < numcorners; i++)
            {
                int score = scores[i];
                xy pos = corners[i];

                //Check left 
                if (i > 0)
                    if (corners[i - 1].x == pos.x - 1 && corners[i - 1].y == pos.y && scores[i - 1] > score)
                        continue;

                //Check right
                if (i < (numcorners - 1))
                    if (corners[i + 1].x == pos.x + 1 && corners[i + 1].y == pos.y && scores[i + 1] > score)
                        continue;

                //Check above (if there is a valid row above)
                if (pos.y != 0 && row_start[pos.y - 1] != -1)
                {
                    //Make sure that current point_above is one
                    //row above.
                    if (corners[point_above].y < pos.y - 1)
                        point_above = row_start[pos.y - 1];

                    //Make point_above point to the first of the pixels above the current point,
                    //if it exists.
                    for (; corners[point_above].y < pos.y && corners[point_above].x < pos.x - 1; point_above++)
                    { }


                    for (j = point_above; corners[j].y < pos.y && corners[j].x <= pos.x + 1; j++)
                    {
                        int x = corners[j].x;
                        if ((x == pos.x - 1 || x == pos.x || x == pos.x + 1) && (scores[j] > score))
                            goto cont;
                    }

                }

                //Check below (if there is anything below)
                if (pos.y != ysize - 1 && row_start[pos.y + 1] != -1 && point_below < numcorners) //Nothing below
                {
                    if (corners[point_below].y < pos.y + 1)
                        point_below = row_start[pos.y + 1];

                    // Make point below point to one of the pixels belowthe current point, if it
                    // exists.
                    for (; point_below < numcorners && corners[point_below].y == pos.y + 1 && corners[point_below].x < pos.x - 1; point_below++)
                    { }

                    for (j = point_below; j < numcorners && corners[j].y == pos.y + 1 && corners[j].x <= pos.x + 1; j++)
                    {
                        int x = corners[j].x;
                        if ((x == pos.x - 1 || x == pos.x || x == pos.x + 1) && (scores[j] > score))
                            goto cont;
                    }
                }

                nonmax_corners.Add(corners[i]);

            cont:
                ;
            }

            return nonmax_corners.ToArray();
        }

        private static unsafe xy[] fast_corner_detect_12(int* im, int xsize, int ysize, int barrier)
        {
            int boundary = 3, y, cb, c_b;
            int* line_max;
            int* line_min;
            const int rsize = 512;
            List<xy> ret = new List<xy>(rsize);
            int* cache_0;
            int* cache_1;
            int* cache_2;
            int[] pixel = new int[16];
            pixel[0] = 0 + 3 * xsize;
            pixel[1] = 1 + 3 * xsize;
            pixel[2] = 2 + 2 * xsize;
            pixel[3] = 3 + 1 * xsize;
            pixel[4] = 3 + 0 * xsize;
            pixel[5] = 3 + -1 * xsize;
            pixel[6] = 2 + -2 * xsize;
            pixel[7] = 1 + -3 * xsize;
            pixel[8] = 0 + -3 * xsize;
            pixel[9] = -1 + -3 * xsize;
            pixel[10] = -2 + -2 * xsize;
            pixel[11] = -3 + -1 * xsize;
            pixel[12] = -3 + 0 * xsize;
            pixel[13] = -3 + 1 * xsize;
            pixel[14] = -2 + 2 * xsize;
            pixel[15] = -1 + 3 * xsize;
            for (y = boundary; y < ysize - boundary; y++)
            {
                cache_0 = im + boundary + y * xsize;
                line_min = cache_0 - boundary;
                line_max = im + xsize - boundary + y * xsize;

                cache_1 = cache_0 + pixel[8];
                cache_2 = cache_0 + pixel[1];

                for (; cache_0 < line_max; cache_0++, cache_1++, cache_2++)
                {
                    cb = *cache_0 + barrier;
                    c_b = *cache_0 - barrier;
                    if (*cache_1 > cb)
                        if (*cache_2 > cb)
                            if (*(cache_0 + -3) > cb)
                                if (*(cache_0 + pixel[11]) > cb)
                                    if (*(cache_0 + pixel[14]) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_2 + -2) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*(cache_1 + -1) > cb)
                                                                if (*(cache_2 + -1) > cb)
                                                                    goto success;
                                                                else if (*(cache_2 + -1) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + 3) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else if (*(cache_1 + -1) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + 3) > cb)
                                                                    if (*(cache_0 + pixel[2]) > cb)
                                                                        if (*(cache_2 + -1) > cb)
                                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else if (*(cache_1 + 1) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_2 + -1) > cb)
                                                                        if (*(cache_1 + -1) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else if (*(cache_0 + pixel[10]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[3]) > cb)
                                                            if (*(cache_0 + 3) > cb)
                                                                if (*(cache_2 + -1) > cb)
                                                                    if (*(cache_0 + pixel[2]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*(cache_2 + -2) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + 3) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                if (*(cache_0 + pixel[5]) > cb)
                                                                    if (*(cache_1 + -1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + pixel[13]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + 3) > cb)
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            goto success;
                                                                        else if (*(cache_0 + pixel[10]) < c_b)
                                                                            continue;
                                                                        else
                                                                            if (*(cache_2 + -2) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + pixel[2]) > cb)
                                                if (*(cache_1 + 1) > cb)
                                                    if (*(cache_0 + 3) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else if (*(cache_1 + 1) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[2]) > cb)
                                                if (*(cache_1 + 1) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + pixel[13]) > cb)
                                                            if (*(cache_2 + -2) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    if (*(cache_2 + -1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else if (*(cache_1 + -1) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + 3) > cb)
                                                                        if (*(cache_2 + -1) > cb)
                                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_1 + 1) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        if (*(cache_2 + -2) > cb)
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_2 + -1) > cb)
                                                                        if (*(cache_1 + -1) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + pixel[14]) < c_b)
                                        if (*(cache_0 + 3) > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + 3) > cb)
                                            if (*(cache_0 + pixel[6]) > cb)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[11]) < c_b)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + pixel[3]) > cb)
                                            if (*(cache_0 + pixel[5]) > cb)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[3]) > cb)
                                                if (*(cache_2 + -2) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_2 + -1) > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    if (*(cache_0 + pixel[14]) > cb)
                                                                        goto success;
                                                                    else if (*(cache_0 + pixel[14]) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else if (*(cache_0 + -3) < c_b)
                                if (*(cache_0 + 3) > cb)
                                    if (*(cache_2 + -2) > cb)
                                        if (*(cache_0 + pixel[3]) > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*(cache_1 + -1) > cb)
                                                                if (*(cache_0 + pixel[6]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_0 + pixel[13]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[13]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_1 + -1) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_1 + -1) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_1 + -1) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else if (*(cache_2 + -2) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + pixel[11]) > cb)
                                            if (*(cache_2 + -1) > cb)
                                                if (*(cache_0 + pixel[3]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_1 + -1) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                            else
                                if (*(cache_0 + 3) > cb)
                                    if (*(cache_2 + -2) > cb)
                                        if (*(cache_0 + pixel[3]) > cb)
                                            if (*(cache_0 + pixel[5]) > cb)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + pixel[2]) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*(cache_2 + -1) > cb)
                                                                    if (*(cache_1 + -1) > cb)
                                                                        goto success;
                                                                    else if (*(cache_1 + -1) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[13]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[13]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[11]) < c_b)
                                                                    if (*(cache_0 + pixel[14]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        if (*(cache_2 + -1) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else if (*(cache_1 + -1) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[13]) > cb)
                                                                        if (*(cache_1 + 1) > cb)
                                                                            if (*(cache_2 + -1) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_2 + -2) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + pixel[11]) > cb)
                                            if (*(cache_2 + -1) > cb)
                                                if (*(cache_0 + pixel[3]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        if (*(cache_1 + -1) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                        else if (*cache_2 < c_b)
                            continue;
                        else
                            if (*(cache_0 + -3) > cb)
                                if (*(cache_0 + 3) > cb)
                                    if (*(cache_0 + pixel[6]) > cb)
                                        if (*(cache_0 + pixel[2]) > cb)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    if (*(cache_0 + pixel[3]) > cb)
                                                                        goto success;
                                                                    else if (*(cache_0 + pixel[3]) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_2 + -2) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[2]) < c_b)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_2 + -2) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_1 + 1) > cb)
                                                        if (*(cache_0 + pixel[3]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[13]) > cb)
                                                                    if (*(cache_0 + pixel[11]) > cb)
                                                                        if (*(cache_1 + -1) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_0 + pixel[3]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_2 + -2) > cb)
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        if (*(cache_0 + pixel[13]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else if (*(cache_0 + 3) < c_b)
                                    continue;
                                else
                                    if (*(cache_2 + -1) > cb)
                                        if (*(cache_0 + pixel[5]) > cb)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    if (*(cache_2 + -2) > cb)
                                                                        if (*(cache_1 + -1) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                continue;
                    else if (*cache_1 < c_b)
                        if (*(cache_2 + -1) > cb)
                            if (*(cache_0 + pixel[3]) > cb)
                                if (*(cache_0 + pixel[11]) > cb)
                                    if (*(cache_0 + pixel[6]) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            goto success;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_0 + pixel[3]) < c_b)
                                if (*(cache_0 + -3) < c_b)
                                    if (*(cache_0 + pixel[14]) > cb)
                                        continue;
                                    else if (*(cache_0 + pixel[14]) < c_b)
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[5]) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + pixel[2]) > cb)
                                                        continue;
                                                    else
                                                        goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[2]) < c_b)
                                            if (*(cache_0 + 3) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                            else
                                continue;
                        else if (*(cache_2 + -1) < c_b)
                            if (*(cache_0 + -3) > cb)
                                if (*(cache_0 + 3) < c_b)
                                    if (*(cache_0 + pixel[10]) > cb)
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_1 + -1) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[10]) < c_b)
                                        if (*(cache_0 + pixel[3]) < c_b)
                                            if (*(cache_2 + -2) > cb)
                                                continue;
                                            else if (*(cache_2 + -2) < c_b)
                                                if (*(cache_0 + pixel[5]) < c_b)
                                                    if (*(cache_1 + -1) < c_b)
                                                        if (*(cache_1 + 1) < c_b)
                                                            if (*(cache_0 + pixel[2]) < c_b)
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[11]) < c_b)
                                                    if (*(cache_1 + 1) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_1 + -1) > cb)
                                                    continue;
                                                else if (*(cache_1 + -1) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_0 + pixel[3]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[13]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                            else if (*(cache_0 + -3) < c_b)
                                if (*(cache_0 + pixel[11]) > cb)
                                    continue;
                                else if (*(cache_0 + pixel[11]) < c_b)
                                    if (*(cache_0 + pixel[14]) > cb)
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[14]) < c_b)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_1 + 1) > cb)
                                                    continue;
                                                else if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[13]) < c_b)
                                                if (*cache_2 > cb)
                                                    continue;
                                                else if (*cache_2 < c_b)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_1 + 1) > cb)
                                                            continue;
                                                        else if (*(cache_1 + 1) < c_b)
                                                            if (*(cache_2 + -2) > cb)
                                                                continue;
                                                            else if (*(cache_2 + -2) < c_b)
                                                                if (*(cache_1 + -1) > cb)
                                                                    continue;
                                                                else if (*(cache_1 + -1) < c_b)
                                                                    goto success;
                                                                else
                                                                    if (*(cache_0 + 3) < c_b)
                                                                        if (*(cache_0 + pixel[3]) < c_b)
                                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                if (*(cache_0 + 3) < c_b)
                                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_0 + pixel[3]) < c_b)
                                                                if (*(cache_2 + -2) < c_b)
                                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + 3) < c_b)
                                                            if (*(cache_0 + pixel[3]) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_2 + -2) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_1 + 1) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    if (*(cache_2 + -2) > cb)
                                                                        continue;
                                                                    else if (*(cache_2 + -2) < c_b)
                                                                        goto success;
                                                                    else
                                                                        if (*(cache_0 + 3) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[3]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*cache_2 < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_1 + 1) > cb)
                                                    continue;
                                                else if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[13]) < c_b)
                                                            if (*(cache_2 + -2) < c_b)
                                                                if (*cache_2 < c_b)
                                                                    if (*(cache_1 + -1) > cb)
                                                                        continue;
                                                                    else if (*(cache_1 + -1) < c_b)
                                                                        goto success;
                                                                    else
                                                                        if (*(cache_0 + 3) < c_b)
                                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) < c_b)
                                                        if (*(cache_0 + pixel[13]) < c_b)
                                                            if (*(cache_2 + -2) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    if (*(cache_1 + -1) < c_b)
                                                                        if (*cache_2 < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_1 + 1) < c_b)
                                                        if (*(cache_0 + pixel[3]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    if (*cache_2 > cb)
                                                                        continue;
                                                                    else if (*cache_2 < c_b)
                                                                        if (*(cache_1 + -1) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[13]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + 3) < c_b)
                                        if (*(cache_0 + pixel[3]) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_0 + pixel[13]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[13]) < c_b)
                                                            if (*(cache_2 + -2) < c_b)
                                                                if (*cache_2 < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        if (*(cache_0 + pixel[14]) > cb)
                                                                            continue;
                                                                        else if (*(cache_0 + pixel[14]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_1 + -1) < c_b)
                                                                if (*(cache_0 + pixel[14]) > cb)
                                                                    continue;
                                                                else if (*(cache_0 + pixel[14]) < c_b)
                                                                    goto success;
                                                                else
                                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                if (*(cache_0 + 3) < c_b)
                                    if (*(cache_0 + pixel[3]) < c_b)
                                        if (*(cache_0 + pixel[5]) < c_b)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[13]) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_1 + -1) < c_b)
                                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_2 + -2) > cb)
                                                        continue;
                                                    else if (*(cache_2 + -2) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*cache_2 < c_b)
                                                                if (*(cache_1 + -1) > cb)
                                                                    continue;
                                                                else if (*(cache_1 + -1) < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    if (*(cache_0 + pixel[13]) < c_b)
                                                                        if (*(cache_1 + 1) < c_b)
                                                                            if (*(cache_0 + pixel[14]) < c_b)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[11]) < c_b)
                                                            if (*(cache_1 + 1) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    if (*cache_2 < c_b)
                                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        if (*(cache_1 + -1) > cb)
                                                            continue;
                                                        else if (*(cache_1 + -1) < c_b)
                                                            if (*(cache_0 + pixel[2]) < c_b)
                                                                if (*(cache_2 + -2) < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        if (*cache_2 < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[13]) < c_b)
                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                    if (*(cache_2 + -2) < c_b)
                                                                        if (*(cache_1 + 1) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            if (*(cache_0 + 3) < c_b)
                                if (*(cache_0 + -3) < c_b)
                                    if (*(cache_0 + pixel[6]) < c_b)
                                        if (*(cache_0 + pixel[2]) > cb)
                                            if (*(cache_2 + -2) < c_b)
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[2]) < c_b)
                                            if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_0 + pixel[13]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[13]) < c_b)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[3]) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                if (*(cache_2 + -2) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*cache_2 < c_b)
                                                            if (*(cache_0 + pixel[3]) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[14]) < c_b)
                                                if (*(cache_0 + pixel[13]) < c_b)
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_2 + -2) > cb)
                                                            continue;
                                                        else if (*(cache_2 + -2) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[3]) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                continue;
                    else
                        if (*(cache_0 + -3) > cb)
                            if (*(cache_0 + 3) > cb)
                                if (*(cache_2 + -2) > cb)
                                    if (*(cache_0 + pixel[10]) > cb)
                                        if (*(cache_0 + pixel[14]) > cb)
                                            if (*(cache_0 + pixel[2]) > cb)
                                                if (*(cache_0 + pixel[5]) > cb)
                                                    if (*(cache_2 + -1) > cb)
                                                        if (*(cache_0 + pixel[13]) > cb)
                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                if (*cache_2 > cb)
                                                                    if (*(cache_0 + pixel[11]) > cb)
                                                                        goto success;
                                                                    else if (*(cache_0 + pixel[11]) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_1 + 1) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[5]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_1 + -1) > cb)
                                                        if (*(cache_0 + pixel[13]) > cb)
                                                            if (*(cache_2 + -1) > cb)
                                                                if (*(cache_0 + pixel[3]) > cb)
                                                                    if (*(cache_0 + pixel[11]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[10]) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[2]) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*(cache_2 + -1) > cb)
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                if (*(cache_0 + pixel[3]) > cb)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        if (*cache_2 > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[11]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                if (*(cache_2 + -1) > cb)
                                                                    if (*cache_2 > cb)
                                                                        if (*(cache_0 + pixel[5]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                            else
                                continue;
                        else if (*(cache_0 + -3) < c_b)
                            if (*(cache_0 + 3) < c_b)
                                if (*(cache_2 + -2) < c_b)
                                    if (*(cache_0 + pixel[10]) > cb)
                                        if (*(cache_1 + 1) < c_b)
                                            if (*(cache_0 + pixel[13]) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[10]) < c_b)
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_0 + pixel[5]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[5]) < c_b)
                                                    if (*(cache_0 + pixel[13]) < c_b)
                                                        if (*(cache_2 + -1) < c_b)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[11]) < c_b)
                                                                if (*cache_2 < c_b)
                                                                    if (*(cache_0 + pixel[3]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                if (*(cache_1 + 1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_1 + -1) < c_b)
                                                        if (*(cache_0 + pixel[13]) < c_b)
                                                            if (*(cache_0 + pixel[11]) < c_b)
                                                                if (*(cache_2 + -1) < c_b)
                                                                    if (*cache_2 < c_b)
                                                                        if (*(cache_0 + pixel[3]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + pixel[13]) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_1 + 1) > cb)
                                                        continue;
                                                    else if (*(cache_1 + 1) < c_b)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_0 + pixel[14]) < c_b)
                                                                if (*(cache_2 + -1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[11]) < c_b)
                                                            if (*(cache_0 + pixel[14]) < c_b)
                                                                if (*(cache_2 + -1) < c_b)
                                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                            else
                                continue;
                        else
                            continue;
                success:
                    ret.Add(new xy((int)(cache_0 - line_min), y));
                }
            }
            return ret.ToArray();
        }

        private static unsafe xy[] fast_corner_detect_11(int* im, int xsize, int ysize, int barrier)
        {
            int boundary = 3, y, cb, c_b;
            int* line_max;
            int* line_min;
            List<xy> ret = new List<xy>();
            int* cache_0;
            int* cache_1;
            int* cache_2;
            int[] pixel = new int[16];
            pixel[0] = 0 + 3 * xsize;
            pixel[1] = 1 + 3 * xsize;
            pixel[2] = 2 + 2 * xsize;
            pixel[3] = 3 + 1 * xsize;
            pixel[4] = 3 + 0 * xsize;
            pixel[5] = 3 + -1 * xsize;
            pixel[6] = 2 + -2 * xsize;
            pixel[7] = 1 + -3 * xsize;
            pixel[8] = 0 + -3 * xsize;
            pixel[9] = -1 + -3 * xsize;
            pixel[10] = -2 + -2 * xsize;
            pixel[11] = -3 + -1 * xsize;
            pixel[12] = -3 + 0 * xsize;
            pixel[13] = -3 + 1 * xsize;
            pixel[14] = -2 + 2 * xsize;
            pixel[15] = -1 + 3 * xsize;
            for (y = boundary; y < ysize - boundary; y++)
            {
                cache_0 = im + boundary + y * xsize;
                line_min = cache_0 - boundary;
                line_max = im + xsize - boundary + y * xsize;

                cache_1 = cache_0 + pixel[8];
                cache_2 = cache_0 + pixel[13];

                for (; cache_0 < line_max; cache_0++, cache_1++, cache_2++)
                {
                    cb = *cache_0 + barrier;
                    c_b = *cache_0 - barrier;
                    if (*cache_1 > cb)
                        if (*(cache_0 + pixel[1]) > cb)
                            if (*(cache_0 + 3) > cb)
                                if (*(cache_2 + 6) > cb)
                                    if (*(cache_0 + pixel[6]) > cb)
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*(cache_0 + pixel[5]) > cb)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    if (*(cache_1 + 1) > cb)
                                                        if (*(cache_1 + -1) > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[0]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    if (*(cache_0 + pixel[10]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else if (*(cache_1 + -1) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else if (*(cache_1 + 1) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + -3) > cb)
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    if (*cache_2 > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + -3) > cb)
                                                        if (*cache_2 > cb)
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    if (*(cache_0 + pixel[11]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + pixel[5]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + -3) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    goto success;
                                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_1 + 1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[15]) < c_b)
                                            if (*(cache_0 + pixel[11]) > cb)
                                                if (*(cache_1 + 1) > cb)
                                                    if (*(cache_1 + -1) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[11]) > cb)
                                                if (*(cache_1 + 1) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else if (*(cache_0 + pixel[2]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + -3) > cb)
                                                                    if (*(cache_1 + -1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    if (*(cache_1 + -1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                    else if (*(cache_0 + pixel[6]) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[15]) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*cache_2 > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_0 + pixel[10]) > cb)
                                                                        goto success;
                                                                    else if (*(cache_0 + pixel[10]) < c_b)
                                                                        continue;
                                                                    else
                                                                        if (*(cache_0 + pixel[5]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_1 + 1) > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            if (*(cache_1 + -1) > cb)
                                                                                goto success;
                                                                            else
                                                                                continue;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_2 + 6) < c_b)
                                    continue;
                                else
                                    if (*(cache_0 + -3) > cb)
                                        if (*(cache_0 + pixel[11]) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*(cache_1 + -1) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    goto success;
                                                                else if (*(cache_0 + pixel[0]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        if (*(cache_0 + pixel[6]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_1 + 1) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else if (*(cache_0 + 3) < c_b)
                                if (*(cache_0 + -3) > cb)
                                    if (*(cache_0 + pixel[11]) > cb)
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_1 + 1) > cb)
                                                    if (*(cache_1 + -1) > cb)
                                                        if (*cache_2 > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_1 + 1) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[2]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                if (*(cache_0 + -3) > cb)
                                    if (*(cache_0 + pixel[11]) > cb)
                                        if (*(cache_0 + pixel[14]) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[15]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*(cache_1 + -1) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    goto success;
                                                                else if (*(cache_0 + pixel[0]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        if (*(cache_0 + pixel[6]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_1 + 1) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else if (*(cache_0 + pixel[1]) < c_b)
                            if (*(cache_0 + pixel[15]) > cb)
                                if (*(cache_0 + pixel[5]) > cb)
                                    if (*(cache_1 + -1) > cb)
                                        if (*(cache_0 + pixel[11]) > cb)
                                            if (*(cache_0 + -3) > cb)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_0 + pixel[15]) < c_b)
                                if (*(cache_0 + pixel[6]) > cb)
                                    if (*cache_2 > cb)
                                        if (*(cache_2 + 6) > cb)
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + 3) > cb)
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*cache_2 < c_b)
                                        if (*(cache_1 + -1) > cb)
                                            if (*(cache_0 + pixel[5]) < c_b)
                                                if (*(cache_0 + pixel[11]) < c_b)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + -3) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_1 + -1) < c_b)
                                            goto success;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (*(cache_0 + pixel[6]) < c_b)
                                    if (*(cache_0 + -3) < c_b)
                                        if (*(cache_2 + 6) < c_b)
                                            if (*(cache_0 + pixel[5]) < c_b)
                                                if (*cache_2 < c_b)
                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + pixel[11]) < c_b)
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_1 + -1) > cb || *(cache_1 + -1) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[5]) < c_b)
                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                if (*(cache_0 + 3) > cb)
                                    if (*(cache_0 + -3) > cb)
                                        if (*(cache_0 + pixel[14]) > cb)
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[14]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_2 + 6) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            if (*(cache_0 + -3) > cb)
                                if (*(cache_0 + pixel[5]) > cb)
                                    if (*(cache_2 + 6) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_1 + 1) > cb)
                                                        if (*cache_2 > cb)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                if (*(cache_1 + -1) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*cache_2 < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    if (*(cache_1 + -1) > cb)
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*cache_2 > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else if (*(cache_2 + 6) < c_b)
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*cache_2 > cb)
                                                                    if (*(cache_1 + -1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[15]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*cache_2 > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    if (*(cache_0 + pixel[11]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                else if (*(cache_0 + pixel[5]) < c_b)
                                    if (*(cache_0 + pixel[6]) > cb)
                                        if (*(cache_0 + pixel[0]) > cb)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                    goto success;
                                                else
                                                    if (*(cache_2 + 6) > cb || *(cache_2 + 6) < c_b)
                                                        continue;
                                                    else
                                                        goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + pixel[0]) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[11]) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_1 + -1) > cb)
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    if (*(cache_0 + pixel[10]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                continue;
                    else if (*cache_1 < c_b)
                        if (*(cache_0 + pixel[0]) > cb)
                            if (*(cache_2 + 6) > cb)
                                if (*(cache_0 + -3) > cb)
                                    if (*(cache_0 + pixel[5]) > cb)
                                        if (*(cache_0 + pixel[11]) > cb)
                                            if (*(cache_0 + pixel[2]) > cb)
                                                if (*(cache_0 + 3) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[11]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + pixel[6]) > cb)
                                                if (*(cache_0 + 3) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else if (*(cache_0 + -3) < c_b)
                                    if (*(cache_0 + 3) < c_b)
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_2 + 6) < c_b)
                                if (*(cache_0 + -3) < c_b)
                                    if (*(cache_0 + pixel[5]) < c_b)
                                        if (*cache_2 > cb)
                                            continue;
                                        else if (*cache_2 < c_b)
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_0 + pixel[14]) > cb)
                                                        goto success;
                                                    else if (*(cache_0 + pixel[14]) < c_b)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[2]) < c_b)
                                                            goto success;
                                                        else
                                                            if (*(cache_0 + 3) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_1 + -1) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_1 + -1) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                if (*(cache_0 + pixel[14]) < c_b)
                                    if (*(cache_0 + 3) < c_b)
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[11]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else if (*(cache_0 + pixel[0]) < c_b)
                            if (*(cache_0 + -3) > cb)
                                if (*(cache_0 + 3) < c_b)
                                    if (*(cache_0 + pixel[5]) < c_b)
                                        if (*(cache_2 + 6) < c_b)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_1 + -1) < c_b)
                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_1 + -1) < c_b)
                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_1 + -1) > cb)
                                                    continue;
                                                else if (*(cache_1 + -1) < c_b)
                                                    if (*(cache_1 + 1) < c_b)
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + pixel[15]) < c_b)
                                                    if (*(cache_1 + -1) > cb)
                                                        continue;
                                                    else if (*(cache_1 + -1) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[2]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_0 + -3) < c_b)
                                if (*(cache_0 + pixel[11]) > cb)
                                    continue;
                                else if (*(cache_0 + pixel[11]) < c_b)
                                    if (*(cache_0 + pixel[14]) > cb)
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[14]) < c_b)
                                        if (*cache_2 > cb)
                                            continue;
                                        else if (*cache_2 < c_b)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_1 + 1) > cb)
                                                    continue;
                                                else if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[15]) < c_b)
                                                            if (*(cache_1 + -1) > cb)
                                                                continue;
                                                            else if (*(cache_1 + -1) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_0 + 3) < c_b)
                                                                    if (*(cache_2 + 6) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_0 + 3) < c_b)
                                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[15]) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                if (*(cache_0 + 3) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_2 + 6) < c_b)
                                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + 3) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_2 + 6) < c_b)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_2 + 6) < c_b)
                                                    if (*(cache_1 + 1) < c_b)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    continue;
                                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                                    if (*(cache_1 + -1) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                                        if (*(cache_1 + -1) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*cache_2 < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + 3) < c_b)
                                        if (*(cache_2 + 6) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[15]) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + pixel[14]) > cb)
                                                                    continue;
                                                                else if (*(cache_0 + pixel[14]) < c_b)
                                                                    goto success;
                                                                else
                                                                    if (*(cache_1 + -1) < c_b)
                                                                        if (*(cache_1 + 1) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                if (*(cache_0 + 3) < c_b)
                                    if (*(cache_2 + 6) < c_b)
                                        if (*(cache_0 + pixel[5]) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[15]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[15]) < c_b)
                                                        if (*(cache_1 + -1) > cb)
                                                            continue;
                                                        else if (*(cache_1 + -1) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[14]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_1 + 1) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            if (*(cache_0 + pixel[11]) < c_b)
                                if (*(cache_0 + 3) > cb)
                                    if (*(cache_0 + pixel[5]) < c_b)
                                        if (*(cache_0 + pixel[15]) < c_b)
                                            if (*(cache_0 + pixel[14]) < c_b)
                                                if (*cache_2 < c_b)
                                                    if (*(cache_1 + 1) < c_b)
                                                        if (*(cache_0 + -3) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (*(cache_0 + 3) < c_b)
                                    if (*(cache_2 + 6) > cb)
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + -3) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_2 + 6) < c_b)
                                        if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + pixel[1]) > cb)
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[2]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[2]) < c_b)
                                                        goto success;
                                                    else
                                                        if (*cache_2 < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_1 + 1) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*cache_2 < c_b)
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_0 + -3) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            goto success;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else if (*cache_2 < c_b)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_1 + 1) < c_b)
                                                                if (*(cache_1 + -1) < c_b)
                                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    if (*(cache_1 + -1) < c_b)
                                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + -3) < c_b)
                                                if (*cache_2 < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[15]) < c_b)
                                        if (*(cache_0 + pixel[5]) < c_b)
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*cache_2 < c_b)
                                                        if (*(cache_1 + 1) < c_b)
                                                            if (*(cache_1 + -1) < c_b)
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                continue;
                    else
                        if (*cache_2 > cb)
                            if (*(cache_2 + 6) > cb)
                                if (*(cache_0 + pixel[11]) > cb)
                                    if (*(cache_0 + pixel[15]) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_1 + -1) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    goto success;
                                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_1 + -1) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + 3) > cb)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                if (*(cache_0 + pixel[5]) > cb)
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_0 + pixel[10]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    if (*(cache_0 + pixel[10]) > cb)
                                                                        if (*(cache_0 + pixel[0]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + -3) < c_b)
                                            continue;
                                        else
                                            if (*(cache_1 + 1) > cb)
                                                if (*(cache_0 + 3) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else if (*(cache_0 + pixel[11]) < c_b)
                                    if (*(cache_0 + pixel[6]) > cb)
                                        if (*(cache_1 + 1) > cb)
                                            if (*(cache_0 + pixel[15]) > cb)
                                                if (*(cache_0 + 3) > cb)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*(cache_0 + pixel[2]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_1 + 1) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_0 + 3) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + pixel[6]) > cb)
                                        if (*(cache_0 + 3) > cb)
                                            if (*(cache_1 + 1) > cb)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    if (*(cache_0 + pixel[14]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_1 + 1) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[15]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_0 + pixel[5]) > cb)
                                                                        if (*(cache_0 + pixel[1]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                continue;
                        else if (*cache_2 < c_b)
                            if (*(cache_0 + 3) > cb)
                                if (*(cache_2 + 6) < c_b)
                                    if (*(cache_0 + pixel[10]) < c_b)
                                        if (*(cache_1 + -1) < c_b)
                                            if (*(cache_0 + pixel[1]) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_0 + 3) < c_b)
                                if (*(cache_0 + pixel[15]) < c_b)
                                    if (*(cache_0 + pixel[11]) > cb)
                                        if (*(cache_1 + 1) < c_b)
                                            if (*(cache_0 + pixel[5]) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[11]) < c_b)
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + -3) > cb)
                                                continue;
                                            else if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                            if (*(cache_2 + 6) < c_b)
                                                                if (*(cache_0 + pixel[1]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_2 + 6) < c_b)
                                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_2 + 6) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_1 + 1) > cb)
                                            continue;
                                        else if (*(cache_1 + 1) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_2 + 6) < c_b)
                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_2 + 6) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                else
                                    continue;
                            else
                                if (*(cache_1 + -1) < c_b)
                                    if (*(cache_2 + 6) < c_b)
                                        if (*(cache_0 + pixel[11]) < c_b)
                                            if (*(cache_0 + pixel[15]) < c_b)
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                if (*(cache_0 + pixel[1]) < c_b)
                                                                    if (*(cache_0 + pixel[0]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            continue;
                success:
                    ret.Add(new xy((int)(cache_0 - line_min), y));
                }
            }
            return ret.ToArray();
        }

        private static unsafe xy[] fast_corner_detect_10(int* im, int xsize, int ysize, int barrier)
        {
            int boundary = 3, y, cb, c_b;
            int* line_max;
            int* line_min;
            List<xy> ret = new List<xy>();
            int* cache_0;
            int* cache_1;
            int* cache_2;
            int[] pixel = new int[16];
            pixel[0] = 0 + 3 * xsize;
            pixel[1] = 1 + 3 * xsize;
            pixel[2] = 2 + 2 * xsize;
            pixel[3] = 3 + 1 * xsize;
            pixel[4] = 3 + 0 * xsize;
            pixel[5] = 3 + -1 * xsize;
            pixel[6] = 2 + -2 * xsize;
            pixel[7] = 1 + -3 * xsize;
            pixel[8] = 0 + -3 * xsize;
            pixel[9] = -1 + -3 * xsize;
            pixel[10] = -2 + -2 * xsize;
            pixel[11] = -3 + -1 * xsize;
            pixel[12] = -3 + 0 * xsize;
            pixel[13] = -3 + 1 * xsize;
            pixel[14] = -2 + 2 * xsize;
            pixel[15] = -1 + 3 * xsize;
            for (y = boundary; y < ysize - boundary; y++)
            {
                cache_0 = im + boundary + y * xsize;
                line_min = cache_0 - boundary;
                line_max = im + xsize - boundary + y * xsize;

                cache_1 = cache_0 + pixel[9];
                cache_2 = cache_0 + pixel[3];

                for (; cache_0 < line_max; cache_0++, cache_1++, cache_2++)
                {
                    cb = *cache_0 + barrier;
                    c_b = *cache_0 - barrier;
                    if (*cache_1 > cb)
                        if (*(cache_0 + pixel[2]) > cb)
                            if (*(cache_0 + 3) > cb)
                                if (*(cache_0 + pixel[6]) > cb)
                                    if (*(cache_0 + pixel[0]) > cb)
                                        if (*cache_2 > cb)
                                            if (*(cache_0 + pixel[5]) > cb)
                                                if (*(cache_1 + 2) > cb)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            goto success;
                                                        else if (*(cache_1 + 1) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*(cache_1 + 2) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[14]) > cb)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + pixel[5]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_2 + -6) > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + pixel[14]) > cb)
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*cache_2 < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_2 + -6) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_1 + 2) > cb)
                                                                goto success;
                                                            else if (*(cache_1 + 2) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + pixel[0]) < c_b)
                                        if (*(cache_0 + pixel[11]) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_1 + 2) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*(cache_1 + 2) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*cache_2 > cb)
                                                                goto success;
                                                            else if (*cache_2 < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_2 + -6) > cb)
                                                                    if (*(cache_0 + -3) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[11]) < c_b)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*cache_2 > cb)
                                                                if (*(cache_1 + 1) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[6]) < c_b)
                                    continue;
                                else
                                    if (*(cache_0 + pixel[15]) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    goto success;
                                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*cache_2 > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[11]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*cache_2 > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else if (*(cache_0 + 3) < c_b)
                                if (*(cache_0 + pixel[11]) > cb)
                                    if (*(cache_2 + -6) > cb)
                                        if (*(cache_0 + pixel[0]) > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[15]) > cb)
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    continue;
                                                                else
                                                                    goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[0]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + pixel[6]) > cb)
                                                goto success;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                if (*(cache_0 + -3) > cb)
                                    if (*(cache_0 + pixel[11]) > cb)
                                        if (*(cache_0 + pixel[14]) > cb)
                                            if (*(cache_0 + pixel[15]) > cb)
                                                if (*(cache_2 + -6) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_1 + 2) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else if (*(cache_0 + pixel[0]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_1 + 2) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[15]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[5]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_1 + 2) > cb)
                                                            if (*(cache_2 + -6) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else if (*(cache_0 + pixel[2]) < c_b)
                            if (*(cache_0 + pixel[14]) > cb)
                                if (*(cache_0 + pixel[6]) > cb)
                                    if (*(cache_0 + pixel[5]) > cb)
                                        if (*(cache_0 + pixel[11]) > cb)
                                            if (*(cache_1 + 2) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[5]) < c_b)
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_1 + 2) > cb)
                                                            if (*(cache_2 + -6) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[6]) < c_b)
                                    if (*(cache_0 + pixel[1]) > cb)
                                        if (*(cache_1 + 1) > cb)
                                            if (*(cache_0 + -3) > cb)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[1]) < c_b)
                                        continue;
                                    else
                                        if (*(cache_1 + 2) > cb)
                                            if (*(cache_0 + pixel[0]) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[0]) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_1 + 1) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            if (*(cache_1 + 2) > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else if (*(cache_1 + 2) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    if (*(cache_2 + -6) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else if (*(cache_0 + pixel[14]) < c_b)
                                if (*(cache_0 + pixel[5]) > cb)
                                    if (*(cache_0 + pixel[10]) > cb)
                                        if (*cache_2 > cb)
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*cache_2 < c_b)
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_0 + 3) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[10]) < c_b)
                                        if (*cache_2 < c_b)
                                            goto success;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[5]) < c_b)
                                    if (*(cache_2 + -6) > cb)
                                        if (*(cache_1 + 2) < c_b)
                                            if (*(cache_0 + 3) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_2 + -6) < c_b)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[15]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + pixel[15]) < c_b)
                                                if (*cache_2 < c_b)
                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_0 + 3) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                        if (*cache_2 < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_1 + 2) < c_b)
                                            if (*(cache_0 + pixel[15]) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[11]) < c_b)
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + -3) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                if (*(cache_0 + 3) > cb)
                                    if (*(cache_2 + -6) > cb)
                                        if (*(cache_1 + 1) > cb)
                                            if (*(cache_0 + pixel[15]) < c_b)
                                                if (*(cache_1 + 2) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + -3) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                goto success;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            if (*(cache_0 + -3) > cb)
                                if (*(cache_0 + pixel[15]) > cb)
                                    if (*(cache_1 + 1) > cb)
                                        if (*(cache_0 + pixel[14]) > cb)
                                            if (*(cache_0 + pixel[11]) > cb)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_2 + -6) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_1 + 2) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_2 + -6) < c_b)
                                                        continue;
                                                    else
                                                        if (*cache_2 > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[6]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*(cache_0 + pixel[0]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_1 + 2) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    goto success;
                                                                else if (*(cache_2 + -6) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*cache_2 > cb)
                                                                        if (*(cache_0 + 3) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[14]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + 3) > cb)
                                                if (*cache_2 > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_1 + 2) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*cache_2 < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_2 + -6) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_0 + pixel[5]) > cb)
                                                                    if (*(cache_0 + pixel[11]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else if (*(cache_0 + pixel[15]) < c_b)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_1 + 2) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*(cache_0 + pixel[11]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*cache_2 < c_b)
                                                continue;
                                            else
                                                if (*(cache_2 + -6) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + 3) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + pixel[14]) > cb)
                                            if (*(cache_0 + pixel[5]) > cb)
                                                if (*(cache_0 + pixel[0]) > cb || *(cache_0 + pixel[0]) < c_b)
                                                    continue;
                                                else
                                                    goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[5]) > cb)
                                        if (*(cache_1 + 2) > cb)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_1 + 1) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    goto success;
                                                                else if (*(cache_2 + -6) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*cache_2 > cb)
                                                                        if (*(cache_0 + 3) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[14]) < c_b)
                                                if (*(cache_0 + 3) > cb)
                                                    if (*cache_2 > cb)
                                                        goto success;
                                                    else if (*cache_2 < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_2 + -6) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + 3) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_1 + 1) > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_0 + pixel[11]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_2 + -6) < c_b)
                                                            continue;
                                                        else
                                                            if (*cache_2 > cb)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_1 + 1) > cb)
                                                                        if (*(cache_0 + pixel[11]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                continue;
                    else if (*cache_1 < c_b)
                        if (*(cache_0 + pixel[1]) > cb)
                            if (*(cache_0 + pixel[6]) > cb)
                                if (*(cache_2 + -6) > cb)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (*(cache_2 + -6) < c_b)
                                    if (*(cache_0 + pixel[14]) > cb)
                                        if (*(cache_1 + 2) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    if (*(cache_0 + 3) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_1 + 2) > cb)
                                        if (*(cache_0 + pixel[14]) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_0 + 3) > cb)
                                                    if (*(cache_0 + pixel[15]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else if (*(cache_0 + pixel[6]) < c_b)
                                if (*(cache_0 + pixel[14]) > cb)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[11]) > cb)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[5]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + 3) < c_b)
                                        if (*(cache_0 + pixel[2]) > cb)
                                            continue;
                                        else if (*(cache_0 + pixel[2]) < c_b)
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_0 + -3) > cb)
                                                    continue;
                                                else if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    goto success;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_2 + -6) > cb)
                                                    if (*cache_2 < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_2 + -6) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*cache_2 > cb)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[14]) < c_b)
                                    if (*(cache_0 + pixel[5]) > cb)
                                        if (*(cache_0 + pixel[15]) < c_b)
                                            if (*(cache_1 + 2) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[5]) < c_b)
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_1 + 2) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[15]) < c_b)
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_1 + 2) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + 3) < c_b)
                                        if (*(cache_0 + -3) > cb)
                                            continue;
                                        else if (*(cache_0 + -3) < c_b)
                                            if (*(cache_2 + -6) > cb)
                                                continue;
                                            else if (*(cache_2 + -6) < c_b)
                                                if (*(cache_1 + 2) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*cache_2 < c_b)
                                                    if (*(cache_1 + 2) < c_b)
                                                        if (*(cache_1 + 1) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_0 + pixel[11]) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else
                                if (*(cache_0 + -3) > cb)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + pixel[5]) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[5]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_1 + 2) > cb || *(cache_1 + 2) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_0 + pixel[14]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                    else if (*(cache_0 + 3) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_2 + -6) > cb)
                                                    if (*(cache_0 + pixel[2]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                        else if (*(cache_0 + pixel[1]) < c_b)
                            if (*(cache_0 + 3) > cb)
                                if (*(cache_0 + -3) < c_b)
                                    if (*(cache_0 + pixel[11]) < c_b)
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_1 + 1) > cb)
                                                goto success;
                                            else if (*(cache_1 + 1) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[15]) < c_b)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[0]) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_0 + 3) < c_b)
                                if (*(cache_0 + pixel[6]) > cb)
                                    if (*(cache_0 + pixel[2]) > cb || *(cache_0 + pixel[2]) < c_b)
                                        continue;
                                    else
                                        if (*(cache_1 + 1) < c_b)
                                            if (*(cache_1 + 2) > cb || *(cache_1 + 2) < c_b)
                                                continue;
                                            else
                                                goto success;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[6]) < c_b)
                                    if (*cache_2 > cb)
                                        continue;
                                    else if (*cache_2 < c_b)
                                        if (*(cache_0 + pixel[5]) > cb)
                                            continue;
                                        else if (*(cache_0 + pixel[5]) < c_b)
                                            if (*(cache_0 + pixel[0]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[0]) < c_b)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_1 + 2) > cb)
                                                        continue;
                                                    else if (*(cache_1 + 2) < c_b)
                                                        if (*(cache_1 + 1) > cb)
                                                            continue;
                                                        else if (*(cache_1 + 1) < c_b)
                                                            goto success;
                                                        else
                                                            if (*(cache_0 + pixel[14]) < c_b)
                                                                if (*(cache_0 + pixel[15]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_2 + -6) < c_b)
                                                            if (*(cache_0 + pixel[14]) < c_b)
                                                                if (*(cache_0 + pixel[15]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[11]) < c_b)
                                                            if (*(cache_1 + 2) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_1 + 2) < c_b)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_1 + 1) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + -3) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[15]) < c_b)
                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_2 + -6) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_2 + -6) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_1 + 1) < c_b)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[5]) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[14]) < c_b)
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[15]) < c_b)
                                                if (*(cache_2 + -6) < c_b)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[11]) < c_b)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    continue;
                                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                                    goto success;
                                                                else
                                                                    if (*cache_2 < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_1 + 1) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                if (*(cache_0 + -3) < c_b)
                                    if (*(cache_0 + pixel[14]) < c_b)
                                        if (*(cache_0 + pixel[11]) < c_b)
                                            if (*(cache_2 + -6) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_1 + 1) > cb)
                                                        continue;
                                                    else if (*(cache_1 + 1) < c_b)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[15]) < c_b)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[0]) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_1 + 2) < c_b)
                                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[15]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            if (*(cache_0 + pixel[11]) < c_b)
                                if (*(cache_0 + pixel[15]) > cb)
                                    if (*cache_2 > cb)
                                        continue;
                                    else if (*cache_2 < c_b)
                                        if (*(cache_0 + pixel[2]) > cb)
                                            continue;
                                        else if (*(cache_0 + pixel[2]) < c_b)
                                            if (*(cache_0 + pixel[5]) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + 3) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + 3) < c_b)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_1 + 2) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_2 + -6) < c_b)
                                            if (*(cache_0 + 3) > cb)
                                                continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + pixel[2]) > cb || *(cache_0 + pixel[2]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*(cache_0 + pixel[5]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[15]) < c_b)
                                    if (*(cache_1 + 2) < c_b)
                                        if (*(cache_0 + -3) > cb)
                                            continue;
                                        else if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[14]) < c_b)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + pixel[0]) < c_b)
                                                        if (*(cache_1 + 1) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_2 + -6) > cb)
                                                        continue;
                                                    else if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_1 + 1) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*cache_2 < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_0 + 3) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + pixel[0]) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_2 + -6) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_2 + -6) > cb)
                                                        continue;
                                                    else if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*cache_2 < c_b)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                        else
                                            if (*cache_2 < c_b)
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_1 + 1) < c_b)
                                                                if (*(cache_0 + pixel[6]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + pixel[5]) < c_b)
                                            if (*(cache_0 + pixel[14]) < c_b)
                                                if (*(cache_1 + 2) < c_b)
                                                    if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + 3) < c_b)
                                        if (*cache_2 > cb)
                                            if (*(cache_2 + -6) < c_b)
                                                if (*(cache_0 + -3) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*cache_2 < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_1 + 2) < c_b)
                                                        if (*(cache_0 + pixel[5]) < c_b)
                                                            if (*(cache_1 + 1) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_1 + 2) < c_b)
                                                            if (*(cache_0 + pixel[5]) < c_b)
                                                                if (*(cache_1 + 1) < c_b)
                                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_2 + -6) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_1 + 2) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                if (*(cache_0 + pixel[5]) < c_b)
                                                                    if (*(cache_1 + 1) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + pixel[14]) < c_b)
                                            if (*(cache_0 + pixel[5]) < c_b)
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_1 + 2) < c_b)
                                                        if (*(cache_2 + -6) < c_b)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                            else
                                continue;
                    else
                        if (*cache_2 > cb)
                            if (*(cache_0 + pixel[15]) > cb)
                                if (*(cache_0 + pixel[6]) > cb)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_2 + -6) > cb)
                                            if (*(cache_0 + pixel[2]) > cb)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else if (*(cache_0 + pixel[14]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_1 + 1) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else if (*(cache_0 + pixel[5]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + -3) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_2 + -6) < c_b)
                                            if (*(cache_1 + 1) > cb)
                                                if (*(cache_0 + pixel[2]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_1 + 1) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_1 + 2) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_1 + 2) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*(cache_1 + 1) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_1 + 1) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + 3) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_2 + -6) > cb)
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*(cache_0 + pixel[14]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[6]) < c_b)
                                    if (*(cache_0 + pixel[10]) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[2]) > cb)
                                                if (*(cache_0 + pixel[11]) > cb)
                                                    if (*(cache_2 + -6) > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[10]) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + 3) > cb)
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_1 + 1) > cb || *(cache_1 + 1) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[11]) > cb)
                                                        if (*(cache_2 + -6) > cb)
                                                            if (*(cache_0 + pixel[5]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[5]) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_0 + pixel[14]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[11]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[5]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + -3) > cb)
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*(cache_2 + -6) > cb)
                                                        if (*(cache_0 + pixel[2]) > cb)
                                                            if (*(cache_0 + pixel[11]) > cb)
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else if (*(cache_0 + pixel[11]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[5]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[10]) < c_b)
                                            if (*(cache_0 + pixel[5]) > cb)
                                                if (*(cache_0 + 3) > cb)
                                                    if (*(cache_1 + 2) > cb || *(cache_1 + 2) < c_b)
                                                        continue;
                                                    else
                                                        goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[14]) > cb)
                                                    if (*(cache_0 + pixel[5]) > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            if (*(cache_2 + -6) > cb)
                                                                if (*(cache_0 + pixel[2]) > cb)
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[5]) < c_b)
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[11]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                if (*(cache_2 + -6) > cb)
                                                                    if (*(cache_0 + pixel[2]) > cb)
                                                                        if (*(cache_0 + pixel[0]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else
                                continue;
                        else if (*cache_2 < c_b)
                            if (*(cache_0 + pixel[15]) < c_b)
                                if (*(cache_0 + pixel[5]) > cb)
                                    if (*(cache_0 + pixel[10]) < c_b)
                                        if (*(cache_1 + 2) > cb)
                                            if (*(cache_0 + pixel[2]) < c_b)
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_1 + 2) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + pixel[11]) < c_b)
                                                if (*(cache_0 + pixel[2]) < c_b)
                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else if (*(cache_0 + pixel[5]) < c_b)
                                    if (*(cache_1 + 2) > cb)
                                        if (*(cache_2 + -6) < c_b)
                                            if (*(cache_0 + pixel[6]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[6]) < c_b)
                                                goto success;
                                            else
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + 3) > cb)
                                                        continue;
                                                    else if (*(cache_0 + 3) < c_b)
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_0 + pixel[2]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else if (*(cache_1 + 2) < c_b)
                                        if (*(cache_0 + 3) > cb)
                                            continue;
                                        else if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[14]) > cb)
                                                if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[14]) < c_b)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_2 + -6) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_1 + 1) < c_b)
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_2 + -6) < c_b)
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_2 + -6) < c_b)
                                            if (*(cache_0 + pixel[6]) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + 3) > cb)
                                                    continue;
                                                else if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_0 + pixel[14]) < c_b)
                                                            if (*(cache_0 + pixel[2]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[11]) < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                if (*(cache_0 + pixel[1]) < c_b)
                                                                    if (*(cache_1 + 1) > cb || *(cache_1 + 1) < c_b)
                                                                        continue;
                                                                    else
                                                                        goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                            if (*(cache_0 + 3) > cb)
                                                                continue;
                                                            else if (*(cache_0 + 3) < c_b)
                                                                if (*(cache_0 + pixel[2]) < c_b)
                                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    if (*(cache_0 + pixel[11]) < c_b)
                                                                        if (*(cache_0 + pixel[2]) < c_b)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[11]) < c_b)
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_0 + pixel[14]) < c_b)
                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                        if (*(cache_2 + -6) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[14]) < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            if (*(cache_2 + -6) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    if (*(cache_0 + pixel[2]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else
                                continue;
                        else
                            continue;
                success:
                    ret.Add(new xy((int)(cache_0 - line_min), y));
                }
            }
            return ret.ToArray();
        }

        private static unsafe xy[] fast_corner_detect_9(int* im, int xsize, int ysize, int barrier)
        {
            int boundary = 3, y, cb, c_b;
            int* line_max;
            int* line_min;
            List<xy> ret = new List<xy>();
            int* cache_0;
            int* cache_1;
            int* cache_2;
            int[] pixel = new int[16];
            pixel[0] = 0 + 3 * xsize;
            pixel[1] = 1 + 3 * xsize;
            pixel[2] = 2 + 2 * xsize;
            pixel[3] = 3 + 1 * xsize;
            pixel[4] = 3 + 0 * xsize;
            pixel[5] = 3 + -1 * xsize;
            pixel[6] = 2 + -2 * xsize;
            pixel[7] = 1 + -3 * xsize;
            pixel[8] = 0 + -3 * xsize;
            pixel[9] = -1 + -3 * xsize;
            pixel[10] = -2 + -2 * xsize;
            pixel[11] = -3 + -1 * xsize;
            pixel[12] = -3 + 0 * xsize;
            pixel[13] = -3 + 1 * xsize;
            pixel[14] = -2 + 2 * xsize;
            pixel[15] = -1 + 3 * xsize;
            for (y = boundary; y < ysize - boundary; y++)
            {
                cache_0 = im + boundary + y * xsize;
                line_min = cache_0 - boundary;
                line_max = im + xsize - boundary + y * xsize;

                cache_1 = cache_0 + pixel[5];
                cache_2 = cache_0 + pixel[14];

                for (; cache_0 < line_max; cache_0++, cache_1++, cache_2++)
                {
                    cb = *cache_0 + barrier;
                    c_b = *cache_0 - barrier;
                    if (*cache_1 > cb)
                        if (*cache_2 > cb)
                            if (*(cache_0 + 3) > cb)
                                if (*(cache_0 + pixel[0]) > cb)
                                    if (*(cache_0 + pixel[3]) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_2 + 4) > cb)
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        goto success;
                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                if (*(cache_0 + pixel[7]) > cb)
                                                                    if (*(cache_0 + pixel[9]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*(cache_0 + pixel[15]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[8]) > cb)
                                                        if (*(cache_0 + pixel[7]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_2 + 4) < c_b)
                                                continue;
                                            else
                                                if (*(cache_1 + -6) > cb)
                                                    if (*(cache_0 + pixel[9]) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                if (*(cache_0 + pixel[7]) > cb)
                                                                    goto success;
                                                                else if (*(cache_0 + pixel[7]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + -3) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else if (*(cache_0 + pixel[8]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + -3) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        if (*(cache_0 + pixel[13]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*(cache_1 + -6) > cb)
                                                        continue;
                                                    else if (*(cache_1 + -6) < c_b)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    if (*(cache_2 + 4) > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            goto success;
                                                        else if (*(cache_0 + pixel[1]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                if (*(cache_1 + -6) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else if (*(cache_2 + 4) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            if (*(cache_0 + -3) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    if (*(cache_0 + pixel[10]) > cb)
                                                                        if (*(cache_1 + -6) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else if (*(cache_0 + pixel[1]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[8]) > cb)
                                                                        if (*(cache_0 + pixel[10]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + pixel[3]) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_1 + -6) > cb)
                                                    if (*(cache_0 + pixel[8]) > cb)
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            goto success;
                                                        else if (*(cache_0 + pixel[9]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_2 + 4) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else if (*(cache_0 + pixel[8]) < c_b)
                                                        if (*(cache_0 + pixel[7]) > cb || *(cache_0 + pixel[7]) < c_b)
                                                            continue;
                                                        else
                                                            goto success;
                                                    else
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_2 + 4) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[9]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    if (*(cache_0 + pixel[13]) > cb)
                                                                        if (*(cache_0 + pixel[15]) > cb)
                                                                            goto success;
                                                                        else
                                                                            continue;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[0]) < c_b)
                                    if (*(cache_0 + pixel[7]) > cb)
                                        if (*(cache_0 + pixel[10]) > cb)
                                            goto success;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + pixel[7]) > cb)
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*(cache_0 + pixel[3]) > cb)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + pixel[8]) > cb)
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[9]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else if (*(cache_2 + 4) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_1 + -6) > cb)
                                                                if (*(cache_0 + pixel[9]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[6]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[15]) > cb)
                                                        if (*(cache_0 + -3) > cb)
                                                            if (*(cache_0 + pixel[9]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + pixel[3]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[8]) > cb)
                                                        if (*(cache_1 + -6) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                if (*(cache_0 + pixel[9]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else if (*(cache_0 + pixel[6]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    if (*(cache_0 + pixel[13]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[10]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + pixel[1]) > cb)
                                                if (*(cache_0 + pixel[9]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[3]) > cb)
                                                                if (*(cache_0 + pixel[8]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else if (*(cache_0 + 3) < c_b)
                                if (*(cache_0 + -3) > cb)
                                    if (*(cache_0 + pixel[9]) > cb)
                                        if (*(cache_1 + -6) > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else
                                if (*(cache_0 + -3) > cb)
                                    if (*(cache_1 + -6) > cb)
                                        if (*(cache_0 + pixel[7]) > cb)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[9]) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[8]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[0]) > cb)
                                                                    if (*(cache_0 + pixel[1]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else if (*(cache_0 + pixel[6]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_0 + pixel[8]) > cb)
                                                                    goto success;
                                                                else if (*(cache_0 + pixel[8]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                    else if (*(cache_0 + pixel[9]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[7]) < c_b)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                if (*(cache_2 + 4) > cb)
                                                    if (*(cache_0 + pixel[13]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[0]) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[13]) > cb)
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else if (*(cache_2 + 4) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[9]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    if (*(cache_0 + pixel[15]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else if (*(cache_0 + pixel[1]) < c_b)
                                                                    continue;
                                                                else
                                                                    if (*(cache_0 + pixel[8]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_0 + pixel[13]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else if (*cache_2 < c_b)
                            if (*(cache_0 + 3) > cb)
                                if (*(cache_0 + pixel[7]) > cb)
                                    if (*(cache_0 + pixel[1]) > cb)
                                        if (*(cache_0 + pixel[9]) > cb)
                                            if (*(cache_2 + 4) > cb)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        if (*(cache_0 + pixel[8]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_2 + 4) < c_b)
                                                continue;
                                            else
                                                if (*(cache_1 + -6) > cb)
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[9]) < c_b)
                                            if (*(cache_0 + pixel[15]) > cb)
                                                if (*(cache_2 + 4) > cb)
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[0]) > cb)
                                                if (*(cache_0 + pixel[8]) > cb)
                                                    if (*(cache_2 + 4) > cb)
                                                        if (*(cache_0 + pixel[3]) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[8]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[15]) > cb)
                                                        if (*(cache_2 + 4) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + pixel[1]) < c_b)
                                        if (*(cache_1 + -6) > cb)
                                            if (*(cache_0 + pixel[3]) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[8]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[3]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_1 + -6) < c_b)
                                            if (*(cache_0 + pixel[9]) > cb)
                                                if (*(cache_0 + pixel[3]) > cb)
                                                    if (*(cache_2 + 4) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else if (*(cache_2 + 4) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[3]) < c_b)
                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                        if (*(cache_0 + -3) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_2 + 4) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + pixel[9]) < c_b)
                                                if (*(cache_0 + pixel[0]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_2 + 4) < c_b)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[15]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[3]) < c_b)
                                                            if (*(cache_0 + pixel[15]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    if (*(cache_0 + -3) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_2 + 4) > cb)
                                                if (*(cache_0 + pixel[8]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*(cache_0 + pixel[3]) > cb)
                                                if (*(cache_2 + 4) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_2 + 4) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_1 + -6) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[9]) > cb)
                                                                if (*(cache_0 + pixel[8]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else if (*(cache_0 + pixel[3]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_1 + -6) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[7]) < c_b)
                                    if (*(cache_1 + -6) < c_b)
                                        if (*(cache_0 + pixel[15]) > cb)
                                            continue;
                                        else if (*(cache_0 + pixel[15]) < c_b)
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + pixel[13]) < c_b)
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[9]) < c_b)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[8]) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_0 + pixel[1]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_2 + 4) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[8]) < c_b)
                                                            if (*(cache_0 + pixel[13]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + pixel[0]) < c_b)
                                        if (*(cache_0 + pixel[10]) > cb)
                                            continue;
                                        else if (*(cache_0 + pixel[10]) < c_b)
                                            if (*(cache_0 + pixel[9]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[9]) < c_b)
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_1 + -6) < c_b)
                                                            if (*(cache_0 + pixel[13]) < c_b)
                                                                if (*(cache_0 + pixel[15]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[8]) < c_b)
                                                            if (*(cache_0 + pixel[13]) < c_b)
                                                                if (*(cache_1 + -6) < c_b)
                                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_2 + 4) < c_b)
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            if (*(cache_1 + -6) < c_b)
                                                                if (*(cache_0 + pixel[13]) < c_b)
                                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[3]) < c_b)
                                                if (*(cache_1 + -6) < c_b)
                                                    if (*(cache_0 + -3) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else if (*(cache_0 + 3) < c_b)
                                if (*(cache_0 + -3) > cb)
                                    if (*(cache_0 + pixel[13]) > cb)
                                        goto success;
                                    else
                                        continue;
                                else if (*(cache_0 + -3) < c_b)
                                    if (*(cache_0 + pixel[9]) > cb)
                                        if (*(cache_0 + pixel[13]) < c_b)
                                            goto success;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[9]) < c_b)
                                        goto success;
                                    else
                                        if (*(cache_0 + pixel[6]) > cb || *(cache_0 + pixel[6]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_2 + 4) < c_b)
                                                goto success;
                                            else
                                                continue;
                                else
                                    continue;
                            else
                                if (*(cache_1 + -6) > cb)
                                    if (*(cache_0 + pixel[13]) > cb)
                                        if (*(cache_0 + pixel[9]) > cb)
                                            if (*(cache_0 + pixel[7]) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (*(cache_1 + -6) < c_b)
                                    if (*(cache_0 + pixel[3]) > cb)
                                        if (*(cache_0 + pixel[8]) < c_b)
                                            if (*(cache_0 + pixel[15]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[15]) < c_b)
                                                if (*(cache_0 + pixel[13]) < c_b)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[0]) < c_b)
                                                        goto success;
                                                    else
                                                        if (*(cache_0 + pixel[7]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[3]) < c_b)
                                        if (*(cache_2 + 4) > cb)
                                            continue;
                                        else if (*(cache_2 + 4) < c_b)
                                            if (*(cache_0 + pixel[0]) < c_b)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[1]) < c_b)
                                                    if (*(cache_0 + pixel[15]) < c_b)
                                                        if (*(cache_0 + -3) < c_b)
                                                            if (*(cache_0 + pixel[13]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[8]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[9]) < c_b)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[1]) < c_b)
                                                    if (*(cache_0 + -3) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[8]) < c_b)
                                                        if (*(cache_0 + pixel[0]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + pixel[1]) > cb)
                                            continue;
                                        else if (*(cache_0 + pixel[1]) < c_b)
                                            if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[9]) > cb)
                                                        if (*(cache_2 + 4) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[9]) < c_b)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[15]) < c_b)
                                                            if (*(cache_0 + pixel[13]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_2 + 4) < c_b)
                                                            if (*(cache_0 + pixel[15]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[7]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[7]) < c_b)
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[15]) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + -3) < c_b)
                                                            if (*(cache_0 + pixel[8]) < c_b)
                                                                if (*(cache_0 + pixel[13]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + pixel[0]) < c_b)
                                                    if (*(cache_0 + pixel[8]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                else
                                    continue;
                        else
                            if (*(cache_0 + pixel[7]) > cb)
                                if (*(cache_0 + pixel[3]) > cb)
                                    if (*(cache_0 + pixel[10]) > cb)
                                        if (*(cache_0 + 3) > cb)
                                            if (*(cache_2 + 4) > cb)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + pixel[8]) > cb)
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            goto success;
                                                        else if (*(cache_0 + pixel[9]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else if (*(cache_0 + pixel[8]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else if (*(cache_2 + 4) < c_b)
                                                if (*(cache_1 + -6) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_1 + -6) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[8]) > cb)
                                                            if (*(cache_0 + pixel[9]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + 3) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[13]) > cb)
                                                    if (*(cache_1 + -6) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                if (*(cache_0 + pixel[9]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (*(cache_0 + pixel[10]) < c_b)
                                        if (*(cache_0 + pixel[15]) > cb)
                                            if (*(cache_2 + 4) > cb)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + 3) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[15]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + pixel[8]) > cb)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    goto success;
                                                else if (*(cache_0 + pixel[0]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[9]) > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            if (*(cache_0 + pixel[6]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + pixel[1]) > cb)
                                            if (*(cache_0 + pixel[9]) > cb)
                                                if (*(cache_0 + pixel[6]) > cb)
                                                    if (*(cache_0 + 3) > cb)
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[8]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[9]) < c_b)
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[0]) > cb)
                                                    if (*(cache_0 + 3) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_2 + 4) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else if (*(cache_0 + pixel[15]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[8]) > cb)
                                                                    if (*(cache_2 + 4) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[3]) < c_b)
                                    if (*(cache_0 + pixel[13]) > cb)
                                        if (*(cache_1 + -6) > cb)
                                            if (*(cache_0 + pixel[9]) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[8]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[13]) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + 3) > cb)
                                            if (*(cache_0 + -3) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + -3) > cb)
                                        if (*(cache_0 + pixel[13]) > cb)
                                            if (*(cache_1 + -6) > cb)
                                                if (*(cache_0 + pixel[9]) > cb)
                                                    if (*(cache_0 + pixel[6]) > cb)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[13]) < c_b)
                                            if (*(cache_0 + pixel[0]) > cb)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[9]) > cb)
                                                    if (*(cache_1 + -6) > cb)
                                                        if (*(cache_0 + pixel[6]) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                if (*(cache_0 + pixel[8]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else
                                continue;
                    else if (*cache_1 < c_b)
                        if (*(cache_0 + pixel[15]) > cb)
                            if (*(cache_1 + -6) > cb)
                                if (*(cache_2 + 4) > cb)
                                    if (*(cache_0 + -3) > cb)
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*cache_2 > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[1]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[7]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[10]) < c_b)
                                            if (*(cache_0 + pixel[3]) > cb)
                                                if (*(cache_0 + pixel[13]) > cb)
                                                    if (*cache_2 > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[3]) > cb)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else if (*(cache_2 + 4) < c_b)
                                    if (*(cache_0 + pixel[7]) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*cache_2 > cb)
                                                if (*(cache_0 + pixel[13]) > cb)
                                                    if (*(cache_0 + pixel[9]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[7]) < c_b)
                                        if (*(cache_0 + pixel[9]) > cb)
                                            if (*(cache_0 + pixel[1]) > cb)
                                                if (*(cache_0 + -3) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[9]) < c_b)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[10]) < c_b)
                                                if (*(cache_0 + pixel[3]) < c_b)
                                                    if (*(cache_0 + 3) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[1]) < c_b)
                                                    if (*(cache_0 + pixel[3]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[0]) < c_b)
                                                goto success;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + pixel[0]) > cb)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_0 + pixel[9]) > cb)
                                                    if (*cache_2 > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            if (*(cache_0 + pixel[10]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else if (*(cache_0 + pixel[1]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                if (*(cache_0 + -3) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[9]) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[1]) > cb)
                                                if (*cache_2 > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + pixel[13]) > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[7]) > cb)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        if (*(cache_0 + pixel[13]) > cb)
                                                            if (*cache_2 > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[7]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        if (*(cache_0 + pixel[8]) > cb)
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                if (*(cache_0 + pixel[10]) > cb)
                                                                    if (*(cache_0 + pixel[13]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                        else
                                            continue;
                                    else
                                        continue;
                            else if (*(cache_1 + -6) < c_b)
                                if (*(cache_0 + pixel[3]) > cb)
                                    if (*(cache_0 + pixel[13]) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + 3) > cb)
                                                goto success;
                                            else
                                                continue;
                                        else if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + 3) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[13]) < c_b)
                                        if (*(cache_0 + pixel[7]) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[8]) < c_b)
                                                    if (*(cache_0 + -3) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[7]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[3]) < c_b)
                                    if (*(cache_0 + pixel[8]) < c_b)
                                        if (*(cache_0 + pixel[9]) < c_b)
                                            if (*(cache_0 + pixel[7]) < c_b)
                                                if (*(cache_0 + 3) > cb)
                                                    continue;
                                                else if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[10]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + pixel[13]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + -3) < c_b)
                                        if (*(cache_0 + 3) > cb)
                                            continue;
                                        else if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[6]) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + pixel[9]) < c_b)
                                                        if (*(cache_0 + pixel[7]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[13]) < c_b)
                                                if (*(cache_0 + pixel[7]) < c_b)
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_0 + pixel[8]) < c_b)
                                                                if (*(cache_0 + pixel[9]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else
                                if (*(cache_2 + 4) > cb)
                                    if (*(cache_0 + 3) > cb)
                                        if (*(cache_0 + -3) > cb)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (*(cache_2 + 4) < c_b)
                                    if (*(cache_0 + pixel[10]) > cb)
                                        continue;
                                    else if (*(cache_0 + pixel[10]) < c_b)
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + pixel[9]) < c_b)
                                                if (*(cache_0 + pixel[3]) < c_b)
                                                    if (*(cache_0 + pixel[7]) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[8]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + pixel[1]) < c_b)
                                            if (*(cache_0 + pixel[9]) < c_b)
                                                if (*(cache_0 + pixel[3]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    continue;
                        else if (*(cache_0 + pixel[15]) < c_b)
                            if (*(cache_0 + 3) > cb)
                                if (*(cache_0 + -3) < c_b)
                                    if (*(cache_1 + -6) < c_b)
                                        if (*(cache_0 + pixel[13]) < c_b)
                                            if (*(cache_0 + pixel[7]) > cb)
                                                continue;
                                            else if (*(cache_0 + pixel[7]) < c_b)
                                                goto success;
                                            else
                                                if (*(cache_0 + pixel[8]) < c_b)
                                                    if (*(cache_0 + pixel[0]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_0 + 3) < c_b)
                                if (*(cache_0 + pixel[6]) > cb)
                                    if (*(cache_0 + pixel[13]) > cb)
                                        if (*cache_2 > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[13]) < c_b)
                                        if (*(cache_0 + pixel[0]) < c_b)
                                            if (*(cache_2 + 4) < c_b)
                                                if (*cache_2 < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else if (*(cache_0 + pixel[6]) < c_b)
                                    if (*(cache_0 + pixel[3]) > cb)
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[1]) < c_b)
                                                continue;
                                            else
                                                goto success;
                                        else
                                            continue;
                                    else if (*(cache_0 + pixel[3]) < c_b)
                                        if (*(cache_0 + pixel[7]) > cb)
                                            if (*cache_2 < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[7]) < c_b)
                                            if (*(cache_2 + 4) > cb)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else if (*(cache_2 + 4) < c_b)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[1]) < c_b)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[0]) < c_b)
                                                        goto success;
                                                    else
                                                        if (*(cache_0 + pixel[9]) < c_b)
                                                            if (*(cache_0 + pixel[8]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[8]) < c_b)
                                                            if (*(cache_0 + pixel[9]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_1 + -6) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[8]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*cache_2 < c_b)
                                                if (*(cache_2 + 4) < c_b)
                                                    if (*(cache_0 + pixel[0]) < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_1 + -6) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + pixel[8]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[8]) < c_b)
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[9]) < c_b)
                                                            if (*(cache_0 + pixel[7]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[7]) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_0 + pixel[13]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            if (*(cache_2 + 4) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + pixel[13]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_0 + pixel[7]) > cb || *(cache_0 + pixel[7]) < c_b)
                                                                    continue;
                                                                else
                                                                    goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[13]) < c_b)
                                        if (*(cache_2 + 4) > cb)
                                            continue;
                                        else if (*(cache_2 + 4) < c_b)
                                            if (*cache_2 < c_b)
                                                if (*(cache_0 + pixel[3]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[3]) < c_b)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[0]) < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[7]) < c_b)
                                                            if (*(cache_1 + -6) < c_b)
                                                                if (*(cache_0 + pixel[8]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    if (*(cache_0 + -3) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                continue;
                                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                                goto success;
                                                            else
                                                                if (*(cache_0 + pixel[7]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + pixel[9]) < c_b)
                                                if (*(cache_1 + -6) < c_b)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[0]) < c_b)
                                                        if (*cache_2 < c_b)
                                                            if (*(cache_0 + pixel[10]) < c_b)
                                                                if (*(cache_0 + -3) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[7]) < c_b)
                                                            if (*(cache_0 + pixel[8]) < c_b)
                                                                if (*(cache_0 + pixel[1]) > cb || *(cache_0 + pixel[1]) < c_b)
                                                                    continue;
                                                                else
                                                                    goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else
                                if (*(cache_0 + -3) < c_b)
                                    if (*(cache_0 + pixel[13]) < c_b)
                                        if (*(cache_1 + -6) < c_b)
                                            if (*(cache_0 + pixel[9]) > cb)
                                                if (*(cache_0 + pixel[3]) < c_b)
                                                    if (*(cache_2 + 4) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[9]) < c_b)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    continue;
                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + pixel[7]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[7]) < c_b)
                                                        if (*cache_2 > cb || *cache_2 < c_b)
                                                            goto success;
                                                        else
                                                            if (*(cache_0 + pixel[6]) < c_b)
                                                                if (*(cache_0 + pixel[8]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[1]) < c_b)
                                                            if (*cache_2 < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_0 + pixel[8]) < c_b)
                                                                    if (*cache_2 < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_2 + 4) < c_b)
                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_0 + pixel[10]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*cache_2 < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[3]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            if (*(cache_0 + pixel[8]) > cb)
                                if (*(cache_0 + pixel[6]) > cb)
                                    if (*cache_2 > cb)
                                        if (*(cache_1 + -6) > cb)
                                            if (*(cache_0 + pixel[10]) > cb)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_0 + pixel[8]) < c_b)
                                if (*(cache_0 + pixel[3]) > cb)
                                    if (*(cache_0 + pixel[13]) > cb)
                                        continue;
                                    else if (*(cache_0 + pixel[13]) < c_b)
                                        if (*(cache_0 + -3) < c_b)
                                            if (*(cache_0 + pixel[7]) < c_b)
                                                if (*(cache_1 + -6) < c_b)
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        if (*(cache_0 + pixel[10]) < c_b)
                                                            if (*(cache_0 + pixel[9]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_0 + 3) < c_b)
                                            if (*(cache_0 + -3) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[3]) < c_b)
                                    if (*(cache_2 + 4) > cb)
                                        if (*(cache_1 + -6) < c_b)
                                            if (*(cache_0 + pixel[7]) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else if (*(cache_2 + 4) < c_b)
                                        if (*(cache_0 + pixel[6]) < c_b)
                                            if (*(cache_0 + 3) > cb)
                                                continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        continue;
                                                    else if (*(cache_0 + pixel[0]) < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        if (*(cache_0 + pixel[9]) < c_b)
                                                            if (*(cache_0 + pixel[1]) < c_b)
                                                                if (*(cache_0 + pixel[7]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + pixel[7]) < c_b)
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[9]) < c_b)
                                                            goto success;
                                                        else
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[9]) < c_b)
                                                            if (*(cache_0 + pixel[7]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_0 + pixel[7]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                            else
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[13]) < c_b)
                                                        if (*(cache_1 + -6) < c_b)
                                                            if (*(cache_0 + pixel[7]) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_1 + -6) < c_b)
                                            if (*(cache_0 + 3) > cb)
                                                continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                if (*(cache_0 + pixel[6]) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[7]) < c_b)
                                                            if (*(cache_0 + pixel[9]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + -3) < c_b)
                                                    if (*(cache_0 + pixel[13]) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[7]) < c_b)
                                                                if (*(cache_0 + pixel[10]) < c_b)
                                                                    if (*(cache_0 + pixel[9]) < c_b)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + -3) < c_b)
                                        if (*(cache_0 + pixel[13]) > cb)
                                            if (*(cache_0 + 3) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[13]) < c_b)
                                            if (*(cache_1 + -6) < c_b)
                                                if (*(cache_0 + pixel[7]) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[6]) < c_b)
                                                            if (*(cache_0 + pixel[9]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + 3) < c_b)
                                                if (*(cache_0 + pixel[10]) < c_b)
                                                    if (*(cache_0 + pixel[6]) < c_b)
                                                        if (*(cache_1 + -6) < c_b)
                                                            if (*(cache_0 + pixel[7]) < c_b)
                                                                if (*(cache_0 + pixel[9]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else
                                continue;
                    else
                        if (*(cache_0 + -3) > cb)
                            if (*cache_2 > cb)
                                if (*(cache_0 + pixel[7]) > cb)
                                    if (*(cache_1 + -6) > cb)
                                        if (*(cache_0 + pixel[6]) > cb)
                                            if (*(cache_0 + pixel[13]) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[9]) > cb)
                                                        if (*(cache_0 + pixel[8]) > cb)
                                                            goto success;
                                                        else if (*(cache_0 + pixel[8]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else if (*(cache_0 + pixel[9]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[6]) < c_b)
                                            continue;
                                        else
                                            if (*(cache_0 + pixel[15]) > cb)
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[13]) > cb)
                                                        if (*(cache_0 + pixel[9]) > cb)
                                                            if (*(cache_0 + pixel[8]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[8]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else if (*(cache_0 + pixel[9]) < c_b)
                                                            continue;
                                                        else
                                                            if (*(cache_2 + 4) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    if (*(cache_0 + pixel[0]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                    else
                                                        continue;
                                                else if (*(cache_0 + pixel[10]) < c_b)
                                                    continue;
                                                else
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            if (*(cache_2 + 4) > cb)
                                                                if (*(cache_0 + pixel[13]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else if (*(cache_1 + -6) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + 3) > cb)
                                            if (*(cache_2 + 4) > cb)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        if (*(cache_0 + pixel[3]) > cb)
                                                            if (*(cache_0 + pixel[13]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[7]) < c_b)
                                    if (*(cache_2 + 4) > cb)
                                        if (*(cache_1 + -6) > cb)
                                            if (*(cache_0 + pixel[3]) > cb)
                                                if (*(cache_0 + pixel[15]) > cb)
                                                    if (*(cache_0 + pixel[13]) > cb)
                                                        if (*(cache_0 + pixel[1]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + pixel[3]) < c_b)
                                                continue;
                                            else
                                                if (*(cache_0 + pixel[10]) > cb)
                                                    if (*(cache_0 + pixel[13]) > cb)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else if (*(cache_1 + -6) < c_b)
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*(cache_0 + pixel[0]) > cb)
                                                        if (*(cache_0 + pixel[3]) > cb)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*(cache_0 + pixel[13]) > cb)
                                                        if (*(cache_0 + pixel[3]) > cb)
                                                            if (*(cache_0 + pixel[0]) > cb)
                                                                if (*(cache_0 + pixel[15]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else if (*(cache_2 + 4) < c_b)
                                        continue;
                                    else
                                        if (*(cache_0 + pixel[9]) > cb)
                                            if (*(cache_0 + pixel[0]) > cb)
                                                if (*(cache_1 + -6) > cb)
                                                    goto success;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                else
                                    if (*(cache_0 + pixel[0]) > cb)
                                        if (*(cache_0 + pixel[10]) > cb)
                                            if (*(cache_2 + 4) > cb)
                                                if (*(cache_0 + pixel[13]) > cb)
                                                    if (*(cache_1 + -6) > cb)
                                                        if (*(cache_0 + pixel[15]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                goto success;
                                                            else if (*(cache_0 + pixel[1]) < c_b)
                                                                continue;
                                                            else
                                                                if (*(cache_0 + pixel[8]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_1 + -6) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + 3) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                            else if (*(cache_2 + 4) < c_b)
                                                if (*(cache_0 + pixel[1]) > cb)
                                                    if (*(cache_0 + pixel[3]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + pixel[9]) > cb)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        if (*(cache_0 + pixel[13]) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_1 + -6) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                        continue;
                                                    else
                                                        if (*(cache_0 + pixel[8]) > cb)
                                                            if (*(cache_1 + -6) > cb)
                                                                if (*(cache_0 + pixel[13]) > cb)
                                                                    if (*(cache_0 + pixel[15]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                else
                                                    continue;
                                        else if (*(cache_0 + pixel[10]) < c_b)
                                            if (*(cache_0 + 3) > cb)
                                                if (*(cache_0 + pixel[13]) > cb)
                                                    if (*(cache_2 + 4) > cb)
                                                        if (*(cache_0 + pixel[3]) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else if (*(cache_0 + 3) < c_b)
                                                continue;
                                            else
                                                if (*(cache_1 + -6) > cb)
                                                    if (*(cache_0 + pixel[3]) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_0 + pixel[3]) > cb)
                                                if (*(cache_1 + -6) > cb)
                                                    if (*(cache_0 + pixel[13]) > cb)
                                                        if (*(cache_2 + 4) > cb)
                                                            if (*(cache_0 + pixel[15]) > cb)
                                                                if (*(cache_0 + pixel[1]) > cb)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else if (*(cache_1 + -6) < c_b)
                                                    if (*(cache_0 + 3) > cb)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + 3) > cb)
                                                        if (*(cache_0 + pixel[13]) > cb)
                                                            if (*(cache_0 + pixel[1]) > cb)
                                                                if (*(cache_2 + 4) > cb)
                                                                    if (*(cache_0 + pixel[15]) > cb)
                                                                        goto success;
                                                                    else
                                                                        continue;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else
                                continue;
                        else if (*(cache_0 + -3) < c_b)
                            if (*(cache_0 + pixel[15]) > cb)
                                if (*cache_2 < c_b)
                                    if (*(cache_0 + pixel[6]) < c_b)
                                        if (*(cache_0 + pixel[10]) < c_b)
                                            if (*(cache_0 + pixel[7]) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                            else if (*(cache_0 + pixel[15]) < c_b)
                                if (*(cache_0 + pixel[10]) > cb)
                                    if (*(cache_0 + 3) > cb)
                                        continue;
                                    else if (*(cache_0 + 3) < c_b)
                                        if (*(cache_0 + pixel[3]) < c_b)
                                            if (*(cache_0 + pixel[13]) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        if (*(cache_1 + -6) < c_b)
                                            if (*(cache_0 + pixel[3]) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else
                                            continue;
                                else if (*(cache_0 + pixel[10]) < c_b)
                                    if (*cache_2 < c_b)
                                        if (*(cache_0 + pixel[9]) > cb)
                                            if (*(cache_2 + 4) < c_b)
                                                goto success;
                                            else
                                                continue;
                                        else if (*(cache_0 + pixel[9]) < c_b)
                                            if (*(cache_1 + -6) > cb)
                                                continue;
                                            else if (*(cache_1 + -6) < c_b)
                                                if (*(cache_0 + pixel[13]) < c_b)
                                                    if (*(cache_0 + pixel[1]) > cb)
                                                        if (*(cache_0 + pixel[7]) < c_b)
                                                            goto success;
                                                        else
                                                            continue;
                                                    else if (*(cache_0 + pixel[1]) < c_b)
                                                        if (*(cache_0 + pixel[0]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[0]) < c_b)
                                                            goto success;
                                                        else
                                                            if (*(cache_0 + pixel[7]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                    else
                                                        if (*(cache_0 + pixel[7]) > cb)
                                                            continue;
                                                        else if (*(cache_0 + pixel[7]) < c_b)
                                                            if (*(cache_0 + pixel[8]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                if (*(cache_0 + pixel[8]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                else
                                                    continue;
                                            else
                                                if (*(cache_0 + 3) < c_b)
                                                    if (*(cache_0 + pixel[3]) < c_b)
                                                        goto success;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                        else
                                            if (*(cache_2 + 4) < c_b)
                                                if (*(cache_1 + -6) > cb)
                                                    continue;
                                                else if (*(cache_1 + -6) < c_b)
                                                    if (*(cache_0 + pixel[13]) < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    if (*(cache_0 + 3) < c_b)
                                                        if (*(cache_0 + pixel[3]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                            else
                                                continue;
                                    else
                                        continue;
                                else
                                    if (*(cache_0 + pixel[3]) < c_b)
                                        if (*(cache_1 + -6) > cb)
                                            continue;
                                        else if (*(cache_1 + -6) < c_b)
                                            if (*(cache_2 + 4) < c_b)
                                                if (*(cache_0 + pixel[13]) < c_b)
                                                    if (*cache_2 < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            if (*(cache_0 + pixel[0]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            if (*(cache_0 + 3) < c_b)
                                                if (*(cache_2 + 4) < c_b)
                                                    if (*cache_2 < c_b)
                                                        if (*(cache_0 + pixel[1]) < c_b)
                                                            if (*(cache_0 + pixel[13]) < c_b)
                                                                if (*(cache_0 + pixel[0]) < c_b)
                                                                    goto success;
                                                                else
                                                                    continue;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                    else
                                        continue;
                            else
                                if (*(cache_0 + pixel[6]) < c_b)
                                    if (*cache_2 < c_b)
                                        if (*(cache_0 + pixel[7]) < c_b)
                                            if (*(cache_1 + -6) < c_b)
                                                if (*(cache_0 + pixel[13]) < c_b)
                                                    if (*(cache_0 + pixel[10]) < c_b)
                                                        if (*(cache_0 + pixel[9]) < c_b)
                                                            if (*(cache_0 + pixel[8]) < c_b)
                                                                goto success;
                                                            else
                                                                continue;
                                                        else
                                                            continue;
                                                    else
                                                        continue;
                                                else
                                                    continue;
                                            else
                                                continue;
                                        else
                                            continue;
                                    else
                                        continue;
                                else
                                    continue;
                        else
                            continue;
                success:
                    ret.Add(new xy((int)(cache_0 - line_min), y));
                }
            }
            return ret.ToArray();
        }
    }
}
