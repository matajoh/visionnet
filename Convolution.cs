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


namespace VisionNET
{
    /// <summary>
	/// This class contains routines for convolving images with image filter kernels.
    /// </summary>
    public static class Convolution
    {
		/// <summary>
		/// Convolves the provided image with a two dimensional Gaussian of the provided sigma and returns the result.
		/// </summary>
        /// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
		/// <param name="image">The image to convolve.</param>
		/// <param name="sigma">The sigma to use in the Gaussian</param>
		/// <returns>A blurred image</returns>
		public static I ConvolveGaussian<I>(IArrayHandler<float> image, float sigma) where I : IArrayHandler<float>, new()
		{
			return ConvolveGaussian<I>(image, sigma, 1);
		}

		/// <summary>
		/// Convolves the provided image with a two dimensional Gaussian of the provided sigma and returns the result,
		/// subsampled as directed.
		/// </summary>
        /// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
		/// <param name="image">The image to convolve.</param>
		/// <param name="sigma">The sigma to use in the Gaussian</param>
		/// <param name="subsample">The subsampling frequency.</param>
		/// <returns>A blurred image</returns>
        public static I ConvolveGaussian<I>(IArrayHandler<float> image, float sigma, int subsample) where I : IArrayHandler<float>, new()
		{
			I result = ConvolveHalf<I>(image, Gaussian.ComputeHalfKernel(sigma), subsample);
			return result;
		}

        /// <summary>
        /// Convolves the image using the provided kernel for both horizontal and vertical convolution.
        /// The kernel is assumed to be radially invariant, seperable and 
        /// take  the form {center value, value 1 pixel from center, value 2 pixels from center, etc.}.
        /// </summary>
        /// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
        /// <param name="image">The image to convolve.</param>
        /// <param name="kernel">The kernel to use in both directions.</param>
        /// <param name="subsample">The amount to subsample the image.</param>
        /// <returns>A fitlered image</returns>
        public static I ConvolveHalf<I>(IArrayHandler<float> image, float[] kernel, int subsample) where I : IArrayHandler<float>, new()
        {
            return ConvolveHalf<I>(image, kernel, kernel, subsample);
        }

		/// <summary>
		/// Convolves an image with the provided kernel.  The kernel is assumed to be radially invariant, seperable and 
		/// take  the form {center value, value 1 pixel from center, value 2 pixels from center, etc.}.
		/// </summary>
        /// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
		/// <param name="image">The image to convolve.</param>
		/// <param name="kernel">The kernel to use for convolution</param>
		/// <returns>A fitlered image</returns>
        public static I ConvolveHalf<I>(IArrayHandler<float> image, float[] kernel) where I : IArrayHandler<float>, new()
		{
			return ConvolveHalf<I>(image, kernel, 1);
		}
  		/// <summary>
		/// Convolves an image with the provided kernels.  Both kernels are assumed to be radially invariant, seperable and 
		/// take the form {center value, value 1 pixel from center, value 2 pixels from center, etc.}.  The result is
		/// sub-sampled using the provided frequency.
		/// </summary>
		/// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
		/// <param name="image">The image to convolve.</param>
		/// <param name="kernelx">The kernel to use for convolution in the horizontal direction</param>
        /// <param name="kernely">The kernel to use for convolution in the vertical direction</param>
        /// <param name="subsample">The subsampling frequency</param>
		/// <returns>a fitlered image</returns>
        public static unsafe I ConvolveHalf<I>(IArrayHandler<float> image, float[] kernelx, float[] kernely, int subsample)
            where I : IArrayHandler<float>, new()
        {
            int rows = image.Rows;
            int columns = image.Columns;
			int channels = image.Channels;
			int sizex = kernelx.Length;
            int sizey = kernely.Length;
			float[, ,] dest = new float[rows, columns, channels];
            fixed (float* src = image.RawArray, dst = dest, knl = kernely)
            {
                float* srcPtr = src;
                float* dstPtr = dst;
                int stride = columns * channels;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        for (int i = 0; i < channels; i++, srcPtr++, dstPtr++)
                        {
                            float* knlPtr = knl;
                            float sum = *knlPtr * *srcPtr;
                            knlPtr++;
                            for (int k = 1; k < sizey; k++, knlPtr++)
                            {
                                int diff = k * stride;
                                int nr = r - k;
                                int pr = r + k;
                                if (nr < 0)
                                    sum += 2 * (*knlPtr * *(srcPtr + diff));
                                else if (pr >= rows)
                                    sum += 2 * (*knlPtr * *(srcPtr - diff));
                                else sum += *knlPtr * *(srcPtr - diff) + *knlPtr * *(srcPtr + diff);
                            }
                            *dstPtr = sum;
                        }
                    }
                }
            }
            int nrows = rows / subsample;
            int ncolumns = columns / subsample;
			float[,,] source = dest;
            dest = new float[nrows, ncolumns, channels];
            fixed (float* src = source, dst = dest, knl = kernelx)
            {
                int stride = columns * channels;
                float* srcPtr = src + stride*(subsample/2) + channels*(subsample/2);
                float* dstPtr = dst;

                for (int r = 0; r < nrows; r++, srcPtr += subsample * stride)
                {
                    float* srcScan = srcPtr;
                    for (int c = 0; c < ncolumns; c++, srcScan += channels * (subsample - 1))
                    {
                        for (int i = 0; i < channels; i++, srcScan++, dstPtr++)
                        {
                            float* knlPtr = knl;
                            float sum = *knlPtr * *srcScan;
                            knlPtr++;
                            for (int k = 1; k < sizex; k++, knlPtr++)
                            {
                                int nc = c - k;
                                int pc = c + k;
                                int diff = k * channels;
                                if (nc < 0)
                                    sum += 2 * (*knlPtr * *(srcScan + diff));
                                else if (pc >= columns)
                                    sum += 2 * (*knlPtr * *(srcScan - diff));
                                else sum += *knlPtr * *(srcScan + diff) + *knlPtr * *(srcScan - diff);
                            }
                            *dstPtr = sum;
                        }
                    }
                }
            }
            I result = new I();
            result.SetData(dest);
            return result;
        }

        /// <summary>
        /// Convolves an image with the provided kernels.  These Kernels are full kernels, in that they go from
        /// a minimum value to a maximum value.  There are no restrictions on what these kernels can be, though
        /// the user is cautioned to make sure that they are passing kernels which make sense, as this code
        /// does not check for any of the necessary kernel conditions.
        /// </summary>
        /// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
        /// <param name="image">The image to convolve.</param>
        /// <param name="kernel">The kernel to use for convolution in the horizontal direction</param>
        /// <returns>a fitlered image</returns>
        public static I ConvolveFull<I>(IArrayHandler<float> image, float[] kernel)
            where I : IArrayHandler<float>, new()
        {
            return ConvolveFull<I>(image, kernel, 1);
        }

        /// <summary>
        /// Convolves an image with the provided kernels.  These Kernels are full kernels, in that they go from
        /// a minimum value to a maximum value.  There are no restrictions on what these kernels can be, though
        /// the user is cautioned to make sure that they are passing kernels which make sense, as this code
        /// does not check for any of the necessary kernel conditions.
        /// </summary>
        /// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
        /// <param name="image">The image to convolve.</param>
        /// <param name="kernel">The kernel to use for convolution in the horizontal direction</param>
        /// <param name="subsample">The subsampling frequency</param>
        /// <returns>a fitlered image</returns>
        public static I ConvolveFull<I>(IArrayHandler<float> image, float[] kernel, int subsample)
            where I : IArrayHandler<float>, new()
        {
            return ConvolveFull<I>(image, kernel, kernel, subsample);
        }

        /// <summary>
        /// Convolves an image with the provided kernels.  These Kernels are full kernels, in that they go from
        /// a minimum value to a maximum value.  There are no restrictions on what these kernels can be, though
        /// the user is cautioned to make sure that they are passing kernels which make sense, as this code
        /// does not check for any of the necessary kernel conditions.
        /// </summary>
        /// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
        /// <param name="image">The image to convolve.</param>
        /// <param name="kernelx">The kernel to use for convolution in the horizontal direction</param>
        /// <param name="kernely">The kernel to use for convolution in the vertical direction</param>
        /// <param name="subsample">The subsampling frequency</param>
        /// <returns>a fitlered image</returns>
        public static unsafe I ConvolveFull<I>(IArrayHandler<float> image, float[] kernelx, float[] kernely, int subsample)
            where I : IArrayHandler<float>, new()
        {
            int rows = image.Rows;
            int columns = image.Columns;
            int channels = image.Channels;
            int sizex = kernelx.Length;
            int halfx = sizex / 2;
            int sizey = kernely.Length;
            int halfy = sizey / 2;
            float[, ,] dest = new float[rows, columns, channels];
            fixed (float* src = image.RawArray, dst = dest, knl = kernely)
            {
                float* srcPtr = src;
                float* dstPtr = dst;
                int stride = columns * channels;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        for (int i = 0; i < channels; i++, srcPtr++)
                        {
                            float* knlPtr = knl;
                            int diff = -halfy;
                            if (r + diff < 0)
                                diff = -r;
                            float* srcScan = srcPtr + diff*stride;
                            float sum = 0;
                            for (int k = 0, rr = r-halfy; k < sizey; k++, knlPtr++, rr++)
                            {
                                sum += *knlPtr * *srcScan;
                                if (rr >= 0 && rr < rows - 1)
                                    srcScan += stride;
                            }
                            *dstPtr++ = sum;
                        }
                    }
                }
            }
            int nrows = rows / subsample;
            int ncolumns = columns / subsample;
            float[, ,] source = dest;
            dest = new float[nrows, ncolumns, channels];
            fixed (float* src = source, dst = dest, knl = kernelx)
            {
                float* srcPtr = src;
                float* dstPtr = dst;
                int stride = columns * channels;

                for (int r = 0, tr = 0; r < nrows; r++, tr += subsample, srcPtr += subsample * stride)
                {
                    float* srcScan = srcPtr;
                    for (int c = 0, tc = 0; c < ncolumns; c++, tc += subsample, srcScan += channels * (subsample - 1))
                    {
                        for (int i = 0; i < channels; i++, srcScan++)
                        {
                            float* knlPtr = knl;
                            int diff = -halfx;
                            if (tc + diff < 0)
                                diff = -tc;
                            float* srcScan1 = srcScan + diff * channels;
                            float sum = 0;
                            for (int k = 0, cc=tc-halfx; k < sizex; k++, knlPtr++, cc++)
                            {
                                sum += *knlPtr * *srcScan1;
                                if (cc >= 0 && cc < columns - 1)
                                    srcScan1 += channels;
                            }
                            *dstPtr++ = sum;
                        }
                    }
                }
            }
            I result = new I();
            result.SetData(dest);
            return result;
        }

        /// <summary>
        /// Convolves an image with the provided two-dimensional kernel.  This is done in the spatial
        /// domain, and as such is not as efficient as using an Fast Fourier Transform.
        /// </summary>
        /// <typeparam name="I">Any image whose pixel values are stored as floats</typeparam>
        /// <param name="image">Image to convolve</param>
        /// <param name="kernel">The two-dimensional kernel.</param>
        /// <returns>The filtered image</returns>
        public static unsafe I Convolve<I>(IArrayHandler<float> image, float[,] kernel)
            where I : IArrayHandler<float>, new()
        {
            int rows = image.Rows;
            int columns = image.Columns;
            int channels = image.Channels;
            int krows = kernel.GetLength(1);
            int kcols = kernel.GetLength(0);
            int kernelCenterX = kcols/2;
            int kernelCenterY = krows/2;
            int stride = columns*channels;
            float[, ,] data = new float[rows, columns, channels];
            fixed (float* src = image.RawArray, dst = data, knl = kernel)
            {
                float* srcPtr = src + kernelCenterY * stride + kernelCenterX * channels;
                float* srcScanStart = src;
                float* dstPtr = dst + kernelCenterY * stride + kernelCenterX * channels;
                for (int r = kernelCenterY; r < rows - kernelCenterY; r++)
                {
                    for (int c = 0; c < columns; c++, srcPtr += channels, srcScanStart+= channels)
                    {
                        float[] sums = new float[channels];
                        float* srcScan = srcScanStart;
                        float* knlPtr = knl;
                        for (int u = 0; u < krows; u++, srcScan += stride - kcols * channels)
                            for (int v = 0; v < kcols; v++, knlPtr++)
                            {
                                float mult = *knlPtr;
                                for (int i = 0; i < channels; i++, srcScan++)
                                    sums[i] += mult * *srcScan;
                            }
                        for (int i = 0; i < channels; i++)
                            *dstPtr++ = sums[i];
                    }
                }
            }
            I result = new I();
            result.SetData(data);
            return result;
        }
    }
}
