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
    /// Method to use when sampling in scale space.
    /// </summary>
    public enum ScaleSpaceSampleMethod {
        /// <summary>
        /// Points are sampled uniformly across the space, thus finer scales are more likely than coarser ones.
        /// </summary>
        Uniform, 
        /// <summary>
        /// Points are first sampled uniformly for scale and then for position, thus giving increasing weight to
        /// points in coarser scales.
        /// </summary>
        ScaleWeighted 
    };

    /// <summary>
    /// Encapsulates the parameters of a scale-space sample.
    /// </summary>
    public class ScaleSpaceSample
    {
        private int _octave, _level, _row, _column;
        private double _x, _y, _imgScale;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="octave">Octave of the pyramid</param>
        /// <param name="level">Level of the pyramid</param>
        /// <param name="row">Row of the image</param>
        /// <param name="column">Column of the image</param>
        public ScaleSpaceSample(int octave, int level, int row, int column)
        {
            _octave = octave;
            _level = level;
            _row = row;
            _column = column;
            _imgScale = Math.Pow(2, octave);
            _x = _column * _imgScale;
            _y = _row * _imgScale;
        }
        
        /// <summary>
        /// Scale of the image for this sample in relation to the base of the pyramid.
        /// This scale is used such that <code>X = ImageScale*Column</code>, and similarly 
        /// <code>Y = ImageScale*Row</code>.
        /// </summary>
        public double ImageScale
        {
            get
            {
                return _imgScale;
            }
        }

        /// <summary>
        /// Returns the scale-adjusted X-coordinate of the center of the sample.
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }
        }

        /// <summary>
        /// Returns the scale-adjusted Y-coordinate of the center of the sample.
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }
        }

        /// <summary>
        /// Octave of the pyramid.
        /// </summary>
        public int Octave
        {
            get
            {
                return _octave;
            }
        }

        /// <summary>
        /// Level of the pyramid.
        /// </summary>
        public int Level
        {
            get
            {
                return _level;
            }
        }

        /// <summary>
        /// Row of the image.
        /// </summary>
        public int Row
        {
            get
            {
                return _row;
            }
        }

        /// <summary>
        /// Column of the image.
        /// </summary>
        public int Column
        {
            get
            {
                return _column;
            }
        }
    }

    /// <summary>
    /// Class encapsulating a scale space pyramid.  A scale-space pyramid consists of several octaves of images.
    /// Every image in an octave is the same dimension, and these dimensions are half the size of the octave above's.
    /// Each octave is divided into a set of levels, images of the same dimension but convolved with Gaussians with
    /// sigmas of increasing size, such that the end of one octave is twice the sigma of the start, thus making it such
    /// that the next octave in the pyramid is a continuation of the previous.
    /// </summary>
    /// <typeparam name="T">A float-based image type</typeparam>
    public class ScaleSpacePyramid<T> where T:IMultichannelImage<float>,new()
    {
        /// <summary>
        /// Default initial sigma used at the base of each octave.
        /// </summary>
        public const float INITIAL_SIGMA = 1.6f;
        private const int MIN_SIZE = 64;

        private T[,] _pyramid;
        private float _sigma;
        private int _levels;
        private int _octaves;

        private ScaleSpacePyramid(T[,] pyramid, float sigma)
        {
            _pyramid = pyramid;
            _octaves = pyramid.GetLength(0);
            _levels = pyramid.GetLength(1);
            _sigma = sigma;
        }

        /// <summary>
        /// Computes the sampling frequency (sigma) of the level and octave.
        /// </summary>
        /// <param name="octave">The octave of interest</param>
        /// <param name="level">The level of interest</param>
        /// <returns>The sigma</returns>
        public float ComputeSigma(int octave, int level)
        {
            return (float)(Math.Pow(2,octave)*Math.Pow(2, (double)level/_levels)*_sigma);
        }

        /// <summary>
        /// Finds the nearest octave and level in this pyramid to the specified sigma.
        /// </summary>
        /// <param name="sigma">Desired sigma</param>
        /// <param name="octave">Nearest octave</param>
        /// <param name="level">Nearest level</param>
        public void FindNearestLevel(float sigma, out int octave, out int level)
        {
            double val = Math.Log(sigma / _sigma);
            if (val < 0)
            {
                octave = -1;
                level = -1;
            }
            octave = (int)val;
            val = _levels * (val - octave);
            level = (int)Math.Round(val);
            if (level == _levels)
            {
                level = 0;
                octave++;
            }
        }

        /// <summary>
        /// Returns the image at this octave and level in the pyramid.  Images within the pyramid are computed on-demand
        /// to increase run-time efficiency.
        /// </summary>
        /// <param name="octave">The desired octave</param>
        /// <param name="level">The desired level</param>
        /// <returns>An image from the pyramid</returns>
        public T this[int octave, int level]
        {
            get
            {
                return _pyramid[octave, level];
            }
        }
        
        /// <summary>
        /// Creates a scale-space sample given the provided sample size.
        /// </summary>
        /// <param name="method">Method to use when sampling scale-space</param>
        /// <param name="imageHeight">Height of the source image</param>
        /// <param name="imageWidth">Width of the source image</param>
        /// <param name="levels">Number of levels in each octave of the pyramid</param>
        /// <returns>The sample parameters</returns>
        public static ScaleSpaceSample CreateSample(int imageWidth, int imageHeight, int levels, ScaleSpaceSampleMethod method)
        {
            int totalCount = 0;
            List<Rectangle> sizes = new List<Rectangle>();
            Rectangle currentSize = new Rectangle { Columns = imageWidth, Rows = imageHeight };
            while (currentSize.Width >= MIN_SIZE && currentSize.Height >= MIN_SIZE)
            {
                sizes.Add(currentSize);
                currentSize.Width /= 2;
                currentSize.Height /= 2;
            }
            int octaves = sizes.Count;
            int[] octaveCounts = new int[octaves];
            int[] octaveWidths = new int[octaves];
            int[] octaveHeights = new int[octaves];
            for (int o = 0; o < octaves; o++)
            {
                int w, h, c;
                Rectangle p = sizes[o];
                octaveWidths[o] = w = p.Width;
                octaveHeights[o] = h = p.Height;
                if (w < 0 || h < 0)
                    octaveCounts[o] = c = 0;
                else octaveCounts[o] = c = w * h;
                totalCount += c;
            }
            int row, column, octave, level, sample, columns;
            row = column = octave = level = sample = columns = -1;

            switch (method)
            {
                case ScaleSpaceSampleMethod.Uniform:
                    sample = ThreadsafeRandom.Next(totalCount);
                    octave = 0;
                    while (sample > octaveCounts[octave])
                    {
                        sample -= octaveCounts[octave];
                        octave++;
                    }
                    columns = octaveWidths[octave];
                    row = sample / columns;
                    column = sample % columns;
                    level = ThreadsafeRandom.Next(levels);
                    break;

                case ScaleSpaceSampleMethod.ScaleWeighted:
                    octave = ThreadsafeRandom.Next(octaves);
                    while (octaveCounts[octave] == 0)
                        octave = ThreadsafeRandom.Next(octaves);
                    level = ThreadsafeRandom.Next(levels);
                    columns = octaveWidths[octave];
                    sample = ThreadsafeRandom.Next(octaveCounts[octave]);
                    row = sample / columns;
                    column = sample % columns;
                    break;
            }
            return new ScaleSpaceSample(octave, level, row, column);
        }

        /// <summary>
        /// Returns a patch from the specified area of scale-space.
        /// </summary>
        /// <param name="octave">Desired octave of the pyramid</param>
        /// <param name="level">Desired level of the pyramid</param>
        /// <param name="row">Desired row of the image</param>
        /// <param name="column">Desired column of the image</param>
        /// <param name="columns">Desired width of the patch</param>
        /// <param name="rows">Desired height of the patch</param>
        /// <returns>A patch</returns>
        public unsafe float[, ,] ExtractRectangle(int octave, int level, int row, int column, int rows, int columns)
        {
            T image = this[octave, level];
            return image.ExtractRectangle(row, column, rows, columns);
        }

        /// <summary>
        /// Initial sigma used to blur the image.
        /// </summary>
        public float InitialSigma
        {
            get
            {
                return _sigma;
            }
        }

        /// <summary>
        /// Number of octaves in the pyramid.
        /// </summary>
        public int Octaves
        {
            get
            {
                return _octaves;
            }
        }

        /// <summary>
        /// Number of levels per octave.
        /// </summary>
        public int Levels
        {
            get
            {
                return _levels;
            }
        }

        /// <summary>
        /// Computes a scale-space pyramid from the source image using a default initial Gaussian with sigma of 
        /// 2^(1/2).
        /// </summary>
        /// <param name="source">Source image</param>
        /// <param name="levels">Number of levels per octave</param>
        /// <returns>a scale-space pyramid</returns>
        public static ScaleSpacePyramid<T> Compute(T source, int levels)
        {
            return Compute(source, levels, INITIAL_SIGMA);
        }

        /// <summary>
        /// Computes a scale-space pyramid from the source image.
        /// </summary>
        /// <param name="source">Source image</param>
        /// <param name="levels">Number of levels per octave</param>
        /// <param name="sigma">Sigma for the initial convolution Gaussian</param>
        /// <returns>A scale-space pyramid</returns>
        public static ScaleSpacePyramid<T> Compute(T source, int levels, float sigma)
        {
            List<T> octaves = new List<T>();
            T current = Convolution.ConvolveGaussian<T>(source, sigma);
            while (current.Width >= MIN_SIZE && current.Height >= MIN_SIZE)
            {
                octaves.Add(current);
                current = Convolution.ConvolveGaussian<T>(current, 2 * sigma, 2);
            }
            T[,] pyramid = new T[octaves.Count, levels];
            float multiplier = (float)Math.Sqrt(Math.Pow(2, 2.0 / levels) - 1);
            for (int o = 0; o < octaves.Count; o++)
            {
                pyramid[o, 0] = octaves[o];
                float currentSigma = multiplier * sigma;
                for (int l = 1; l < levels; l++)
                {
                    pyramid[o, l] = Convolution.ConvolveGaussian<T>(pyramid[o, l - 1], currentSigma);
                    currentSigma *= multiplier;
                }
            }
            return new ScaleSpacePyramid<T>(pyramid, sigma);
        }
    }
}
