using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionNET.Texture
{
    /// <summary>
    /// LM Filter bank (see http://www.robots.ox.ac.uk/~vgg/research/texclass/filters.html for details)
    /// </summary>
    public class LeungMalik : FilterBank
    {
        private const double SCALE_MULT = 1.4142135623730950488016887242097;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="channel">The channel to use when computing the filter bank</param>
        /// <param name="small">Whether the small or larger version of the bank should be used</param>
        public LeungMalik(int channel, bool small = true) : base(generateFilters(channel, small))
        {
            
        }

        private static void addFilters(List<Filter> filters, double start, Func<float, float, Filter> create)
        {
            double stdDev = start;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 1; j < 7; j++)
                {
                    float orientation = (float)((j * Math.PI) / 6);
                    filters.Add(create((float)stdDev, orientation));
                }
                stdDev *= SCALE_MULT;
            }
        }

        private static Filter[] generateFilters(int channel, bool small)
        {
            List<Filter> filters = new List<Filter>();
            double start = small ? 1 : Math.Sqrt(2);
            addFilters(filters, start, (s, o) => new EdgeFilter(s, o, channel));
            addFilters(filters, start, (s, o) => new BarFilter(s, o, channel));

            double stddev = start;
            for (int i = 0; i < 4; i++)
            {
                filters.Add(new GaussianFilter((float)stddev, channel));
                stddev *= SCALE_MULT;
            }

            stddev = start;
            for (int i = 0; i < 8; i++)
            {
                filters.Add(new LaplacianFilter((float)stddev, channel));
                stddev *= SCALE_MULT;
            }
             
            return filters.ToArray();
        }
    }
}
