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
using VisionNET.Learning;
using VisionNET.Comparison;
using System.Threading.Tasks;

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// This class encapsulates a decision forest, which makes decisions based upon the consensus of a collection of decision trees.
    /// </summary>
    /// <typeparam name="T">A type of data point</typeparam>
    /// <typeparam name="D">The underlying type of the data point type</typeparam>
    [Serializable]
    public sealed class DecisionForest<T,D> where T:IDataPoint<D>
    {
        private byte _numTrees;
        private int _numLabels;
        private int _leafCount;
        private byte _levelCount;
        internal DecisionTree<T,D>[] _trees;
        private string[] _labelNames;
        private Dictionary<string, int> _testCounts;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="trees">The trees in this forest</param>
        /// <param name="labelNames">The names associated with the labels this tree was trained with</param>
        public DecisionForest(DecisionTree<T,D>[] trees, string[] labelNames)
        {
            _trees = trees;
            _numTrees = (byte)trees.Length;
            _numLabels = labelNames.Length;
            _labelNames = labelNames;
            _leafCount = 0;
            _testCounts = new Dictionary<string, int>();
            RefreshMetadata();
        }

        /// <summary>
        /// An array of the tests used in this forest.
        /// </summary>
        public string[] TestsUsed
        {
            get
            {
                return new List<string>(_testCounts.Keys).ToArray();
            }
        }

        /// <summary>
        /// Counts for each test in the tree.
        /// </summary>
        public Dictionary<string, int> TestCounts
        {
            get
            {
                return _testCounts;
            }
        }

        /// <summary>
        /// The maximum number of levels in the trees in this forest.
        /// </summary>
        public byte LevelCount
        {
            get
            {
                return _levelCount;
            }
        }

        /// <summary>
        /// The names of the labels this tree was trained with.
        /// </summary>
        public string[] LabelNames
        {
            get
            {
                return _labelNames;
            }
        }

        /// <summary>
        /// The total number of leaves in the tree.
        /// </summary>
        public int LeafCount
        {
            get
            {
                return _leafCount;
            }
        }

        /// <summary>
        /// Indexes the trees in this forest.
        /// </summary>
        /// <param name="tree">The tree to retrieve</param>
        /// <returns>The desired tree</returns>
        public DecisionTree<T,D> this[int tree]
        {
            get
            {
                return _trees[tree];
            }
        }

        /// <summary>
        /// The number of trees currently in use in the forest.
        /// </summary>
        public byte TreeCount
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
        /// The total number of trees available to the forest.
        /// </summary>
        public int TotalTrees
        {
            get
            {
                return _trees.Length;
            }
        }

        /// <summary>
        /// The number of labels the forest was trained with.
        /// </summary>
        public int LabelCount
        {
            get
            {
                return _numLabels;
            }
        }

        /// <summary>
        /// Clears all training data from every tree in the forest.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _numTrees; i++)
                _trees[i].Clear();
        }

        /// <summary>
        /// Refreshes the node metadata in all trees in the forest.
        /// </summary>
        public void RefreshMetadata()
        {
            _leafCount = 0;
            _levelCount = 0;
            _testCounts = new Dictionary<string, int>();
            for(byte i=0; i<_numTrees; i++)
            {
                DecisionTree<T,D> tree = _trees[i];
                _leafCount = tree.SetTreeLabel(i, _leafCount);
                _levelCount = Math.Max(_levelCount, tree.LevelCount);
                Dictionary<string, int> testCounts = tree.TestCounts;
                foreach (string key in testCounts.Keys)
                {
                    if (!_testCounts.ContainsKey(key))
                        _testCounts[key] = 0;
                    _testCounts[key] += testCounts[key];
                }
            }
        }

        /// <summary>
        /// Creates a forest from a random subset of this forest's trees.
        /// </summary>
        /// <param name="forestSize"></param>
        /// <returns></returns>
        public DecisionForest<T, D> CreateRandomSubForest(int forestSize)
        {
            DecisionTree<T, D>[] trees = _trees.Permute(forestSize).ToArray();
            return new DecisionForest<T, D>(trees, _labelNames);
        }

        /// <summary>
        /// Adds the points from <paramref name="data"/> to each tree in the forest.
        /// </summary>
        /// <param name="data">Data to learn from</param>
        public void Fill(List<T> data)
        {
            for (int i = 0; i < _numTrees; i++)
                _trees[i].Fill(data);
            RefreshMetadata();
        }

        /// <summary>
        /// Classifies a point, returning a distribution over labels.
        /// </summary>
        /// <param name="point">Point to classify</param>
        /// <returns>A distribution over labels</returns>
        public float[] ClassifySoft(T point)
        {
            float[] dist = new float[_numLabels];
            for (int t = 0; t < _numTrees; t++)
            {
                _trees[t].ClassifySoft(point, dist);
            }
            return dist.Normalize().ToArray();
        }

        /// <summary>
        /// Classifies a point, giving it a label.
        /// </summary>
        /// <param name="point">The point to classify</param>
        /// <returns>The inferred label for this point</returns>
        public short Classify(T point)
        {
            return (short)ClassifySoft(point).MaxIndex();
        }

        /// <summary>
        /// Gets a sparse coding for this point, with one code per tree, where each dimension represents a code for that tree which can be compared
        /// to other points classified by that tree using Euclidean distance and have that distance reflect the number of branches they share, i.e.
        /// more branches shared = more similar = lower Euclidean distance.
        /// </summary>
        /// <param name="point">The point to code</param>
        /// <returns>A T dimensional coding</returns>
        public int[] GetSparseCoding(T point)
        {
            Task<int>[] tasks = new Task<int>[_numTrees];
            TaskFactory<int> factory = new TaskFactory<int>();
            for (int t = 0; t < _numTrees; t++)
            {
                tasks[t] = factory.StartNew(arg =>
                {
                    DecisionTree<T, D> tree = (DecisionTree<T, D>)arg;
                    return tree.GetSparseCode(point);
                }, _trees[t]);
            }
            Task.WaitAll(tasks);
            return tasks.Select(o => o.Result).ToArray();
        }

        /// <summary>
        /// Normalizes the node distributions in the forest.
        /// </summary>
        public void Normalize()
        {
            for (int i = 0; i < _numTrees; i++)
                _trees[i].Normalize();
        }

        /// <summary>
        /// Returns all of the node metadata for this forest.
        /// </summary>
        /// <returns>The node metadata</returns>
        public Dictionary<int, INodeInfo<T,D>>[] GetForestInfo()
        {
            Dictionary<int, INodeInfo<T,D>>[] info = new Dictionary<int, INodeInfo<T,D>>[_numTrees];
            for (int i = 0; i < _numTrees; i++)
                info[i] = _trees[i].NodeInfo;
            return info;
        }
        
        /// <summary>
        /// Computes a histogram for all trees from the provided points.
        /// </summary>
        /// <param name="points">Points to classify</param>
        /// <returns>The histogram</returns>
        public TreeHistogram ComputeHistogram(List<T> points)
        {
            TreeHistogram histogram = _trees[0].ComputeHistogram(points);
            for (int t = 1; t < _numTrees; t++)
                histogram = TreeHistogram.Union(histogram, _trees[t].ComputeHistogram(points));
            return TreeHistogram.Divide(histogram, points.Count);
        }

        /// <summary>
        /// Returns a histogram of the training data by node.
        /// </summary>
        /// <returns>An array of training data presence within each node in the forest</returns>
        public float[] GetNodeHistogram()
        {
            float[] histogram = new float[_leafCount];
            for (int i = 0; i < _numTrees; i++)
                _trees[i].FillNodeHistogram(histogram);
            return histogram;
        }

        /// <summary>
        /// Returns the metadata for the leaf nodes of the forest.
        /// </summary>
        /// <returns>An array of metadata</returns>
        public INodeInfo<T,D>[] GetLeafNodes()
        {
            INodeInfo<T,D>[] leafNodes = new INodeInfo<T,D>[_leafCount];
            for (int i = 0; i < _numTrees; i++)
                _trees[i].FillLeafNodes(leafNodes);
            return leafNodes;
        }

        /// <summary>
        /// Returns the total number of training pixels in the forest.
        /// </summary>
        /// <returns>The total number of training pixels in the forest</returns>
        public float GetTrainingDataCount()
        {
            return _trees[0].GetTrainingDataCount();
        }

        /// <summary>
        /// Trains a decision forest from <paramref name="splits"/> based on the provided parameters using the depth first algorithm.
        /// </summary>
        /// <param name="numTrees">Number of trees in the forest</param>
        /// <param name="splits">Data splits to use when training the tree.</param>
        /// <param name="factory">The feature factory</param>
        /// <param name="numFeatures">The number of features to try for each node</param>
        /// <param name="numThresholds">The number of thresholds to try for each node</param>
        /// <param name="labelNames">The names for the labels</param>
        /// <param name="labelWeights">An array of weights for each label</param>
        /// <returns>The trained forest</returns>
        public static DecisionForest<T,D> ComputeDepthFirst(
            int numTrees,
            List<T>[] splits,
            IFeatureFactory<T,D> factory,
            int numFeatures,
            int numThresholds,
            string[] labelNames,
            float[] labelWeights
            )
        {
            int numLabels = labelNames.Length;
            DecisionTree<T,D>[] trees = new DecisionTree<T,D>[numTrees];
            int count = 0;
            var indices = Enumerable.Range(0, numTrees).Select(o=>(byte)o);
            if(splits[0][0] is IComparable<T>)
                foreach (var split in splits)
                    split.Sort();
            foreach(var i in indices)
            {
                int split = i % splits.Length;
                UpdateManager.WriteLine(string.Format("Training tree {0} of {1}...", i + 1, numTrees));
                trees[i] = DecisionTree<T, D>.ComputeDepthFirst(splits[split], factory, numFeatures, numThresholds, numLabels, labelWeights);
                trees[i].LabelCount = labelNames.Length;
                count = trees[i].SetTreeLabel(i, count);
                UpdateManager.WriteLine("\ndone");
            };
            UpdateManager.WriteLine("Training complete");

            return new DecisionForest<T,D>(trees, labelNames);
        }

        /// <summary>
        /// Trains a decision forest from <paramref name="splits"/> based on the provided parameters using the breadth first algorithm.
        /// </summary>
        /// <param name="numTrees">Number of trees in the forest</param>
        /// <param name="splits">Data splits to use when training the tree.</param>
        /// <param name="factory">The feature factory</param>
        /// <param name="numFeatures">The number of features to try for each node</param>
        /// <param name="numThresholds">The number of thresholds to try for each node</param>
        /// <param name="labelNames">The names for the labels</param>
        /// <param name="labelWeights">An array of weights for each label</param>
        /// <param name="threshold">The threshold to use to determine a "good" feature test</param>
        /// <returns>The trained forest</returns>
        public static DecisionForest<T,D> ComputeBreadthFirst(
            int numTrees,
            List<T>[] splits,
            IFeatureFactory<T,D> factory,
            int numFeatures,
            int numThresholds,
            string[] labelNames,
            float[] labelWeights,
            float threshold
            )
        {
            int numLabels = labelNames.Length;
            DecisionTree<T,D>[] trees = new DecisionTree<T,D>[numTrees];
            int count = 0;
            for (byte i = 0; i < numTrees; i++)
            {
                UpdateManager.WriteLine(string.Format("Training tree {0} of {1}...", i+1, numTrees));
                int split = i % splits.Length;
                trees[i] = DecisionTree<T,D>.ComputeBreadthFirst(splits[split], factory, numFeatures, numThresholds, numLabels, labelWeights, threshold);
                count = trees[i].SetTreeLabel(i, count);
                UpdateManager.WriteLine("\ndone");
            }
            UpdateManager.WriteLine("Training complete");

            return new DecisionForest<T,D>(trees, labelNames);
        }
    }
}

