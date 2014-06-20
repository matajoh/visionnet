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

namespace VisionNET
{
    /// <summary>
    /// Class encapsulating a Gamma distribution.
    /// </summary>
    public class GammaDistribution
    {
        private static double gammastirf(double x)
        {
            double result = 0;
            double y = 0;
            double w = 0;
            double v = 0;
            double stir = 0;

            w = 1 / x;
            stir = 7.87311395793093628397E-4;
            stir = -2.29549961613378126380E-4 + w * stir;
            stir = -2.68132617805781232825E-3 + w * stir;
            stir = 3.47222221605458667310E-3 + w * stir;
            stir = 8.33333333333482257126E-2 + w * stir;
            w = 1 + w * stir;
            y = Math.Exp(x);
            if (x > 143.01608)
            {
                v = Math.Pow(x, 0.5 * x - 0.25);
                y = v * (v / y);
            }
            else
            {
                y = Math.Pow(x, x - 0.5) / y;
            }
            result = 2.50662827463100050242 * y * w;
            return result;
        }

        /// <summary>
        /// Computes the Gamma function for <paramref name="x"/>.
        /// </summary>
        /// <param name="x">Argument value</param>
        /// <returns>Result of the Gamma function for x</returns>
        public static double GammaFunction(double x)
        {
            double result = 0;
            double p = 0;
            double pp = 0;
            double q = 0;
            double qq = 0;
            double z = 0;
            int i = 0;
            double sgngam = 0;

            sgngam = 1;
            q = Math.Abs(x);
            if (q > 33.0)
            {
                if (x < 0.0)
                {
                    p = (int)Math.Floor(q);
                    i = (int)Math.Round(p);
                    if (i % 2 == 0)
                    {
                        sgngam = -1;
                    }
                    z = q - p;
                    if (z > 0.5)
                    {
                        p = p + 1;
                        z = q - p;
                    }
                    z = q * Math.Sin(Math.PI * z);
                    z = Math.Abs(z);
                    z = Math.PI / (z * gammastirf(q));
                }
                else
                {
                    z = gammastirf(x);
                }
                result = sgngam * z;
                return result;
            }
            z = 1;
            while (x >= 3)
            {
                x = x - 1;
                z = z * x;
            }
            while (x < 0)
            {
                if (x > -0.000000001)
                {
                    result = z / ((1 + 0.5772156649015329 * x) * x);
                    return result;
                }
                z = z / x;
                x = x + 1;
            }
            while (x < 2)
            {
                if (x < 0.000000001)
                {
                    result = z / ((1 + 0.5772156649015329 * x) * x);
                    return result;
                }
                z = z / x;
                x = x + 1.0;
            }
            if (x == 2)
            {
                result = z;
                return result;
            }
            x = x - 2.0;
            pp = 1.60119522476751861407E-4;
            pp = 1.19135147006586384913E-3 + x * pp;
            pp = 1.04213797561761569935E-2 + x * pp;
            pp = 4.76367800457137231464E-2 + x * pp;
            pp = 2.07448227648435975150E-1 + x * pp;
            pp = 4.94214826801497100753E-1 + x * pp;
            pp = 9.99999999999999996796E-1 + x * pp;
            qq = -2.31581873324120129819E-5;
            qq = 5.39605580493303397842E-4 + x * qq;
            qq = -4.45641913851797240494E-3 + x * qq;
            qq = 1.18139785222060435552E-2 + x * qq;
            qq = 3.58236398605498653373E-2 + x * qq;
            qq = -2.34591795718243348568E-1 + x * qq;
            qq = 7.14304917030273074085E-2 + x * qq;
            qq = 1.00000000000000000320 + x * qq;
            result = z * pp / qq;
            return result;
        }

        private double _k, _theta;
        private float _mean;
        private double _den;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="k">K parameter</param>
        /// <param name="theta">Theta parameter.</param>
        public GammaDistribution(double k, double theta)
        {
            _den = GammaFunction(k) * Math.Pow(theta, k);
            _k = k;
            _theta = theta;
            _mean = (float)(_k * _theta);
        }

        /// <summary>
        /// Mean of the distribution.
        /// </summary>
        public float Mean
        {
            get
            {
                return _mean;
            }
        }

        /// <summary>
        /// Computes the Gamma distribution for <paramref name="x"/>.
        /// </summary>
        /// <param name="x">The argument</param>
        /// <returns>The Gamma distribution computed at <paramref name="x"/></returns>
        public float Compute(float x)
        {
            double num = Math.Exp(-x / _theta);
            double mult = Math.Pow(x, _k - 1);
            return (float)(mult * num / _den);
        }

        private static double digamma(double k)
        {
            if (k >= 8)
            {
                double a = .1 - 1 / (21 * k * k);
                double b = 1 - a / (k * k);
                double c = 1 + b / (6 * k);
                return c / (2 * k);
            }
            else
            {
                return digamma(k + 1) - 1 / k;
            }
        }

        private static double trigamma(double k)
        {
            if (k >= 8)
            {
                double a = .2 - 1 / (7 * k * k);
                double b = 1 - a / (k * k);
                double c = 1 + b / (3 * k);
                double d = 1 + c / (2 * k);
                return d / k;
            }
            else
            {
                return trigamma(k + 1) + 1 / (k * k);
            }
        }

        private static double newton(double k, double s)
        {
            double num = Math.Log(k, Math.E) - digamma(k) - s;
            double den = 1 / k - trigamma(k);
            return k - num / den;
        }

        private static double K(double s)
        {
            // estimate of k
            double a = Math.Pow(s - 3, 2) + 24 * s;
            double num = 3 - s + Math.Sqrt(a);
            double k = num / (12 * s);
            // Newton's method
            double diff = double.MaxValue;
            while (diff > .000001)
            {
                double newK = newton(k,s);
                diff = Math.Abs(newK - k);
                k = newK;
            }
            return k;
        }

        /// <summary>
        /// Estimates a Gamma distribution from the data.
        /// </summary>
        /// <param name="data">Data from which to estimate a distribution</param>
        /// <returns>The estimated distribution</returns>
        public static GammaDistribution Estimate(List<float> data)
        {
            int N = data.Count;
            double sum = 0;
            double lnSum = 0;
            foreach (float d in data)
            {
                sum += d;
                lnSum += Math.Log(d, Math.E);
            }
            double s = Math.Log(sum / N, Math.E) - lnSum / N;
            double k = K(s);
            double theta = sum / (k * N);
            return new GammaDistribution(k, theta);
        }
    }
}
