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
    /// Feature factory for offset rectangle features.  The feature is computed as the sum of values within a rectangle offset from the 
    /// test point.  These features use integral images for computational efficiency.  For details see Textonboost (Shotton et al 2006).
    /// </summary>
    public class RectangleFeatureFactory : IFeatureFactory<ImageDataPoint<float>, float[]>
    {
        [Serializable]
        private class RectangleFeature : IFeature<ImageDataPoint<float>, float[]>
        {
            private static IArrayHandler<float> _integralImage;
            private static string _currentID;

            private Rectangle _rect;
            private int _channel;

            public RectangleFeature(Rectangle rect, int channel)
            {
                _rect = rect;
                _channel = channel;
            }

            public override string ToString()
            {
                return string.Format("Rectangle");
            }

            #region IFeature<float> Members
            public Dictionary<string, object> Metadata
            {
                get { throw new NotImplementedException(); }
            }

            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            public float Compute(ImageDataPoint<float> point)
            {
                if (point.ImageID != _currentID)
                {
                    _currentID = point.ImageID;
                    if (point.Image.IsIntegral || DecisionTree<ImageDataPoint<float>,float[]>.IsBuilding)
                        _integralImage = point.Image;
                    else
                    {
                        _integralImage = IntegralImage.ComputeFloat<FloatArrayHandler>(point.Image);
                    }
                }
                int row = point.Row;
                int column = point.Column;
                float sum = _integralImage.ComputeRectangleSum(row, column, _channel, _rect);
                Debug.Assert(!float.IsNaN(sum), "Rectangle sum is NaN");
                return sum;
            }

            #endregion

            #region ITestInfo<float> Members

            public string Name
            {
                get { return ToString(); }
            }

            public ImageCell Cell0
            {
                get { return new ImageCell { Row = _rect.Top, Column = _rect.Left, Channel = _channel }; }
            }

            public ImageCell Cell1
            {
                get { return new ImageCell { Row = _rect.Bottom, Column = _rect.Right, Channel = _channel }; }
            }

            #endregion
        }

        private int _boxSize;
        private int _numChannels;
        private int _maxRows;
        private int _maxColumns;

        /// <summary>
        /// Size of the box around the test point the factory can sample from.
        /// </summary>
        public int BoxSize
        {
            get { return _boxSize; }
            set { _boxSize = value; }
        }

        /// <summary>
        /// Number of channels the factory can sample from.
        /// </summary>
        public int ChannelCount
        {
            get { return _numChannels; }
            set { _numChannels = value; }
        }

        /// <summary>
        /// Maximum number of rows in the test rectangle.
        /// </summary>
        public int MaxRows
        {
            get { return _maxRows; }
            set { _maxRows = value; }
        }

        /// <summary>
        /// Maximum number of columns in the test rectangle.
        /// </summary>
        public int MaxColumns
        {
            get { return _maxColumns; }
            set { _maxColumns = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="boxSize">Size of the box around the test point the factory can sample from.</param>
        /// <param name="numChannels">Number of channels the factory can sample from.</param>
        /// <param name="maxRows">Maximum number of rows in the test rectangle.</param>
        /// <param name="maxColumns">Maximum number of columns in the test rectangle.</param>
        public RectangleFeatureFactory(int boxSize, int numChannels, int maxRows, int maxColumns)
        {
            _boxSize = boxSize;
            _numChannels = numChannels;
            _maxRows = maxRows;
            _maxColumns = maxColumns;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public RectangleFeatureFactory()
        {
        }

        /// <summary>
        /// Creates a new feature.
        /// </summary>
        /// <returns>An object which implements <see cref="T:IFeature" /></returns>
        public IFeature<ImageDataPoint<float>, float[]> Create()
        {
            int row = ThreadsafeRandom.Next(-BoxSize, BoxSize);
            int column = ThreadsafeRandom.Next(-BoxSize, BoxSize);
            int rows = ThreadsafeRandom.Next(1, MaxRows);
            int columns = ThreadsafeRandom.Next(1, MaxColumns);

            int channel = ThreadsafeRandom.Next(ChannelCount);
            return new RectangleFeature(new Rectangle{R=row, C=column, Rows=rows, Columns=columns}, channel);
        }

        /// <summary>
        /// Returns whether <paramref name="feature"/> is a product of this factory.
        /// </summary>
        /// <param name="feature">Feature to test</param>
        /// <returns>True if it came from this factory, false otherwise.</returns>
        public bool IsProduct(IFeature<ImageDataPoint<float>, float[]> feature)
        {
            return feature is RectangleFeature;
        }
    }
}
