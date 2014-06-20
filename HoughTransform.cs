using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Single;

namespace VisionNET
{
    /// <summary>
    /// Class encapsulating the logic for a Hough transform.
    /// </summary>
    public class HoughTransform
    {
        private class Index
        {
            private int[] _values;

            public Index(params int[] values)
            {
                _values = values;
            }

            public List<Index> Neighbors()
            {
                List<Index> neighbors = new List<Index>();
                for (int i = 0; i < _values.Length; i++)
                {
                    int[] copy = (int[])_values.Clone();
                    copy[i] = _values[i] - 1;
                    neighbors.Add(new Index(copy));
                    copy = (int[])_values.Clone();
                    copy[i] = _values[i] + 1;
                    neighbors.Add(new Index(copy));
                }
                return neighbors;
            }

            public override bool Equals(object obj)
            {
                if (obj is Index)
                {
                    int[] compare = (obj as Index)._values;
                    if (compare.Length == _values.Length)
                    {
                        for (int i = 0; i < compare.Length; i++)
                            if (compare[i] != _values[i])
                                return false;
                        return true;
                    }
                    else return false;
                }
                else return false;
            }

            public override int GetHashCode()
            {
                int sum = 0;
                foreach (int val in _values)
                    sum = 7 * sum + 13 * val;
                return sum;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("({0}", _values[0]);
                for (int i = 1; i < _values.Length; i++)
                    sb.AppendFormat(", {0}", _values[i]);
                sb.Append(")");
                return sb.ToString();
            }

            public Vector ToVector(int binSize)
            {
                return new DenseVector(_values.Select(o => (float)o * binSize).ToArray());
            }
        }

        private int _binSize;
        private Dictionary<Index, List<Vector>> _transform;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="binSize">The size of the bins</param>
        /// <param name="data">Data to transform</param>
        public HoughTransform(int binSize, List<Vector> data)
        {
            _binSize = binSize;
            _transform = new Dictionary<Index, List<Vector>>();
            foreach (Vector point in data)
            {
                Index index = new Index(point.Select(o => (int)(o / binSize)).ToArray());
                if (!_transform.ContainsKey(index))
                    _transform[index] = new List<Vector>();
                _transform[index].Add(point);
                foreach(Index neighbor in index.Neighbors())
                {
                    if (!_transform.ContainsKey(neighbor))
                        _transform[neighbor] = new List<Vector>();
                    _transform[neighbor].Add(point);
                }
            }
        }

        /// <summary>
        /// Returns the bins and their contents.
        /// </summary>
        /// <returns>The transform bins</returns>
        public List<KeyValuePair<Vector, List<Vector>>> Bins
        {
            get
            {
                return _transform.Select(o => new KeyValuePair<Vector, List<Vector>>(o.Key.ToVector(_binSize), o.Value)).ToList();
            }
        }
    }
}
