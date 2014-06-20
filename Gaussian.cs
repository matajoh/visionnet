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

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Double;

namespace VisionNET
{
    /// <summary>
    /// Class representing the second derivative of a Gaussian distribution in one dimension.
    /// </summary>
    [Serializable]
    public class GaussianSecondDerivative
    {
        private double _mean;
        private double _variance;
        private double _stddev;

        private double _a;
        private double _b;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mean">Desired mean</param>
        /// <param name="stddev">Desired standard deviation of the mean</param>
        public GaussianSecondDerivative(float mean, float stddev)
        {
            _mean = mean;
            _stddev = stddev;
            _variance = stddev * stddev;

            precomputeValues();
        }

        private void precomputeValues()
        {
            _a = 1 / (_variance * _variance * Math.Sqrt(2 * Math.PI * _variance));
            _b = -1 / (2 * _variance);
        }

        /// <summary>
        /// Mean of the Gaussian distribution.
        /// </summary>
        public double Mean
        {
            get
            {
                return _mean;
            }
            set
            {
                _mean = value;
            }
        }

        /// <summary>
        /// Standard deviation of the Gaussian distribution.
        /// </summary>
        public double StandardDeviation
        {
            get
            {
                return _stddev;
            }
            set
            {
                _stddev = value;
                _variance = value * value;
                precomputeValues();
            }
        }

        /// <summary>
        /// Computes the value of the function at the position indicated.
        /// </summary>
        /// <param name="x">The position to evaluate the function at</param>
        /// <returns>The value of the function at x</returns>
        public float Compute(float x)
        {
            double dx = x - _mean;
            return (float)((dx*dx - _variance) * _a * Math.Exp(dx * dx * _b));
        }

        /// <summary>
        /// Computes a kernel based on the second derivative of a Gaussian at the correct size.  This kernel
        /// will sum to 0.
        /// </summary>
        /// <param name="stddev">Standard deviation of the Gaussian second derivative</param>
        /// <returns>A kernel for convolution</returns>
        public static float[] ComputeKernel(float stddev)
        {
            GaussianSecondDerivative func = new GaussianSecondDerivative(0, stddev);
            int size = (int)Math.Ceiling(stddev * 6);
            if (size % 2 == 0)
                size++;
            float[] kernel = new float[size];
            float sum = 0;
            for (int i = 0; i < kernel.Length; i++)
            {
                float x = i - size / 2;
                kernel[i] = func.Compute(x);
                sum += kernel[i];
            }
            sum /= size;
            float[] result = new float[size];
            for (int i = 0; i < kernel.Length; i++)
                result[i] = kernel[i]  - sum;
            return result;
        }
    }

    /// <summary>
    /// Class representing the first derivative of a Gaussian distribution in one dimension.
    /// </summary>
    [Serializable]
    public class GaussianFirstDerivative
    {
        private double _mean;
        private double _variance;
        private double _stddev;

        private double _a;
        private double _b;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mean">Desired mean</param>
        /// <param name="stddev">Desired standard deviation of the mean</param>
        public GaussianFirstDerivative(float mean, float stddev)
        {
            _mean = mean;
            _variance = stddev * stddev;
            _stddev = stddev;

            precomputeValues();
        }

        private void precomputeValues()
        {
            _a = -1 / (_variance*Math.Sqrt(2 * Math.PI * _variance));
            _b = -1/(2 * _variance);
        }

        /// <summary>
        /// Mean of the Gaussian distribution.
        /// </summary>
        public double Mean
        {
            get
            {
                return _mean;
            }
            set
            {
                _mean = value;
            }
        }

        /// <summary>
        /// Standard deviation of the Gaussian distribution.
        /// </summary>
        public double StandardDeviation
        {
            get
            {
                return _stddev;
            }
            set
            {
                _stddev = value;
                _variance = value * value;
                precomputeValues();
            }
        }

        /// <summary>
        /// Computes the value of this function at the position indicated.
        /// </summary>
        /// <param name="x">The position to evalute at</param>
        /// <returns>The value of the function at x</returns>
        public float Compute(float x)
        {
            double dx = x - _mean;
            return (float)(dx*_a*Math.Exp(dx*dx*_b));
        }

        /// <summary>
        /// Computes a kernel based on the second derivative of a Gaussian at the correct size.  This kernel
        /// will sum to 0.
        /// </summary>
        /// <param name="stddev">Standard deviation of the Gaussian second derivative</param>
        /// <returns>A kernel for convolution</returns>
        public static float[] ComputeKernel(float stddev)
        {
            GaussianFirstDerivative func = new GaussianFirstDerivative(0, stddev);
            int size = (int)Math.Ceiling(stddev * 6);
            if (size % 2 == 0)
                size++;
            float[] kernel = new float[size];
            float sum = 0;
            for (int i = 0; i < kernel.Length; i++)
            {
                float x = i - size / 2;
                kernel[i] = func.Compute(x);
                sum += kernel[i];
            }
            sum /= size;
            float[] result = new float[size];
            for (int i = 0; i < kernel.Length; i++)
                result[i] = kernel[i] - sum;
            return result;
        }
    }

    /// <summary>
    /// Class representing a Gaussian (or Normal) distribution in one dimension.
    /// </summary>
    [Serializable]
    public class Gaussian
    {
        private double _mean;
        private double _variance;
        private double _stddev;

        private double _a;
        private double _b;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mean">Desired mean of the Gaussian</param>
        /// <param name="stdDev">Desired standard deviation of the mean.</param>
        public Gaussian(double mean, double stdDev)
        {
            _mean = mean;
            _variance = stdDev*stdDev;;
            _stddev = stdDev;

            precomputeValues();
        }

        private void precomputeValues()
        {
            _a = 1 / (Math.Sqrt(2 * Math.PI) * _stddev);
            _b = -1 / (2 * _variance);
        }

        /// <summary>
        /// The variance of the distribution.
        /// </summary>
        public double Variance
        {
            get
            {
                return _variance;
            }
            set
            {
                _variance = value;
                _stddev = Math.Sqrt(_variance);
                precomputeValues();
            }
        }

        /// <summary>
        /// The mean of the distribution.
        /// </summary>
        public double Mean
        {
            get
            {
                return _mean;
            }
            set
            {
                _mean = value;
            }
        }

        /// <summary>
        /// The standard deviation of the distribution.
        /// </summary>
        public double StandardDeviation
        {
            get
            {
                return _stddev;
            }
            set
            {
                _stddev = value;
                _variance = value * value;
                precomputeValues();
            }
        }

        /// <summary>
        /// Randomly samples the Gaussian distribution.
        /// </summary>
        /// <returns>A sample from the Gaussian distribution</returns>
        public float Sample()
        {
            double x1, x2, w, y1, y2;

            do
            {
                x1 = 2.0 * ThreadsafeRandom.NextDouble() - 1.0;
                x2 = 2.0 * ThreadsafeRandom.NextDouble() - 1.0;
                w = x1 * x1 + x2 * x2;
            } while (w >= 1.0);

            w = Math.Sqrt((-2.0 * Math.Log(w)) / w);
            y1 = x1 * w;
            y2 = x2 * w;

            return (float)(y1 * _stddev + _mean);
        }

        /// <summary>
        /// Estimates a Gaussian distribution from a collection of data points.
        /// </summary>
        /// <param name="data">The data points to model</param>
        /// <returns>An estimated distribution</returns>
        public static Gaussian Estimate(IEnumerable<float> data)
        {
            int n = 0;
            double mean = 0;
            double S = 0;

            foreach(float x in data){
                n++;
                double delta = x-mean;
                mean = mean + delta/n;
                S = S + delta*(x-mean);
            }

            double variance = S / (n - 1);
            return new Gaussian(mean, Math.Sqrt(variance));
        }

        /// <summary>
        /// Evaluates the Gaussian at x.
        /// </summary>
        /// <param name="x">The value to evaluate at</param>
        /// <returns>The Gaussian evaluated at x</returns>
        public double Compute(double x)
        {
            double dx = x - _mean;
            return _a * Math.Exp(_b * dx * dx);
        }

        /// <summary>
        /// Estimates a Gaussian distribution from a collection of data points.
        /// </summary>
        /// <param name="data">The data points to model</param>
        /// <returns>An estimated distribution</returns>
        public static Gaussian Estimate(IEnumerable<double> data)
        {
            int n = 0;
            double mean = 0;
            double S = 0;

            foreach (double x in data)
            {
                n++;
                double delta = x - mean;
                mean = mean + delta / n;
                S = S + delta * (x - mean);
            }

            double variance = S / (n - 1);
            return new Gaussian(mean, Math.Sqrt(variance));
        }

        /// <summary>
        /// Evaluates the Gaussian at x.
        /// </summary>
        /// <param name="x">The value to evaluate at</param>
        /// <returns>The Gaussian evaluated at x</returns>
        public float Compute(float x)
        {
            double dx = x - _mean;
            return (float)(_a * Math.Exp(_b * dx * dx));
        }

        /// <summary>
        /// Computes a Gaussian kernel, takes the form {center value, value 1 pixel from center, value 2 pixels from center, etc.}.
        /// </summary>
        /// <param name="stddev">The standard deviation of the Gaussian</param>
        /// <returns>The kernel</returns>
        public static float[] ComputeHalfKernel(float stddev)
        {
            int size = (int)Math.Ceiling(stddev * 3);
            float[] kernel = new float[size];
            float sum = 0;
            float half_inverse_squared = -.5f / (stddev * stddev);
            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] = (float)Math.Exp(i * i * half_inverse_squared);
                sum += kernel[i];
                if (i > 0)
                    sum += kernel[i];
            }
            float[] result = new float[size];
            for (int i = 0; i < kernel.Length; i++)
                result[i] = kernel[i] / sum;
            return result;
        }

        /// <summary>
        /// Convolves the input array with a Gaussian.
        /// </summary>
        /// <param name="input">The input array</param>
        /// <param name="stddev">The standard deviation of the Gaussian to use in convolution</param>
        /// <returns>The convolved array</returns>
        public static float[] Convolve(float[] input, float stddev)
        {
            int length = input.Length;
            float[] output = new float[length];
            float[] kernel = ComputeHalfKernel(stddev);
            int size = kernel.Length;

            for (int i = 0; i < length; i++)
            {
                output[i] = kernel[0] * input[i];
                for (int j = 1; j < size; j++)
                {
                    if (i - j < 0)
                        output[i] += 2 * kernel[j] * input[i + j];
                    else if (i + j >= length)
                        output[i] += 2 * kernel[j] * input[i - j];
                    else output[i] += kernel[j] * (input[i - j] + input[i + j]);
                }
            }
            return output;
        }

        /// <summary>
        /// Computes a kernel based on the second derivative of a Gaussian at the correct size.  This kernel
        /// will sum to 0.
        /// </summary>
        /// <param name="stddev">Standard deviation of the Gaussian second derivative</param>
        /// <returns>A kernel for convolution</returns>
        public static float[] ComputeKernel(float stddev)
        {
            Gaussian func = new Gaussian(0, stddev);
            int size = (int)Math.Ceiling(stddev * 6);
            if (size % 2 == 0)
                size++;
            float[] kernel = new float[size];
            float sum = 0;
            for (int i = 0; i < kernel.Length; i++)
            {
                float x = i - size / 2;
                kernel[i] = func.Compute(x);
                sum += kernel[i];
            }
            float[] result = new float[size];
            for (int i = 0; i < kernel.Length; i++)
                result[i] = kernel[i] / sum;
            return result;
        }
    }

    /// <summary>
    /// Class representing a bi-variate normal distribution with diagonal covariance.
    /// </summary>
    public class Gaussian2D
    {
        private Vector _mean;
        private Matrix _covariance;
        private Matrix _covarianceInverse;
        private double _A;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mean">Mean of the distribution</param>
        /// <param name="covariance">Covariance matrix</param>
        public Gaussian2D(Vector mean, Matrix covariance)
        {
            _mean = mean;
            _covariance = covariance;
            _covarianceInverse = (Matrix)covariance.Inverse();
            _A = 1 / Math.Pow(2 * Math.PI, mean.Count * .5);
            _A *= 1 / Math.Sqrt(_covariance.Determinant());
        }

        /// <summary>
        /// The mean of the distribution.
        /// </summary>
        public Vector Mean
        {
            get
            {
                return _mean;
            }
        }

        /// <summary>
        /// The covariance of the distribution.
        /// </summary>
        public Matrix Variance
        {
            get
            {
                return _covariance;
            }
        }

        /// <summary>
        /// Computes the value of the distribution at the provided point.
        /// </summary>
        /// <param name="x">The point to test</param>
        /// <returns>The value of the distribution at the provided point</returns>
        public double Compute(Vector x)
        {
            var X = (x - _mean).ToColumnMatrix();
            double exp = -0.5 * X.Transpose().Multiply(_covarianceInverse).Multiply(X)[0, 0];
            return _A * Math.Exp(exp);
        }
    }
}
