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

namespace VisionNET.Learning
{
    /// <summary>
    /// Implementation of a KD Tree.
    /// </summary>
    /// <typeparam name="T">The data point type</typeparam>
    public class KDTree<T> where T:IDataPoint<float[]>
    {
        private class KDNode
        {
            private float _threshold;

            public float Threshold
            {
                get { return _threshold; }
                set { _threshold = value; }
            }
            private int _dimension;

            public int Dimension
            {
                get { return _dimension; }
                set { _dimension = value; }
            }
            private KDNode _left;

            public KDNode Left
            {
                get { return _left; }
                set
                {
                    _left = value;
                    minMaxFromChild(_left);
                }
            }
            private KDNode _right;

            public KDNode Right
            {
                get { return _right; }
                set
                {
                    _right = value;
                    minMaxFromChild(_right);
                }
            }

            private List<T> _members;

            public List<T> Members
            {
                get { return _members; }
                set
                {
                    _members = value;
                    minMaxFromData();
                }
            }

            private NodeType _nodeType;

            public NodeType NodeType
            {
                get { return _nodeType; }
                set { _nodeType = value; }
            }

            public float[] NearestPoint(float[] query)
            {
                return _bounds.NearestPoint(query);
            }

            public T NearestMember(float[] query, ref float distance)
            {
                if (_members == null || _members.Count == 0)
                    return default(T);

                T nearest = _members[0];
                distance = KDTree<T>.metric(query, nearest.Data, distance);
                for (int i = 1; i < _members.Count; i++)
                {
                    float test = KDTree<T>.metric(query, _members[i].Data, distance);
                    if (test < distance)
                    {
                        distance = test;
                        nearest = _members[i];
                    }
                }
                return nearest;
            }

            private Hyperrectangle<float> _bounds;

            private void initializeMinMax(int length)
            {
                _bounds = new Hyperrectangle<float>(length, float.MaxValue, float.MinValue);
            }

            private void minMaxFromData()
            {
                if(_members == null || _members.Count == 0)
                    return;

                initializeMinMax(_members[0].Data.Length);

                foreach (T member in _members)
                    _bounds.Add(member.Data);
            }

            private void minMaxFromChild(KDNode child)
            {
                if (child == null)
                    return;
                if (_bounds == null)
                    _bounds = child._bounds.Clone() as Hyperrectangle<float>;
                else _bounds += child._bounds;
            }
        }

        private class PriorityQueue
        {
            private SortedDictionary<float, KDNode> _nodes;

            public PriorityQueue()
            {
                _nodes = new SortedDictionary<float, KDNode>();
            }

            public int Count
            {
                get
                {
                    return _nodes.Count;
                }
            }

            public void Insert(float distance, KDNode node)
            {
                _nodes[distance] = node;
            }

            public KDNode Remove()
            {
                var result = _nodes.First();
                _nodes.Remove(result.Key);
                return result.Value;
            }

            public float MinimumDistance
            {
                get
                {
                    if (_nodes.Any())
                        return _nodes.Keys.Min();
                    else return float.MaxValue;
                }
            }
        }

        /// <summary>
        /// Default number of metric tests to perform.
        /// </summary>
        public const int DEFAULT_LIMIT = 400;

        private KDNode _root;

        private KDTree(KDNode root)
        {
            _root = root;
            _limit = DEFAULT_LIMIT;
        }

        private int _limit;

        /// <summary>
        /// Maximum number of comparisons to try before stopping the branch and bound search.
        /// </summary>
        public int Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Finds the nearest neighbor to the query in the tree using a best bin first branching and bounding search.
        /// </summary>
        /// <param name="query">The query point</param>
        /// <param name="resultsCount">The number of results to return</param>
        /// <returns>The N nearest neighbors, where N is equal to <paramref name="resultsCount"/></returns>
        public T[] FindNearest(float[] query, int resultsCount)
        {
            PriorityQueue queue = new PriorityQueue();

            float minDist = float.MaxValue;
            T nearest = delve(_root, query, queue, ref minDist);
            nearest.FeatureValue = minDist;

            if (_limit < 0)
                return new T[]{nearest};

            int count = _limit;
            float test = 0;

            List<T> topResults = new List<T>();
            topResults.Add(nearest);

            while (queue.MinimumDistance < minDist)
            {
                if (_limit != 0 && count == 0)
                    break;

                KDNode node = queue.Remove();
                if (node.NodeType == NodeType.Leaf)
                {
                    test = float.MaxValue;
                    T nearestMember = node.NearestMember(query, ref test);
                    if (test < minDist)
                    {
                        nearestMember.FeatureValue = test;
                        int index = 0;
                        for (index = 0; index < topResults.Count; index++)
                        {
                            if (test < topResults[index].FeatureValue)
                                break;
                        }
                        topResults.Insert(index, nearestMember);
                        index++;
                        if (index == resultsCount || (index < resultsCount && index == topResults.Count))
                            minDist = test;
                    }
                    count -= node.Members.Count;
                }
                else
                {
                    float dist = metric(node.Left.NearestPoint(query), query, minDist);
                    queue.Insert(dist, node.Left);
                    dist = metric(node.Right.NearestPoint(query), query, minDist);
                    queue.Insert(dist, node.Right);
                    count -= 2;
                }
            }           

            topResults = topResults.Take(resultsCount).ToList();
            foreach (var result in topResults)
                result.FeatureValue = (float)Math.Sqrt(result.FeatureValue);

            return topResults.ToArray();
        }

        private static T delve(KDNode node, float[] query, PriorityQueue queue, ref float distance)
        {
            if (node.NodeType == NodeType.Leaf)
                return node.NearestMember(query, ref distance);

            if (query[node.Dimension] < node.Threshold)
            {
                float[] nearest = node.Right.NearestPoint(query);
                float dist = metric(query, nearest);
                queue.Insert(dist, node.Right);
                return delve(node.Left, query, queue, ref distance);
            }
            else
            {
                float[] nearest = node.Left.NearestPoint(query);
                float dist = metric(query, nearest);
                queue.Insert(dist, node.Left);
                return delve(node.Right, query, queue, ref distance);
            }
        }

        /// <summary>
        /// Computes a KD Tree from the data using the provided maximum leaf size.
        /// </summary>
        /// <param name="data">The data to use when constructing the tree</param>
        /// <param name="leafSize">The maximum number of points in a leaf</param>
        /// <returns>A KD Tree</returns>
        public static KDTree<T> Compute(List<T> data, int leafSize)
        {
            List<T> copy = data.Select(o => (T)o.Clone()).Distinct().ToList();
            return new KDTree<T>(split(copy, leafSize));
        }

        /// <summary>
        /// Computes a KD Tree from the data with a maximum leaf size of 1.
        /// </summary>
        /// <param name="data">The data to use when constructing the tree</param>
        /// <returns>a KD Tree</returns>
        public static KDTree<T> Compute(List<T> data)
        {
            return Compute(data, 1);
        }

        private static float metric(float[] lhs_array, float[] rhs_array, float minDistance)
        {
            int i;
            float dx, distance = 0;
            int count = lhs_array.Length;
            if (rhs_array.Length != count)
                throw new Exception("Unequal array lengths");
            for (i = 0; i < count && distance < minDistance; i++)
            {
                dx = lhs_array[i] - rhs_array[i];
                distance += dx*dx;
            }

            if (i < count)
                return float.MaxValue;

            return distance;
        }

        private static float metric(float[] lhs_array, float[] rhs_array)
        {
            int i;
            float dx, distance = 0;
            int count = lhs_array.Length;
            if (rhs_array.Length != count)
                throw new Exception("Unequal array lengths");
            for (i = 0; i < count; i++)
            {
                dx = lhs_array[i] - rhs_array[i];
                distance += dx*dx;
            }

            return distance;
        }

        private static KDNode split(List<T> data, int leafSize)
        {
            if (data.Count == 0)
                return null;
            if (data.Count <= leafSize)
                return new KDNode { Members = data, NodeType = NodeType.Leaf };

            // step 1: compute variance for each dimension
            int size = data[0].Data.Length;
            int count = data.Count;
            float[] sums = new float[size];
            float[] sumSquareds = new float[size];
            foreach (T point in data)
            {
                float[] feature = point.Data;
                for (int i = 0; i < size; i++)
                {
                    float val = feature[i];
                    sums[i] += val;
                    sumSquareds[i] += val * val;
                }
            }
            // step 2: choose a dimension
            int maxDimension = 0;
            float mean = 0;
            float maxVariance = 0;
            for (int i = 0; i < size; i++)
            {
                float variance = (sumSquareds[i] - ((sums[i] * sums[i]) / count)) / count;
                if (variance > maxVariance)
                {
                    maxDimension = i;
                    maxVariance = variance;
                    mean = sums[i] / count;
                }
            }

            if (maxVariance == 0)
                throw new Exception("Error condition: identical points in dataset");

            // step 3: split along that dimension into two groups
            List<T> left = new List<T>();
            List<T> right = new List<T>();
            foreach (T point in data)
            {
                if (point.Data[maxDimension] < mean)
                    left.Add(point);
                else right.Add(point);
            }
            return new KDNode { 
                Dimension = maxDimension, 
                Threshold = mean, 
                Left = split(left, leafSize), 
                Right = split(right, leafSize), 
                NodeType = NodeType.Branch 
            };
        }
    }
}
