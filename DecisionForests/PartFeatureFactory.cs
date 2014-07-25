using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisionNET.Learning;

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// Creates part features, which consist of weighted offset responses across multiple channels.
    /// </summary>
    public class PartFeatureFactory : IFeatureFactory<ImageDataPoint<float>, float[]>
    {
        [Serializable]
        private class Part{
            private int _row, _column, _channel;
            private float _weight;

            public Part(int row, int column, int channel, float weight)
            {
                _row = row;
                _column = column;
                _channel = channel;
                _weight = weight;
            }
            public float Compute(float[,,] data, int row, int column, int rows, int columns)
            {
                row += _row;
                column += _column;
                if (row < 0 || row >= rows || column < 0 || column >= columns)
                    return 0;
                return data[row, column, _channel] * _weight;
            }
            public override string ToString()
            {
                return string.Format("({0},{1},{2}):{3:f4}", _row, _column, _channel, _weight);
            }
        }

        [Serializable]
        private class PartFeature : IFeature<ImageDataPoint<float>, float[]>
        {
            private Part[] _parts;

            public PartFeature(Part[] parts)
            {
                _parts = parts;
            }

            public float Compute(ImageDataPoint<float> point)
            {
                float[,,] data = point.Image.RawArray;
                int rows = data.GetLength(0);
                int columns = data.GetLength(1);
                int row = point.Row;
                int column = point.Column;
                float sum = 0;
                foreach(Part part in _parts)
                    sum += part.Compute(data, row, column, rows, columns);
                return sum;
            }

            public string Name
            {
                get { return string.Join(",", _parts.Select(o=>o.ToString())); }
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

        private int _boxSize;
        private int _numChannels;
        private int _numParts;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="boxSize">Radius of offset points to sample from</param>
        /// <param name="numChannels">Number of channels to sample from</param>
        /// <param name="numParts">Number of parts to combine</param>
        public PartFeatureFactory(int boxSize, int numChannels, int numParts)
        {
            _boxSize = boxSize;
            _numChannels = numChannels;
            _numParts = numParts;
        }

        /// <summary>
        /// Creates a new part feature.
        /// </summary>
        /// <returns>The part feature</returns>
        public IFeature<ImageDataPoint<float>, float[]> Create()
        {
            Part[] parts = new Part[_numParts];
            for (int i = 0; i < _numParts; i++)
            {
                parts[i] = new Part(
                    ThreadsafeRandom.Next(-_boxSize, _boxSize),
                    ThreadsafeRandom.Next(-_boxSize, _boxSize),
                    ThreadsafeRandom.Next(0, _numChannels),
                    1f - ThreadsafeRandom.NextFloat(2)
                );
            }
            return new PartFeature(parts);
        }

        /// <summary>
        /// Determines whether the provided feature was created by this factory.
        /// </summary>
        /// <param name="feature">The feature to test</param>
        /// <returns>Whether this feature was created by this factory</returns>
        public bool IsProduct(IFeature<ImageDataPoint<float>, float[]> feature)
        {
            return feature is PartFeature;
        }
    }
}
