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
using System.Collections.Generic;

namespace VisionNET.Learning
{
    /// <summary>
    /// Interface for a feature used in a decision forest.
    /// </summary>
    /// <typeparam name="T">A type of data point</typeparam>
    /// <typeparam name="D">The underlying type of the data point type</typeparam>
    public interface IFeature<T,D> where T:IDataPoint<D>
    {
        /// <summary>
        /// Computes the feature for <paramref name="point"/>.
        /// </summary>
        /// <param name="point">Point to use when computing the feature</param>
        /// <returns>The computed feature value</returns>
        float Compute(T point);
        /// <summary>
        /// The name of the feature.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Generates a hard-coded version of this feature test using the variable name provided.
        /// </summary>
        /// <param name="variableName">The variable name to use</param>
        /// <returns>Code which performs this feature test</returns>
        string GenerateCode(string variableName);

        /// <summary>
        /// Stores metadata about this feature.
        /// </summary>
        Dictionary<string, object> Metadata { get; }
    }

    /// <remarks>
    /// Interface for an object which creates other objects that implement <see cref="T:IFeature" />.
    /// </remarks>
    /// <typeparam name="T">A type of data point</typeparam>
    /// <typeparam name="D">The underlying type of the data point type</typeparam>
    public interface IFeatureFactory<T,D> where T:IDataPoint<D>
    {
        /// <summary>
        /// Creates a new feature.
        /// </summary>
        /// <returns>An object which implements <see cref="T:IFeature" /></returns>
        IFeature<T,D> Create();
        /// <summary>
        /// Returns whether <paramref name="feature"/> is a product of this factory.
        /// </summary>
        /// <param name="feature">Feature to test</param>
        /// <returns>True if it came from this factory, false otherwise.</returns>
        bool IsProduct(IFeature<T,D> feature);
    }
}
