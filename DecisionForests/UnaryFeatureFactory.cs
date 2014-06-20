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
using VisionNET.Learning;
using System.Collections.Generic;

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// Enumeration of the various output modifiers that can be applied to a feature result.
    /// </summary>
    [Flags]
    public enum OutputModifier{
        /// <summary>
        /// Output is left unmodified
        /// </summary>
        None = 0,
        /// <summary>
        /// The log of the output is taken
        /// </summary>
        Log = 1,
        /// <summary>
        /// The absolute value of the output is used
        /// </summary>
        AbsoluteValue = 2,
        /// <summary>
        /// Apply all transformations to the output.
        /// </summary>
        All = 3
    }

    /// <remarks>
    /// Factory for unary features.  Will produce random unary image features for a decision tree based upon various parameters.
    /// </remarks>
    [Serializable]
    public class UnaryFeatureFactory<T> : IFeatureFactory<T,float[]> where T:IDataPoint<float[]>
    {
        [Serializable]
        private class UnaryFeature : IFeature<T,float[]>
        {
            private int _index;
            private bool _useLog;
            private bool _useAbsoluteValue;

            public UnaryFeature(int index, OutputModifier modifier)
            {
                _index = index;
                _useLog = (modifier & OutputModifier.Log) == OutputModifier.Log;
                _useAbsoluteValue = (modifier & OutputModifier.AbsoluteValue) == OutputModifier.AbsoluteValue;
            }

            #region IFeature<float> Members
            public Dictionary<string, object> Metadata
            {
                get { throw new NotImplementedException(); }
            }

            private static void fixValue(ref int value, int min, int max)
            {
                if (value < min)
                    value = min;
                else if (value >= max)
                {
                    value = max - 1;
                }
            }

            public float Compute(T point)
            {
                float val = point.Data[_index];
                if (_useLog)
                {
                    val = Math.Abs(val);
                    val = Math.Max(val, Decider<ImageDataPoint<float>, float[]>.DIRICHLET_PRIOR);
                    val = (float)Math.Log(val);
                }
                if (_useAbsoluteValue)
                    val = Math.Abs(val);
                return val;
            }

            public override string ToString()
            {
                if (_useAbsoluteValue)
                    return "|A|";
                else if (_useLog)
                    return "log(A)";
                return "A";
            }

            #endregion

            public int Index
            {
                get
                {
                    return _index;
                }
            }

            public bool UseLog
            {
                get
                {
                    return _useLog;
                }
            }

            public string Name
            {
                get { return ToString(); }
            }

            public string GenerateCode(string variableName)
            {
                return string.Format("{1} = x[{0}];", _index, variableName);
            }
        }

        private int _length;
        private int _possibleFeatureCount;
        private OutputModifier _modifier;
        private bool _random;
        private int _currentIndex;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="length">The dimensionality of the datapoints</param>
        /// <param name="modifier">The modifier to apply to the value at a dimension</param>
        /// <param name="chooseRandomly">Whether to choose dimensions randomly, or in a round-robin manner.</param>
        public UnaryFeatureFactory(int length, OutputModifier modifier, bool chooseRandomly)
        {
            _length = length;
            _possibleFeatureCount = _length;
            _modifier = modifier;
            _random = chooseRandomly;
        }

        /// <summary>
        /// Creates a new feature.
        /// </summary>
        /// <returns>An object which implements <see cref="T:IFeature" /></returns>
        public IFeature<T, float[]> Create()
        {
            int index;
            if (_random)
                index = ThreadsafeRandom.Next(0, _length);
            else
            {
                index = _currentIndex++;
                if (_currentIndex == _length)
                    _currentIndex = 0;
            }
            return new UnaryFeature(index, _modifier);
        }

        /// <summary>
        /// Returns whether <paramref name="feature"/> is a product of this factory.
        /// </summary>
        /// <param name="feature">Feature to test</param>
        /// <returns>True if it came from this factory, false otherwise.</returns>
        public bool IsProduct(IFeature<T, float[]> feature)
        {
            return feature is UnaryFeature;
        }
    }


    /// <remarks>
    /// Factory for unary features.  Will produce random unary image features for a decision tree based upon various parameters.
    /// </remarks>
    [Serializable]
    public class UnaryImageFeatureFactory : IFeatureFactory<ImageDataPoint<float>, float[]>
    {
        [Serializable]
        private class UnaryFeature : IFeature<ImageDataPoint<float>, float[]>
        {
            private int _row, _column, _channel;
            private bool _useLog;
            private bool _useAbsoluteValue;

            public UnaryFeature(int row, int column, int channel, OutputModifier modifier)
            {
                _row = row;
                _column = column;
                _channel = channel;
                _useLog = (modifier & OutputModifier.Log) == OutputModifier.Log;
                _useAbsoluteValue = (modifier & OutputModifier.AbsoluteValue) == OutputModifier.AbsoluteValue;
            }

            #region IFeature<float> Members
            public Dictionary<string, object> Metadata
            {
                get { throw new NotImplementedException(); }
            }

            private static void fixValue(ref int value, int min, int max)
            {
                if (value < min)
                    value = min;
                else if (value >= max)
                {
                    value = max - 1;
                }
            }

            public float Compute(ImageDataPoint<float> point)
            {
                int row = point.Row + _row;
                int column = point.Column + _column;
                IMultichannelImage<float> image = point.Image;
                fixValue(ref row, 0, image.Rows);
                fixValue(ref column, 0, image.Columns);

                float val = image.RawArray[row, column, _channel];
                if (_useLog)
                {
                    val = Math.Abs(val);
                    val = Math.Max(val, Decider<ImageDataPoint<float>,float[]>.DIRICHLET_PRIOR);
                    val = (float)Math.Log(val);
                }
                if (_useAbsoluteValue)
                    val = Math.Abs(val);
                return val;
            }

            public override string ToString()
            {
                if (_useAbsoluteValue)
                    return string.Format("|Unary|");
                else if (_useLog)
                    return string.Format("log(Unary)");
                return string.Format("Unary");
            }

            #endregion

            public int Row
            {
                get
                {
                    return _row;
                }
            }

            public int Column
            {
                get
                {
                    return _column;
                }
            }

            public int Channel
            {
                get
                {
                    return _channel;
                }
            }

            public bool UseLog
            {
                get
                {
                    return _useLog;
                }
            }

            #region ITestInfo Members

            public string Name
            {
                get { return ToString(); }
            }

            public ImageCell Cell0
            {
                get { return new ImageCell { Row = Row, Column = Column, Channel = Channel }; }
            }

            public ImageCell Cell1
            {
                get { return null; }
            }

            #endregion

            #region IFeature<ImageDataPoint<float>,float[]> Members


            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        private int _boxRows;
        private int _boxColumns;
        private int _numChannels;
        private int _possibleFeatureCount;
        private OutputModifier _modifier;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="boxRows">Number of rows in the box around a point in the image that can be used</param>
        /// <param name="boxColumns">Number of columns in the box around a point in the image that can be used</param>
        /// <param name="numChannels">Number of channels in the image to choose from</param>
        /// <param name="modifier">Modifier to apply to the output of the feature</param>
        public UnaryImageFeatureFactory(int boxRows, int boxColumns, int numChannels, OutputModifier modifier)
        {
            _boxRows = boxRows;
            _boxColumns = boxColumns;
            _numChannels = numChannels;
            _possibleFeatureCount = (2*_boxRows+1) * (2*_boxColumns+1) * _numChannels;
            _modifier = modifier;
        }

        /// <summary>
        /// Creates a new feature.
        /// </summary>
        /// <returns>An object which implements <see cref="T:IFeature" /></returns>
        public IFeature<ImageDataPoint<float>, float[]> Create()
        {
            int row, column;
            row = ThreadsafeRandom.Next(-_boxRows, _boxRows);
            column = ThreadsafeRandom.Next(-_boxColumns, _boxColumns);
            int channel = ThreadsafeRandom.Next(0, _numChannels);
            return new UnaryFeature(row, column, channel, _modifier);
        }

        /// <summary>
        /// Returns whether <paramref name="feature"/> is a product of this factory.
        /// </summary>
        /// <param name="feature">Feature to test</param>
        /// <returns>True if it came from this factory, false otherwise.</returns>
        public bool IsProduct(IFeature<ImageDataPoint<float>, float[]> feature)
        {
            return feature is UnaryFeature;
        }
    }
}
