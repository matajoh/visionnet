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
using System.Linq;

namespace VisionNET.Comparison
{
    /// <summary>
    /// Encapsulates the radial basis function acting upon the leaf nodes of a <see cref="TreeHistogram"/>.
    /// </summary>
    public class RBF
    {
        private double _gamma;

        /// <summary>
        /// Gamma of the radial basis function.
        /// </summary>
        public double Gamma
        {
            get
            {
                return _gamma;
            }
            set
            {
                _gamma = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="gamma">Gamma to use in the radial basis function.</param>
        public RBF(float gamma)
        {
            _gamma = gamma;
        }

        /// <summary>
        /// Computes the similarity between <paramref name="lhs"/> and <paramref name="rhs"/> using
        /// a radial basis fuction computed on their leaf nodes.
        /// </summary>
        /// <param name="lhs">The first histogram</param>
        /// <param name="rhs">The second histogram</param>
        /// <returns>A value between 0 and 1 indicating the similarity between these histograms</returns>
        public float Similarity(TreeHistogram lhs, TreeHistogram rhs)
        {
            return (float)Math.Exp(-_gamma * (dot(lhs, lhs) + dot(rhs, rhs) - 2 * dot(lhs, rhs)));
        }

        private static float dot(TreeHistogram xHist, TreeHistogram yHist)
        {
            float sum = 0;
            TreeNode[] x = (from node in xHist
                            where node.LeafIndex >= 0
                            select node).ToArray();
            TreeNode[] y = (from node in yHist
                            where node.LeafIndex >= 0
                            select node).ToArray();
            int xlen = x.Length;
            int ylen = y.Length;
            int i = 0;
            int j = 0;
            while (i < xlen && j < ylen)
            {
                TreeNode lhs = x[i];
                TreeNode rhs = y[j];
                if (lhs.LeafIndex < rhs.LeafIndex)
                    i++;
                else if (rhs.LeafIndex < lhs.LeafIndex)
                    j++;
                else
                {
                    sum += lhs.Value * rhs.Value;
                    i++;
                    j++;
                }                
            }
            return sum;
        }
    }
}
