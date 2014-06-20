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

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// Enumeration which encapsulates the decisions that a feature test can make: whether to choose the left or right child.
    /// </summary>
    public enum Decision {
        /// <summary>
        /// Choose the left tree.
        /// </summary>
        Left, 
        /// <summary>
        /// Choose the right tree.
        /// </summary>
        Right 
    };

    /// <summary>
    /// A class which encapsulates the feature test.  It is a combination of a feature test and a threshold, and is able to compute quickly for a collection
    /// of points whether for each it should go to the left or right child of the node to which the decider is attached.
    /// </summary>
    /// <typeparam name="T">A type of data point</typeparam>
    /// <typeparam name="D">The underlying type of the data point type</typeparam>
    [Serializable]
    public class Decider<T,D> : ITestInfo<T,D> where T:IDataPoint<D>
    {
        internal const float DIRICHLET_PRIOR = .0001f;

        [NonSerialized]
        private float[,] _leftDistributions;
        [NonSerialized]
        private float[,] _rightDistributions;
        [NonSerialized]
        private float[] _leftCounts;
        [NonSerialized]
        private float[] _rightCounts;
        [NonSerialized]
        private int _numThresholds;
        [NonSerialized]
        private int _numLabels;
        [NonSerialized]
        private LabelCounter _labelCounter;
        [NonSerialized]
        private int _numExamples;
        [NonSerialized]
        private float[] _values;
        [NonSerialized]
        private int[] _labels;
        [NonSerialized]
        private float[] _thresholds;
        [NonSerialized]
        private float _minValue;
        [NonSerialized]
        private float _maxValue;
        [NonSerialized]
        private Gaussian _gauss;
        [NonSerialized]
        private float _interval;
        [NonSerialized]
        private float _dirichlet;

        private void updateDataArrays(int numExamples)
        {
            _numExamples = numExamples;
            _values = new float[numExamples];
            _labels = new int[numExamples];

            _interval = (float)_numExamples / (_numThresholds + 1);
            _gauss = new Gaussian(0, _interval / 6);
        }

        private void updateThresholdArrays(int numThresholds, int numLabels)
        {
            _numThresholds = numThresholds;
            _numLabels = numLabels;
            _labelCounter = new LabelCounter(numThresholds, numLabels);
            _thresholds = new float[_numThresholds];
            _leftDistributions = new float[_numThresholds, _numLabels];
            _rightDistributions = new float[_numThresholds, _numLabels];
            _leftCounts = new float[_numThresholds];
            _rightCounts = new float[_numThresholds];

            _interval = (float)_numExamples / (_numThresholds + 1);
            _gauss = new Gaussian(0, _interval / 6);
            _dirichlet = DIRICHLET_PRIOR / numLabels;
        }

        private IFeature<T,D> _feature;
        private float _threshold;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="factory">The factory to use to create the feature for this Decider</param>
        public Decider(IFeatureFactory<T,D> factory) : this(factory.Create(), 0) { }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="feature">The feature to use with this decider</param>
        /// <param name="threshold">The threshold to use with this feature</param>
        public Decider(IFeature<T,D> feature, float threshold)
        {
            _feature = feature;
            _threshold = threshold;
        }

        /// <summary>
        /// Returns a string representation of the decider in the form "feature" "threshold".
        /// </summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", _feature, _threshold);
        }

        /// <summary>
        /// Sets the data this decider will use to choose a threshold.
        /// </summary>
        /// <param name="values">The values to use</param>
        /// <param name="labels">The labels</param>
        public void SetData(float[] values, int[] labels)
        {
            _numExamples = values.Length;
            _values = values;
            _labels = labels;

            _interval = (float)_numExamples / (_numThresholds + 1);
            _gauss = new Gaussian(0, _interval / 6);
        }

        /// <summary>
        /// Applies this decider's feature to all of the points, storing the value in the <see cref="T:ImageDataPoint.FeatureValue"/> property.
        /// </summary>
        /// <param name="points">Points to use when applying the feature</param>
        public void ApplyFeature(List<T> points)
        {
            int count = points.Count;
            for (int i = 0; i < count; i++)
                points[i].FeatureValue = _feature.Compute(points[i]);
        }

        /// <summary>
        /// Loads the data this decider will use for determining a threshold by applying the feature to each data point in <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The data to use when choosing a threshold</param>
        public void LoadData(List<T> data)
        {
            _minValue = float.MaxValue;
            _maxValue = float.MinValue;
            int count = data.Count;
            updateDataArrays(count);
            for (int i = 0; i < count; i++)
            {
                float value = _feature.Compute(data[i]);
                _minValue = Math.Min(value, _minValue);
                _maxValue = Math.Max(value, _maxValue);
                _values[i] = value;
                _labels[i] = data[i].Label;
            }
        }

        internal static void Normalize(float[] dist)
        {
            int numLabels = dist.Length;
            float dirichlet = DIRICHLET_PRIOR / numLabels;
            float sum = 0;
            for (int i = 0; i < numLabels; i++)
                sum += dist[i];
            float norm = 1 / (sum + numLabels * dirichlet);
            for (int i = 0; i < numLabels; i++)
                dist[i] = (dist[i] + dirichlet) * norm;
        }

        private float calculateEntropyGain(int threshold)
        {
            // Uses entropy measure from LePetit CVPR 2005 paper
            // Compute left and right entropies
            double normLeft = 1 / (_leftCounts[threshold] + _numLabels * _dirichlet);
            double normRight = 1 / (_rightCounts[threshold] + _numLabels * _dirichlet);
            double leftEntropy = 0, rightEntropy = 0;
            for (int c = 0; c < _numLabels; c++)
            {
                double leftClassProb = (_leftDistributions[threshold, c] + _dirichlet) * normLeft;
                leftEntropy -= leftClassProb * Math.Log(leftClassProb, 2);

                double rightClassProb = (_rightDistributions[threshold, c] + _dirichlet) * normRight;
                rightEntropy -= rightClassProb * Math.Log(rightClassProb, 2);
            }

            // Compute expected gain
            return (float)(-(_leftCounts[threshold] * leftEntropy + _rightCounts[threshold] * rightEntropy) / (_leftCounts[threshold]+_rightCounts[threshold]));
        }

        private void generateThresholds()
        {
            //// Array-index based
            //float[] points = new float[_numExamples];
            //Array.Copy(_values, points, _numExamples);
            //Array.Sort<float>(points);
            //_minValue = points[0];
            //_maxValue = points[points.Length-1];
            //for (int i = 0; i < _numThresholds; i++)
            //{
            //    int index = (int)((i + 1) * _interval + _gauss.Sample());
            //    if (index < 0)
            //        index = 0;
            //    if (index >= points.Length)
            //        index = points.Length-1;
            //for (int i = 0; i < _numThresholds; i++)
            //{
            //    int index = (int)(_rand.Next(0, _numExamples) + _gauss.Sample());
            //    if (index < 0)
            //        index = 0;
            //    if (index >= _numExamples)
            //        index = _numExamples - 1;

            //    _thresholds[i] = _values[index];
            //}

            //// Uniform sampling
            //    _thresholds[i] = points[index];
            //}
            //_minValue = float.MaxValue;
            //_maxValue = float.MinValue;
            //for (int i = 0; i < _numExamples; i++)
            //{
            //    _minValue = Math.Min(_minValue, _values[i]);
            //    _maxValue = Math.Max(_maxValue, _values[i]);
            //}
            //float scale = _maxValue - _minValue;
            //for (int i = 0; i < _numThresholds; i++)
            //    _thresholds[i] = (float)(_rand.NextDouble() * scale + _minValue);

            // Gaussian
            _gauss = Gaussian.Estimate(_values);
            _thresholds[0] = (float)_gauss.Mean;
            for (int i = 1; i < _numThresholds; i++)
                _thresholds[i] = _gauss.Sample();
            Array.Sort<float>(_thresholds);
        }

        /// <summary>
        /// The underlying feature of this decider.
        /// </summary>
        public IFeature<T,D> Feature
        {
            get
            {
                return _feature;
            }
        }

        /// <summary>
        /// The threshold used for a feature.
        /// </summary>
        public float Threshold
        {
            get
            {
                return _threshold;
            }
        }

        /// <summary>
        /// Chooses a threshold based on <paramref name="labelWeights"/> from <paramref name="numThresholds"/> choices, and outputs the resulting label distributions
        /// for the left and right children in <paramref name="leftDistribution"/> and <paramref name="rightDistribution"/>, respectively.
        /// </summary>
        /// <param name="numThresholds">Number of thresholds to try</param>
        /// <param name="numLabels">Number of labels</param>
        /// <param name="labelWeights">Weights for each label</param>
        /// <param name="leftDistribution">Resulting label distribution for the right child if the chosen threshold is used</param>
        /// <param name="rightDistribution">Resulting label distribution for the right child if the chosen threshold is used</param>
        /// <returns>The entropy gain from using the chosen threshold</returns>
        public float ChooseThreshold(int numThresholds, int numLabels, float[] labelWeights, out float[] leftDistribution, out float[] rightDistribution)
        {
            updateThresholdArrays(numThresholds, numLabels);
            generateThresholds();
            if (labelWeights != null)
                _labelCounter.Count(
                    _leftDistributions,
                    _rightDistributions,
                    _values,
                    _labels,
                    _thresholds,
                    _leftCounts,
                    _rightCounts,
                    labelWeights);
            else _labelCounter.Count(
                _leftDistributions,
                _rightDistributions,
                _values,
                _labels,
                _thresholds,
                _leftCounts,
                _rightCounts);
            int maxIndex = -1;
            float maxScore = float.MinValue;
            for (int i = 0; i < _numThresholds; i++)
            {
                float score = calculateEntropyGain(i);
                if (score > maxScore)
                {
                    maxScore = score;
                    maxIndex = i;
                }
            }
            leftDistribution = new float[_numLabels];
            rightDistribution = new float[_numLabels];
            for (int i = 0; i < _numLabels; i++)
            {
                leftDistribution[i] = _leftDistributions[maxIndex, i];
                rightDistribution[i] = _rightDistributions[maxIndex, i];
            }
            _threshold = _thresholds[maxIndex];
            if (_leftCounts[maxIndex] == 0 || _rightCounts[maxIndex] == 0)
                return float.MinValue;
            return maxScore;
        }

        /// <summary>
        /// Makes a decision (left or right child) for the <paramref name="point"/>.
        /// </summary>
        /// <param name="point">The point to make a decision about</param>
        /// <returns>Left or right child</returns>
        public Decision Decide(T point)
        {
            float value = _feature.Compute(point);
            point.FeatureValue = value;
            return value < _threshold ? Decision.Left : Decision.Right;
        }

        /// <summary>
        /// Makes a decision (left or right child) for each point in <paramref name="points"/>.
        /// </summary>
        /// <param name="points">Points to make decision about</param>
        /// <returns>Whether to go to the left or right child for each provided point</returns>
        public Decision[] Decide(List<T> points)
        {
            ApplyFeature(points);
            Decision[] decisions = new Decision[points.Count];
            for (int i = 0; i < decisions.Length; i++)
                decisions[i] = points[i].FeatureValue < _threshold ? Decision.Left : Decision.Right;
            return decisions;
        }

        /// <summary>
        /// Computes the feature for <paramref name="point"/>.
        /// </summary>
        /// <param name="point">Point to use when computing the feature</param>
        /// <returns>The computed feature value</returns>
        public float Compute(T point)
        {
            return _feature.Compute(point);
        }

        /// <summary>
        /// The name of the feature.
        /// </summary>
        public string Name
        {
            get { return _feature.Name; }
        }

        /// <summary>
        /// Generates a hard-coded version of this feature test using the variable name provided.
        /// </summary>
        /// <param name="variableName">The variable name to use</param>
        /// <returns>Code which performs this feature test</returns>
        public string GenerateCode(string variableName)
        {
            return _feature.GenerateCode(variableName);
        }

        /// <summary>
        /// Stores metadata about this feature.
        /// </summary>
        public Dictionary<string, object> Metadata
        {
            get { return _feature.Metadata; }
        }
    }
}
