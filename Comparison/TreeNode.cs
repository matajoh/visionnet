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

namespace VisionNET.Comparison
{
    /// <summary>
    /// A node in a tree histogram.
    /// </summary>
    [Serializable]
    public class TreeNode : IComparable<TreeNode>
    {
        /// <summary>
        /// The tree associated with this node.
        /// </summary>
        public byte Tree;
        /// <summary>
        /// The level of the node within the tree.
        /// </summary>
        public byte Level;
        /// <summary>
        /// The bin within the level associated with this node.
        /// </summary>
        public int Bin;
        /// <summary>
        /// The value stored at this node.
        /// </summary>
        public float Value;
        /// <summary>
        /// The leaf index of this node (if appropriate)
        /// </summary>
        public int LeafIndex;
        /// <summary>
        /// The index of this node within the tree.
        /// </summary>
        public int Index;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tree">The tree associated with this node</param>
        /// <param name="value">The value to store at this node</param>
        /// <param name="leafIndex">The leaf index of this node (if appropriate, otherwise -1)</param>
        /// <param name="index">The index of this node within the tree</param>
        public TreeNode(byte tree, float value, int leafIndex, int index)
        {
            Tree = tree;
            Level = Utilities.Log2(index);
            Bin = index-Utilities.Pow2(Level);
            Value = value;
            LeafIndex = leafIndex;
            Index = index;
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="example">Example to build this node from.</param>
        /// <param name="updatedValue">Updated value for the node.</param>
        public TreeNode(TreeNode example, float updatedValue)
        {
            Tree = example.Tree;
            Level = example.Level;
            Bin = example.Bin;
            Value = updatedValue;
            LeafIndex = example.LeafIndex;
            Index = example.Index;
        }

        /// <summary>
        /// Returns a string representation of the node in the form "Tree:Level:Bin:Value"
        /// </summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}:{2}:{3}", Tree, Level, Bin, Value);
        }

        /// <summary>
        /// Compares this node with another using <see cref="Tree" /> and <see cref="Index" />.
        /// </summary>
        /// <param name="other">Node to compare</param>
        /// <returns>A positive number if greater, a negative number if lesser, and zero if equal.</returns>
        public int CompareTo(TreeNode other)
        {
            if (Tree == other.Tree)
            {
                return Index.CompareTo(other.Index);
            }
            else return Tree.CompareTo(other.Tree);
        }
    }
}
