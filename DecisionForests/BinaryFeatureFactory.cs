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
    /// Enumeration of the different kinds of pixel combinations.
    /// </summary>
    public enum BinaryCombination {
        /// <summary>
        /// Subtracts one pixel from another
        /// </summary>
        Subtract,
        /// <summary>
        /// Adds two pixels together
        /// </summary>
        Add,
        /// <summary>
        /// Multiplies two pixels together
        /// </summary>
        Multiply,
        /// <summary>
        /// Divides one pixel by another
        /// </summary>
        Divide,
        /// <summary>
        /// Multiplies one pixel by the log of the other.
        /// </summary>
        Log
    };

    /// <summary>
    /// Factory which creates features that combine two pixels within the image.
    /// </summary>
    [Serializable]
    public class BinaryFeatureFactory<T> : IFeatureFactory<T, float[]> where T:IDataPoint<float[]>
    {
        [Serializable]
        private class BinaryFeature
        {
            protected int _index1, _index2;
            protected bool _absoluteValue;

            public BinaryFeature(
                int index1, int index2,
                OutputModifier modifier
                )
            {
                _index1 = index1;
                _index2 = index2;
                _absoluteValue = (modifier & OutputModifier.AbsoluteValue) == OutputModifier.AbsoluteValue;
            }

            private static void fixValue(ref int value, int min, int max)
            {
                if (value < min)
                    //value *= -1;
                    value = min;
                else if (value >= max)
                {
                    value = max - 1;
                    //int diff = value - max + 1;
                    //value -= 2 * diff;
                }
            }

            public bool AbsoluteValue
            {
                get
                {
                    return _absoluteValue;
                }
            }

            public int Index1
            {
                get
                {
                    return _index1;
                }
            }

            public int Index2
            {
                get
                {
                    return _index2;
                }
            }

            public override string ToString()
            {
                string s = GetType().Name;
                if (_absoluteValue)
                    return string.Format("|{0}|", s, _index1, _index2);
                else return string.Format("{0}", s, _index1, _index2);
            }

            public string Name
            {
                get { return ToString(); }
            }

            public Dictionary<string, object> Metadata
            {
                get { throw new NotImplementedException(); }
            }
        }

        [Serializable]
        private sealed class Subtract : BinaryFeature, IFeature<T, float[]>
        {
            public Subtract(
                int index1, int index2,
                OutputModifier modifier
                )
                : base(index1, index2, modifier)
            {
            }

            #region IFeature<float> Members

            public string GenerateCode(string variableName)
            {
                if (_absoluteValue)
                    return string.Format("{2} = x[{0}] - x[{1}]; {2} = {2} < 0 ? -{2} : {2};", _index1, _index2, variableName);
                return string.Format("{2} = x[{0}] - x[{1}];", _index1, _index2, variableName);
            }

            public float Compute(T point)
            {
                float value1 = point.Data[_index1];
                float value2 = point.Data[_index2];
                float val = value1 - value2;
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }

            #endregion
        }

        [Serializable]
        private sealed class Add : BinaryFeature, IFeature<T, float[]>
        {
            public Add(
                int index1, int index2,
                OutputModifier modifier
                )
                : base(index1, index2, modifier)
            {
            }
            #region IFeature<float> Members
            public string GenerateCode(string variableName)
            {
                if (_absoluteValue)
                    return string.Format("{2} = x[{0}] + x[{1}]; {2} = {2} < 0 ? -{2} : {2};", _index1, _index2, variableName);
                return string.Format("{2} = x[{0}] + x[{1}];", _index1, _index2, variableName);
            }

            public float Compute(T point)
            {
                float value1 = point.Data[_index1];
                float value2 = point.Data[_index2];
                float val = value1 + value2;
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }
            #endregion
        }

        [Serializable]
        private sealed class Log : BinaryFeature, IFeature<T, float[]>
        {
            public Log(
                int index1, int index2,
                OutputModifier modifier
                )
                : base(index1, index2, modifier)
            {
            }
            #region IFeature<float> Members
            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            public float Compute(T point)
            {
                float value1 = point.Data[_index1];
                float value2 = point.Data[_index2];
                if (value2 == 0)
                    value2 = Decider<T, float[]>.DIRICHLET_PRIOR;
                else value2 = Math.Abs(value2);
                float val = (float)(value1 * Math.Log(value2));
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }
            #endregion
        }

        [Serializable]
        private sealed class Divide : BinaryFeature, IFeature<T, float[]>
        {
            public Divide(
                int index1, int index2,
                OutputModifier modifier
                )
                : base(index1, index2, modifier)
            {
            }
            #region IFeature<float> Members
            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            public float Compute(T point)
            {
                float value1 = point.Data[_index1];
                float value2 = point.Data[_index2];
                if (value2 == 0)
                    value2 = Decider<ImageDataPoint<float>, float[]>.DIRICHLET_PRIOR;
                float val = value1 / value2;
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }
            #endregion
        }

        [Serializable]
        private sealed class Multiply : BinaryFeature, IFeature<T, float[]>
        {
            public Multiply(
                int index1, int index2,
                OutputModifier modifier
                )
                : base(index1, index2, modifier)
            {
            }

            #region IFeature<float> Members
            public string GenerateCode(string variableName)
            {
                if (_absoluteValue)
                    return string.Format("{2} = x[{0}] * x[{1}]; {2} = {2} < 0 ? -{2} : {2};", _index1, _index2, variableName);
                return string.Format("{2} = x[{0}] - x[{1}];", _index1, _index2, variableName);
            }

            public float Compute(T point)
            {
                float value1 = point.Data[_index1];
                float value2 = point.Data[_index2];
                float val = value1 * value2;
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }
            #endregion
        }

        private BinaryCombination _combo;
        private int _length;
        private int _possibleFeatureCount;
        private OutputModifier _modifier;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="length">The dimensionality of the data points</param>
        /// <param name="combo">How to combine two different dimensions</param>
        /// <param name="modifier">Which modification to apply to the result</param>
        public BinaryFeatureFactory(int length, BinaryCombination combo, OutputModifier modifier)
        {
            _length = length;
            _modifier = modifier;
            _combo = combo;
            _possibleFeatureCount = length * (length - 1);
        }

        /// <summary>
        /// Creates a new feature.
        /// </summary>
        /// <returns>An object which implements <see cref="T:IFeature" /></returns>
        public IFeature<T, float[]> Create()
        {
            int index1 = ThreadsafeRandom.Next(0, _length);
            int index2 = ThreadsafeRandom.Next(0, _length);
            while (index2 == index1)
                index2 = ThreadsafeRandom.Next(0, _length);

            switch (_combo)
            {
                case BinaryCombination.Add:
                    return new Add(index1, index2, _modifier);

                case BinaryCombination.Divide:
                    return new Divide(index1, index2, _modifier);

                case BinaryCombination.Log:
                    return new Log(index1, index2, _modifier);

                case BinaryCombination.Multiply:
                    return new Multiply(index1, index2, _modifier);

                case BinaryCombination.Subtract:
                default:
                    return new Subtract(index1, index2, _modifier);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="feature"/> is a product of this factory.
        /// </summary>
        /// <param name="feature">Feature to test</param>
        /// <returns>True if it came from this factory, false otherwise.</returns>
        public bool IsProduct(IFeature<T, float[]> feature)
        {
            return feature is BinaryFeature;
        }
    }

    /// <remarks>
    /// Factory which creates features that combine two pixels within the image.
    /// </remarks>
    [Serializable]
    public class BinaryImageFeatureFactory : IFeatureFactory<ImageDataPoint<float>, float[]>
    {
        [Serializable]
        private class BinaryFeature
        {
            private int _row1, _row2, _column1, _column2, _channel1, _channel2;
            protected bool _absoluteValue;

            public BinaryFeature(
                int row1, int column1, int channel1,
                int row2, int column2, int channel2,
                OutputModifier modifier
                )
            {
                _row1 = row1;
                _row2 = row2;
                _column1 = column1;
                _column2 = column2;
                _channel1 = channel1;
                _channel2 = channel2;
                _absoluteValue = (modifier & OutputModifier.AbsoluteValue) == OutputModifier.AbsoluteValue;
            }

            private static void fixValue(ref int value, int min, int max)
            {
                if (value < min)
                    //value *= -1;
                    value = min;
                else if (value >= max)
                {
                    value = max - 1;
                    //int diff = value - max + 1;
                    //value -= 2 * diff;
                }
            }

            protected float GetValue1(int row, int column, ImageDataPoint<float> point)
            {
                row += _row1;
                column += _column1;
                IMultichannelImage<float> image = point.Image;
                fixValue(ref row, 0, image.Rows);
                fixValue(ref column, 0, image.Columns);
                return image.RawArray[row, column, _channel1];
            }

            protected float GetValue2(int row, int column, ImageDataPoint<float> point)
            {
                row += _row2;
                column += _column2;
                IMultichannelImage<float> image = point.Image;
                fixValue(ref row, 0, image.Rows);
                fixValue(ref column, 0, image.Columns);
                return image.RawArray[row, column, _channel2];
            }

            public bool AbsoluteValue
            {
                get
                {
                    return _absoluteValue;
                }
            }

            public int Row1
            {
                get
                {
                    return _row1;
                }
            }

            public int Column1
            {
                get
                {
                    return _column1;
                }
            }

            public int Channel1
            {
                get
                {
                    return _channel1;
                }
            }

            public int Row2
            {
                get
                {
                    return _row2;
                }
            }

            public int Column2
            {
                get
                {
                    return _column2;
                }
            }

            public int Channel2
            {
                get
                {
                    return _channel2;
                }
            }

            public override string ToString()
            {
                string s = GetType().Name;
                if (_absoluteValue)
                    return string.Format("|{0}|", s, _channel1, _channel2);
                else return string.Format("{0}", s, _channel1, _channel2);
            }

            #region ITestInfo Members

            public string Name
            {
                get { return ToString(); }
            }

            public Dictionary<string, object> Metadata
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }

        [Serializable]
        private sealed class Subtract : BinaryFeature, IFeature<ImageDataPoint<float>, float[]>
        {
            public Subtract(
                int row1, int column1, int channel1,
                int row2, int column2, int channel2,
                OutputModifier modifier
                )
                : base(row1, row2, channel1, row2, column2, channel2, modifier)
            {
            }
                
            #region IFeature<float> Members

            public float Compute(ImageDataPoint<float> point)
            {
                int row = point.Row;
                int column = point.Column;
                float value1 = GetValue1(row,column, point);
                float value2 = GetValue2(row,column, point);
                float val = value1 - value2;
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }

            #endregion

            #region IFeature<ImageDataPoint<float>,float[]> Members


            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        [Serializable]
        private sealed class Add : BinaryFeature, IFeature<ImageDataPoint<float>, float[]>
        {
            public Add(
                int row1, int column1, int channel1,
                int row2, int column2, int channel2,
                OutputModifier modifier
                )
                : base(row1, row2, channel1, row2, column2, channel2, modifier)
            {
            }
            #region IFeature<float> Members

            public float Compute(ImageDataPoint<float> point)
            {
                int row = point.Row;
                int column = point.Column;
                float value1 = GetValue1(row, column, point);
                float value2 = GetValue2(row, column, point);
                float val = value1 + value2;
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }
            #endregion

            #region IFeature<ImageDataPoint<float>,float[]> Members


            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        [Serializable]
        private sealed class Log : BinaryFeature, IFeature<ImageDataPoint<float>, float[]>
        {
            public Log(
                int row1, int column1, int channel1,
                int row2, int column2, int channel2,
                OutputModifier modifier
                )
                : base(row1, row2, channel1, row2, column2, channel2, modifier)
            {
            }
            #region IFeature<float> Members

            public float Compute(ImageDataPoint<float> point)
            {
                int row = point.Row;
                int column = point.Column;
                float value1 = GetValue1(row, column,point);
                float value2 = GetValue2(row, column,point);
                if (value2 == 0)
                    value2 = Decider<ImageDataPoint<float>, float[]>.DIRICHLET_PRIOR;
                else value2 = Math.Abs(value2);
                float val = (float)(value1 * Math.Log(value2));
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }
            #endregion

            #region IFeature<ImageDataPoint<float>,float[]> Members


            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        [Serializable]
        private sealed class Divide : BinaryFeature, IFeature<ImageDataPoint<float>, float[]>
        {
            public Divide(
                int row1, int column1, int channel1,
                int row2, int column2, int channel2,
                OutputModifier modifier
                )
                : base(row1, row2, channel1, row2, column2, channel2, modifier)
            {
            }
            #region IFeature<float> Members

            public float Compute(ImageDataPoint<float> point)
            {
                int row = point.Row;
                int column = point.Column;
                float value1 = GetValue1(row, column,point);
                float value2 = GetValue2(row, column,point);
                if (value2 == 0)
                    value2 = Decider<ImageDataPoint<float>, float[]>.DIRICHLET_PRIOR;
                float val = value1 / value2;
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }
            #endregion

            #region IFeature<ImageDataPoint<float>,float[]> Members


            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        [Serializable]
        private sealed class Multiply : BinaryFeature, IFeature<ImageDataPoint<float>, float[]>
        {
            public Multiply(
                int row1, int column1, int channel1,
                int row2, int column2, int channel2,
                OutputModifier modifier
                )
                : base(row1, row2, channel1, row2, column2, channel2, modifier)
            {
            }

            #region IFeature<float> Members

            public float Compute(ImageDataPoint<float> point)
            {
                int row = point.Row;
                int column = point.Column;
                float value1 = GetValue1(row, column,point);
                float value2 = GetValue2(row, column,point);
                float val = value1 * value2;
                if (_absoluteValue)
                    val = Math.Abs(val);
#if DEBUG
                if (float.IsNegativeInfinity(val) || float.IsNaN(val) || float.IsPositiveInfinity(val))
                    throw new Exception("invalid feature value!");
#endif
                return val;
            }
            #endregion

            #region IFeature<ImageDataPoint<float>,float[]> Members


            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        private BinaryCombination _combo;
        private int _boxRows;
        private int _boxColumns;
        //private double _previousValueProbability;
        private int _numChannels;
        private bool _mixChannels;
        private int _possibleFeatureCount;
        private OutputModifier _modifier;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="boxRows">Rows in the box around a data point to sample points</param>
        /// <param name="boxColumns">Rows in the box around a data point to sample points</param>
        /// <param name="numChannels">Number of channels to sample from</param>
        /// <param name="combo">Which binary combination to use</param>
        /// <param name="mixChannels">Whether to mix one channel with another, or to choose both points from the same channel</param>
        /// <param name="modifier">Modifier on the output of the feature</param>
        public BinaryImageFeatureFactory(int boxRows, int boxColumns, int numChannels, BinaryCombination combo, bool mixChannels, OutputModifier modifier)
        {
            _boxRows = boxRows;
            _boxColumns = boxColumns;
            _numChannels = numChannels;
            _mixChannels = mixChannels;
            _modifier = modifier;
            _combo = combo;
            int numOperands = (2 * boxRows + 1) * (2 * boxColumns + 1);
            int channelMultiplier = mixChannels ? numChannels * numChannels : numChannels;
            _possibleFeatureCount = numOperands * numOperands * channelMultiplier;
        }

        private int randomRow()
        {
            return ThreadsafeRandom.Next(-_boxRows, _boxRows);
        }

        private int randomColumn()
        {
            return ThreadsafeRandom.Next(-_boxColumns, _boxColumns);
        }

        /// <summary>
        /// Creates a new feature.
        /// </summary>
        /// <returns>An object which implements <see cref="T:IFeature" /></returns>
        public IFeature<ImageDataPoint<float>, float[]> Create()
        {
            int row1, row2, column1, column2, channel1, channel2;
            row1 = randomRow();
            row2 = randomRow();
            column1 = randomColumn();
            column2 = randomColumn();
            channel1 = ThreadsafeRandom.Next(0, _numChannels);
            if (_mixChannels)
                channel2 = ThreadsafeRandom.Next(0, _numChannels);
            else channel2 = channel1;

            switch (_combo)
            {
                case BinaryCombination.Add:
                    return new Add(row1, column1, channel1, row2, column2, channel2, _modifier);

                case BinaryCombination.Divide:
                    return new Divide(row1, column1, channel1, row2, column2, channel2, _modifier);

                case BinaryCombination.Log:
                    return new Log(row1, column1, channel1, row2, column2, channel2, _modifier);

                case BinaryCombination.Multiply:
                    return new Multiply(row1, column1, channel1, row2, column2, channel2, _modifier);

                case BinaryCombination.Subtract:
                default:
                    return new Subtract(row1, column1, channel1, row2, column2, channel2, _modifier);
            }
        }

        /// <summary>
        /// Returns whether <paramref name="feature"/> is a product of this factory.
        /// </summary>
        /// <param name="feature">Feature to test</param>
        /// <returns>True if it came from this factory, false otherwise.</returns>
        public bool IsProduct(IFeature<ImageDataPoint<float>, float[]> feature)
        {
            return feature is BinaryFeature;
        }
    }
}
