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


namespace VisionNET
{
    /// <summary>
    /// A "cell", or rectangular location in a grid imposed over an image.
    /// </summary>
    public class ImageCell
    {
        private int _row, _column, _channel;

        /// <summary>
        /// The channel of the location.
        /// </summary>
        public int Channel
        {
            get { return _channel; }
            set { _channel = value; }
        }

        /// <summary>
        /// The column of the location.
        /// </summary>
        public int Column
        {
            get { return _column; }
            set { _column = value; }
        }

        /// <summary>
        /// The row of the location.
        /// </summary>
        public int Row
        {
            get { return _row; }
            set { _row = value; }
        }

        /// <summary>
        /// X-coordinate of the point (equivalent to Column)
        /// </summary>
        public int X
        {
            get
            {
                return _column;
            }
        }

        /// <summary>
        /// Y-coordinate of the point (equivalent to Row)
        /// </summary>
        public int Y
        {
            get
            {
                return _row;
            }
        }

        /// <summary>
        /// A string representation of the form "[Row, Column, Channel]".
        /// </summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            return string.Format("[{0}, {1}, {2}]", Row, Column, Channel);
        }
    }
}
