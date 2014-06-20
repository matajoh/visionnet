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

namespace VisionNET
{
    /// <summary>
    /// Encapsulates a keypoint, with an x and y coordinate and optional scale, source scale, orientation and
    /// descriptor metadata.
    /// </summary>
    public class Keypoint
    {
        private double _x, _y, _imgScale, _scale, _orientation;
        private float[] _descriptor;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">X-coordinate of the point.</param>
        /// <param name="y">Y-coordinate of the point.</param>
        public Keypoint(double x, double y)
        {
            _x = x;
            _y = y;
        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">X-coordinate of the point</param>
        /// <param name="y">Y-coordinate of the point</param>
        /// <param name="imageScale">Scale of the source image (in relation to the original image)</param>
        /// <param name="scale">Scale of this point</param>
        /// <param name="orientation">Orientation of this point</param>
        public Keypoint(double x, double y, double imageScale, double scale, double orientation)
            : this(x, y)
        {
            _imgScale = imageScale;
            _scale = scale;
            _orientation = orientation;
        }

        /// <summary>
        /// Returns a string representation of the keypoint.
        /// </summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2} {3}", _x, _y, _scale, _orientation);
            if (HasDescriptor)
            {
                for (int i = 0; i < _descriptor.Length; i++)
                    sb.AppendFormat(" {0}", _descriptor[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// X-coordinate.
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                _x = value;
            }
        }

        /// <summary>
        /// Y-coordinate.
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                _y = value;
            }
        }

        /// <summary>
        /// Scale of the source image.
        /// </summary>
        public double ImageScale
        {
            get
            {
                return _imgScale;
            }
            set
            {
                _imgScale = value;
            }
        }
        
        /// <summary>
        /// Scale of the point.
        /// </summary>
        public double Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;
            }
        }

        /// <summary>
        /// Orientation of the point.
        /// </summary>
        public double Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                _orientation = value;
            }
        }

        /// <summary>
        /// Whether this point has a descriptor.
        /// </summary>
        public bool HasDescriptor
        {
            get
            {
                return _descriptor != null;
            }
        }

        /// <summary>
        /// Descriptor for this point.
        /// </summary>
        public float[] Descriptor
        {
            get
            {
                return _descriptor;
            }
            set
            {
                _descriptor = value;
            }
        }
    }
}
