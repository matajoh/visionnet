using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VisionNET.Learning;

namespace VisionNET.DecisionForests
{
    [Serializable]
    internal sealed class DecisionVineNode<T,D> where T:IDataPoint<D>
    {        
        private DecisionVineNode<T,D> _left, _right;
        private float[] _distribution;
        private Decider<T, D> _decider;
        private int _index;
        private NodeType _nodeType;

        public DecisionVineNode<T, D> Left
        {
            get
            {
                return _left;
            }
            set
            {
                removeParent(_left, LeftCounts);
                _left = value;
                addParent(_left, LeftCounts);
            }
        }
        public DecisionVineNode<T, D> Right
        {
            get
            {
                return _right;
            }
            set
            {
                removeParent(_right, RightCounts);
                _right = value;
                addParent(_right, RightCounts);
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

        public NodeType NodeType
        {
            get
            {
                return _nodeType;
            }
            set
            {
                _nodeType = value;
            }
        }

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }

        public void addParent(DecisionVineNode<T, D> node, float[] dist)
        {
            if(node != null)
                node.AddDistribution(dist);
        }

        public void removeParent(DecisionVineNode<T, D> node, float[] dist)
        {
            if (node != null)
                node.RemoveDistribution(dist);
        }

        public void AddDistribution(float[] dist)
        {
            if (_distribution == null)
                _distribution = new float[dist.Length];

            for (int i = 0; i < dist.Length; i++)
                _distribution[i] += dist[i];
        }

        public void RemoveDistribution(float[] dist)
        {
            for (int i = 0; i < dist.Length; i++)
                _distribution[i] -= dist[i];
        }

        [NonSerialized]
        private List<T> _data;
        [NonSerialized]
        private float[] _leftCounts;
        [NonSerialized]
        private float[] _rightCounts;

        public List<T> Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }
        public float[] LeftCounts
        {
            get
            {
                return _leftCounts;
            }
            set
            {
                _leftCounts = value;
            }
        }
        public float[] RightCounts
        {
            get
            {
                return _rightCounts;
            }
            set
            {
                _rightCounts = value;
            }
        }
    }

    /// <summary>
    /// This class represents a Decision Vine, in which the number of children node is constrained to be no more than a maximum number and thus
    /// nodes can have more than one parent, resulting in a Directed Acyclic Graph, or DAG.  Unless otherwise noted, techniques are explained in
    /// "Decision Jungles: Compact and Rich Models for Classification, Shotton et al.".
    /// </summary>
    /// <typeparam name="T">The type of the feature point</typeparam>
    /// <typeparam name="D">The underly data type of the feature</typeparam>
    [Serializable]
    public sealed class DecisionVine<T,D> where T:IDataPoint<D>
    {
        /// <summary>
        /// Minimum amount of data required for a node to continue splitting.
        /// </summary>
        public static int MinimumSupport { get; set; }

        static DecisionVine()
        {
            MinimumSupport = 10;
        }

        private DecisionVineNode<T, D>[][] _levels;
        private int _numLevels;
        private int _numLeaves;

        private DecisionVine(DecisionVineNode<T, D>[][] levels)
        {
            _levels = levels;
            _numLevels = _levels.Length;
            _numLeaves = _levels[_numLevels - 1].Length;
            UpdateManager.WriteLine("Data Distribution: [{0}]", string.Join(",", _levels.Last().Select(o => o.Data.Count)));
        }

        /// <summary>
        /// Construct a Decision Vine using the LSearch methodology.
        /// </summary>
        /// <param name="data">The data to use in training the vine</param>
        /// <param name="factory">The feature factory to use when creating decision stumps</param>
        /// <param name="numFeatures">The number of potential features to try</param>
        /// <param name="numThresholds">The number of thresholds to try per feature</param>
        /// <param name="maxChildren">The maximum allowed number of children</param>
        /// <param name="maximumDepth">The maximum depth of the tree</param>
        /// <param name="maxIterations">The number of optimization iterations to perform per level</param>
        /// <param name="numLabels">The number of labels found in the data</param>
        /// <returns>The Decision Vine</returns>
        public static DecisionVine<T,D> ConstructUsingLSearch(List<T> data, IFeatureFactory<T,D> factory, int numFeatures, int numThresholds, int maxChildren, int maximumDepth, int maxIterations, int numLabels)
        {
            UpdateManager.WriteLine("Training Decision Vine with {0} data points...", data.Count);
            DecisionVineNode<T, D> root = new DecisionVineNode<T, D>();
            root.Data = data;
            root.NodeType = NodeType.Branch;
            root.Distribution = data.ComputeDistribution<T,D>(numLabels);
            DecisionVineNode<T, D>[][] levels = new DecisionVineNode<T, D>[maximumDepth][];
            levels[0] = new DecisionVineNode<T,D>[]{ root };
            for (int i = 1; i < maximumDepth; i++)
            {
                int numChildren = Math.Min(1 << i, maxChildren);
                int numIterations = numChildren < maxChildren ? 0 : maxIterations;
                UpdateManager.WriteLine("Training level {0} with {1} children and {2} optimization iterations...", i, numChildren, numIterations);
                levels[i] = computeLSearchLevel(levels[i - 1], factory, numChildren, numFeatures, numLabels, numThresholds, numIterations);
                UpdateManager.WriteLine("Level {0} complete with entropy {1}", i, computeEntropy(levels[i]));
                UpdateManager.WriteLine("Data distribution: [{0}]", string.Join(",", levels[i].Select(o => o.Data.Count)));
            }
            foreach(var level in levels[maximumDepth-1])
            {
                level.Distribution = level.Distribution.Normalize();
            }

            UpdateManager.WriteLine("Complete.");

            return new DecisionVine<T, D>(levels);
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

        private static DecisionVineNode<T, D>[] computeLSearchLevel(DecisionVineNode<T, D>[] parents, IFeatureFactory<T,D> factory, int numChildren, int numFeatures, int numLabels, int numThresholds, int numIterations)
        {
            DecisionVineNode<T, D>[] children = new DecisionVineNode<T, D>[numChildren];

            // assign children in a greedy manner first
            int index = 0;
            Queue<DecisionVineNode<T, D>> parentQueue = new Queue<DecisionVineNode<T, D>>(parents.OrderByDescending(o => o.Data.Count*o.Distribution.CalculateEntropy()));
            UpdateManager.WriteLine("Initializing children using highest-energy parents...");
            while (index < numChildren)
            {                
                var parent = parentQueue.Dequeue();
                findSplit(parent, factory, new float[numLabels], new float[numLabels], numFeatures, numLabels, numThresholds);
                children[index] = parent.Left = new DecisionVineNode<T, D>();
                children[index].Index = index++;
                if (index < numChildren)
                {
                    children[index] = parent.Right = new DecisionVineNode<T, D>();
                    children[index].Index = index++;
                }
                else
                {
                    parent.Right = findBestChild(parents, children, parent.RightCounts);
                }
            }

            if (parentQueue.Any())
            {
                UpdateManager.WriteLine("Adding in parents without children...");
                // we need to start adding nodes in without increasing the number of children
                while (parentQueue.Any())
                {
                    var parent = parentQueue.Dequeue();

                    if (parent.NodeType == NodeType.Leaf)
                        continue;

                    findSplit(parent, factory, new float[numLabels], new float[numLabels], numFeatures, numLabels, numThresholds);

                    parent.Left = findBestChild(parents, children, parent.LeftCounts);
                    parent.Right = findBestChild(parents, children, parent.RightCounts);

                    parent.Left.RemoveDistribution(parent.LeftCounts);
                    parent.Right.RemoveDistribution(parent.RightCounts);
                    findSplit(parent, factory, parent.Left.Distribution, parent.Right.Distribution, numFeatures, numLabels, numThresholds);
                    parent.Left.AddDistribution(parent.LeftCounts);
                    parent.Right.AddDistribution(parent.RightCounts);
                }
            }

            UpdateManager.WriteLine("Optimizing...");
            // optimize the nodes on this level
            foreach (int i in UpdateManager.ProgressEnum(Enumerable.Range(0, numIterations)))
            {
                var parent = parents.SelectRandom();

                if (parent.NodeType == NodeType.Leaf)
                    continue;

                parent.Left = null;
                parent.Left = findBestChild(parents, children, parent.LeftCounts);
                parent.Right = null;
                parent.Right = findBestChild(parents, children, parent.RightCounts);

                parent.Left.RemoveDistribution(parent.LeftCounts);
                parent.Right.RemoveDistribution(parent.RightCounts);
                findSplit(parent, factory, parent.Left.Distribution, parent.Right.Distribution, numFeatures, numLabels, numThresholds);
                parent.Left.AddDistribution(parent.LeftCounts);
                parent.Right.AddDistribution(parent.RightCounts);
            }
            UpdateManager.WriteLine(" Done");

            UpdateManager.WriteLine("Portioning out data to children...");
            // fill the data
            for (int i = 0; i < children.Length; i++)
                children[i].Data = new List<T>();

            for (int i = 0; i < parents.Length; i++)
            {
                var parent = parents[i];

                if (parent.NodeType == NodeType.Leaf)
                    continue;

                Decision[] decisions = parent.Decider.Decide(parent.Data);
                for (int j = 0; j < decisions.Length; j++)
                {
                    if (decisions[j] == Decision.Left)
                        parent.Left.Data.Add(parent.Data[j]);
                    else parent.Right.Data.Add(parent.Data[j]);
                }
                parent.Data.Clear();
                parent.Data = null;
            }

            for (int i = 0; i < children.Length; i++)
            {
                if (checkDelta(children[i].Data) || children[i].Data.Count < MinimumSupport)
                    children[i].NodeType = NodeType.Leaf;
                else children[i].NodeType = NodeType.Branch;
            }

            return children;
        }

        private static float computeEntropy(DecisionVineNode<T, D>[] level)
        {
            float entropy = 0;
            for (int i = 0; i < level.Length; i++)
                entropy += level[i].Distribution.Sum() * level[i].Distribution.CalculateEntropy();
            return entropy;
        }

        private static DecisionVineNode<T,D> findBestChild(DecisionVineNode<T,D>[] parents, DecisionVineNode<T,D>[] children, float[] distribution)
        {
            float[] entropy = new float[children.Length];
            for (int i = 0; i < entropy.Length; i++)
            {
                entropy[i] = children[i].Distribution.Sum()*children[i].Distribution.CalculateEntropy();
            }

            int index = -1;
            float minEntropy = float.MaxValue;
            float totalEntropy = entropy.Sum();
            for (int i = 0; i < entropy.Length; i++)
            {
                float[] temp = children[i].Distribution.Add(distribution);
                float testEntropy = totalEntropy - entropy[i] + (temp.CalculateEntropy() * temp.Sum());
                if (testEntropy < minEntropy)
                {
                    minEntropy = testEntropy;
                    index = i;
                }
            }
            return children[index];
        }

        private class DeciderState
        {
            public DeciderState(IFeatureFactory<T, D> factory)
            {
                Current = new Decider<T, D>(factory);
                BestEnergy = float.MaxValue;
            }

            public float BestEnergy { get; set; }
            public Decider<T, D> Best { get; set; }
            public Decider<T, D> Current { get; set; }
        }

        private static void findSplit(DecisionVineNode<T, D> node, IFeatureFactory<T,D> factory, float[] leftDistribution, float[] rightDistribution, int numFeatures, int numLabels, int numThresholds)
        {
            int dataCount = node.Data.Count;
            using (ThreadLocal<DeciderState> results = new ThreadLocal<DeciderState>(() => new DeciderState(factory), true))
            {
                Parallel.For(0, numFeatures, i =>
                {                    
                    results.Value.Current.LoadData(node.Data);
                    float energy = results.Value.Current.ChooseThreshold(numThresholds, numLabels, leftDistribution, rightDistribution);
                    if (energy < results.Value.BestEnergy)
                    {
                        results.Value.Best = results.Value.Current;
                        results.Value.BestEnergy = energy;
                        results.Value.Current = new Decider<T, D>(factory);
                    }
                });
                node.Decider = results.Values.OrderBy(o => o.BestEnergy).First().Best;

                Decision[] decisions = node.Decider.Decide(node.Data);
                float[] leftCounts = new float[numLabels];
                float[] rightCounts = new float[numLabels];
                for (int i = 0; i < decisions.Length; i++) {
                    if (decisions[i] == Decision.Left)
                        leftCounts[node.Data[i].Label] += 1;
                    else rightCounts[node.Data[i].Label] += 1;
                }
                node.LeftCounts = leftCounts;
                node.RightCounts = rightCounts;
            }
        }

        /// <summary>
        /// Classify a point and provide a soft distribution over the labels.
        /// </summary>
        /// <param name="point">The point to classify</param>
        /// <returns>The classification</returns>
        public float[] ClassifySoft(T point)
        {
            var node = _levels[0][0];
            for (int level = 0; level < _levels.Length - 1; level++)
            {
                Decision decision = node.Decider.Decide(point);
                node = decision == Decision.Left ? node.Left : node.Right;
                if (node.NodeType == NodeType.Leaf)
                    break;
            }
            return node.Distribution;
        }

        /// <summary>
        /// Classify a point to the maximally likely label.
        /// </summary>
        /// <param name="point">The point to classify</param>
        /// <returns>The predicted label for this point</returns>
        public int Classify(T point)
        {
            return ClassifySoft(point).MaxIndex();
        }

        /// <summary>
        /// Returns the leaf-level distributions of the vine.
        /// </summary>
        /// <returns>The leaf distributions</returns>
        public float[][] GetDistributions()
        {
            return _levels.Last().Select(o => o.Distribution).ToArray();
        }

        /// <summary>
        /// Compute a Leaf Count X Point Count response array, where each value is the point computed on all paths to a leaf node and then max pooled.
        /// </summary>
        /// <param name="points">The points to compute responses for</param>
        /// <param name="init">Initializations to the reduction</param>
        /// <param name="reduce">Function used to reduce the various path responses in a node</param>
        /// <returns>The responses</returns>
        public float[][] ComputeResponses(List<T> points, Func<float,float,float> reduce, Func<float> init)
        {
            int numPoints = points.Count;
            float[][] result = new float[numPoints][];
            float norm = 1.0f/(.5f * (_numLevels * _numLevels - _numLevels));

            Parallel.For(0, numPoints, point =>
            {
                float[] parents = new float[_numLeaves];
                float[] children = new float[_numLeaves];
                for (int i = 0; i < _numLeaves; i++)
                    children[i] = init();
                for (int level = 0; level < _numLevels - 1; level++)
                {
                    var nodes = _levels[level];
                    for (int node = 0; node < nodes.Length; node++)
                    {
                        float response = parents[node] + (level + 1) * nodes[node].Decider.Compute(points[point]);
                        int left = nodes[node].Left.Index;
                        int right = nodes[node].Right.Index;
                        if (nodes.Length < _numLeaves)
                        {
                            children[left] = response;
                            children[right] = response;
                        }
                        else
                        {
                            children[left] = reduce(children[left], response);
                            children[right] = reduce(children[right], response);
                        }
                    }
                    swap(ref parents, ref children);
                    for (int i = 0; i < _numLeaves; i++)
                        children[i] = init();
                }
                for (int i = 0; i < _numLeaves; i++)
                    parents[i] *= norm;

                result[point] = parents;
            });


            return result;
        }

        private void swap(ref float[] lhs, ref float[] rhs)
        {
            float[] tmp = lhs;
            lhs = rhs;
            rhs = tmp;
        }
    }
}
