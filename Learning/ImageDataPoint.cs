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

namespace VisionNET.Learning
{
    /// <summary>
    /// This class represents a learning data point extracted from an image.  It encapsulates the source image, its label and position in that image,
    /// and also the most recent feature value computed for that point.
    /// </summary>
    /// <typeparam name="T">The data type of the source image</typeparam>
    [Serializable]
    public class ImageDataPoint<T> : IComparable<ImageDataPoint<T>>, IDataPoint<T[]>
    {
        private IMultichannelImage<T> _image;
        private short _row, _column;
        private int _label;
        private float _featureValue;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public ImageDataPoint()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="image">Source image</param>
        /// <param name="row">Image row</param>
        /// <param name="column">Image column</param>
        /// <param name="label">Ground truth label</param>
        public ImageDataPoint(IMultichannelImage<T> image, short row, short column, int label)
        {
            _image = image;
            _row = row;
            _column = column;
            _label = label;
        }

        /// <summary>
        /// ID of the source image.
        /// </summary>
        public string ImageID
        {
            get
            {
                return _image.ID;
            }
        }

        /// <summary>
        /// The last feature value computed for this point.
        /// </summary>
        public float FeatureValue
        {
            get
            {
                return _featureValue;
            }
            set
            {
                _featureValue = value;
            }
        }

        /// <summary>
        /// Source image.
        /// </summary>
        public IMultichannelImage<T> Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
        }

        /// <summary>
        /// Row from the source image.
        /// </summary>
        public short Row
        {
            get
            {
                return _row;
            }
            set
            {
                _row = value;
            }
        }
        
        /// <summary>
        /// Column from the source image.
        /// </summary>
        public short Column
        {
            get
            {
                return _column;
            }
            set
            {
                _column = value;
            }
        }

        /// <summary>
        /// Compares this point to <paramref name="other"/>.
        /// </summary>
        /// <param name="other">Point to compare to</param>
        /// <returns>Positive if this point is greater, negative if this point is less than, zero if equal</returns>
        public int CompareTo(ImageDataPoint<T> other)
        {
            if (ImageID == other.ImageID)
            {
                if (_row == other._row)
                {
                    return _column.CompareTo(other._column);
                }
                else
                {
                    return _row.CompareTo(other._row);
                }
            }
            else
            {
                return ImageID.CompareTo(other.ImageID);
            }
        }

        #region IDataPoint<float> Members

        /// <summary>
        /// The pixel values of the image at this data point's location.
        /// </summary>
        public T[] Data
        {
            get
            {
                return (from channel in Enumerable.Range(0, _image.Channels)
                        select _image[_row, _column, channel]).ToArray();
            }
            set
            {
                if(value.Length != _image.Channels)
                    throw new ArgumentException("Array does not match dimensionality of the image.");
                for (int i = 0; i < value.Length; i++)
                    _image[_row, _column, i] = value[i];

            }
        }

        /// <summary>
        /// The label associated with this data point.
        /// </summary>
        public int Label
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

        #endregion

        /// <summary>
        /// Clones this data point.
        /// </summary>
        /// <returns>A copy of the data point</returns>
        public object Clone()
        {
            ImageDataPoint<T> clone = new ImageDataPoint<T>();
            clone._row = _row;
            clone._column = _column;
            clone._featureValue = _featureValue;
            clone._label = _label;
            clone._image = _image;
            return clone;
        }
    }
}
