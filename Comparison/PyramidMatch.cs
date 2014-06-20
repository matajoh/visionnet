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
using System.Diagnostics;
using System.IO;

namespace VisionNET.Comparison
{
    /// <summary>
    /// Encapsulates the pyramid match similarity metric, comparing two <see cref="TreeHistogram"/> objects using all of their nodes with
    /// using histogram intersection and a weighted combination of level histograms.  See <see href="http://www.cs.utexas.edu/~grauman/research/projects/pmk/pmk_projectpage.htm">Grauman and Darrell</see>.
    /// for details.
    /// </summary>
    [Serializable]
    public class PyramidMatch
    {
        private int _numTrees;
        private int _numLevels;
        private bool _normalized;

        private static float[,] _XX;
        private static float[,] _YY;
        private static float[,] _XY;
        private static int _sNumTrees;
        private static int _sNumLevels;

        private static void setDimensions(int numTrees, int numLevels)
        {
            if (_sNumTrees != numTrees || _sNumLevels != numLevels)
            {
                _XX = new float[numTrees, numLevels];
                _XY = new float[numTrees, numLevels];
                _YY = new float[numTrees, numLevels];
                _sNumTrees = numTrees;
                _sNumLevels = numLevels;
            }
        }

        /// <summary>
        /// Returns whether the histograms being passed to the Similarity method are assumed to be normalized.
        /// </summary>
        public bool HistogramsAreNormalized
        {
          get { return _normalized; }
          set { _normalized = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="numTrees">Number of trees in the histograms to be matched</param>
        /// <param name="numLevels">Number of levels in the histograms to be matched</param>
        /// <param name="normalized">Whether the histograms to be matched are normalized</param>
        public PyramidMatch(int numTrees, int numLevels, bool normalized)
        {
            _numTrees = numTrees;
            _numLevels = numLevels;
            _normalized = normalized;
        }

        /// <summary>
        /// Number of trees in the histograms to be matched.
        /// </summary>
        public int TreeCount
        {
            get
            {
                return _numTrees;
            }
            set
            {
                _numTrees = value;
            }
        }

        /// <summary>
        /// Number of levels in the histograms to be matched.
        /// </summary>
        public int LevelCount
        {
            get
            {
                return _numLevels;
            }
            set
            {
                _numLevels = value;
            }
        }

        /// <summary>
        /// Computes the similarity between two <see cref="TreeHistogram"/> objects using the pyramid match method of <see href="http://www.cs.utexas.edu/~grauman/research/projects/pmk/pmk_projectpage.htm">Grauman and Darrell</see>.
        /// </summary>
        /// <param name="lhs">First histogram</param>
        /// <param name="rhs">Second histogram</param>
        /// <returns>A value between 0 and 1 indicating the similarity of the two histograms</returns>
        public float Similarity(TreeHistogram lhs, TreeHistogram rhs)
        {
            setDimensions(_numTrees, _numLevels);
            if (_normalized)
                return matchNormalized(lhs._nodes.ToArray(), rhs._nodes.ToArray());
            return match(lhs._nodes.ToArray(), rhs._nodes.ToArray());
        }

        /// <summary>
        /// Reads a PyramidMatch object from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream containing the encoded PyramidMatch object</param>
        /// <returns>A PyramidMatch object</returns>
        public static PyramidMatch Read(Stream stream)
        {
            BinaryReader input = new BinaryReader(stream);
            int numTrees = input.ReadInt32();
            int numLevels = input.ReadInt32();
            bool normalized = input.ReadBoolean();
            return new PyramidMatch(numTrees, numLevels, normalized);        
        }

        /// <summary>
        /// Writes <paramref name="match"/> to <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="match">The object to write</param>
        public static void Write(Stream stream, PyramidMatch match)
        {
            BinaryWriter output = new BinaryWriter(stream);
            output.Write(match.TreeCount);
            output.Write(match.LevelCount);
            output.Write(match._normalized);
        }

        private static float match(TreeNode[] x, TreeNode[] y)
        {
            int xIndex, yIndex;
            xIndex = yIndex = 0;
            zero();
            TreeNode xNode, yNode;
            while (xIndex < x.Length && yIndex < y.Length)
            {
                xNode = x[xIndex];
                yNode = y[yIndex];

                if (xNode.Tree < yNode.Tree)
                {
                    _XX[xNode.Tree, xNode.Level] += xNode.Value;
                    xIndex++;
                }
                else if (yNode.Tree < xNode.Tree)
                {
                    _YY[yNode.Tree, yNode.Level] += yNode.Value;
                    yIndex++;
                }
                else if (xNode.Level < yNode.Level)
                {
                    _XX[xNode.Tree, xNode.Level] += xNode.Value;
                    xIndex++;
                }
                else if (yNode.Level < xNode.Level)
                {
                    _YY[yNode.Tree, yNode.Level] += yNode.Value;
                    yIndex++;
                }
                else if (xNode.Bin < yNode.Bin)
                {
                    _XX[xNode.Tree, xNode.Level] += xNode.Value;
                    xIndex++;
                }
                else if (yNode.Bin < xNode.Bin)
                {
                    _YY[yNode.Tree, yNode.Level] += yNode.Value;
                    yIndex++;
                }
                else
                {
                    Debug.Assert(
                        xNode.Tree == yNode.Tree &&
                        xNode.Level == yNode.Level &&
                        xNode.Bin == yNode.Bin,
                        string.Format("x({0}, {1}, {2}) != y({3}, {4}, {5})",
                        xNode.Tree, xNode.Level, xNode.Bin,
                        yNode.Tree, yNode.Level, yNode.Bin)
                    );
                    float xVal = xNode.Value;
                    float yVal = yNode.Value;
                    _XX[xNode.Tree, xNode.Level] += xVal;
                    _YY[yNode.Tree, yNode.Level] += yVal;
                    _XY[xNode.Tree, xNode.Level] += Math.Min(xVal, yVal);
                    xIndex++;
                    yIndex++;
                }
            }
            while (xIndex < x.Length)
            {
                xNode = x[xIndex];
                _XX[xNode.Tree, xNode.Level] += xNode.Value;
                xIndex++;
            }
            while (yIndex < y.Length)
            {
                yNode = y[yIndex];
                _YY[yNode.Tree, yNode.Level] += yNode.Value;
                yIndex++;
            }

            float match = 0;
            for (int i = 0; i < _sNumTrees; i++)
            {
                float XXscore = computeScore(_XX, i, _sNumLevels);
                float YYscore = computeScore(_YY, i, _sNumLevels);
                float KdeltXYtilde = computeScore(_XY, i, _sNumLevels);
                // Avoid bias by large input sets
                float C = (float)(1 / Math.Sqrt(XXscore * YYscore));
                match += C * KdeltXYtilde;
            }

            return match/_sNumTrees;
        }

        private static void zero()
        {
            for(int t=0; t<_sNumTrees; t++)
                for (int l = 0; l < _sNumLevels; l++)
                {
                    _XX[t, l] = 0;
                    _YY[t, l] = 0;
                    _XY[t, l] = 0;
                }
        }

        private static float matchNormalized(TreeNode[] x, TreeNode[] y)
        {
            int xIndex, yIndex;
            xIndex = yIndex = 0;
            zero();
            TreeNode xNode;
            TreeNode yNode;
            while (xIndex < x.Length && yIndex < y.Length)
            {
                xNode = x[xIndex];
                yNode = y[yIndex];

                if (xNode.Tree < yNode.Tree)
                {
                    xIndex++;
                }
                else if (yNode.Tree < xNode.Tree)
                {
                    yIndex++;
                }
                else if (xNode.Level < yNode.Level)
                {
                    xIndex++;
                }
                else if (yNode.Level < xNode.Level)
                {
                    yIndex++;
                }
                else if (xNode.Bin < yNode.Bin)
                {
                    xIndex++;
                }
                else if (yNode.Bin < xNode.Bin)
                {
                    yIndex++;
                }
                else
                {
                    Debug.Assert(
                        xNode.Tree == yNode.Tree &&
                        xNode.Level == yNode.Level &&
                        xNode.Bin == yNode.Bin,
                        string.Format("x({0}, {1}, {2}) != y({3}, {4}, {5})",
                        xNode.Tree, xNode.Level, xNode.Bin,
                        yNode.Tree, yNode.Level, yNode.Bin)
                    );
                    float xVal = xNode.Value;
                    float yVal = yNode.Value;
                    _XY[xNode.Tree, xNode.Level] += Math.Min(xVal, yVal);
                    xIndex++;
                    yIndex++;
                }
            }

            float match = 0;
            for (int i = 0; i < _sNumTrees; i++)
            {
                float KdeltXYtilde = computeScore(_XY, i, _sNumLevels);
                match += KdeltXYtilde;
            }

            return match / _sNumTrees;
        }


        private static float computeScore(float[,] histograms, int tree, int numLevels)
        {
            double KdeltTilde = histograms[tree,numLevels - 1];
            for (int level = numLevels - 2; level >= 0; level--)
            {
                KdeltTilde += (histograms[tree,level] - histograms[tree,level + 1]) / pow2(numLevels - level - 1);
            }
            return (float)KdeltTilde;
        }

        private static int pow2(int power)
        {
            return 1 << power;
        }
    }
}
