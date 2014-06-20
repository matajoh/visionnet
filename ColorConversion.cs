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
    /// Color conversion routines for changing colors to and from the RGB color space to other color spaces.
    /// </summary>
    public static class ColorConversion
    {
        /// <summary>
        /// Converts HSV to RGB in place.
        /// </summary>
        /// <param name="h">hue</param>
        /// <param name="s">saturation</param>
        /// <param name="v">value</param>
        /// <param name="r">red (0-255)</param>
        /// <param name="g">green (0-255)</param>
        /// <param name="b">blue (0-255)</param>
        public static void HSV2RGB(float h, float s, float v, ref float r, ref float g, ref float b)
        {
            float f = h * 6;
            int hi = (int)(f);
            f -= hi;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            switch (hi)
            {
                case 0:
                    r = v;
                    g = t;
                    b = p;
                    break;

                case 1:
                    r = q;
                    g = v;
                    b = p;
                    break;

                case 2:
                    r = p;
                    g = v;
                    b = t;
                    break;

                case 3:
                    r = p;
                    g = q;
                    b = v;
                    break;

                case 4:
                    r = t;
                    g = p;
                    b = v;
                    break;

                case 5:
                    r = v;
                    g = p;
                    b = q;
                    break;

                default:
                    throw new Exception("Color Conversion Error: invalid Hi value");
            }
            r *= 255;
            g *= 255;
            b *= 255;
        }

        /// <summary>
        /// Converts RGB to HSV in place.
        /// </summary>
        /// <param name="r">red (0-255)</param>
        /// <param name="g">green (0-255)</param>
        /// <param name="b">blue (0-255)</param>
        /// <param name="h">hue</param>
        /// <param name="s">saturation</param>
        /// <param name="v">value</param>
        public static void RGB2HSV(float r, float g, float b, ref float h, ref float s, ref float v)
        {
            r /= 255;
            g /= 255;
            b /= 255;
            v = Math.Max(Math.Max(r, g), b);
            float min = Math.Min(Math.Min(r, g), b);
            s = v - min;
            if (s == 0)
                h = 0;
            else
            {
                if (r == v)
                    h = ((g - b) / s) * 60;
                else if (g == v)
                    h = (2 + (b - r) / s) * 60;
                else h = (4 + (r - g) / s) * 60;
            }
            while (h > 360)
                h -= 360;
            while (h < 0)
                h += 360;
            h /= 360;
        }

        /// <summary>
        /// Converts RGB to YUV in place.
        /// </summary>
        /// <param name="r">red (0-255)</param>
        /// <param name="g">green (0-255)</param>
        /// <param name="b">blue (0-255)</param>
        /// <param name="y">y (luminance)</param>
        /// <param name="u">u (chrominance 1)</param>
        /// <param name="v">v (chromonance 2)</param>
        public static void RGB2YUV(float r, float g, float b, ref float y, ref float u, ref float v)
        {
            r /= 255;
            g /= 255;
            b /= 255;
            y = .299f * r + .587f * g + .114f * b;
            u = -.147f * r - .289f * g + .436f * b;
            v = .615f * r - .515f * g - .1f * b;
        }

        /// <summary>
        /// Converts RGB to YUV in place using integer arithmetic.  Note: all values must fall between 0 and 255.
        /// </summary>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        /// <param name="y">y (luminance)</param>
        /// <param name="u">u (chrominance 1)</param>
        /// <param name="v">v (chrominance 2)</param>
        public static void RGB2YUV(int r, int g, int b, ref int y, ref int u, ref int v)
        {
            y = ((66 * r + 129 * g + 25 * b + 128) >> 8) + 16;
            u = ((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128;
            v = ((112 * r - 94 * g - 18 * b + 128) >> 8) + 128;
        }

        /// <summary>
        /// Converts YUV to RGB in place using integer arithmetic.  Note: all values must fall between 0 and 255.
        /// </summary>
        /// <param name="y">y (luminance)</param>
        /// <param name="u">u (chrominance 1)</param>
        /// <param name="v">v (chrominance 2)</param>
        /// <param name="r">red</param>
        /// <param name="g">green</param>
        /// <param name="b">blue</param>
        private static void YUV2RGB(int y, int u, int v, ref int r, ref int g, ref int b)
        {
            r = clip((298 * y + 409 * v + 128) >> 8);
            g = clip((298 * y - 100 * u - 208 * v + 128) >> 8);
            b = clip((298 * y + 516 * u + 128) >> 8);
        }

        private static int clip(int x)
        {
            return x < 0 ? 0 : (x > 255 ? 255 : x);
        }

        /// <summary>
        /// Converts YUV to RGB in place.
        /// </summary>
        /// <param name="y">y (luminance)</param>
        /// <param name="u">u (chrominance 1)</param>
        /// <param name="v">v (chrominance 2)</param>
        /// <param name="r">red (0-255)</param>
        /// <param name="g">green (0-255)</param>
        /// <param name="b">blue (0-255)</param>

        public static void YUV2RGB(float y, float u, float v, ref float r, ref float g, ref float b)
        {
            r = y + 1.140f * v;
            g = y - 0.395f * u - 0.581f * v;
            b = y + 2.032f * u;
            r *= 255;
            g *= 255;
            b *= 255;
        }

        private const float BLACK = 20;
        private const float YELLOW = 70;

        /// <summary>
        /// Converts RGB to CIEL*a*b* in place.
        /// </summary>
        /// <param name="R">red (0-255)</param>
        /// <param name="G">green (0-255)</param>
        /// <param name="B">blue (0-255)</param>
        /// <param name="L">Luminance</param>
        /// <param name="a">Greenness to Redness</param>
        /// <param name="b">Blueness to Yellowness</param>
        public static void RGB2Lab(float R, float G, float B, ref float L, ref float a, ref float b)
        {
            float X, Y, Z, fX, fY, fZ;

            X = 0.412453f * R + 0.357580f * G + 0.180423f * B;
            Y = 0.212671f * R + 0.715160f * G + 0.072169f * B;
            Z = 0.019334f * R + 0.119193f * G + 0.950227f * B;

            X /= (255 * 0.950456f);
            Y /= 255;
            Z /= (255 * 1.088754f);

            if (Y > 0.008856)
            {
                fY = (float)Math.Pow(Y, 1.0 / 3.0);
                L = 116f * fY - 16f;
            }
            else
            {
                fY = 7.787f * Y + 16f / 116f;
                L = 903.3f * Y;
            }

            if (X > 0.008856f)
                fX = (float)Math.Pow(X, 1.0 / 3.0);
            else
                fX = 7.787f * X + 16f / 116f;

            if (Z > 0.008856f)
                fZ = (float)Math.Pow(Z, 1.0 / 3.0);
            else
                fZ = 7.787f * Z + 16f / 116f;

            a = 500f * (fX - fY);
            b = 200f * (fY - fZ);

            if (L < BLACK)
            {
                a *= (float)Math.Exp((L - BLACK) / (BLACK / 4));
                b *= (float)Math.Exp((L - BLACK) / (BLACK / 4));
                L = BLACK;
            }
            if (b > YELLOW)
                b = YELLOW;

        }

        /// <summary>
        /// Converts CIEL*a*b* to RGB in place.
        /// </summary>
        /// <param name="L">Luminance</param>
        /// <param name="a">Greenness to Redness</param>
        /// <param name="b">Blueness to Yellowness</param>
        /// <param name="R">red (0-255)</param>
        /// <param name="G">green (0-255)</param>
        /// <param name="B">blue (0-255)</param>
        public static void Lab2RGB(float L, float a, float b, ref float R, ref float G, ref float B)
        {
            float X, Y, Z, fX, fY, fZ;
            float RR, GG, BB;

            fY = (float)Math.Pow((L + 16.0) / 116.0, 3.0);
            if (fY < 0.008856f)
                fY = L / 903.3f;
            Y = fY;

            if (fY > 0.008856f)
                fY = (float)Math.Pow(fY, 1.0 / 3.0);
            else
                fY = 7.787f * fY + 16f / 116f;

            fX = a / 500f + fY;
            if (fX > 0.206893f)
                X = (float)Math.Pow(fX, 3.0);
            else
                X = (fX - 16f / 116f) / 7.787f;

            fZ = fY - b / 200f;
            if (fZ > 0.206893f)
                Z = (float)Math.Pow(fZ, 3.0);
            else
                Z = (fZ - 16f / 116f) / 7.787f;

            X *= (0.950456f * 255);
            Y *= 255;
            Z *= (1.088754f * 255);

            RR = 3.240479f * X - 1.537150f * Y - 0.498535f * Z;
            GG = -0.969256f * X + 1.875992f * Y + 0.041556f * Z;
            BB = 0.055648f * X - 0.204043f * Y + 1.057311f * Z;

            R = (RR < 0 ? 0 : RR > 255 ? 255 : RR);
            G = (GG < 0 ? 0 : GG > 255 ? 255 : GG);
            B = (BB < 0 ? 0 : BB > 255 ? 255 : BB);

        }

        /// <summary>
        /// Converts RGB from a 0 to 255 scale to a 0 to 1 scale.
        /// </summary>
        /// <param name="R">red (0-255)</param>
        /// <param name="G">green (0-255)</param>
        /// <param name="B">blue (0-255)</param>
        /// <param name="r">red (0-1)</param>
        /// <param name="g">green (0-1)</param>
        /// <param name="b">blue (0-1)</param>
        public static void RGB2rgb(float R, float G, float B, ref float r, ref float g, ref float b)
        {
            r = R / 255;
            g = G / 255;
            b = B / 255;
        }

        /// <summary>
        /// Converts RGB from a 0 to 1 scale to a 0 to 255 scale.
        /// </summary>
        /// <param name="r">red (0-1)</param>
        /// <param name="g">green (0-1)</param>
        /// <param name="b">blue (0-1)</param>
        /// <param name="R">red (0-255)</param>
        /// <param name="G">green (0-255)</param>
        /// <param name="B">blue (0-255)</param>
        public static void rgb2RGB(float r, float g, float b, ref float R, ref float G, ref float B)
        {
            fixValue(ref r, 0, 1);
            fixValue(ref g, 0, 1);
            fixValue(ref b, 0, 1);
            R = r * 255;
            G = g * 255;
            B = b * 255;
        }

        private static void fixValue(ref float value, float min, float max)
        {
            if(value < min)
                value = min;
            if (value > max)
                value = max;
        }
    }
}
