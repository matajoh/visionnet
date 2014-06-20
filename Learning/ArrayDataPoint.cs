using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Learning
{
    /// <summary>
    /// Class encapsulating a multi-dimensional data point
    /// </summary>
    /// <typeparam name="T">Underlying type of the data</typeparam>
    public class ArrayDataPoint<T> : IDataPoint<T[]>
    {
        private T[] _data;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ArrayDataPoint()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="length">Length of the data</param>
        public ArrayDataPoint(int length)
        {
            _data = new T[length];
        }       

        /// <summary>
        /// The data values of the point.
        /// </summary>
        public T[] Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        /// <summary>
        /// The label of the point.
        /// </summary>
        public int Label { get; set; }

        /// <summary>
        /// The feature value of the point.
        /// </summary>
        public float FeatureValue { get; set; }

        /// <summary>
        /// Returns a copy of the point.
        /// </summary>
        /// <returns>A copy of the point</returns>
        public object Clone()
        {
            ArrayDataPoint<T> clone = new ArrayDataPoint<T>();
            clone._data = (T[])_data.Clone();
            clone.Label = Label;
            clone.FeatureValue = FeatureValue;
            return clone;
        }
    }
}
