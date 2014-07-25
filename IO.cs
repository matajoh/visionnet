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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace VisionNET
{
    /// <summary>
    /// Utility class for writing objects to and reading objects from the disk.  If there exists a static "Write" or "Read" method in the class definition, it will
    /// attempt to use it for serialization, otherwise it will use the default .NET serializer.
    /// </summary>
    public static class IO
    {
        private static BinaryFormatter _format = new BinaryFormatter();

        /// <summary>
        /// Returns the appropriate bitmap encoder for the file, if it exists.
        /// </summary>
        /// <param name="filename">The filename to check</param>
        /// <returns>An encoder, if it exists</returns>
        public static BitmapEncoder GetEncoder(string filename)
        {
            filename = filename.ToLower();
            if (filename.EndsWith(".png"))
                return new PngBitmapEncoder();
            if (filename.EndsWith(".bmp"))
                return new BmpBitmapEncoder();
            if (filename.EndsWith(".jpg") || filename.EndsWith(".jpeg"))
                return new JpegBitmapEncoder();
            if (filename.EndsWith(".gif"))
                return new GifBitmapEncoder();
            if (filename.EndsWith(".tiff") || filename.EndsWith(".tif"))
                return new TiffBitmapEncoder();
            throw new ArgumentException("Unknown file ending, no decoder available.");
        }

        /// <summary>
        /// Writes <paramref name="item"/> to the disk at location <paramref name="filename"/>.
        /// </summary>
        /// <typeparam name="T">Type of the object to write</typeparam>
        /// <param name="filename">Location on the disk to write to</param>
        /// <param name="item">The item to write</param>
        public static void Write<T>(string filename, T item)
        {
            using (FileStream stream = File.Open(filename, FileMode.Create, FileAccess.Write))
            {
                Write<T>(stream, item);
            }
        }
        /// <summary>
        /// Writes <paramref name="item"/> to <paramref name="stream"/>.  If a method of the exact same signature exists in <typeparamref name="T"/> then it will attempt to use that one first,
        /// otherwise it will use the default .NET serializer.
        /// </summary>
        /// <typeparam name="T">Type of the object to write</typeparam>
        /// <param name="stream">Stream to write to</param>
        /// <param name="item">Item to write</param>
        public static void Write<T>(Stream stream, T item)
        {
            MethodInfo info = typeof(T).GetMethod("Write");
            if (info != null)
            {
                info.Invoke(null, new object[] { stream, item });
            }
            else
            {
                _format.Serialize(stream, item);
            }
        }

        /// <summary>
        /// Reads an object of type <typeparamref name="T"/> from <paramref name="stream"/>.  If there exists a Read method of the same signature in <typeparamref name="T"/> then it will
        /// attempt to use that one first, otherwise it will use the default .NET Serializer.
        /// </summary>
        /// <typeparam name="T">Type of the object to read</typeparam>
        /// <param name="stream">The stream to read from</param>
        /// <returns>The object</returns>
        public static T Read<T>(Stream stream)
        {
            MethodInfo info = typeof(T).GetMethod("Read");
            if(info != null)
                return (T)info.Invoke(null, new object[]{stream});
            return (T)_format.Deserialize(stream);
        }

        /// <summary>
        /// Reads an object of type <typeparamref name="T"/> from <paramref name="filename"/> on the disk.
        /// </summary>
        /// <typeparam name="T">The type of the object to read</typeparam>
        /// <param name="filename">The location on the disk to attempt to read</param>
        /// <returns>The object</returns>
        public static T Read<T>(string filename)
        {
            using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read<T>(stream);
            }
        }
    }
}
