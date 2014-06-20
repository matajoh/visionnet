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

namespace VisionNET
{
    /// <summary>
    /// A rectangle.
    /// </summary>
    [Serializable]
    public struct Rectangle
    {
        private int _r;

        /// <summary>
        /// Starting row of the rectangle.
        /// </summary>
        public int R
        {
            get { return _r; }
            set { _r = value; }
        }
        private int _c;

        /// <summary>
        /// Starting column of the rectangle.
        /// </summary>
        public int C
        {
            get { return _c; }
            set { _c = value; }
        }
        private int _rows;

        /// <summary>
        /// Number of rows in the rectangle.
        /// </summary>
        public int Rows
        {
            get { return _rows; }
            set { _rows = value; }
        }
        private int _columns;

        /// <summary>
        /// Number of columns in the rectangle.
        /// </summary>
        public int Columns
        {
            get { return _columns; }
            set { _columns = value; }
        }

        /// <summary>
        /// Top edge of the rectangle.
        /// </summary>
        public int Top
        {
            get
            {
                return _r;
            }
        }

        /// <summary>
        /// Left edge of the rectangle.
        /// </summary>
        public int Left
        {
            get
            {
                return _c;
            }
        }

        /// <summary>
        /// Right edge of the rectangle.
        /// </summary>
        public int Right
        {
            get
            {
                return _c + _columns;
            }
        }
        
        /// <summary>
        /// Bottom edge of the rectangle.
        /// </summary>
        public int Bottom
        {
            get
            {
                return _r + _rows;
            }
        }

        /// <summary>
        /// Width of the rectangle (equivalent to <see cref="Columns"/>).
        /// </summary>
        public int Width
        {
            get
            {
                return Columns;
            }
            set
            {
                Columns = value;
            }
        }

        /// <summary>
        /// Height of the rectangle (equivalent to <see cref="Rows"/>).
        /// </summary>
        public int Height
        {
            get
            {
                return Rows;
            }
            set
            {
                Rows = value;
            }
        }

        /// <summary>
        /// Outputs a string version of the Rectangle object as [topmost row],[leftmost column],[height in rows],[width in columns].
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3}", R, C, Rows, Columns);
        }

        /// <summary>
        /// X-offset of the rectangle (equivalent to <see cref="C"/>).
        /// </summary>
        public int X
        {
            get
            {
                return C;
            }
            set
            {
                C = value;
            }
        }

        /// <summary>
        /// Y-offset of the rectangle (equivalent to <see cref="R"/>).
        /// </summary>
        public int Y
        {
            get
            {
                return R;
            }
            set
            {
                R = value;
            }
        }

        /// <summary>
        /// Returns a random rectangle within the rectangle provided.
        /// </summary>
        /// <param name="minRow">Minimum row</param>
        /// <param name="minColumn">Minimum column</param>
        /// <param name="maxRow">Maximum bound for a rectangle</param>
        /// <param name="maxColumn">Maximum bound for a column</param>
        /// <returns></returns>
        public static Rectangle Random(int minRow, int minColumn, int maxRow, int maxColumn)
        {
            Rectangle rect = new Rectangle();
            rect.R = ThreadsafeRandom.Next(minRow, maxRow);
            rect.C = ThreadsafeRandom.Next(minColumn, maxColumn);
            rect.Rows = ThreadsafeRandom.Next(maxRow - rect.R) + 1;
            rect.Columns = ThreadsafeRandom.Next(maxColumn - rect.C) + 1;
            return rect;
        }
    }
}
