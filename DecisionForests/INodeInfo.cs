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


using System.Collections.Generic;
using VisionNET.Learning;
namespace VisionNET.DecisionForests
{
    /// <summary>
    /// Provides debugging information about a feature.
    /// </summary>
    /// <typeparam name="T">A type of data point</typeparam>
    /// <typeparam name="D">The underlying type of the data point type</typeparam>
    public interface ITestInfo<T, D> : IFeature<T, D> where T : IDataPoint<D>
    {
        /// <summary>
        /// The threshold used for a feature.
        /// </summary>
        float Threshold { get; }
    }

    /// <remarks>
    /// Provides debugging information about a node.
    /// </remarks>
    /// <typeparam name="T">A type of data point</typeparam>
    /// <typeparam name="D">The underlying type of the data point type</typeparam>
    public interface INodeInfo<T, D> where T : IDataPoint<D>
    {
        /// <summary>
        /// Amount of training data which arrived at this node.
        /// </summary>
        float TrainingDataCount { get; }
        /// <summary>
        /// Type of the node.
        /// </summary>
        NodeType NodeType { get; }
        /// <summary>
        /// Class distribution at this node, computed from the training data.
        /// </summary>
        float[] Distribution { get; }
        /// <summary>
        /// Leaf node index of the node.
        /// </summary>
        int LeafNodeIndex { get; }
        /// <summary>
        /// Level of the node.
        /// </summary>
        byte Level { get; }
        /// <summary>
        /// Tree the node belongs to.
        /// </summary>
        byte Tree { get; }
        /// <summary>
        /// Index within the level.
        /// </summary>
        int LevelIndex { get; }
        /// <summary>
        /// Code which, when compared to other codes in this tree, reflects the number of branches they share in Euclidean space.
        /// </summary>
        int SparseCode { get; }
        /// <summary>
        /// Entropy of the distribution at this node.
        /// </summary>
        float Entropy { get; }
        /// <summary>
        /// Index of the node within the tree.
        /// </summary>
        int TreeIndex { get; }
        /// <summary>
        /// Debugging information about the node's feature test. 
        /// </summary>
        ITestInfo<T,D> TestInfo { get; }
    }
}
