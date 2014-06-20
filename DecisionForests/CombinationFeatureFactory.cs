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
using VisionNET.Learning;

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// A feature factory made up of other feature factories.  A random factory is chosen
    /// </summary>
    /// <typeparam name="T">A type of data point</typeparam>
    /// <typeparam name="D">The underlying type of the data point type</typeparam>
    [Serializable]
    public class CombinationFeatureFactory<T,D> : IFeatureFactory<T, D> where T:IDataPoint<D>
    {
        private List<IFeatureFactory<T, D>> _factories;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CombinationFeatureFactory()
        {
            _factories = new List<IFeatureFactory<T,D>>();
        }

        /// <summary>
        /// Adds <paramref name="factory"/> to the list of factories this combination factory will choose from.
        /// </summary>
        /// <param name="factory">Factory to add</param>
        public void AddFactory(IFeatureFactory<T, D> factory)
        {
            _factories.Add(factory);
        }

        /// <summary>
        /// Removes <paramref name="factory"/> from the list of factories this combination factory chooses from.
        /// </summary>
        /// <param name="factory">Factory to remove</param>
        public void RemoveFactory(IFeatureFactory<T, D> factory)
        {
            _factories.Remove(factory);
        }

        #region IFeatureFactory<T> Members

        /// <summary>
        /// Creates a new feature.
        /// </summary>
        /// <returns>An object which implements <see cref="T:IFeature" /></returns>
        public IFeature<T,D> Create()
        {
            return _factories.SelectRandom().Create();
        }

        /// <summary>
        /// Returns whether <paramref name="feature"/> is a product of this factory.
        /// </summary>
        /// <param name="feature">Feature to test</param>
        /// <returns>True if it came from this factory, false otherwise.</returns>
        public bool IsProduct(IFeature<T,D> feature)
        {
            foreach (IFeatureFactory<T,D> factory in _factories)
                if (factory.IsProduct(feature))
                    return true;
            return false;
        }

        #endregion
    }
}
