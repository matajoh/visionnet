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
using System.Text;

namespace VisionNET.Learning
{
    /// <summary>
    /// A class encapsulating a set of labels.  This is a set in the sense that all values with it are unique, i.e. no repeated labels.
    /// </summary>
    [Serializable]
    public class LabelSet : IEnumerable<short>, IComparable<LabelSet>
    {
        private List<short> _labels;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="labels">List of labels which may or may not have repeat values</param>
        public LabelSet(IEnumerable<short> labels)
        {
            _labels = new List<short>();
            foreach (short label in labels)
                Add(label);
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="labels">Labels to use when creating the set</param>
        public LabelSet(params short[] labels):this(new List<short>(labels))
        {
        }

        /// <summary>
        /// Indexes the set.
        /// </summary>
        /// <param name="index">Set index</param>
        /// <returns>The label at <paramref name="index"/></returns>
        public short this[int index]
        {
            get
            {
                return _labels[index];
            }
        }

        /// <summary>
        /// Number of labels in the set.
        /// </summary>
        public int Count
        {
            get
            {
                return _labels.Count;
            }
        }

        /// <summary>
        /// Whether this set is equal to another set.
        /// </summary>
        /// <param name="obj">Set to compare to</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            return CompareTo((LabelSet)obj) == 0;
        }

        /// <summary>
        /// Returns a unique hash code for the label set.
        /// </summary>
        /// <returns>A hash code for this object</returns>
        public override int GetHashCode()
        {
            int hash = 0;
            foreach (short label in _labels)
                hash *= label.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the set of the form "[value1, value2, ...]".
        /// </summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            if (_labels.Count == 0)
                return "[]";
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[{0}", _labels[0]);
            for (int i = 1; i < _labels.Count; i++)
                sb.AppendFormat(", {0}", _labels[i]);
            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Adds <paramref name="label"/> to the set.
        /// </summary>
        /// <param name="label">Label to add</param>
        /// <returns>True if successful, false if label is already in the set</returns>
        public bool Add(short label)
        {
            int index = _labels.BinarySearch(label);
            if (index >= 0)
                return false;
            _labels.Insert(~index, label);
            return true;
        }

        /// <summary>
        /// Removes a label from the set.
        /// </summary>
        /// <param name="label">Label to remove</param>
        public void Remove(short label)
        {
            _labels.Remove(label);
        }

        /// <summary>
        /// Whether the set contains <paramref name="label"/>.
        /// </summary>
        /// <param name="label">The label to look for</param>
        /// <returns>Whether the set contains <paramref name="label"/></returns>
        public bool Contains(short label)
        {
            return _labels.BinarySearch(label) >= 0;
        }

        /// <summary>
        /// Returns an enumerator for the set values.
        /// </summary>
        /// <returns>An enumerator</returns>
        public IEnumerator<short> GetEnumerator()
        {
            return _labels.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator for the set values.
        /// </summary>
        /// <returns>An enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Compares this set to <paramref name="other"/>.
        /// </summary>
        /// <param name="other">Set to compare to.</param>
        /// <returns>Whether this set is "less" or "more" than <paramref name="other"/></returns>
        public int CompareTo(LabelSet other)
        {
            if (other.Count == Count)
            {
                List<short> otherLabels = other._labels;
                for (int i = 0; i < _labels.Count; i++)
                    if (_labels[i] != otherLabels[i])
                        return _labels[i].CompareTo(otherLabels[i]);
                return 0;
            }
            else return Count.CompareTo(other.Count);
        }

        /// <summary>
        /// Returns a string representation of this label set using <paramref name="labelDictionary"/> to lookup the label values for representative strings.
        /// </summary>
        /// <param name="labelDictionary">Lookup dictionary</param>
        /// <returns>A string representation</returns>
        public string ToString(LabelDictionary labelDictionary)
        {
            if (_labels.Count == 0)
                return "[]";
            string[] names = labelDictionary.LabelNames;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[{0}", names[_labels[0]]);
            for (int i = 1; i < _labels.Count; i++)
                sb.AppendFormat(", {0}", names[_labels[i]]);
            sb.Append("]");
            return sb.ToString();
        }
    }
}
