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
using VisionNET.Learning;
using VisionNET.Comparison;
using System.Threading.Tasks;

namespace VisionNET.DecisionForests
{
    [Serializable]
    internal sealed class DecisionTreeNode<T,D> : INodeInfo<T,D> where T:IDataPoint<D>
    {
        private NodeType _type;
        private float _trainingCount;

        private Decider<T,D> _decider;
        private DecisionTreeNode<T, D> _left;
        private DecisionTreeNode<T, D> _right;
        private float[] _distribution;
        private int _leafNodeIndex;
        private byte _level;
        private byte _tree;

        public byte Tree
        {
            get { return _tree; }
            set { _tree = value; }
        }
        private int _levelIndex;
        private float _entropy;
        private int _index;

        public DecisionTreeNode()
        {
        }

        public DecisionTreeNode(Decider<T, D> decider, DecisionTreeNode<T, D> left, DecisionTreeNode<T, D> right)
        {
            _type = NodeType.Branch;
            _decider = decider;
            _left = left;
            _right = right;
        }

        public DecisionTreeNode(float[] distribution)
        {
            _type = NodeType.Leaf;
            _distribution = distribution;
        }

        public NodeType NodeType
        {
            get{
                return _type;
            }
            set{
                _type = value;
            }
        }

        public Decider<T, D> Decider
        {
            get
            {
                return _decider;
            }
            set
            {
                _decider = value;
            }
        }

        public DecisionTreeNode<T, D> Left
        {
            get
            {
                return _left;
            }
            set
            {
                _left = value;
            }
        }

        public DecisionTreeNode<T, D> Right
        {
            get
            {
                return _right;
            }
            set
            {
                _right = value;
            }
        }

        public float[] Distribution
        {
            get
            {
                return _distribution;
            }
            set
            {
                _distribution = value;
            }
        }

        public float TrainingDataCount
        {
            get { return _trainingCount; }
            set { _trainingCount = value; }
        }

        public int LeafNodeIndex
        {
            get { return _leafNodeIndex; }
            set { _leafNodeIndex = value; }
        }

        public byte Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public int LevelIndex
        {
            get { return _levelIndex; }
            set { _levelIndex = value; }
        }

        public float Entropy
        {
            get { return _entropy; }
            set { _entropy = value; }
        }

        public int TreeIndex
        {
            get { return _index; }
            set { _index = value; }
        }

        public ITestInfo<T, D> TestInfo
        {
            get 
            {
                return _decider;
            }
        }
    }

    /// <summary>
    /// Class which encapsulates a decision tree which operates on images.
    /// </summary>
    /// <typeparam name="T">A type of data point</typeparam>
    /// <typeparam name="D">The underlying type of the data point type</typeparam>
    [Serializable]
    public sealed class DecisionTree<T, D> where T:IDataPoint<D>
    {
        private static uint _minimumSupport = 0;
        private static byte _minimumLevels = 3;
        private static byte _maxDepth = 10;
        private static byte _numberOfTries = 5;
        private static bool _isBuilding;

        /// <summary>
        /// Minimum number of training data points required in a node.
        /// </summary>
        public static uint MinimumSupport
        {
            get
            {
                return _minimumSupport;
            }
            set
            {
                _minimumSupport = value;
            }
        }

        /// <summary>
        /// Minimum number of levels in a tree.
        /// </summary>
        public static byte MinimumDepth
        {
            get
            {
                return _minimumLevels;
            }
            set
            {
                _minimumLevels = value;
            }
        }

        /// <summary>
        /// Maximum number of levels in a tree.
        /// </summary>
        public static byte MaximumDepth
        {
            get
            {
                return _maxDepth;
            }
            set
            {
                _maxDepth = value;
            }
        }

        /// <summary>
        /// Number of times to try splitting a node.
        /// </summary>
        public static byte NumberOfTries
        {
            get
            {
                return _numberOfTries;
            }
            set
            {
                _numberOfTries = value;
            }
        }

        /// <summary>
        /// Whether a tree is currently being built.
        /// </summary>
        public static bool IsBuilding
        {
            get { return _isBuilding; }
        }

        internal DecisionTreeNode<T, D> _root;
        private byte _treeLabel;
        private int _leafCount;
        private float[] _labelWeights;
        private int _numLabels;
        private int _nodeCount;
        private byte _levelCount;

        /// <summary>
        /// Number of possible labels for data points.
        /// </summary>
        public int LabelCount
        {
            get { return _numLabels; }
            set { _numLabels = value; }
        }
        private Dictionary<int, INodeInfo<T,D>> _nodeInfo;
        private Dictionary<string, int> _testCounts;

        private DecisionTree(DecisionTreeNode<T,D> root, float[] labelWeights, int numLabels)
        {
            _root = root;
            _labelWeights = labelWeights;
            _nodeInfo = new Dictionary<int, INodeInfo<T,D>>();
            _testCounts = new Dictionary<string, int>();
            _leafCount = gatherNodeInformation(_root, 1, 0);
            _nodeCount = _nodeInfo.Count;
            _numLabels = numLabels;
            UpdateManager.WriteLine("Tree created with {0} leaf nodes", _leafCount);
        }

        private int gatherNodeInformation(DecisionTreeNode<T,D> node, int index, int count)
        {
            _nodeInfo[index] = node;
            node.Tree = _treeLabel;
            node.Level = Utilities.Log2(index);
            node.LevelIndex = index - Utilities.Pow2(node.Level);
            node.TreeIndex = index;
            if (node.NodeType == NodeType.Leaf)
            {
                node.LeafNodeIndex = count;
                if (node.Level + 1 > _levelCount)
                    _levelCount = (byte)(node.Level+1);
                return ++count;
            }
            else
            {
                node.LeafNodeIndex = -1;
                if (!_testCounts.ContainsKey(node.TestInfo.Name))
                    _testCounts[node.TestInfo.Name] = 0;
                _testCounts[node.TestInfo.Name]++;
                count = gatherNodeInformation(node.Left, 2 * index, count);
                count = gatherNodeInformation(node.Right, 2 * index + 1, count);
                return count;
            }
        }

        /// <summary>
        /// Sets the label of a tree.
        /// </summary>
        /// <param name="label">The label to use for the tree</param>
        /// <param name="leafNodeStartIndex">The starting index for the leaf nodes in this tree</param>
        /// <returns>The maximum leaf node index in the tree</returns>
        public int SetTreeLabel(byte label, int leafNodeStartIndex)
        {
            _treeLabel = label;
            return gatherNodeInformation(_root, 1, leafNodeStartIndex);
        }

        /// <summary>
        /// Counts of all the possible tests within a tree.
        /// </summary>
        public Dictionary<string, int> TestCounts
        {
            get
            {
                return _testCounts;
            }
        }

        /// <summary>
        /// Number of levels.
        /// </summary>
        public byte LevelCount
        {
            get
            {
                return _levelCount;
            }
        }
        
        /// <summary>
        /// Total number of nodes.
        /// </summary>
        public int NodeCount
        {
            get
            {
                return _nodeCount;
            }
        }

        /// <summary>
        /// Info on all the nodes in the tree, indexed by tree index.
        /// </summary>
        public Dictionary<int, INodeInfo<T,D>> NodeInfo
        {
            get
            {
                return _nodeInfo;
            }
        }

        /// <summary>
        /// Number of leaf nodes.
        /// </summary>
        public int LeafCount
        {
            get
            {
                return _leafCount;
            }
        }

        /// <summary>
        /// Label for this tree.
        /// </summary>
        public byte TreeLabel
        {
            get
            {
                return _treeLabel;
            }
        }

        /// <summary>
        /// Weights for the different class labels.
        /// </summary>
        public float[] LabelWeights
        {
            get
            {
                return _labelWeights;
            }
            set
            {
                _labelWeights = value;
            }
        }

        /// <summary>
        /// Classifies an individual point.
        /// </summary>
        /// <param name="point">The point to classify</param>
        /// <returns>The maximum likelihood label for the point</returns>
        public short Classify(T point)
        {
            return (short)ClassifySoft(point).MaxIndex();
        }

        /// <summary>
        /// Classifiest an individual point and adds the label probabilities to the provided array.
        /// </summary>
        /// <param name="point">Point to classify</param>
        /// <param name="distribution">Provided array</param>
        public void ClassifySoft(T point, float[] distribution)
        {
            float[] dist = findLeaf(_root, point).Distribution;
            for (int i = 0; i < _numLabels; i++)
                distribution[i] += dist[i];
        }

        /// <summary>
        /// Classifies <paramref name="point"/>, producing a distribution over all pixel labels.
        /// </summary>
        /// <param name="point">Point to classify</param>
        /// <returns>A distribution over pixel labels</returns>
        public float[] ClassifySoft(T point)
        {
            return findLeaf(_root, point).Distribution;
        }

        internal static void assignLabels(DecisionTreeNode<T,D> node, List<T> points, INodeInfo<T,D>[] nodeInfo, List<int> indices)
        {
            if (node.NodeType == NodeType.Leaf)
            {
                foreach (int index in indices)
                    nodeInfo[index] = node;
            }
            else
            {
                Decision[] decision = node.Decider.Decide(points);
                List<int> left = new List<int>();
                List<T> leftPoints = new List<T>();
                List<int> right = new List<int>();
                List<T> rightPoints = new List<T>();
                for (int i = 0; i < decision.Length; i++)
                    if (decision[i] == Decision.Left)
                    {
                        left.Add(indices[i]);
                        leftPoints.Add(points[i]);
                    }
                    else
                    {
                        right.Add(indices[i]);
                        rightPoints.Add(points[i]);
                    }
                assignLabels(node.Left, leftPoints, nodeInfo, left);
                assignLabels(node.Right, rightPoints, nodeInfo, right);
            }
        }

        private static INodeInfo<T,D> findLeaf(DecisionTreeNode<T,D> node, T point)
        {
            if (node.NodeType == NodeType.Leaf)
                return node;
            else
            {
                Decision d = node.Decider.Decide(point);
                if (d == Decision.Left)
                    return findLeaf(node.Left, point);
                else return findLeaf(node.Right, point);
            }
        }



        private int findLeafNumber(DecisionTreeNode<T,D> node, int index, T point)
        {
            if (node.NodeType == NodeType.Leaf)
                return node.LeafNodeIndex;
            else
            {
                Decision d = node.Decider.Decide(point);
                if (d == Decision.Left)
                    return findLeafNumber(node.Left, index * 2, point);
                else return findLeafNumber(node.Right, index * 2 + 1, point);
            }
        }

        /// <summary>
        /// Computes a tree histogram from the list of points.
        /// </summary>
        /// <param name="points">Points to use when creating the tree histogram</param>
        /// <returns>A tree histogram</returns>
        public TreeHistogram ComputeHistogram(List<T> points)
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            findCounts(_root, counts, points);
            List<TreeNode> nodes = new List<TreeNode>();
            foreach(int index in counts.Keys)
            {
                if (counts[index] > 0)
                {
                    nodes.Add(new TreeNode(_treeLabel, counts[index], _nodeInfo[index].LeafNodeIndex, index));
                }
            }
            return new TreeHistogram(nodes);
        }

        private static void findCounts(DecisionTreeNode<T,D> node, Dictionary<int,int> counts, List<T> points)
        {
            int index = node.TreeIndex;
            counts[index] = points.Count;
            if (node.NodeType == NodeType.Branch)
            {
                List<T> left = new List<T>();
                List<T> right = new List<T>();
                Decision[] decisions = node.Decider.Decide(points);
                for (int i = 0; i < decisions.Length; i++)
                    if (decisions[i] == Decision.Left)
                        left.Add(points[i]);
                    else right.Add(points[i]);
                if (left.Count > 0)
                    findCounts(node.Left, counts, left);
                if (right.Count > 0)
                    findCounts(node.Right, counts, right);
            }
        }

        /// <summary>
        /// Clears the training data from the tree.
        /// </summary>
        public void Clear()
        {
            clear(_root);
        }

        private void clear(DecisionTreeNode<T,D> node)
        {
            node.TrainingDataCount = 0;
            node.Distribution = new float[LabelCount];
            if(node.NodeType == NodeType.Branch)
            {
                clear(node.Left);
                clear(node.Right);
            }
        }

        /// <summary>
        /// Classifies each point in <paramref name="points"/> and trackes which nodes it visits.
        /// </summary>
        /// <param name="points"></param>
        public void Fill(List<T> points)
        {
            fill(_root, points);
        }

        private void fill(DecisionTreeNode<T,D> node, List<T> points)
        {
            foreach (T point in points)
                node.Distribution[point.Label] += 1;
            if (node.NodeType == NodeType.Branch)
            {
                List<T> left = new List<T>();
                List<T> right = new List<T>();
                Decision[] decisions = node.Decider.Decide(points);
                for (int i = 0; i < decisions.Length; i++)
                {
                    if (decisions[i] == Decision.Left)
                        left.Add(points[i]);
                    else right.Add(points[i]);
                }
                if (left.Count > 0)
                    fill(node.Left, left);
                if (right.Count > 0)
                    fill(node.Right, right);
            }
        }

        /// <summary>
        /// Normalizes all of the node distributions in the tree.
        /// </summary>
        public void Normalize()
        {
            normalize(_root);
        }

        private void normalize(DecisionTreeNode<T,D> node)
        {
            if (_labelWeights != null)
            {
                for (int i = 0; i < _labelWeights.Length; i++)
                {
                    node.Distribution[i] *= _labelWeights[i];
                }
            }
            float sum = 0;
            for (int i = 0; i < _numLabels; i++)
                sum += node.Distribution[i];
            node.TrainingDataCount = sum; 
            Decider<T,D>.Normalize(node.Distribution);
            float entropy = 0;
            for (int i = 0; i < LabelCount; i++)
                entropy += node.Distribution[i] * (float)Math.Log(node.Distribution[i], 2);
            node.Entropy = -entropy;
            if(node.NodeType == NodeType.Branch)
            {
                normalize(node.Left);
                normalize(node.Right);
            }
        }

        #region Construction Methods
        /// <summary>
        /// Constructs a new decision tree using the depth-first method.  This method will continue to split each node until there is less than <see cref="F:MinimumSupport" /> points
        /// in the node or <see cref="F:MaximumDepth" /> is reached, and then backtracks up the tree.
        /// </summary>
        /// <param name="data">The data to use when constructing the tree</param>
        /// <param name="factory">The feature factory to use for producing sample feature test for each node</param>
        /// <param name="numFeatures">The number of sample feature tests to try</param>
        /// <param name="numThresholds">The number of test thresholds to try with each test</param>
        /// <param name="numLabels">The number of possible labels for a point</param>
        /// <param name="labelWeights">The weights for each label</param>
        /// <returns>A new decision tree</returns>
        public static DecisionTree<T,D> ComputeDepthFirst
        (
            List<T> data,
            IFeatureFactory<T,D> factory,
            int numFeatures,
            int numThresholds,
            int numLabels,
            float[] labelWeights
        )
        {
            _isBuilding = true;
            DecisionTreeNode<T,D> root = computeDepthFirst(new DecisionTreeNode<T,D>(), data, factory, numFeatures, numThresholds, numLabels, labelWeights, 0);
            _isBuilding = false;
            DecisionTree<T,D> tree = new DecisionTree<T,D>(root, labelWeights, numLabels);
            return tree;
        }

        /// <summary>
        /// Constructs a new decision tree using the breadth-first method.  This method will attempt to split each leaf node in the tree with each step, and will stop when
        /// <see cref="F:MaximumDepth" /> is reached or it is unable to split any leaf nodes.  If no nodes split in a step, it will try again <see cref="F:NumberOfTries" /> times, and then
        /// stop.  A node will only be split if the resulting entropy increase is above <paramref name="threshold"/>.
        /// </summary>
        /// <param name="data">The data to use when constructing the tree</param>
        /// <param name="factory">The feature factory to use for producing sample feature test for each node</param>
        /// <param name="numFeatures">The number of sample feature tests to try</param>
        /// <param name="numThresholds">The number of test thresholds to try with each test</param>
        /// <param name="numLabels">The number of possible labels for a point</param>
        /// <param name="labelWeights">The weights for each label</param>
        /// <param name="threshold">Threshold used to determine a good feature test</param>
        /// <returns>A new decision tree</returns>
        public static DecisionTree<T,D> ComputeBreadthFirst
        (
            List<T> data,
            IFeatureFactory<T,D> factory,
            int numFeatures,
            int numThresholds,
            int numLabels,
            float[] labelWeights,
            float threshold
        )
        {
            _isBuilding = true;
            DecisionTreeNode<T,D> root = computeBreadthFirst(threshold, data, factory, numFeatures, numThresholds, numLabels, labelWeights);
            _isBuilding = false;
            return new DecisionTree<T,D>(root, labelWeights, numLabels);
        }

        private static bool checkDelta(List<T> data)
        {
            int deltaLabel = data[0].Label;
            int count = data.Count;
            for (int i = 1; i < count; i++)
                if (data[i].Label != deltaLabel)
                    return false;
            return true;
        }

        private class DecisionResult
        {
            public float[] LeftDistribution { get; set; }
            public float[] RightDistribution { get; set; }
            public Decider<T, D> Decider { get; set; }
            public float Score { get; set; }
        }

        private static DecisionTreeNode<T,D> computeDepthFirst(DecisionTreeNode<T,D> node, List<T> data, IFeatureFactory<T,D> factory, int numFeatures, int numThresholds, int numLabels, float[] labelWeights, int depth)
        {
            GC.Collect();
            if (data.Count == 0)
            {
                UpdateManager.WriteLine("No data at depth {0}", depth);
                return null;
            }
            if(data[0] is IComparable<T>)
                data.Sort();
            if (checkDelta(data))
            {
                UpdateManager.WriteLine("Delta function at depth {0}", depth);
                int label = data[0].Label;
                float[] dist = new float[numLabels];
                dist[label] = 1;
                return new DecisionTreeNode<T,D>(dist);
            }
            int dataCount = data.Count;
            Decider<T,D> bestDecider = null;
            float bestScore = float.MinValue;
            float[] bestLeftDistribution = null;
            float[] bestRightDistribution = null;
            List<Task<DecisionResult>> tasks = new List<Task<DecisionResult>>();
            TaskFactory<DecisionResult> taskFactory = new TaskFactory<DecisionResult>();
            for (int i = 0; i < numFeatures; i++)
            {
                tasks.Add(taskFactory.StartNew(() =>
                {
                    float[] leftDistribution;
                    float[] rightDistribution;
                    Decider<T, D> decider = new Decider<T, D>(factory);
                    decider.LoadData(data);
                    float score = decider.ChooseThreshold(numThresholds, numLabels, labelWeights, out leftDistribution, out rightDistribution);
                    return new DecisionResult { LeftDistribution = leftDistribution, RightDistribution = rightDistribution, Decider = decider, Score = score };
                }));
            }
            Task.WaitAll(tasks.ToArray());
            foreach (var task in tasks)
            {
                if (task.Result.Score > bestScore)
                {
                    bestLeftDistribution = task.Result.LeftDistribution;
                    bestRightDistribution = task.Result.RightDistribution;
                    bestDecider = task.Result.Decider;
                    bestScore = task.Result.Score;
                }
            }
            float support = 0;
            if (labelWeights != null)
            {
                foreach (T point in data)
                    support += labelWeights[point.Label];
            }
            else support = dataCount;
            if(bestScore == float.MinValue || dataCount < MinimumSupport){
                UpdateManager.WriteLine("Stopping due to lack of data at depth {0}, {1} < {2}", depth, dataCount, MinimumSupport);
                float[] distribution = new float[labelWeights.Length];
                for (int i = 0; i < dataCount; i++)
                    distribution[data[i].Label]++;
                for(int i=0; i<distribution.Length; i++)
                    distribution[i] *= labelWeights[i];
                return new DecisionTreeNode<T,D>(distribution);
            }
            if (depth == MaximumDepth - 2)
            {
                UpdateManager.WriteLine("Last branch node trained at depth {0}", depth);
                node.Left = new DecisionTreeNode<T,D>(bestLeftDistribution);
                node.Right = new DecisionTreeNode<T,D>(bestRightDistribution);
                node.NodeType = NodeType.Branch;
                node.Decider = bestDecider;
                return node;
            }
            Decision[] decisions = bestDecider.Decide(data);
            List<T> leftData = new List<T>();
            List<T> rightData = new List<T>();
            for (int i = 0; i < decisions.Length; i++)
                if (decisions[i] == Decision.Left)
                    leftData.Add(data[i]);
                else rightData.Add(data[i]);
            if(leftData.Count == 0 || rightData.Count == 0)
                throw new Exception("Error");
            UpdateManager.WriteLine("Branch node at depth {0} trained.", depth);
            node.Left = computeDepthFirst(new DecisionTreeNode<T,D>(), leftData, factory, numFeatures, numThresholds, numLabels, labelWeights, depth + 1);
            node.Right = computeDepthFirst(new DecisionTreeNode<T,D>(), rightData, factory, numFeatures, numThresholds, numLabels, labelWeights, depth + 1);
            node.Decider = bestDecider;
            node.NodeType = NodeType.Branch;
            return node;
        }

        private static float calculateEntropy(List<T> data, List<int> indices, float[] labelWeights, int numLabels)
        {
            const float DIRICHLET = Decider<T,D>.DIRICHLET_PRIOR;
            double count = 0;
            double entropy = 0;
            double[] dist = new double[numLabels];
            foreach(int index in indices)
                dist[data[index].Label]++;
            if(labelWeights != null){
                for(int i=0; i<numLabels; i++)
                    dist[i] *= labelWeights[i];
            }
            for (int i = 0; i < numLabels; i++)
                count += dist[i];
            double norm = 1 / (count + numLabels * DIRICHLET);
            for (int c = 0; c < numLabels; c++)
            {
                double classProb = (dist[c] + DIRICHLET) * norm;
                entropy -= classProb * Math.Log(classProb, 2);
            }
            return (float)entropy;
        }

        private static float calculateSupport(List<T> points, List<int> indices, float[] labelWeights)
        {
            return indices.Count;
        }

        private class SplitCandidate
        {
            public List<int> Indices;
            public int Index;
            public int Level;
            public float Entropy;
            public float EntropyGain;
            public float Support;
            public bool Delta;
            public Decider<T,D> Decider;
            public float[] Values;
            public int[] Labels;
            public float[] Weights;

            public SplitCandidate(List<int> indices, int index, int level)
            {
                Indices = indices;
                Index = index;
                Level = level;
            }
        }

        private static bool calculateDelta(List<T> points, List<int> indices)
        {
            int delta = points[indices[0]].Label;
            int dataCount = indices.Count;
            for (int i = 0; i < dataCount; i++)
                if (delta != points[indices[i]].Label)
                    return false;
            return true;
        }

        private static DecisionTreeNode<T,D> computeBreadthFirst(float threshold, List<T> data, IFeatureFactory<T,D> factory, int numFeatures, int numThresholds, int numLabels, float[] labelWeights)
        {
            string id = "DecisionTree.ComputeBreadthFirst";
            Queue<SplitCandidate> candidates = new Queue<SplitCandidate>();
            SplitCandidate start = new SplitCandidate(new List<int>(), 1, 0);
            for (int i = 0; i < data.Count; i++)
                start.Indices.Add(i);
            start.Entropy = calculateEntropy(data, start.Indices, labelWeights, numLabels);
            start.Support = calculateSupport(data, start.Indices, labelWeights);
            candidates.Enqueue(start);
            bool changed = true;
            float[] leftDistribution, rightDistribution;
            int tries = (int)_numberOfTries;
            float increment = threshold / tries;
            Dictionary<int,Decider<T,D>> deciders = new Dictionary<int,Decider<T,D>>();
            while (tries > 0)
            {
                if (!changed)
                {
                    threshold -= increment;
                    UpdateManager.WriteLine("Decreasing threshold to {0}", threshold);
                }
                GC.Collect();
                int count = candidates.Count;
                for (int i = 0; i < count; i++)
                {
                    SplitCandidate candidate = candidates.Dequeue();

                    if (candidate.Delta)
                    {
                        candidates.Enqueue(candidate);
                        continue;
                    }
                    if (MaximumDepth > 0 && candidate.Level >= MaximumDepth-1)
                    {
                        candidates.Enqueue(candidate);
                        continue;
                    }
                    if (candidate.Support < MinimumSupport)
                    {
                        candidates.Enqueue(candidate);
                        continue;
                    }
                    int dataCount = candidate.Indices.Count;
                    if (candidate.Values == null)
                        candidate.Values = new float[dataCount];
                    if(candidate.Labels == null)
                        candidate.Labels = new int[dataCount];
                    if (candidate.Weights == null)
                        candidate.Weights = new float[dataCount];

                    candidates.Enqueue(candidate);
                }
                float bestGain = float.MinValue;
                for (int k = 0; k < numFeatures; k++)
                {
                    UpdateManager.RaiseProgress(k, numFeatures);
                    Decider<T,D> decider = new Decider<T,D>(factory);
                    decider.ApplyFeature(data);
                    for (int i = 0; i < count; i++)
                    {
                        SplitCandidate candidate = candidates.Dequeue();
                        if (MaximumDepth > 0 && candidate.Level >= MaximumDepth-1)
                        {
                            candidates.Enqueue(candidate);
                            continue;
                        }
                        if (candidate.Delta)
                        {
                            candidates.Enqueue(candidate);
                            continue;
                        }
                        if (candidate.Support < MinimumSupport)
                        {
                            candidates.Enqueue(candidate);
                            continue;
                        }
                        List<int> indices = candidate.Indices;
                        int dataCount = indices.Count;
                        for (int j = 0; j < dataCount; j++)
                        {
                            T point = data[indices[j]];
                            candidate.Values[j] = point.FeatureValue;
                            candidate.Labels[j] = point.Label;
                            candidate.Weights[j] = point.Weight;
                        }
                        decider.SetData(candidate.Values, candidate.Weights, candidate.Labels);
                        float gain = candidate.Entropy + decider.ChooseThreshold(numThresholds, numLabels, labelWeights, out leftDistribution, out rightDistribution);
                        bestGain = Math.Max(gain, bestGain);
                        if ((gain > threshold || candidate.Level < MinimumDepth) && gain > candidate.EntropyGain)
                        {
                            candidate.EntropyGain = gain;
                            candidate.Decider = new Decider<T,D>(decider.Feature, decider.Threshold);
                        }
                        candidates.Enqueue(candidate);
                    }
                }
                UpdateManager.WriteLine(id, "\rNodes Added:");
                changed = false;
                for (int i = 0; i < count; i++)
                {
                    SplitCandidate candidate = candidates.Dequeue();
                    if (candidate.Decider == null)
                    {
                        candidates.Enqueue(candidate);
                        continue;
                    }
                    changed = true;
                    List<int> indices = candidate.Indices;
                    int dataCount = candidate.Indices.Count;
                    List<T> points = new List<T>();
                    for (int j = 0; j < dataCount; j++)
                        points.Add(data[indices[j]]);
                    Decision[] decisions = candidate.Decider.Decide(points);
                    List<int> left = new List<int>();
                    List<int> right = new List<int>();
                    for (int j = 0; j < dataCount; j++)
                        if (decisions[j] == Decision.Left)
                            left.Add(indices[j]);
                        else right.Add(indices[j]);
                    SplitCandidate leftCandidate = new SplitCandidate(left, 2 * candidate.Index, candidate.Level + 1);
                    SplitCandidate rightCandidate = new SplitCandidate(right, 2 * candidate.Index+1, candidate.Level + 1);
                    leftCandidate.Entropy = calculateEntropy(data, left, labelWeights, numLabels);
                    leftCandidate.Support = calculateSupport(data, left, labelWeights);
                    leftCandidate.Delta = calculateDelta(data, left);
                    rightCandidate.Entropy = calculateEntropy(data, right, labelWeights, numLabels);
                    rightCandidate.Support = calculateSupport(data, right, labelWeights);
                    rightCandidate.Delta = calculateDelta(data, right);
                    UpdateManager.WriteLine(id, "{3:00000}:{0:0.000}|{1:0.000} {2:0.000} {4}", leftCandidate.Support/candidate.Support, rightCandidate.Support/candidate.Support, candidate.EntropyGain, candidate.Index, candidate.Decider);
                    deciders[candidate.Index] = candidate.Decider;
                    candidates.Enqueue(leftCandidate);
                    candidates.Enqueue(rightCandidate);
                }
                if (!changed)
                {
                    UpdateManager.WriteLine("No new nodes added, best entropy gain was {0}", bestGain);
                    tries--;
                }
                if (bestGain == float.MinValue)
                    break;
            }
            Dictionary<int, List<int>> leafIndices = new Dictionary<int, List<int>>();
            while (candidates.Count > 0)
            {
                SplitCandidate candidate = candidates.Dequeue();
                leafIndices[candidate.Index] = candidate.Indices;
            }
            return buildTree(new DecisionTreeNode<T,D>(), 1, deciders, leafIndices, data, numLabels, labelWeights);
        }

        private static DecisionTreeNode<T,D> buildTree(DecisionTreeNode<T,D> node, int index, Dictionary<int,Decider<T,D>> deciders, Dictionary<int,List<int>> indices, List<T> data, int numLabels, float[] labelWeights)
        {
            if (!deciders.ContainsKey(index))
            {
                float[] dist = new float[numLabels];
                foreach (int i in indices[index])
                    dist[data[i].Label] += 1;
                if (labelWeights != null)
                {
                    for (int i = 0; i < numLabels; i++)
                        dist[i] *= labelWeights[i];
                }
                node.NodeType = NodeType.Leaf;
                node.Distribution = dist;
                return node;
            }
            else
            {
                node.NodeType = NodeType.Branch;
                node.Decider = deciders[index];
                node.Left = buildTree(new DecisionTreeNode<T,D>(), 2 * index, deciders, indices, data, numLabels, labelWeights);
                node.Right = buildTree(new DecisionTreeNode<T,D>(), 2 * index + 1, deciders, indices, data, numLabels, labelWeights);
                return node;
            }
        }

        #endregion

        /// <summary>
        /// Fills <paramref name="histogram"/> with the training data count at each node.
        /// </summary>
        /// <param name="histogram">The histogram to fill</param>
        public void FillNodeHistogram(float[] histogram)
        {
            fillNodeHistogram(_root, histogram);
        }

        private void fillNodeHistogram(DecisionTreeNode<T,D> node, float[] histogram)
        {
            if (node.NodeType == NodeType.Leaf)
                histogram[node.LeafNodeIndex] = node.TrainingDataCount;
            else
            {
                fillNodeHistogram(node.Left, histogram);
                fillNodeHistogram(node.Right, histogram);
            }

        }

        /// <summary>
        /// Returns the amount of training data in the tree.
        /// </summary>
        /// <returns>The amount of training data.</returns>
        public float GetTrainingDataCount()
        {
            return _root.TrainingDataCount;
        }

        /// <summary>
        /// Fills <paramref name="leafNodes"/> with the metadata information about this tree.
        /// </summary>
        /// <param name="leafNodes">The array to fill</param>
        public void FillLeafNodes(INodeInfo<T,D>[] leafNodes)
        {
            fillLeafNodes(_root, leafNodes);
        }

        private void fillLeafNodes(DecisionTreeNode<T,D> node, INodeInfo<T,D>[] leafNodes)
        {
            if (node.NodeType == NodeType.Leaf)
                leafNodes[node.LeafNodeIndex] = node;
            else
            {
                fillLeafNodes(node.Left, leafNodes);
                fillLeafNodes(node.Right, leafNodes);
            }
        }
    }
}
