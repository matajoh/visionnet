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
using System.IO;

namespace VisionNET.Comparison
{
    /// <summary>
    /// A tree histogram is a hierarchical structure, in which each node "contains" in some way the values of the subtree for which it is the root.  Thus, each level
    /// is a histogram which desribes the same entity but with a different granularity.  This particular implementation does not store the structure, but only a list
    /// of nodes which store within themselves their location within the tree.  The reason for this is that this is the more memory-efficient way of storing trees for which
    /// the majority of the bins will be zero, and in which it is necessary to store several trees at once.  The unfortunate result of this is that the onus is on the user
    /// to correctly create the nodes.
    /// </summary>
    [Serializable]
    public class TreeHistogram : IEnumerable<TreeNode>
    {
        internal List<TreeNode> _nodes;
        private string _id;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TreeHistogram()
        {
            _nodes = new List<TreeNode>();
        }
        /// <summary>
        /// Constructs this histogram from <paramref name="nodes"/> and marks it with <paramref name="id"/>.
        /// </summary>
        /// <param name="nodes">The nodes to use for building the histogram.  Must already be populated with their location information.</param>
        /// <param name="id">The ID of this histogram</param>
        public TreeHistogram(List<TreeNode> nodes, string id)
        {
            _nodes = nodes;
            _nodes.Sort();
            _id = id;
        }
        /// <summary>
        /// Constructs this histogram from <paramref name="nodes" />.
        /// </summary>
        /// <param name="nodes">The nodes to use for building the histogram.  Must already be populated with their location information.</param>
        public TreeHistogram(List<TreeNode> nodes)
            : this(nodes, null)
        {
        }

        /// <summary>
        /// ID of this histogram.
        /// </summary>
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        /// <summary>
        /// Indexes the histogram.  Uses a binary search to find the appropriate node, and thus should not be used in computationally intensive circumstances.
        /// </summary>
        /// <param name="tree">Tree of the node</param>
        /// <param name="index">Index of the node</param>
        /// <returns>The value of the node within the tree</returns>
        public float this[byte tree, int index]
        {
            get
            {
                TreeNode query = new TreeNode(tree, 0, 0, index);
                int find = _nodes.BinarySearch(query);
                if (find < 0)
                    return 0;
                return _nodes[find].Value;
            }
            set
            {
                TreeNode query = new TreeNode(tree,0,0,index);
                int find = _nodes.BinarySearch(query);
                if (find < 0)
                    throw new ArgumentException("Histogram does not contain specified bin");
                TreeNode current = _nodes[find];
                _nodes[find] = new TreeNode(current, value);
            }
        }

        /// <summary>
        /// The number of nodes in the tree.
        /// </summary>
        public int Count
        {
            get
            {
                return _nodes.Count;
            }
        }

        /// <summary>
        /// Whether this node is already contained within the tree.
        /// </summary>
        /// <param name="node">The node to search for</param>
        /// <returns>Whether this node is present</returns>
        public bool Contains(TreeNode node)
        {
            return _nodes.BinarySearch(node) >= 0;
        }

        /// <summary>
        /// Divides the all nodes in <paramref name="hist"/> by <paramref name="value"/>.  Does not alter the arguments.
        /// </summary>
        /// <param name="hist">Histogram to divide</param>
        /// <param name="value">Divisor</param>
        /// <returns>The resulting histogram</returns>
        public static TreeHistogram Divide(TreeHistogram hist, float value)
        {
            List<TreeNode> nodes = new List<TreeNode>();
            foreach (TreeNode node in hist)
                nodes.Add(new TreeNode(node, node.Value / value));
            return new TreeHistogram(nodes);
        }

        /// <summary>
        /// Creates a union of two TreeHistograms.
        /// </summary>
        /// <param name="lhs">Histogram to union</param>
        /// <param name="rhs">Second histogram</param>
        /// <returns>Union of both histograms</returns>
        public static TreeHistogram Union(TreeHistogram lhs, TreeHistogram rhs)
        {
            int count0 = lhs._nodes.Count;
            int count1 = rhs._nodes.Count;
            int index0 = 0;
            int index1 = 0;
            List<TreeNode> nodes = new List<TreeNode>();
            while (index0 < count0 && index1 < count1)
            {
                TreeNode node0 = lhs._nodes[index0];
                TreeNode node1 = rhs._nodes[index1];
                if (node0.Tree < node1.Tree)
                {
                    nodes.Add(node0);
                    index0++;
                }
                else if (node1.Tree < node0.Tree)
                {
                    nodes.Add(node1);
                    index1++;
                }
                else if (node0.Index < node1.Index)
                {
                    nodes.Add(node0);
                    index0++;
                }
                else if (node1.Index < node0.Index)
                {
                    nodes.Add(node1);
                    index1++;
                }
                else
                {
                    nodes.Add(new TreeNode(node0, node0.Value + node1.Value));
                    index0++;
                    index1++;
                }
            }
            while(index0 < count0)
                nodes.Add(lhs._nodes[index0++]);
            while (index1 < count1)
                nodes.Add(rhs._nodes[index1++]);
            return new TreeHistogram(nodes);
        }

        /// <summary>
        /// Subtracts the values in one histogram from another.
        /// </summary>
        /// <param name="lhs">A histogram</param>
        /// <param name="rhs">The histogram to subtract</param>
        /// <returns>The difference of the two histograms</returns>
        public static TreeHistogram Subtract(TreeHistogram lhs, TreeHistogram rhs)
        {
            int count0 = lhs._nodes.Count;
            int count1 = rhs._nodes.Count;
            int index0 = 0;
            int index1 = 0;
            List<TreeNode> nodes = new List<TreeNode>();
            while (index0 < count0 && index1 < count1)
            {
                TreeNode node0 = lhs._nodes[index0];
                TreeNode node1 = rhs._nodes[index1];
                if (node0.Tree < node1.Tree)
                {
                    nodes.Add(node0);
                    index0++;
                }
                else if (node1.Tree < node0.Tree)
                {
                    nodes.Add(new TreeNode(node1, -node1.Value));
                    index1++;
                }
                else if (node0.Index < node1.Index)
                {
                    nodes.Add(node0);
                    index0++;
                }
                else if (node1.Index < node0.Index)
                {
                    nodes.Add(new TreeNode(node1, -node1.Value));
                    index1++;
                }
                else
                {
                    nodes.Add(new TreeNode(node0, node0.Value - node1.Value));
                    index0++;
                    index1++;
                }
            }
            while (index0 < count0)
                nodes.Add(lhs._nodes[index0++]);
            while (index1 < count1)
            {
                TreeNode node1 = rhs._nodes[index1++];
                nodes.Add(new TreeNode(node1, -node1.Value));
            }
            return new TreeHistogram(nodes);
        }

        /// <summary>
        /// Intersects two histograms.
        /// </summary>
        /// <param name="lhs">Histogram</param>
        /// <param name="rhs">The second histogram</param>
        /// <returns>The histogram intersection of <paramref name="lhs"/> and <paramref name="rhs"/></returns>
        public static TreeHistogram Intersect(TreeHistogram lhs, TreeHistogram rhs)
        {
            int count0 = lhs._nodes.Count;
            int count1 = rhs._nodes.Count;
            int index0 = 0;
            int index1 = 0;
            List<TreeNode> nodes = new List<TreeNode>();
            while (index0 < count0 && index1 < count1)
            {
                TreeNode node0 = lhs._nodes[index0];
                TreeNode node1 = rhs._nodes[index1];
                if (node0.Tree < node1.Tree)
                {
                    index0++;
                }
                else if (node1.Tree < node0.Tree)
                {
                    index1++;
                }
                else if (node0.Index < node1.Index)
                {
                    index0++;
                }
                else if (node1.Index < node0.Index)
                {
                    index1++;
                }
                else
                {
                    nodes.Add(new TreeNode(node0, Math.Min(node0.Value, node1.Value)));
                    index0++;
                    index1++;
                }
            }
            return new TreeHistogram(nodes);
        }

        /// <summary>
        /// Writes <paramref name="hist"/> to <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream to use for writing the histogram</param>
        /// <param name="hist">Histogram to write</param>
        public static void Write(Stream stream, TreeHistogram hist)
        {
            BinaryWriter output = new BinaryWriter(stream);
            List<TreeNode> nodes = hist._nodes;
            output.Write(hist._id);
            output.Write(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                TreeNode node = nodes[i];
                output.Write(node.Tree);
                output.Write(node.Index);
                output.Write(node.LeafIndex);
                output.Write(node.Value);
            }
        }

        /// <summary>
        /// Reads a histogram from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <returns>The stored histogram</returns>
        public static TreeHistogram Read(Stream stream)
        {
            BinaryReader input = new BinaryReader(stream);
            List<TreeNode> nodes = new List<TreeNode>();
            string id = input.ReadString();
            int count = input.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                byte tree = input.ReadByte();
                int index = input.ReadInt32();
                int leafIndex = input.ReadInt32();
                float value = input.ReadSingle();
                nodes.Add(new TreeNode(tree, value, leafIndex, index));
            }
            return new TreeHistogram(nodes, id);
        }

        /// <summary>
        /// Returns an enumerator for this histogram.
        /// </summary>
        /// <returns>An enumerator</returns>
        public IEnumerator<TreeNode> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator for this histogram.
        /// </summary>
        /// <returns>An enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }
    }
}
