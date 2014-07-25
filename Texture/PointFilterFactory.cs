using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionNET.Texture
{
    /// <summary>
    /// A factory creates random Point Filters.
    /// </summary>
    public class PointFilterFactory : IFilterFactory
    {
        private PointFilter[][] _filters;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="size">Size of the filter patch</param>
        /// <param name="channels">Number of potential channels</param>
        /// <param name="stddev">The distribution from which to draw standard deviation values</param>
        /// <param name="border">The border at the edge of the patch to exclude from sampling</param>
        public PointFilterFactory(int size, int channels, int border, float stddev)
        {
            _filters = new PointFilter[channels][];
            for (int i = 0; i < channels; i++)
            {
                int window = size - 2 * border;
                _filters[i] = new PointFilter[window*window];
                for (int r = border, j = 0; r < size - border; r++)
                    for (int c = border; c < size - border; c++, j++)
                        _filters[i][j] = new PointFilter(size, r, c, stddev, i);
            }
        }

        /// <summary>
        /// Create a new point filter filter.
        /// </summary>
        /// <returns>The new filter</returns>
        public Filter Create()
        {
            return _filters.SelectRandom().SelectRandom();
        }
    }
}
