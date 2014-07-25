using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Texture
{
    /// <summary>
    /// A Gaussian filter.
    /// </summary>
    [Serializable]
    public class GaussianFilter : Filter
    {
        private float _stddev;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stddev">Standard deviation of the mean</param>
        /// <param name="channel">Channel to use when computing filter response</param>
        public GaussianFilter(float stddev, int channel)
            : base(ComputeFilter(stddev), channel)
        {
            _stddev = stddev;
        }

        /// <summary>
        /// Computes a 2-dimensional convolution filter for the provided sigma.
        /// </summary>
        /// <param name="stddev">Standard deviation of the Gaussian second derivative</param>
        /// <returns>Convolution filter</returns>
        public static float[,] ComputeFilter(float stddev)
        {
            int halfsize = (int)Math.Ceiling(stddev * 3);
            int size = halfsize * 2 + 1;
            float center = size * .5f;
            Gaussian gauss = new Gaussian(0, stddev);
            float[,] filter = new float[size, size];
            for (int r = 0; r < size; r++)
            {
                float dr = r + .5f - center;
                for (int c = 0; c < size; c++)
                {
                    float dc = c + .5f - center;
                    float distance = (float)Math.Sqrt(dr * dr + dc * dc);
                    float value = gauss.Compute(distance);
                    filter[r, c] = value;
                }
            }
            return filter;
        }

        /// <summary>
        /// Generates a string that describes the filter.
        /// </summary>
        /// <returns>A useful description</returns>
        public override string ToString()
        {
            return string.Format("{0} gauss s={1:f4}", base.ToString(), _stddev);
        }        
    }
}
