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
using System.Windows.Media;
using System.IO;

namespace VisionNET.Learning
{
    /// <summary>
    /// Class encapsulating a lookup dictionary for colors and labels.
    /// </summary>
    [Serializable]
    public class LabelDictionary
    {
        private Dictionary<Color, short> _labelLookup;
        private string[] _labelNames;
        private int _count;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="labelNames">Names of the labels</param>
        /// <param name="colors">Colors corresponding to the labels</param>
        public LabelDictionary(string[] labelNames, Color[] colors)
        {
            _labelNames = labelNames;
            _labelLookup = new Dictionary<Color, short>();
            for (short label = 0; label < colors.Length; label++)
                _labelLookup[colors[label]] = label;
            _count = _labelNames.Length;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="labelNames">The names of the labels</param>
        public LabelDictionary(string[] labelNames)
        {
            _labelNames = labelNames;
            _labelLookup = new Dictionary<Color, short>();
            for (short label = 0; label < _labelNames.Length; label++)
                _labelLookup[LabelToColor(label)] = label;
            _count = _labelNames.Length;
        }

        /// <summary>
        /// Convert a pixel to an id number.
        /// </summary>
        /// <param name="color">The color value.</param>
        /// <returns>The id number.</returns>
        public static short ColorToLabel(Color color)
        {
            int r = color.R;
            int g = color.G;
            int b = color.B;

            // Convert to id
            int id = 0;
            for (int j = 0; j < 8; j++)
                id = (id << 3)
                    | (((r >> j) & 1) << 0)
                    | (((g >> j) & 1) << 1)
                    | (((b >> j) & 1) << 2);
            return (short)id;
        }

        /// <summary>
        /// Convert an id number to a pixel.
        /// </summary>
        /// <param name="label">The id number.</param>
        /// <returns>The color.</returns>
        public static Color LabelToColor(short label)
        {
            // Convert id to rgb pixel
            int r = 0, g = 0, b = 0;
            for (int j = 0; label > 0; j++, label >>= 3)
            {
                r |= ((label >> 0) & 1) << (7 - j);
                g |= ((label >> 1) & 1) << (7 - j);
                b |= ((label >> 2) & 1) << (7 - j);
            }
            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Writes <paramref name="dict"/> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="dict">The dictionary to write</param>
        public static void Write(Stream stream, LabelDictionary dict)
        {
            BinaryWriter output = new BinaryWriter(stream);
            output.Write(dict.Count);
            foreach (Color key in dict.LabelLookup.Keys)
            {
                short label = dict.LabelLookup[key];
                if (label >= dict.Count)
                    continue;
                string name = dict.LabelNames[label];
                output.Write(label);
                output.Write(name);
                output.Write(key.R);
                output.Write(key.G);
                output.Write(key.B);
            }
        }

        /// <summary>
        /// Reads a dictionary from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <returns>A label dictionary</returns>
        public static LabelDictionary Read(Stream stream)
        {
            BinaryReader input = new BinaryReader(stream);
            int count = input.ReadInt32();
            Color[] colors = new Color[count];
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                short label = input.ReadInt16();
                string name = input.ReadString();
                byte R = input.ReadByte();
                byte G = input.ReadByte();
                byte B = input.ReadByte();
                colors[label] = Color.FromRgb(R, G, B);
                names[label] = name;
            }
            return new LabelDictionary(names, colors);
        }

        /// <summary>
        /// Number of labels in the dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Lookup dictionary for colors to labels.
        /// </summary>
        public Dictionary<Color, short> LabelLookup
        {
            get
            {
                return _labelLookup;
            }
        }

        /// <summary>
        /// Names of the labels.
        /// </summary>
        public string[] LabelNames
        {
            get
            {
                return _labelNames;
            }
        }
    }
}
