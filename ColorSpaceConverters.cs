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
    /// Convenience class with static members for the color space converters supported natively by Vision.NET.
    /// </summary>
    public static class ColorSpaceConverters
    {
        /// <summary>
        /// Converts RGB to YUV.
        /// </summary>
        public static ColorSpaceConverter RGB2YUV
        {
            get
            {
                return new ColorSpaceConverter(ColorConversion.RGB2YUV);
            }
        }

        /// <summary>
        /// Converts RGB to HSV.
        /// </summary>
        public static ColorSpaceConverter RGB2HSV
        {
            get
            {
                return new ColorSpaceConverter(ColorConversion.RGB2HSV);
            }
        }

        /// <summary>
        /// Converts RGB to Lab.
        /// </summary>
        public static ColorSpaceConverter RGB2Lab
        {
            get
            {
                return new ColorSpaceConverter(ColorConversion.RGB2Lab);
            }
        }

        /// <summary>
        /// Converts Lab to RGB.
        /// </summary>
        public static ColorSpaceConverter Lab2RGB
        {
            get
            {
                return new ColorSpaceConverter(ColorConversion.Lab2RGB);
            }
        }

        /// <summary>
        /// Converts HSV to RGB.
        /// </summary>
        public static ColorSpaceConverter HSV2RGB
        {
            get
            {
                return new ColorSpaceConverter(ColorConversion.HSV2RGB);
            }
        }

        /// <summary>
        /// Converts YUV to RGB.
        /// </summary>
        public static ColorSpaceConverter YUV2RGB
        {
            get
            {
                return new ColorSpaceConverter(ColorConversion.YUV2RGB);
            }
        }
    }
}
