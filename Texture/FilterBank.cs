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
using VisionNET.Learning;

namespace VisionNET.Texture
{
    /// <summary>
    /// Calculates a descriptor as an array of filter responses for a patch.
    /// </summary>
    [Serializable]
    public class FilterBank : IEnumerable<Filter>
    {
        private List<Filter> _filters;
        private Rectangle _idealPatchSize;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FilterBank()
        {
            _filters = new List<Filter>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filters">Filters to use when creating the bank</param>
        public FilterBank(params Filter[] filters)
        {
            _filters = new List<Filter>(filters);
            computeIdealPatchSize();
        }

        private void computeIdealPatchSize()
        {
            int width = int.MinValue;
            int height = int.MinValue;
            foreach (Filter f in _filters)
            {
                width = Math.Max(width, f.Columns);
                height = Math.Max(height, f.Rows);
            }
            _idealPatchSize = new Rectangle { Width = width, Height = height };
        }

        /// <summary>
        /// Indexes the filters in this filter bank.
        /// </summary>
        /// <param name="index">Index of the filter</param>
        /// <returns>A filter</returns>
        public Filter this[int index]
        {
            get
            {
                return _filters[index];
            }
        }

        /// <summary>
        /// Number of filters in the bank.
        /// </summary>
        public int Count
        {
            get
            {
                return _filters.Count;
            }
        }

        /// <summary>
        /// The length of the final output of the filter bank.
        /// </summary>
        public virtual int DescriptorLength
        {
            get
            {
                return Count;
            }
        }

        /// <summary>
        /// Adds a new filter to the bank.
        /// </summary>
        /// <param name="f"></param>
        public void AddFilter(Filter f)
        {
            _filters.Add(f);
            computeIdealPatchSize();
        }

        /// <summary>
        /// Removes a filter from the bank.
        /// </summary>
        /// <param name="f">Filter to remove</param>
        public void RemoveFilter(Filter f)
        {
            _filters.Remove(f);
            computeIdealPatchSize();
        }

        /// <summary>
        /// The ideal patch size to be used for this filter bank.
        /// </summary>
        public Rectangle IdealPatchSize
        {
            get
            {
                return _idealPatchSize;
            }
        }

        /// <summary>
        /// Computes the descriptor as an array of filter responses.
        /// </summary>
        /// <param name="samples">Samples to compute the filter bank response for</param>
        /// <param name="pyramid">Pyramid to use when computing responses</param>
        /// <returns>Filter bank descriptor</returns>
        public virtual List<Keypoint> Compute<T>(List<ScaleSpaceSample> samples, ScaleSpacePyramid<T> pyramid) where T:IMultichannelImage<float>,new()
        {
            List<Keypoint> points = new List<Keypoint>();
            foreach (ScaleSpaceSample sample in samples)
            {
                float[] desc = new float[_filters.Count];
                for (int i = 0; i < desc.Length; i++)
                    desc[i] = _filters[i].Compute<T>(sample, pyramid);
                Keypoint point = new Keypoint(sample.X, sample.Y, sample.ImageScale, pyramid.ComputeSigma(sample.Octave, sample.Level), 0);
                point.Descriptor = desc;
                points.Add(point);
            }
            return points;
        }

        /// <summary>
        /// Computes the filter bank at the provided point.
        /// </summary>
        /// <param name="point">Desired location</param>
        /// <returns>Filter response</returns>
        public virtual float[] Compute(ImageDataPoint<float> point)
        {
            float[] desc = new float[_filters.Count];
            for (int i = 0; i < desc.Length; i++)
                desc[i] = _filters[i].Compute(point);
            return desc;
        }

        /// <summary>
        /// Returns an enumerator for the filter bank
        /// </summary>
        /// <returns>The filter enumerator</returns>
        public IEnumerator<Filter> GetEnumerator()
        {
            return _filters.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
