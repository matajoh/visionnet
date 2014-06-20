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

using System.Windows.Media.Imaging;

namespace VisionNET
{
    /// <summary>
    /// Interface for images with multiple color/data channels.
    /// </summary>
    /// <typeparam name="T">Underlying type of the image</typeparam>
    public interface IMultichannelImage<T>:IArrayHandler<T>
    {
        /// <summary>
        /// Width of the image (equivalent to <see cref="P:Columns" />)
        /// </summary>
        int Width { get; }
        /// <summary>
        /// Height of the image (equivalment to <see cref="P:Rows" />)
        /// </summary>
        int Height { get; }
        /// <summary>
        /// Converts this image to a bitmap.
        /// </summary>
        /// <returns>A bitmap version of the image</returns>
        BitmapSource ToBitmap();
        /// <summary>
        /// ID for this image.
        /// </summary>
        string ID { get; set;  }
    }
}
