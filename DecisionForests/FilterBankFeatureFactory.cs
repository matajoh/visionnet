using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisionNET.Learning;
using VisionNET.Texture;

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// Factory which creates features that compute a filter bank at a point and then index into that filter bank's responses.
    /// </summary>
    [Serializable]
    public class FilterBankFeatureFactory : IFeatureFactory<ImageDataPoint<float>,float[]>
    {
        [Serializable]
        private class FilterBankFeature : IFeature<ImageDataPoint<float>, float[]>
        {
            private FilterBank _fb;
            private int _index;
            private short _row, _column;
            public FilterBankFeature(FilterBank fb, short row, short column, int index)
            {
                _fb = fb;
                _index = index;
                _row = row;
                _column = column;
            }
            public float Compute(ImageDataPoint<float> point)
            {
                ImageDataPoint<float> test = point.Clone() as ImageDataPoint<float>;
                test.Row += _row;
                test.Column += _column;
                return _fb.Compute(test)[_index];
            }

            public string Name
            {
                get { return string.Format("{0}:{1} at ({2},{3})", _fb, _index, _row,_column); }
            }

            public string GenerateCode(string variableName)
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, object> Metadata
            {
                get { throw new NotImplementedException(); }
            }
        }

        private FilterBank[] _banks;
        private int _boxSize;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="banks">Banks to select from when computing features.</param>
        /// <param name="boxSize">The distance to sample away from a point (i.e. boxSize of 5 results in points that are (-5,5) from the point)</param>
        public FilterBankFeatureFactory(FilterBank[] banks, int boxSize)
        {
            _banks = banks;
            _boxSize = boxSize;
        }

        /// <summary>
        /// Creates a feature.
        /// </summary>
        /// <returns>A filter bank feature</returns>
        public IFeature<ImageDataPoint<float>, float[]> Create()
        {
            short row = (short)ThreadsafeRandom.Next(-_boxSize, _boxSize);
            short column = (short)ThreadsafeRandom.Next(-_boxSize, _boxSize);
            FilterBank fb = ThreadsafeRandom.SelectRandom(_banks);
            int index = ThreadsafeRandom.Next(fb.DescriptorLength);
            return new FilterBankFeature(fb, row, column, index);
        }

        /// <summary>
        /// Returns whether the provided feature was created by this factory.
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <returns>Whether it is a filter bank feature</returns>
        public bool IsProduct(IFeature<ImageDataPoint<float>, float[]> feature)
        {
            return feature is FilterBankFeature;
        }
    }
}
