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
using System.Linq;
using System.Text;

namespace VisionNET.Learning
{
    /// <summary>
    /// Interface for a generic datapoint used in learning.
    /// </summary>
    /// <typeparam name="T">Type used to localize the data point</typeparam>
    public interface IDataPoint<T> : ICloneable
    {
        /// <summary>
        /// The "location" of the datapoint.
        /// </summary>
        T Data { get; set; }
        /// <summary>
        /// The label associated with this datapoint.
        /// </summary>
        int Label { get; set; }
        /// <summary>
        /// A feature value computed for the datapoint.
        /// </summary>
        float FeatureValue { get; set; }
    }
}
