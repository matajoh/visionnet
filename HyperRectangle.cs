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

namespace VisionNET
{
    /// <summary>
    /// Represents a Hyperrectangle.
    /// </summary>
    /// <typeparam name="T">The underlying type of the space the Hyperrectangle exists in</typeparam>
    [Serializable]
    public class Hyperrectangle<T> : ICloneable where T : IComparable<T>
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Hyperrectangle()
        {
        }
        /// <summary>
        /// Constructs a Hyperrectangle whose dimensionality is determined by <paramref name="numDimensions"/>.
        /// </summary>
        /// <param name="numDimensions">Number of dimensions of the space the hyperrectangle exists in</param>
        public Hyperrectangle(int numDimensions)
        {
            Dimensions = numDimensions;
        }
        /// <summary>
        /// Constructs a Hyperrectangle of uniform size whose dimensionality is determined by <paramref name="numDimensions"/>.
        /// </summary>
        /// <param name="numDimensions">Number of dimensions of the space the hyperrectangle exists in</param>
        /// <param name="minimumValue">Minimum value in each dimension</param>
        /// <param name="maximumValue">Maximum value in each dimension</param>
        public Hyperrectangle(int numDimensions, T minimumValue, T maximumValue)
        {
            Dimensions = numDimensions;
            for (int i = 0; i < numDimensions; i++)
            {
                _minimumBound[i] = minimumValue;
                _maximumBound[i] = maximumValue;
            }
        }

        /// <summary>
        /// Determines whether this hyperrectangle contains a certain point.
        /// </summary>
        /// <param name="point">The point to analyze</param>
        /// <returns>True if the point is within the rectangle, false otherwise</returns>
        public bool Contains(T[] point)
        {
            for (int i = 0; i < _numDimensions; i++)
                if (point[i].CompareTo(_minimumBound[i]) < 0 || point[i].CompareTo(_maximumBound[i]) > 0)
                    return false;
            return true;
        }

        private int _numDimensions;

        /// <summary>
        /// The dimensionality of this hyperrectangle.
        /// </summary>
        public int Dimensions
        {
            get { return _numDimensions; }
            set { 
                _numDimensions = value;
                _minimumBound = new T[_numDimensions];
                _maximumBound = new T[_numDimensions];                
            }
        }
        private T[] _minimumBound;

        /// <summary>
        /// The minimum bound of the hyperrectangle in each dimension.
        /// </summary>
        public T[] MinimumBound
        {
            get { return _minimumBound; }
        }
        private T[] _maximumBound;

        /// <summary>
        /// The maximum bound of the hyperrectangle in each dimension.
        /// </summary>
        public T[] MaximumBound
        {
            get { return _maximumBound; }
        }

        /// <summary>
        /// Finds the nearest point on the hyperrectangle's boundary to the provided point.
        /// </summary>
        /// <param name="point">The point to analyze</param>
        /// <returns>The nearest point on the boundary of the hyperrectangle</returns>
        public T[] NearestPoint(T[] point)
        {
            T[] result = new T[point.Length];
            for (int i = 0; i < result.Length; i++)
            {
                if (point[i].CompareTo(_minimumBound[i]) < 0)
                    result[i] = _minimumBound[i];
                else if (point[i].CompareTo(_maximumBound[i]) > 0)
                    result[i] = _maximumBound[i];
                else result[i] = point[i];
            }
            return result;
        }

        /// <summary>
        /// Returns a string representation of the hyperrectangle of the form {[minBound0, maxBound0], [minBound1, maxBound1], ... , [minBoundN, maxBoundN]}.
        /// </summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            if (_numDimensions > 0)
            {
                sb.AppendFormat("[{0},{1}]", _minimumBound[0], _maximumBound[0]);
                for (int i = 1; i < _numDimensions; i++)
                    sb.AppendFormat(", [{0}, {1}]", _minimumBound[i], _maximumBound[i]);
            }
            sb.Append("}");
            return sb.ToString();
        }

        #region ICloneable Members

        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns>A clone of the object.</returns>
        public object Clone()
        {
            Hyperrectangle<T> copy = new Hyperrectangle<T> { Dimensions = _numDimensions };
            Array.Copy(_minimumBound, copy._minimumBound, _numDimensions);
            Array.Copy(_maximumBound, copy._maximumBound, _numDimensions);
            return copy;
        }

        #endregion

        /// <summary>
        /// Adds <paramref name="point"/> to the hyperrectangle, expanding it if necessary to contain it.
        /// </summary>
        /// <param name="point">The point to add</param>
        public void Add(T[] point)
        {
            for (int i = 0; i < _numDimensions; i++)
            {
                T index = point[i];
                if (index.CompareTo(_minimumBound[i]) < 0)
                    _minimumBound[i] = index;
                if (index.CompareTo(_maximumBound[i]) > 0)
                    _maximumBound[i] = index;
            }
        }

        /// <summary>
        /// Creates a hyperrectangle which contains both arguments.
        /// </summary>
        /// <param name="lhs">The first hyperrectangle</param>
        /// <param name="rhs">The second hyperrectangle</param>
        /// <returns>The union of the two arguments</returns>
        public static Hyperrectangle<T> Union(Hyperrectangle<T> lhs, Hyperrectangle<T> rhs)
        {
            if (lhs.Dimensions != rhs.Dimensions)
                throw new ArgumentException("Hyperrectangles must be of the same dimensionality.");
            Hyperrectangle<T> result = new Hyperrectangle<T>(lhs.Dimensions);
            for (int i = 0; i < result._numDimensions; i++)
            {
                T lhsVal = lhs._minimumBound[i];
                T rhsVal = rhs._minimumBound[i];
                result._minimumBound[i] = lhsVal.CompareTo(rhsVal) < 0 ? lhsVal : rhsVal;
                lhsVal = lhs._maximumBound[i];
                rhsVal = rhs._maximumBound[i];
                result._maximumBound[i] = lhsVal.CompareTo(rhsVal) > 0 ? lhsVal : rhsVal;
            }
            return result;
        }

        /// <summary>
        /// Creates a hyperrectangle which contains both arguments.
        /// </summary>
        /// <param name="lhs">The first hyperrectangle</param>
        /// <param name="rhs">The second hyperrectangle</param>
        /// <returns>The union of the two arguments</returns>
        public static Hyperrectangle<T> operator +(Hyperrectangle<T> lhs, Hyperrectangle<T> rhs)
        {
            return Union(lhs, rhs);
        }
    }
}
