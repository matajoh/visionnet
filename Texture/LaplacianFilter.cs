using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Texture
{
    /// <summary>
    /// A 2D filter modeled from the laplacian of a Gaussian.
    /// </summary>
    [Serializable]
    public class LaplacianFilter : Filter
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="stddev">Standard deviation of the mean</param>
        /// <param name="channel">Channel to use when computing filter response</param>
        public LaplacianFilter(float stddev, int channel)
            : base(ComputeFilter(stddev), channel)
        {
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
            GaussianSecondDerivative sd = new GaussianSecondDerivative(0, stddev);
            float[,] filter = new float[size, size];
            float sum = 0;
            for (int r = 0; r < size; r++)
            {
                float dr = r + .5f - center;
                for (int c = 0; c < size; c++)
                {
                    float dc = c + .5f - center;
                    float distance = (float)Math.Sqrt(dr * dr + dc * dc);
                    filter[r, c] = sd.Compute(distance);
                    sum += filter[r, c];
                }
            }
            sum /= size * size;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    filter[r, c] -= sum;

            return filter;
        }
    }
}
