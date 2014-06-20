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
using System.Threading;
using System.Linq;

namespace VisionNET
{
    /// <summary>
    /// The .NET Random class will create hard to trace, truly heinous bugs if called asynchronously from multiple threads.  In order to avoid this, I have
    /// included this class for those who need to do so.  Also, it has several utility methods for those who want to generate more than a random integer
    /// or double value.
    /// </summary>
    public static class ThreadsafeRandom
    {
        private static Random _rand;
        private static Semaphore _sem;

        /// <summary>
        /// Selects a random member of a list.  This is an extension method for all classes which implement <see cref="T:IEnumerable"/>.
        /// </summary>
        /// <typeparam name="T">Type of the list</typeparam>
        /// <param name="list">The list from which to select</param>
        /// <returns>A random member of the list</returns>
        public static T SelectRandom<T>(this IEnumerable<T> list)
        {
            int count = list.Count();
            int index = Next(count);
            return list.ElementAt(index);
        }

        static ThreadsafeRandom()
        {
            _rand = new Random();
            _sem = new Semaphore(1, 1);
        }

        /// <summary>
        /// Initializes the randomizer with <paramref name="newSeed"/>.
        /// </summary>
        /// <param name="newSeed">The new seed to use</param>
        public static void Initialize(int newSeed)
        {
            _sem.WaitOne();
            _rand = new Random(newSeed);
            _sem.Release();
        }

        /// <summary>
        /// Generates a random double.
        /// </summary>
        /// <returns>A random double value</returns>
        public static double NextDouble()
        {
            _sem.WaitOne();
            double val = _rand.NextDouble();
            _sem.Release();
            return val;
        }

        /// <summary>
        /// Generates a random integer.
        /// </summary>
        /// <returns>A random integer value</returns>
        public static int Next()
        {
            _sem.WaitOne();
            int val = _rand.Next();
            _sem.Release();
            return val;
        }

        /// <summary>
        /// Generates a random integer in the range of <paramref name="minValue"/> to <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="minValue">The minimum value to generate</param>
        /// <param name="maxValue">The maximum value to generate</param>
        /// <returns>A random integer</returns>
        public static int Next(int minValue, int maxValue)
        {
            _sem.WaitOne();
            int val = _rand.Next(minValue, maxValue);
            _sem.Release();
            return val;
        }

        /// <summary>
        /// Generates a random integer between 0 and <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="maxValue">The maximum value to generate</param>
        /// <returns>A random integer</returns>
        public static int Next(int maxValue)
        {
            _sem.WaitOne();
            int val = _rand.Next(maxValue);
            _sem.Release();
            return val;
        }

        /// <summary>
        /// Generates a random float between 0 and <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="maxValue">The maximum value to generate</param>
        /// <returns>A random float</returns>
        public static float NextFloat(float maxValue)
        {
            return NextFloat(0, maxValue);
        }
        /// <summary>
        /// Generates a random float in the range of <paramref name="minValue"/> to <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="minValue">The minimum value to generate</param>
        /// <param name="maxValue">The maximum value to generate</param>
        /// <returns>A random float</returns>
        public static float NextFloat(float minValue, float maxValue)
        {
            return NextFloat() * (maxValue - minValue) + minValue;
        }
        /// <summary>
        /// Generates a random float.
        /// </summary>
        /// <returns>A random float value</returns>
        public static float NextFloat()
        {
            return (float)NextDouble();
        }

        /// <summary>
        /// Generates a random double between 0 and <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="maxValue">The maximum value to generate</param>
        /// <returns>A random double</returns>
        public static double NextDouble(double maxValue)
        {
            return NextDouble(0, maxValue);
        }
        /// <summary>
        /// Generates a random double in the range of <paramref name="minValue"/> to <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="minValue">The minimum value to generate</param>
        /// <param name="maxValue">The maximum value to generate</param>
        /// <returns>A random double</returns>
        public static double NextDouble(double minValue, double maxValue)
        {
            return NextDouble() * (maxValue - minValue) + minValue;
        }

        /// <summary>
        /// Returns true if a random value is less than <paramref name="threshold"/>.
        /// </summary>
        /// <param name="threshold">A threshold between 0 and 1</param>
        /// <returns>True if a random value is less than <paramref name="threshold"/>, false if otherwise</returns>
        public static bool Test(double threshold)
        {
            return NextDouble() < threshold;
        }

        /// <summary>
        /// Generate a random distribution of the provided length.
        /// </summary>
        /// <param name="length">The length of desired distribution</param>
        /// <returns>The randomized distribution</returns>
        public static double[] GenerateRandomDistribution(int length)
        {
            double[] dist = new double[length];
            double sum = 0;
            for (int i = 0; i < dist.Length; i++)
            {
                double val = Math.Pow(NextDouble(), 6);
                dist[i] = val;
                sum += val;
            }
            double norm = 1.0 / sum;
            for (int i = 0; i < dist.Length; i++)
                dist[i] *= norm;
            return dist;
        }
    }
}
