using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionNET.Texture
{
    /// <summary>
    /// A Point Filter, essentially a Gaussian filter offset from the 
    /// </summary>
    public class PointFilter : Filter
    {
        private int _row, _column;
        private float _stddev;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="size">The size of the filter patch</param>
        /// <param name="row">The row of the point</param>
        /// <param name="column">The column of the point</param>
        /// <param name="stddev">The standard deviation of the Gaussian</param>
        /// <param name="channel">The channel to use when computing the filter</param>
        public PointFilter(int size, int row, int column, float stddev, int channel) : base(ComputeFilter(size, row, column, stddev), channel)
        {
            _row = row;
            _column = column;
            _stddev = stddev;
        }

        /// <summary>
        /// Computes a 2-dimensional convolution filter for the provided sigma and point.
        /// </summary>
        /// <param name="column">Column of the point</param>
        /// <param name="row">Row of the point</param>
        /// <param name="size">Size of the patch</param>
        /// <param name="stddev">Standard deviation of the Gaussian second derivative</param>
        /// <returns>Convolution filter</returns>
        public static float[,] ComputeFilter(int size, int row, int column, float stddev)
        {
            Gaussian gauss = new Gaussian(0, stddev);
            float[,] filter = new float[size, size];
            for (int r = 0; r < size; r++)
            {
                float dr = r + .5f - row;
                for (int c = 0; c < size; c++)
                {
                    float dc = c + .5f - column;
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
            return string.Format("{0} point s={1:f4} x=({2},{3})", base.ToString(), _stddev, _row, _column);
        }   
    }
}
