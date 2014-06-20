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

namespace VisionNET.Comparison
{
    /// <summary>
    /// A method which compares two items and returns a high value if they are similar, and a low value if they are dissimilar.  Convention scales this
    /// value between 0 and 1.
    /// </summary>
    /// <typeparam name="T">Any type</typeparam>
    /// <param name="lhs">The first item</param>
    /// <param name="rhs">The second item</param>
    /// <returns>High value if similar, low value if dissimilar</returns>
    public delegate float SimilarityMetric<T>(T lhs, T rhs);

    /// <remarks>
    /// Encapsulates the similarity between two lists of items all of the same type.  Similarity is encoded as a float, where a high value indicates
    /// high similarity and a low value, low similarity (convention scales this value between 0 and 1).  The value at (r,c) indicates the similarity between item 'r' in the first list and item 'c'
    /// in the second list.
    /// </remarks>
    /// <typeparam name="T">Any type</typeparam>
    [Serializable]
    public class SimilarityMatrix<T>
    {
        private float[,] _similarities;
        private int _rows;
        private int _columns;

        /// <summary>
        /// Rows in the matrix (number of items in the first list)
        /// </summary>
        public int Rows
        {
            get
            {
                return _rows;
            }
        }

        /// <summary>
        /// Columns in the matrix (number of items in the second list)
        /// </summary>
        public int Columns
        {
            get
            {
                return _columns;
            }
        }

        /// <summary>
        /// Indexes this matrix.
        /// </summary>
        /// <param name="row">Item in first list</param>
        /// <param name="column">Item in second list</param>
        /// <returns>The similarity between item <paramref name="row"/> and item <paramref name="column"/></returns>
        public float this[int row, int column]
        {
            get
            {
                return _similarities[row, column];
            }
        }

        /// <summary>
        /// Creates a similarity matrix from <paramref name="similarityMatrix"/>
        /// </summary>
        /// <param name="similarityMatrix">The values to use for this matrix</param>
        public SimilarityMatrix(float[,] similarityMatrix)
        {
            _similarities = similarityMatrix;
            _rows = _similarities.GetLength(0);
            _columns = _similarities.GetLength(1);
        }

        /// <summary>
        /// Compares two arrays of items.
        /// </summary>
        /// <param name="lhs">First array</param>
        /// <param name="rhs">Second array</param>
        /// <param name="metric">Metric to use</param>
        /// <returns>A similarity matrix</returns>
        public static SimilarityMatrix<T> Compare(T[] lhs, T[] rhs, SimilarityMetric<T> metric)
        {
            if (lhs.Length == 0 || rhs.Length == 0)
                return new SimilarityMatrix<T>(new float[1, 1]);
            UpdateManager.Clear();
            UpdateManager.WriteLine("Comparing items...");
            int lhsLength = lhs.Length;
            int rhsLength = rhs.Length;
            int maximum = lhsLength * rhsLength;
            int count = 0;
            float[,] similarities = new float[lhsLength, rhsLength];
            for (int r = 0; r < lhsLength; r++)
            {
                T x = lhs[r];
                for (int c = 0; c < rhsLength; c++)
                {
                    T y = rhs[c];
                    similarities[r, c] = metric(x, y);
                    UpdateManager.RaiseProgress(++count, maximum);
                }
            }
            UpdateManager.WriteLine("Done");
            return new SimilarityMatrix<T>(similarities);
        }

        /// <summary>
        /// Compares an array of items to itself.
        /// </summary>
        /// <param name="items">Item array</param>
        /// <param name="metric">Metric to use</param>
        /// <returns>Similarity matrix</returns>
        public static SimilarityMatrix<T> Compare(T[] items, SimilarityMetric<T> metric)
        {
            if (items.Length == 0)
                return new SimilarityMatrix<T>(new float[1, 1]);
            UpdateManager.Clear();
            UpdateManager.WriteLine("Comparing items...");
            int count = items.Length;
            int maximum = 0;
            for (int r = 0; r < items.Length; r++)
                maximum += items.Length - r;

            float[,] similarities = new float[items.Length, items.Length];
            int index = 0;
            for (int r = 0; r < count; r++)
            {
                T x = items[r];
                for (int c = r; c < count; c++)
                {
                    T y = items[c];
                    similarities[r, c] = metric(x, y);
                    UpdateManager.RaiseProgress(++index, maximum);
                }
                GC.Collect();
            }
            for (int r = 0; r < count; r++)
                for (int c = 0; c < r; c++)
                    similarities[r, c] = similarities[c, r];
            UpdateManager.WriteLine("Done");
            return new SimilarityMatrix<T>(similarities);
        }

        /// <summary>
        /// Creates a similarity matrix by comparing a list of items against themselves.  Thus, the result is a symmetric matrix with a diagonal of the maximum value
        /// for <paramref name="metric"/>.  The values themselves are stored in a lookup dictionary.
        /// </summary>
        /// <param name="itemLookup">Stores the values</param>
        /// <param name="members">The members to compare</param>
        /// <param name="metric">The metric to use</param>
        /// <returns>A similarity matrix</returns>
        public static SimilarityMatrix<T> Compare(Dictionary<string, T> itemLookup, List<string> members, SimilarityMetric<T> metric)
        {
            if (itemLookup.Count == 0 || members.Count == 0)
                return new SimilarityMatrix<T>(new float[1, 1]);
            T[] items = new T[members.Count];
            for (int i = 0; i < items.Length; i++)
                items[i] = itemLookup[members[i]];
            return Compare(items, metric);            
        }

        /// <summary>
        /// Creates a similarity matrix between two list of items.  The values themselves are stored in a lookup dictionary.
        /// </summary>
        /// <param name="itemLookup">Stores the values</param>
        /// <param name="lhs">First list of items</param>
        /// <param name="rhs">Second list of items</param>
        /// <param name="metric">The metric to use</param>
        /// <returns>A similarity matrix</returns>
        public static SimilarityMatrix<T> Compare(Dictionary<string, T> itemLookup, List<string> lhs, List<string> rhs, SimilarityMetric<T> metric)
        {
            if (lhs.Count == 0 || rhs.Count == 0 || itemLookup.Count == 0)
                return new SimilarityMatrix<T>(new float[1, 1]);
            T[] items0 = (from id in lhs
                          select itemLookup[id]).ToArray();
            T[] items1 = (from id in rhs
                          select itemLookup[id]).ToArray();
            return Compare(items0, items1, metric);
        }

        /// <summary>
        /// Translates a similarity matrix into a two dimension float array.
        /// </summary>
        /// <param name="matrix">Matrix to convert</param>
        /// <returns>A float array</returns>
        public static implicit operator float[,](SimilarityMatrix<T> matrix)
        {
            return matrix._similarities;
        }
    }
}
