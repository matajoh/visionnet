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
    /// A randomized version of a clustering tree, which avoids the over-fitting those trees are otherwise prone to.
    /// </summary>
    /// <typeparam name="T">The underlying type of the data points.</typeparam>
    [Serializable]
    public class RandomClusterTree<T> where T : IDataPoint<float[]>, new()
    {
        [Serializable]
        private class Node
        {
            private NodeType _nodeType;

            public NodeType NodeType
            {
                get { return _nodeType; }
                set { _nodeType = value; }
            }
            private short _label;

            public short Label
            {
                get { return _label; }
                set { _label = value; }
            }

            private IFeature<T, float[]> _feature;

            public IFeature<T, float[]> Feature
            {
                get { return _feature; }
                set { _feature = value; }
            }
            private float _threshold;

            public float Threshold
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
        }

        [Serializable]
        private class Split
        {
            private IFeature<T, float[]> _feature;

            public IFeature<T, float[]> Feature
            {
                get { return _feature; }
                set { _feature = value; }
            }
            private float _threshold;

            public float Threshold
            {
                get { return _threshold; }
                set { _threshold = value; }
            }
            private float _score;

            public float Score
            {
                get { return _score; }
                set { _score = value; }
            }

            private int _NLeft;

            public int NLeft
            {
                get { return _NLeft; }
                set { _NLeft = value; }
            }
        }

        private Node _root;
        private short _labelCount;

        /// <summary>
        /// Number of regions in the tree.
        /// </summary>
        public short LabelCount
        {
            get { return _labelCount; }
            set { _labelCount = value; }
        }

        private RandomClusterTree(Node root)
        {
            _root = root;
            applyLabels(_root);
        }

        private void applyLabels(Node node)
        {
            if (node.NodeType == NodeType.Leaf)
                node.Label = _labelCount++;
            else
            {
                applyLabels(node.Left);
                applyLabels(node.Right);
            }
        }

        /// <summary>
        /// Generates code which classifies points based upon the feature tests in the tree.
        /// </summary>
        /// <returns>Generated code</returns>
        public string GenerateCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("public static short GetLabel(float[] x)");
            sb.AppendLine("{");
            sb.AppendLine("\tint test;");
            generateCode(_root, sb, "\t");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void generateCode(Node node, StringBuilder sb, string tabs)
        {
            if (node.NodeType == NodeType.Leaf)
                sb.AppendFormat("{0}return {1};\n", tabs, node.Label);
            else
            {
                sb.AppendFormat("{0}{1}\n", tabs, node.Feature.GenerateCode("test"));
                sb.AppendFormat("{0}if(test < {1}){{\n", tabs, node.Threshold);
                generateCode(node.Left, sb, tabs + "\t");
                sb.AppendFormat("{0}}}else{{\n", tabs);
                generateCode(node.Right, sb, tabs + "\t");
                sb.AppendFormat("{0}}}\n", tabs);
            }
        }

        /// <summary>
        /// Classifies a query point by returning its corresponding region label.
        /// </summary>
        /// <param name="query">The query point</param>
        /// <returns>The label for the query</returns>
        public short Classify(float[] query)
        {
            return Classify(new T { Data = query });
        }

        /// <summary>
        /// Classifies a query point by returning its corresponding region label.
        /// </summary>
        /// <param name="query">The query point</param>
        /// <returns>The label for the query</returns>
        public short Classify(T query)
        {
            return classify(_root, query);
        }

        private static short classify(Node node, T query)
        {
            if (node.NodeType == NodeType.Leaf)
                return node.Label;

            if (node.Feature.Compute(query) < node.Threshold)
                return classify(node.Left, query);
            else return classify(node.Right, query);
        }

        /// <summary>
        /// Computes the tree from the provided data.
        /// </summary>
        /// <param name="data">The data to use when computing the tree</param>
        /// <param name="factory">The factory which generates the random features</param>
        /// <param name="numFeatures">The number of features to try at each level</param>
        /// <param name="numThresholds">Number of test thresholds to try</param>
        /// <param name="min_rd">The minimum relative density of a node (as a stopping condition)</param>
        /// <param name="min_y">The minimum number of points in a node as a percentage of total data points (used as a stopping condition)</param>
        /// <param name="maxDepth">The maximum depth of the tree</param>
        /// <returns></returns>
        public static RandomClusterTree<T> Compute(
            List<T> data, 
            IFeatureFactory<T,float[]> factory, 
            int numFeatures, 
            int numThresholds, 
            float min_rd,
            float min_y,
            byte maxDepth)
        {
            return new RandomClusterTree<T>(compute(data, data.Count, factory, numFeatures, numThresholds, min_rd, (int)(min_y*data.Count), 0, maxDepth));
        }

        private static Split findBestSplit(float[] sortedValues, float min_rd, int min_y, int NCount, int numThresholds)
        {
            int YCount = sortedValues.Length;

            float min = sortedValues[0];
            float max = sortedValues[sortedValues.Length - 1];

            float thresholdInterval = (max - min) / (numThresholds + 1);
            float NInterval = (float)NCount / (numThresholds + 1);

            int[] YCountLeft = new int[numThresholds+1];
            int[] NCountLeft = new int[numThresholds+1];
            int[] YCountRight = new int[numThresholds];
            int[] NCountRight = new int[numThresholds];
            float[] thresholds = new float[numThresholds];

            float threshold = min + thresholdInterval;
            float NLeft = NInterval;
            int YLeft = 0;
            int YRight = YCount;

            for (int i = 0; i < numThresholds; i++)
            {
                thresholds[i] = threshold;

                for (; sortedValues[YLeft] < threshold; YLeft++, YRight--) ;

                YCountLeft[i] = YLeft;
                YCountRight[i] = YRight;
                NCountLeft[i] = (int)NLeft;
                NCountRight[i] = NCount - (int)NLeft;

                threshold += thresholdInterval;
                NLeft += NInterval;
            }
            YCountLeft[numThresholds] = YCount;
            NCountLeft[numThresholds] = NCount;

            Split best = new Split { Score = float.MinValue };

            for (int i = 0; i < numThresholds; i++)
            {
                if (YCountLeft[i] < min_y)
                    continue;
                float left_rd = (float)YCountLeft[i] / Math.Max(NCountLeft[i], 1);

                for (int j = i + 1; j < numThresholds; j++)
                {
                    if (YCountRight[i] < min_y)
                        continue;
                    float right_rd = (float)YCountRight[i] / Math.Max(NCountRight[i], 1);

                    float middleY = YCountLeft[j] - YCountLeft[i];
                    float middleN = Math.Max(NCountLeft[j] - NCountLeft[i], 1);
                    float middle_rd = middleY / middleN;
                    if (middle_rd < min_rd)
                    {
                        Split test = new Split { Score = left_rd+right_rd-middle_rd};

                        if (test.Score > best.Score)
                        {
                            if (j - i > 3)
                            {
                                float[] density = new float[j - i];
                                for (int k = 0; k < density.Length; k++)
                                    density[k] = (float)(YCountLeft[i + k + 1] - YCountLeft[i + k]) / (NCountLeft[i + k + 1] - NCountLeft[i + k]);
                                density = Gaussian.Convolve(density, 1);
                                int minIndex = density.MinIndex();
                                test.Threshold = thresholds[i + minIndex];
                            }
                            else test.Threshold = (thresholds[i] + thresholds[j])/2;
                            test.NLeft = (int)(((test.Threshold - min) * NCount) / (max - min));
                            best = test;
                        }
                    }
                }
            }

            return best;
        }

        private static float calculateEntropy(int y, int n)
        {
            double p_y = (double)y / (y + n);
            double p_n = (double)n / (y + n);
            return (float)(-p_y * Math.Log(p_y) - p_n * Math.Log(p_n));
        }

        private static Node compute(List<T> data, int NCount, IFeatureFactory<T, float[]> factory, int numFeatures, int numThresholds, float min_rd, int min_y, int currentDepth, int maxDepth)
        {
            if (currentDepth == maxDepth - 1)
                return new Node { NodeType = NodeType.Leaf };

            int YCount = data.Count;
            if (NCount < YCount)
                NCount = YCount;

            Split best = new Split{Score = float.MinValue};
            for (int i = 0; i < numFeatures; i++)
            {
                IFeature<T, float[]> feature = factory.Create();
                var featureValues = from point in data
                                    select feature.Compute(point);

                Split split = findBestSplit(featureValues.OrderBy(o => o).ToArray(), min_rd, min_y, NCount, numThresholds);
                if (split.Score > best.Score)
                {
                    best = split;
                    best.Feature = feature;
                }
            }
            if (best.Feature == null)
                return new Node { NodeType = NodeType.Leaf };

            Node node = new Node { NodeType = NodeType.Branch, Feature=best.Feature, Threshold = best.Threshold };
            List<T> left = new List<T>();
            List<T> right = new List<T>();
            foreach (T point in data)
                if (best.Feature.Compute(point) < best.Threshold)
                    left.Add(point);
                else right.Add(point);

            if (left.Count == 0 || right.Count == 0)
                return new Node { NodeType = NodeType.Leaf };

            UpdateManager.WriteLine("{0}:{1} {2}|{3} {4}", currentDepth, best.Score, left.Count, right.Count, best.Feature);

            node.Left = compute(left, best.NLeft, factory, numFeatures, numThresholds, min_rd, min_y, currentDepth + 1, maxDepth);
            node.Right = compute(right, NCount - best.NLeft, factory, numFeatures, numThresholds, min_rd, min_y, currentDepth + 1, maxDepth);
            return node;
        }
    }
}
