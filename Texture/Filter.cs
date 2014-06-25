using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisionNET.Learning;

namespace VisionNET.Texture
{
    /// <summary>
    /// Convolution filter which is computed over a patch.
    /// </summary>
    [Serializable]
    public class Filter
    {
        private int _rows;
        private int _columns;
        private int _channel;
        private float[,] _filterValues;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filterValues">Array of filter values</param>
        /// <param name="channel">Channel to compute filter on</param>
        public Filter(float[,] filterValues, int channel)
        {
            _filterValues = filterValues;
            _rows = filterValues.GetLength(0);
            _columns = filterValues.GetLength(1);
            _channel = channel;
        }

        /// <summary>
        /// Number of rows in the filter.
        /// </summary>
        public int Rows
        {
            get
            {
                return _rows;
            }
        }

        /// <summary>
        /// Number of columns in the filter.
        /// </summary>
        public int Columns
        {
            get
            {
                return _columns;
            }
        }

        /// <summary>
        /// Returns a grayscale representation of the filter.
        /// </summary>
        /// <returns>A grayscale representation of the filter</returns>
        public GrayscaleImage GetRepresentation()
        {
            GrayscaleImage image = new GrayscaleImage(_rows, _columns);
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _columns; c++)
                    image[r, c] = _filterValues[r, c];
            return image;
        }

        /// <summary>
        /// Computes filter by multiplying the filter values against the patch values.  If the patch or filter
        /// sizes do not match up, the two will be centered on each other and computation done on overlapping
        /// indices.
        /// </summary>
        /// <param name="sample">Sample to compute</param>
        /// <param name="pyramid">Pyramid to use for computation</param>
        /// <returns>Filter response for the desired sample</returns>
        public unsafe float Compute<T>(ScaleSpaceSample sample, ScaleSpacePyramid<T> pyramid) where T : IMultichannelImage<float>, new()
        {
            return compute(sample.Row, sample.Column, pyramid[sample.Octave, sample.Level]);
        }

        private unsafe float compute(int row, int column, IMultichannelImage<float> image)
        {
            int startR = row - _rows / 2;
            int startC = column - _columns / 2;
            int stride = image.Columns * image.Channels;
            int rows = image.Rows;
            int columns = image.Columns;
            int channels = image.Channels;

            float sum = 0;
            fixed (float* patch = image.RawArray, filter = _filterValues)
            {
                float* filterPtr = filter;

                float* patchPtr;
                if (startR < 0)
                {
                    if (startC < 0)
                        patchPtr = patch + _channel;
                    else patchPtr = patch + startC * channels + _channel;
                }
                else if (startC < 0)
                    patchPtr = patch + startR * stride + _channel;
                else patchPtr = patch + startR * stride + startC * channels + _channel;
                for (int r = 0; r < _rows; r++)
                {
                    int rr = startR + r;
                    float* patchScan = patchPtr;
                    if (rr >= 0 && rr < rows - 1)
                        patchPtr += stride;
                    for (int c = 0; c < _columns; c++, filterPtr++)
                    {
                        float val = *patchScan;
                        int cc = startC + c;
                        if (cc >= 0 && cc < columns - 1)
                            patchScan += channels;
                        //if (val != image[Math.Max(0, Math.Min(rr, rows - 1)), Math.Max(0, Math.Min(cc, columns - 1)), _channel])
                        //    throw new Exception("testing");
                        sum += *filterPtr * val;
                    }
                }
            }
            return sum;
        }

        /// <summary>
        /// Computes filter by multiplying the filter values against the patch values.  If the patch or filter
        /// sizes do not match up, the two will be centered on each other and computation done on overlapping
        /// indices.
        /// </summary>
        /// <param name="point">Point to use when computing filter</param>
        /// <returns>Filter response for the desired sample</returns>
        public unsafe float Compute(ImageDataPoint<float> point)
        {
            return compute(point.Row, point.Column, point.Image);
        }
    }
}
