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
    internal static class Utilities
    {
        public static byte Log2(int value)
        {
            byte result = 0;
            while (value > 1)
            {
                value = value >> 1;
                result++;
            }
            return result;
        }

        public static int FixValue(int value, int min, int max)
        {
            if (value < min)
                value = min;
            else if (value >= max)
                value = max - 1;
            return value;
        }

        public static int Pow2(int power)
        {
            return 1 << power;
        }
    }
}
