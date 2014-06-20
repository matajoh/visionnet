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
using System.Text;
using System.IO;
using System.Linq;

namespace VisionNET.Learning
{
    /// <summary>
    /// Encapsulates a confusion matrix.
    /// </summary>
    public class ConfusionMatrix
    {
        private List<int> _ignoredLabels;
        private int _size;
        private bool _changed;
        private float _overallAccuracy;
        private float _averageAccuracy;
        private float[,] _matrix;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="numLabels">Number of labels in the matrix</param>
        public ConfusionMatrix(int numLabels)
        {
            _ignoredLabels = new List<int>();
            _matrix = new float[numLabels, numLabels];
            _size = numLabels;
        }

        private ConfusionMatrix(float[,] matrix)
        {
            _size = matrix.GetLength(0);
            _matrix = matrix;
            _ignoredLabels = new List<int>();
        }

        /// <summary>
        /// Ignores a particular label in the matrix when computing the accuracies.
        /// </summary>
        /// <param name="label"></param>
        public void IgnoreLabel(int label)
        {
            _ignoredLabels.Add(label);
            _changed = true;
        }

        /// <summary>
        /// Clears ignored labels.
        /// </summary>
        public void ClearIgnoredLabels()
        {
            _ignoredLabels.Clear();
        }

        /// <summary>
        /// Adds a value to the matrix.
        /// </summary>
        /// <param name="trueCategory">The true category</param>
        /// <param name="inferredCategory">The inferred category</param>
        /// <param name="certainty">Certainty of the inferred label</param>
        public void Add(int trueCategory, int inferredCategory, float certainty)
        {
            _matrix[trueCategory, inferredCategory] += certainty;
            _changed = true;
        }

        /// <summary>
        /// Adds a value to the matrix.
        /// </summary>
        /// <param name="trueCategory">The true category</param>
        /// <param name="inferredCategory">The inferred category</param>
        public void Add(int trueCategory, int inferredCategory)
        {
            Add(trueCategory, inferredCategory, 1);
        }

        /// <summary>
        /// Computes the sum for a row.
        /// </summary>
        /// <param name="row">The row to sum</param>
        /// <returns>The row sum</returns>
        public float ComputeRowSum(int row)
        {
            return (from column in Enumerable.Range(0, _size)
                    select _matrix[row, column]).Sum();
        }

        /// <summary>
        /// Indexes the confusion matrix.
        /// </summary>
        /// <param name="trueCategory">True category (the row)</param>
        /// <param name="inferredCategory">Inferred category (the column)</param>
        /// <returns>The value</returns>
        public float this[int trueCategory, int inferredCategory]
        {
            get
            {
                return _matrix[trueCategory, inferredCategory];
            }
        }

        /// <summary>
        /// Number of labels in the matrix.
        /// </summary>
        public int LabelCount
        {
            get
            {
                return _size;
            }
        }

        private void computeAccuracies()
        {
            float[] rowSum = new float[_size];
            for (int i = 0; i < _size; i++)
            {
                if (_ignoredLabels.Contains(i))
                    continue;
                for (int j = 0; j < _size; j++)
                {
                    if (_ignoredLabels.Contains(j))
                        continue;
                    rowSum[i] += _matrix[i, j];
                }
            }
            float sum = 0;
            for (int i = 0; i < _size; i++)
            {
                if (_ignoredLabels.Contains(i))
                    continue;
                sum += rowSum[i];
            }
            _overallAccuracy = 0;
            _averageAccuracy = 0;
            for(int i=0; i<_size; i++){
                if (_ignoredLabels.Contains(i))
                    continue;
                _overallAccuracy += _matrix[i,i];
                float divisor = Math.Max(1, rowSum[i]);
                _averageAccuracy += _matrix[i,i]/divisor;
            }
            _overallAccuracy /= sum;
            _averageAccuracy /= (_size-_ignoredLabels.Count);
            _changed = false;
        }

        /// <summary>
        /// The overall accuracy of the matrix, computed as the total number of correct pixels divided by the total number of pixels, for all labels.
        /// </summary>
        public float OverallAccuracy
        {
            get
            {
                if(_changed)
                    computeAccuracies();
                return _overallAccuracy;
            }
        }

        /// <summary>
        /// The average accuracy of the matrix, computed as the average of the category accuracies.
        /// </summary>
        public float AverageAccuracy
        {
            get
            {
                if (_changed)
                    computeAccuracies();
                return _averageAccuracy;
            }
        }

        /// <summary>
        /// Writes <paramref name="matrix"/> to <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="matrix">The matrix to write</param>
        public static void Write(Stream stream, ConfusionMatrix matrix)
        {
            StreamWriter output = new StreamWriter(stream);
            output.WriteLine("{0} {1}", matrix.OverallAccuracy, matrix.AverageAccuracy);
            for (int r = 0; r < matrix._size; r++)
            {
                output.Write("{0}", matrix._matrix[r, 0]);
                for (int c = 1; c < matrix._size; c++)
                {
                    output.Write("\t{0}", matrix._matrix[r, c]);
                }
                output.WriteLine();
            }
            output.Flush();
        }

        /// <summary>
        /// Reads a matrix from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <returns>The matrix</returns>
        public static ConfusionMatrix Read(Stream stream)
        {
            StreamReader input = new StreamReader(stream);
            string[] parts = input.ReadLine().Split('\t');
            int size = parts.Length;
            float[,] matrix = new float[size, size];
            int r = 0;
            do
            {
                for (int c = 0; c < size; c++)
                    matrix[r, c] = float.Parse(parts[c]);
                parts = input.ReadLine().Split('\t');
                r++;
            } while (r < size);
            return new ConfusionMatrix(matrix);
        }
        
        /// <summary>
        /// Returns a string representation of the matrix.
        /// </summary>
        /// <returns>A string representation of the matrix</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _size; i++)
            {
                sb.Append(_matrix[i, 0]);
                for (int j = 1; j < _size; j++)
                    sb.AppendFormat("\t{0}", _matrix[i,j]);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
