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
using VisionNET.DecisionForests;
using System.Diagnostics;

namespace VisionNET.Learning
{
    /// <summary>
    /// Implementation of a Clustering Tree, see Liu et al. "Clustering Via Decision Tree Construction".
    /// </summary>
    /// <typeparam name="T">Data point type</typeparam>
    [Serializable]
    public class CLTree<T> where T:IDataPoint<float[]>,new()
    {
        [Serializable]
        private class Threshold
        {
            private bool _isInclusive;

            public bool IsInclusive
            {
                get { return _isInclusive; }
                set { _isInclusive = value; }
            }
            private IFeature<T, float[]> _feature;

            public IFeature<T, float[]> Feature
            {
                get { return _feature; }
                set { _feature = value; }
            }

            private int _dimension;

            public int Dimension
            {
                get { return _dimension; }
                set { _dimension = value; }
            }


            private float _value;

            public float Value
            {
                get { return _value; }
                set { _value = value; }
            }

            public void ApplyFeature(List<T> points)
            {
                foreach (T point in points)
                    point.FeatureValue = _feature.Compute(point);
            }

            public bool IsLeft(float value)
            {
                if (_isInclusive)
                    return value <= _value;
                else return value < _value;
            }

            public bool IsLeft(T query)
            {
                return IsLeft(query.FeatureValue);
            }

            public override string ToString()
            {
                if (_isInclusive)
                    return string.Format("{0} <= {1}", _feature, _value);
                return string.Format("{0} < {1}", _feature, _value);
            }
        }

        private class Region
        {
            public override string ToString()
            {
                return string.Format("{0}{1},{2}{3} Y={4} N={5}", _includeLeft ? "[" : "(", _min, _max, _includeRight ? "]" : ")", _YCount, _NCount);
            }

            private int _YCount;

            public int YCount
            {
                get { return _YCount; }
                set { _YCount = value; }
            }
            private int _NCount;

            public int NCount
            {
                get { return _NCount; }
                set { _NCount = value; }
            }

            private float _min;

            public float Min
            {
                get { return _min; }
                set { _min = value; }
            }
            private float _max;

            public float Max
            {
                get { return _max; }
                set { _max = value; }
            }

            public float RelativeDensity
            {
                get
                {
                    return (float)_YCount / _NCount;
                }
            }

            private bool _includeLeft;

            public bool IncludeLeft
            {
                get { return _includeLeft; }
                set { _includeLeft = value; }
            }
            private bool _includeRight;

            public bool IncludeRight
            {
                get { return _includeRight; }
                set { _includeRight = value; }
            }

            public bool Includes(float value)
            {
                if (_includeLeft)
                {
                    if (_includeRight)
                        return value >= _min && value <= _max;
                    else return value >= _min && value < _max;
                }
                else
                {
                    if (_includeRight)
                        return value > _min && value <= _max;
                    else return value > _min && value < _max;
                }
            }

            public void CalculateCounts(float[] values, float min, float max, int NCount)
            {
                _YCount = 0;
                int startIndex;
                if (_min < values[0])
                {
                    startIndex = 0;
                    if (_includeLeft)
                        _YCount++;
                }
                else
                {
                    startIndex = Array.BinarySearch<float>(values, _min);
                    if (startIndex < 0)
                        startIndex = ~startIndex;
                    else if (_includeLeft)
                        _YCount++;
                }
                int endIndex;
                if (_max > values[values.Length - 1])
                {
                    endIndex = values.Length - 1;
                    if (_includeRight)
                        _YCount++;
                }
                else
                {
                    endIndex = Array.BinarySearch<float>(values, _max);
                    if (endIndex < 0)
                        endIndex = ~endIndex;
                    else if (_includeRight)
                        _YCount++;
                }
                _YCount += endIndex - startIndex - 1;
                float range = Max - Min;
                float totalRange = max - min;
                _NCount = Math.Max(_YCount, (int)((range * NCount) / totalRange));
            }

            public float[] Limit(float[] values)
            {
                int startIndex = Array.BinarySearch<float>(values, _min);
                if (startIndex < 0)
                    startIndex = ~startIndex;
                else if (!_includeLeft)
                    startIndex++;
                int endIndex = Array.BinarySearch<float>(values, _max);
                if (endIndex < 0)
                    endIndex = ~endIndex;
                else if (_includeRight)
                    endIndex++;
                float[] limit = new float[endIndex - startIndex];
                Array.Copy(values, startIndex, limit, 0, limit.Length);

                return limit;
            }
        }

        [Serializable]
        private class Node
        {
            private Hyperrectangle<float> _bounds;

            public Hyperrectangle<float> Bounds
            {
                get { return _bounds; }
                set { _bounds = value; }
            }
            private int _y;

            public int Y
            {
                get { return _y; }
                set { _y = value; }
            }
            private int _n;

            public int N
            {
                get { return _n; }
                set { _n = value; }
            }

            public float RelativeDensity
            {
                get{
                    return (float)_y/_n;
                }
            }

            private NodeType _nodeType;

            public NodeType NodeType
            {
                get { return _nodeType; }
                set { _nodeType = value; }
            }
            private Threshold _threshold;

            public Threshold Threshold
            {
                get { return _threshold; }
                set { _threshold = value; }
            }

            private Node _left;

            public Node Left
            {
                get { return _left; }
                set { _left = value; }
            }
            private Node _right;

            public Node Right
            {
                get { return _right; }
                set { _right = value; }
            }

            private bool _empty;

            public bool Empty
            {
                get { return _empty; }
                set { _empty = value; }
            }

            private short _label;

            public short Label
            {
                get { return _label; }
                set { _label = value; }
            }
            private bool _stop;

            public bool Stop
            {
                get { return _stop; }
                set { _stop = value; }
            }
        }

        private Node _root;
        private int _numDimensions;
        private IFeature<T,float[]>[] _features;
        private short _label = 1;

        private static IFeature<T,float[]>[] _buildFeatures;
        private static float[][] _featureValues;
        private static int _numFeatures;

        private static void fillFeatureValues(IFeatureFactory<T,float[]> factory, int numFeatures, List<T> data)
        {
            _numFeatures = numFeatures;
            _buildFeatures = new IFeature<T, float[]>[numFeatures];
            _featureValues = new float[numFeatures][];
            for (int i = 0; i < numFeatures; i++)
            {
                IFeature<T, float[]> feature = factory.Create();
                _featureValues[i] = data.Select(o => feature.Compute(o)).ToArray();
                _buildFeatures[i] = feature;
            }
        }

        private CLTree(Node root, IFeature<T,float[]>[] features)
        {
            _root = root;
            _features = features;
            _numDimensions = _features.Length;
        }

        private void clearPruning(Node node)
        {
            if (node == null)
                return;

            node.Stop = false;
            node.Empty = false;
            clearPruning(node.Left);
            clearPruning(node.Right);
        }

        private void evaluatePrune(Node node, int min_y, float min_rd)
        {
            if (node.NodeType == NodeType.Leaf)
                node.Stop = true;
            else
            {
                Node LeftChild = node.Left;
                Node RightChild = node.Right;
                if (LeftChild.Y < min_y)
                {
                    node.Left.Stop = true;
                    node.Left.Empty = true;
                    if (RightChild.Y < min_y)
                    {
                        node.Stop = true;
                        node.Empty = true;
                    }
                    else
                    {
                        evaluatePrune(RightChild, min_y, min_rd);
                        if (LeftChild.RelativeDensity > min_rd)
                            node.Stop = true;
                    }
                }
                else
                {
                    evaluatePrune(LeftChild, min_y, min_rd);
                    if (RightChild.Y < min_y)
                    {
                        node.Right.Stop = true;
                        node.Right.Empty = true;
                        if (RightChild.RelativeDensity > min_rd)
                            node.Stop = true;
                    }
                    else
                    {
                        evaluatePrune(RightChild, min_y, min_rd);
                        if ((node.Left.Stop && !node.Left.Empty) && (node.Right.Stop && !node.Right.Empty))
                            node.Stop = true;
                    }
                }
            }
        }

        private void assignLabels(Node node)
        {
            if (node.Stop)
            {
                if (node.Empty)
                    node.Label = 0;
                else node.Label = _label++;
            }
            else
            {
                assignLabels(node.Left);
                assignLabels(node.Right);
            }
        }

        /// <summary>
        /// The number of regions in the CLTree.
        /// </summary>
        public int LabelCount
        {
            get
            {
                return _label;
            }
        }

        /// <summary>
        /// Prunes the tree to require regions to have a minimum number of points and a minimum relative density.
        /// </summary>
        /// <param name="min_Y">The minimum number of points allowed in a cluster (as a percentage of total points)</param>
        /// <param name="min_rd">The minimum relative density in a cluster</param>
        public void Prune(float min_Y, float min_rd)
        {
            clearPruning(_root);
            evaluatePrune(_root, (int)(min_Y * _root.Y), min_rd);
            _label = 1;
            assignLabels(_root);
        }

        private short classify(Node node, T query)
        {
            if (node.NodeType == NodeType.Leaf || node.Stop)
                return node.Label;
            query.FeatureValue = node.Threshold.Feature.Compute(query);
            if (node.Threshold.IsLeft(query))
                return classify(node.Left, query);
            else return classify(node.Right, query);
        }

        /// <summary>
        /// Classifies a query by returning its cluster number.
        /// </summary>
        /// <param name="query">The query point</param>
        /// <returns>The point's cluster number</returns>
        public short Classify(float[] query)
        {
            return Classify(new T { Data = query });
        }

        /// <summary>
        /// Classifies a query by returning its cluster number.
        /// </summary>
        /// <param name="query">The query point</param>
        /// <returns>The point's cluster number</returns>
        public short Classify(T query)
        {
            return classify(_root, query);
        }

        /// <summary>
        /// Constructs a CLTree from the provided data.
        /// </summary>
        /// <param name="data">The data to use in constructing the tree</param>
        /// <param name="factory">The factory used to create features</param>
        /// <param name="numFeatures">The number of features to use</param>
        /// <returns>A CLTree</returns>
        public static CLTree<T> Compute(List<T> data, IFeatureFactory<T, float[]> factory, int numFeatures)
        {
            Hyperrectangle<float> bounds = new Hyperrectangle<float>(numFeatures, float.MaxValue, float.MinValue);
            fillFeatureValues(factory, numFeatures, data);
            for (int i = 0; i < numFeatures; i++)
            {
                bounds.MinimumBound[i] = _featureValues[i].Min();
                bounds.MaximumBound[i] = _featureValues[i].Max();
            }
            return new CLTree<T>(split(Enumerable.Range(0, data.Count).ToList(), data.Count, bounds), _buildFeatures);
        }

        private static void generateCode(Node node, StringBuilder sb, string tabs, Dictionary<int, string> featureVariables)
        {
            if (node.Stop)
                sb.AppendFormat("{0}return {1};\n", tabs, node.Label);
            else
            {
                string variableName;
                if (!featureVariables.ContainsKey(node.Threshold.Dimension))
                {
                    string name = "y" + node.Threshold.Dimension;
                    sb.AppendFormat("{0}{1}\n", tabs, node.Threshold.Feature.GenerateCode(name));
                    featureVariables[node.Threshold.Dimension] = name;
                }
                variableName = featureVariables[node.Threshold.Dimension];
                sb.AppendFormat("{0}if({3} {1} {2}){{\n", tabs, node.Threshold.IsInclusive ? "<=" : "<", node.Threshold.Value, variableName);
                generateCode(node.Left, sb, tabs + "\t", featureVariables);
                sb.AppendFormat("{0}}}else{{\n", tabs);
                generateCode(node.Right, sb, tabs + "\t", featureVariables);
                sb.AppendFormat("{0}}}\n", tabs);
                featureVariables.Remove(node.Threshold.Dimension);
            }
        }

        private void fillFeaturesArray(Node node)
        {
            if (node.NodeType == NodeType.Leaf)
                return;
            _features[node.Threshold.Dimension] = node.Threshold.Feature;
            fillFeaturesArray(node.Left);
            fillFeaturesArray(node.Right);
        }

        private void FillFeaturesArray()
        {
            _features = new IFeature<T, float[]>[_numDimensions];
            fillFeaturesArray(_root);
        }

        private static void inferBounds(Node node, Hyperrectangle<float> bounds)
        {
            if (node == null)
                return;
            node.Bounds = bounds;
            if (node.NodeType == NodeType.Leaf)
                return;

            Threshold test = node.Threshold;
            float min = bounds.MinimumBound[test.Dimension];
            float max = bounds.MaximumBound[test.Dimension];
            Hyperrectangle<float> leftBounds = (Hyperrectangle<float>)bounds.Clone();
            Hyperrectangle<float> rightBounds = (Hyperrectangle<float>)bounds.Clone();
            leftBounds.MaximumBound[test.Dimension] = test.Value;
            rightBounds.MinimumBound[test.Dimension] = test.Value;
            inferBounds(node.Left, leftBounds);
            inferBounds(node.Right, rightBounds);
        }

        private void InferBounds()
        {
            inferBounds(_root, _root.Bounds);
        }

        /// <summary>
        /// Generates code from the tree which will classify a point as belonging to a particular cluster.
        /// </summary>
        /// <returns>Generated code</returns>
        public string GenerateCode()
        {
            FillFeaturesArray();
            InferBounds();
            StringBuilder sb = new StringBuilder();
            sb.Append("short getLabel(float[] x)\n{\n");
            for (int i = 0; i < _features.Length; i++)
            {
                sb.AppendFormat("\tfloat {0}\n", _features[i].GenerateCode("y" + i));
            }
            sb.Append("\n\t");
            Dictionary<Hyperrectangle<float>,short> regions = GetRegions();
            Hyperrectangle<float> space = _root.Bounds;
            foreach (var region in regions.Keys)
            {
                var ranges = from dim in Enumerable.Range(0, _numDimensions)
                             select new
                             {
                                 Dim = dim,
                                 Min = region.MinimumBound[dim],
                                 Max = region.MaximumBound[dim]
                             };
                ranges = ranges.OrderBy(o => o.Max - o.Min);
                var first = ranges.First();
                sb.AppendFormat("if(({0} < {1} && {0} > {2})", "y" + first.Dim, first.Max, first.Min);
                foreach (var range in ranges.Skip(1))
                {
                    if (range.Min == space.MinimumBound[range.Dim] && range.Max == space.MaximumBound[range.Dim])
                        continue;
                    sb.AppendFormat(" && ({0} < {1} && {0} > {2})", "y" + range.Dim, range.Max, range.Min);
                }
                sb.AppendFormat("){{\n\t\treturn {0};\n\t}}else ", regions[region]);
            }

            sb.Append("return 0;\n}");
            return sb.ToString();
        }

        /// <summary>
        /// Constructs a CLTree from the provided data.
        /// </summary>
        /// <param name="data">The data to use in construction</param>
        /// <param name="factory">A factory to create feature dimensions</param>
        /// <param name="numFeatures">The number of features to use</param>
        /// <param name="bounds">The bounds of the data</param>
        /// <returns>A CLTree</returns>
        public static CLTree<T> Compute(List<T> data, IFeatureFactory<T, float[]> factory, int numFeatures, Hyperrectangle<float> bounds)
        {
            fillFeatureValues(factory, numFeatures, data);
            return new CLTree<T>(split(Enumerable.Range(0, data.Count).ToList(),  data.Count, bounds), _buildFeatures);
        }

        private static double calculateEntropyGain(float[] leftDist, float[] rightDist)
        {
            float leftCount = leftDist[0] + leftDist[1];
            float rightCount = rightDist[0] + rightDist[1];
            leftDist[0] /= leftCount;
            leftDist[1] /= leftCount;
            rightDist[0] /= rightCount;
            rightDist[1] /= rightCount;

            double leftEntropy = -leftDist[0] * Math.Log(leftDist[0], 2) - leftDist[1] * Math.Log(leftDist[1], 2);
            double rightEntropy = -rightDist[0] * Math.Log(rightDist[0], 2) - rightDist[1] * Math.Log(rightDist[1], 2);

            return -(leftCount * leftEntropy + rightCount * rightEntropy) / (leftCount + rightCount);
        }

        private static void getRegions(Node node, Dictionary<Hyperrectangle<float>, short> regions)
        {
            if (node.Stop)
            {
                if (node.Label > 0)
                    regions[node.Bounds] = node.Label;
            }
            else
            {
                getRegions(node.Left, regions);
                getRegions(node.Right, regions);
            }
        }

        /// <summary>
        /// Returns the cluster regions found by the tree.
        /// </summary>
        /// <returns>The cluster regions</returns>
        public Dictionary<Hyperrectangle<float>, short> GetRegions()
        {
            Dictionary<Hyperrectangle<float>, short> regions = new Dictionary<Hyperrectangle<float>, short>();
            getRegions(_root, regions);
            return regions;
        }

        private static Threshold findCut(Region R, IEnumerable<float> values)
        {
            var thresholds = values.GroupBy(o => o);
            float min = R.Min;
            float max = R.Max;
            float range = max - min;
            double maxGain = float.MinValue;
            Threshold cut = null;
            int y = 0;

            foreach(var threshold in thresholds)
            {
                int n = (int)(((threshold.Key - min) * R.NCount) / range);

                float[] leftDist = new float[2] { y, n };
                float[] rightDist = new float[2] { R.YCount - y, R.NCount - n };
                double gain = calculateEntropyGain(leftDist, rightDist);
                if (gain > maxGain)
                {
                    maxGain = gain;
                    cut = new Threshold { Value = threshold.Key  };
                }
                y += threshold.Count();

                leftDist = new float[2] { y, n };
                rightDist = new float[2] { R.YCount - y, R.NCount - n };
                gain = calculateEntropyGain(leftDist, rightDist);
                if (gain > maxGain)
                {
                    maxGain = gain;
                    cut = new Threshold { Value = threshold.Key, IsInclusive = true };
                }
            }

            return cut;
        }

        private static int _depth;

        private static int _minY;

        /// <summary>
        /// The minimum number of points in a cluster, used for early stopping in construction.
        /// </summary>
        public static int MinY
        {
            get { return _minY; }
            set { _minY = value; }
        }

        private static Node split(List<int> indices, int NCount, Hyperrectangle<float> bounds)
        {
            _depth++;
            int YCount = indices.Count;
            if (YCount <= MinY)
            {
                _depth--;
                return new Node { NodeType = NodeType.Leaf, Y = YCount, N = NCount, Bounds = bounds };
            }

            if (NCount < YCount)
                NCount = YCount;

            int length = _numFeatures;

            float min, max;

            Dictionary<Threshold, float> thresholds = new Dictionary<Threshold, float>();
            float[] values = new float[YCount];

            for(int i=0; i<length; i++)
            {               
                float[] featureValues = _featureValues[i];
                for (int j = 0; j < YCount; j++)
                    values[j] = featureValues[indices[j]];
                Array.Sort<float>(values);

                min = bounds.MinimumBound[i];
                max = bounds.MaximumBound[i];

                Threshold cut1 = findCut(new Region { Min = min, Max = max, YCount = YCount, NCount = NCount }, values);

                if (cut1 == null)
                    continue;

                Region r0 = new Region { Min = min, Max = cut1.Value, IncludeLeft = true, IncludeRight = cut1.IsInclusive };
                Region r1 = new Region { Min = cut1.Value, Max = max, IncludeLeft = !cut1.IsInclusive, IncludeRight = true };
                r0.CalculateCounts(values, min, max, NCount);
                r1.CalculateCounts(values, min, max, NCount);

                Region L = r0.RelativeDensity < r1.RelativeDensity ? r0 : r1;
                float[] LValues = L.Limit(values);
                if (LValues.Length == 0)
                {
                    cut1.Dimension = i;
                    cut1.Feature = _buildFeatures[i];
                    thresholds[cut1] = L.RelativeDensity;
                    continue;
                }

                Threshold cut2 = findCut(L, LValues);

                if (cut2 == null)
                    continue;

                if (L == r0)
                {
                    r0 = new Region { Min = r0.Min, Max = cut2.Value, IncludeLeft = true, IncludeRight = cut2.IsInclusive };
                    r1 = new Region { Min = cut2.Value, Max = cut1.Value, IncludeLeft = !cut2.IsInclusive, IncludeRight = cut1.IsInclusive };
                    r0.CalculateCounts(values, min, max, NCount);
                    r1.CalculateCounts(values, min, max, NCount);
                    if (r1.RelativeDensity > r0.RelativeDensity)
                    {
                        cut2.Dimension = i;
                        cut2.Feature = _buildFeatures[i];
                        thresholds[cut2] = r0.RelativeDensity;
                        continue;
                    }
                }
                else
                {
                    r0 = new Region { Min = cut1.Value, Max = cut2.Value, IncludeLeft = !cut1.IsInclusive, IncludeRight = cut2.IsInclusive };
                    r1 = new Region { Min = cut2.Value, Max = r1.Max, IncludeLeft = !cut2.IsInclusive, IncludeRight = true };
                    r0.CalculateCounts(values, min, max, NCount);
                    r1.CalculateCounts(values, min, max, NCount);
                    if (r0.RelativeDensity > r1.RelativeDensity)
                    {
                        cut2.Dimension = i;
                        cut2.Feature = _buildFeatures[i];
                        thresholds[cut2] = r1.RelativeDensity;
                        continue;
                    }
                }

                L = r0.RelativeDensity < r1.RelativeDensity ? r0 : r1;
                LValues = L.Limit(LValues);
                if (LValues.Length == 0)
                {
                    continue;
                }

                Threshold cut3 = findCut(L, LValues);

                if (cut3 == null)
                    continue;

                if (cut1.Value < cut3.Value)
                    r0 = new Region { Min = cut1.Value, Max = cut3.Value, IncludeLeft = !cut1.IsInclusive, IncludeRight = cut3.IsInclusive };
                else r0 = new Region { Min = cut3.Value, Max = cut1.Value, IncludeLeft = !cut3.IsInclusive, IncludeRight = cut1.IsInclusive };
                if (cut2.Value < cut3.Value)
                    r1 = new Region { Min = cut2.Value, Max = cut3.Value, IncludeLeft = !cut2.IsInclusive, IncludeRight = cut3.IsInclusive };
                else r1 = new Region { Min = cut3.Value, Max = cut2.Value, IncludeLeft = !cut3.IsInclusive, IncludeRight = cut2.IsInclusive };
                r0.CalculateCounts(values, min, max, NCount);
                r1.CalculateCounts(values, min, max, NCount);
                cut3.Feature = _buildFeatures[i];
                cut3.Dimension = i;
                thresholds[cut3] = Math.Min(r0.RelativeDensity, r1.RelativeDensity);
            }

            values = null;

            if (thresholds.Count() == 0)
            {
                _depth--;
                return new Node { NodeType = NodeType.Leaf, Y = YCount, N = NCount, Bounds = bounds };
            }

            Threshold best = thresholds.OrderBy(o => o.Value).First().Key;

            List<int> left = new List<int>();
            List<int> right = new List<int>();
            values = _featureValues[best.Dimension];
            foreach (int index in indices)
            {
                float value = values[index];
                if (best.IsLeft(value))
                    left.Add(index);
                else right.Add(index);
            }

            UpdateManager.WriteLine("Splitting at {0}: {1} {2}", best, left.Count, right.Count);

            min = bounds.MinimumBound[best.Dimension];
            max = bounds.MaximumBound[best.Dimension];
            Hyperrectangle<float> leftBounds = (Hyperrectangle<float>)bounds.Clone();
            Hyperrectangle<float> rightBounds = (Hyperrectangle<float>)bounds.Clone();
            leftBounds.MaximumBound[best.Dimension] = best.Value;
            rightBounds.MinimumBound[best.Dimension] = best.Value;
            int leftNCount = (int)(((best.Value - min) * NCount) / (max - min));
            Node node = new Node { Threshold = best, NodeType = NodeType.Branch, Y=YCount, N=NCount, Bounds = bounds };
            node.Left = split(left, leftNCount, leftBounds);
            node.Right = split(right, NCount - leftNCount, rightBounds);
            _depth--;
            return node;
        }
    }
}
