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
    public class FilterFeatureFactory : IFeatureFactory<ImageDataPoint<float>,float[]>
    {
        [Serializable]
        private class FilterFeature : IFeature<ImageDataPoint<float>, float[]>
        {
            private Filter _filter;
            public FilterFeature(Filter filter)
            {
                _filter = filter;
            }
            public float Compute(ImageDataPoint<float> point)
            {
                return _filter.Compute(point);
            }

            public string Name
            {
                get { return _filter.ToString(); }
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

        private static readonly float[][] WEIGHTS = {
                                        new float[]{.1f, .8f, .1f},
                                        new float[]{.33f, .34f, .33f},
                                        new float[]{.4f, .2f, .4f}
                                    };
        private IFilterFactory _factory;
        

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="factory">Factory to use to generate the filters</param>
        /// <param name="sampleWeights">Whether to randomly weight the filter samples</param>
        public FilterFeatureFactory(IFilterFactory factory, bool sampleWeights)
        {
            _factory = factory;
        }

        /// <summary>
        /// Creates a feature.
        /// </summary>
        /// <returns>A filter bank feature</returns>
        public IFeature<ImageDataPoint<float>, float[]> Create()
        {
            return new FilterFeature(_factory.Create());

        }

        /// <summary>
        /// Returns whether the provided feature was created by this factory.
        /// </summary>
        /// <param name="feature">The feature</param>
        /// <returns>Whether it is a filter bank feature</returns>
        public bool IsProduct(IFeature<ImageDataPoint<float>, float[]> feature)
        {
            return feature is FilterFeature;
        }
    }
}
