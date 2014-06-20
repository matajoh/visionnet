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

namespace VisionNET.Learning
{
    /// <summary>
    /// Class encapsulating a pair of values where one is used to rank the pair within a list.
    /// </summary>
    /// <typeparam name="T">Type of the non-ranking value</typeparam>
    public class RankPair<T> : IComparable<RankPair<T>>
    {
        private float _rank;
        private T _label;

        /// <summary>
        /// Constructor.
        /// </summary>
        public RankPair()
        {
        }

        /// <summary>
        /// Non-ranking, label value.
        /// </summary>
        public T Label
        {
            get
            {
                return _label;
            }
            set
            {
                _label = value;
            }
        }

        /// <summary>
        /// Ranking value.
        /// </summary>
        public float Rank
        {
            get
            {
                return _rank;
            }
            set
            {
                _rank = value;
            }
        }

        /// <summary>
        /// Returns a string representation of the pair as "label:rank".
        /// </summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", Label, Rank);
        }

        /// <summary>
        /// Compares this pair to <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The pair to compare to</param>
        /// <returns>A positive value if this is greater, negative if it is less than, and zero if equal</returns>
        public int CompareTo(RankPair<T> other)
        {
            return _rank.CompareTo(other.Rank);
        }
    }
}
