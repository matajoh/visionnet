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
using VisionNET.Learning;
using System.Diagnostics;
using System.Collections.Generic;

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// Factory which creates Haar-like features.  These operate on integral images for computational efficiency.  See Viola and Jones for details.
    /// </summary>
    public class HaarFeatureFactory : IFeatureFactory<ImageDataPoint<float>, float[]>
    {
        [Serializable]
        private class HaarFeature : IFeature<ImageDataPoint<float>, float[]>
        {
            private static FloatArrayHandler _integralImage;
            private static string _currentID;

            private Rectangle[] _rectangles;
            private int _count;
            private int[] _channels;

            public HaarFeature(Rectangle[] rectangles, int[] channels)
            {
                _rectangles = rectangles;
                _channels = channels;
                _count = _rectangles.Length;
            }

            public override string ToString()
            {
                return string.Format("{0} Haar", _count);
            }

            #region IFeature<float> Members

            public float Compute(ImageDataPoint<float> point)
            {
                if (point.ImageID != _currentID)
                {
                    _currentID = point.ImageID;
                    if (point.Image.IsIntegral || DecisionTree<ImageDataPoint<float>, float[]>.IsBuilding)
                        _integralImage = new FloatArrayHandler(point.Image.RawArray, true);
                    else
                    {
                        _integralImage = IntegralImage.ComputeFloat<FloatArrayHandler>(point.Image);
                    }
                }
                int row = point.Row;
                int column = point.Column;
                IMultichannelImage<float> image = point.Image;
                float sum = 0;
                for (int i = 0; i < _count; i++)
                {
                    if (i % 2 == 0)
                        sum += _integralImage.ComputeRectangleSum(row, column, _channels[i], _rectangles[i]);
                    else sum -= _integralImage.ComputeRectangleSum(row, column, _channels[i], _rectangles[i]);
                }
                Debug.Assert(!float.IsNaN(sum), "Rectangle sum is NaN!");
                return sum;
            }

            public Dictionary<string, object> Metadata
            {
                get { throw new NotImplementedException(); }
            }

            #endregion

            #region ITestInfo Members

            public string Name
            {
                get { return ToString(); }
            }

            #endregion

            #region IFeature<ImageDataPoint<float>,float[]> Members


            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        private int _numChannels;
        private int _possibleFeatureCount;
        private bool _mixChannels;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="squareSize">Size of the square to build the Haar-like Feature within.</param>
        /// <param name="numChannels">Number of channels to sample from</param>
        /// <param name="mixChannels">Whether to allow the mixing channels when computing the difference</param>
        public HaarFeatureFactory(int squareSize, int numChannels, bool mixChannels)
        {
            HaarFeatures.SetSquareSize(2*squareSize + 1);
            _possibleFeatureCount = numChannels*HaarFeatures.FEATURES.Length;
            _numChannels = numChannels;
            _mixChannels = mixChannels;
        }

        #region IFeatureFactory<float> Members

        /// <summary>
        /// Creates a new feature.
        /// </summary>
        /// <returns>An object which implements <see cref="T:IFeature" /></returns>
        public IFeature<ImageDataPoint<float>, float[]> Create()
        {
            int index = ThreadsafeRandom.Next(0, HaarFeatures.FEATURES.Length);
            Rectangle[] rectangles = HaarFeatures.FEATURES[index];
            int[] channels = new int[rectangles.Length];
            if (_mixChannels)
            {
                for (int i = 0; i < channels.Length; i++)
                    channels[i] = ThreadsafeRandom.Next(0, _numChannels);
            }
            else
            {
                int channel = ThreadsafeRandom.Next(0, _numChannels);
                for (int i = 0; i < channels.Length; i++)
                    channels[i] = channel;
            }
            return new HaarFeature(HaarFeatures.FEATURES[index], channels);
        }

        /// <summary>
        /// Returns whether <paramref name="feature"/> is a product of this factory.
        /// </summary>
        /// <param name="feature">Feature to test</param>
        /// <returns>True if it came from this factory, false otherwise.</returns>
        public bool IsProduct(IFeature<ImageDataPoint<float>, float[]> feature)
        {
            return feature is HaarFeature;
        }

        #endregion
    }
}
