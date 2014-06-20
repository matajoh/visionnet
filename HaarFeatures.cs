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

using System.Collections.Generic;

namespace VisionNET
{
    /// <summary>
    /// Utility class containing the Haar rectangle features for a provided square size.
    /// </summary>
    public static class HaarFeatures
    {
        /// <summary>
        /// The square size used when computing the rectangle features.
        /// </summary>
        public static int SquareSize
        {
            get
            {
                return _squareSize;
            }
        }

        private static int _squareSize;
        /// <summary>
        /// Each feature consists of a list of Rectangles which are alternatively added and subtracted from the final sum.
        /// </summary>
        public static Rectangle[][] FEATURES;
        /// <summary>
        /// Static constructor.  Builds the features.
        /// </summary>
        static HaarFeatures()
        {
            setFeatures();
        }

        /// <summary>
        /// Set the square size which the rectangles are computed for.
        /// </summary>
        /// <param name="squareSize">The new square size</param>
        public static void SetSquareSize(int squareSize)
        {
            _squareSize = squareSize;
            setFeatures();
        }

        // generates features offset from a center point
        private static void setFeatures()
        {
            List<Rectangle[]> features = new List<Rectangle[]>();
            // 2 rectangle horizontal features
            for (int width = 1; width <= _squareSize / 2; width++)
            {
                int total_width = width + width;
                for (int i = 0; i <= _squareSize - total_width; i++)
                    for (int height = 1; height <= _squareSize; height++)
                        for (int j = 0; j <= _squareSize - height; j++)
                        {
                            Rectangle[] feature = new Rectangle[2];
                            feature[0] = new Rectangle{C=i, R=j, Columns=width, Rows=height};
                            feature[1] = new Rectangle{C=i + width, R=j, Columns=width, Rows=height};
                            features.Add(feature);
                        }
            }
            // 2 rectangle vertical features
            for (int height = 1; height <= _squareSize / 2; height++)
            {
                int total_height = height + height;
                for (int j = 0; j <= _squareSize - total_height; j++)
                    for (int width = 1; width <= _squareSize; width++)
                        for (int i = 0; i <= _squareSize - width; i++)
                        {
                            Rectangle[] feature = new Rectangle[2];
                            feature[0] = new Rectangle { C = i, R = j, Columns = width, Rows = height };
                            feature[1] = new Rectangle { C = i, R = j + height, Columns = width, Rows = height };
                            features.Add(feature);
                        }
            }

            // 3 rectangle horizontal features
            for (int width = 1; width <= _squareSize / 3; width++)
            {
                int total_width = width * 3;
                for (int i = 0; i <= _squareSize - total_width; i++)
                    for (int height = 1; height <= _squareSize; height++)
                        for (int j = 0; j <= _squareSize - height; j++)
                        {
                            Rectangle[] feature = new Rectangle[3];
                            feature[0] = new Rectangle { C = i, R = j, Columns = width, Rows = height };
                            feature[1] = new Rectangle { C = i + width, R = j, Columns = width, Rows = height };
                            feature[2] = new Rectangle { C = i + width + width, R = j, Columns = width, Rows = height };
                            features.Add(feature);
                        }
            }

            // 3 rectangle vertical features
            for (int height = 1; height <= _squareSize / 3; height++)
            {
                int total_height = height * 3;
                for (int j = 0; j <= _squareSize - total_height; j++)
                    for (int width = 1; width <= _squareSize; width++)
                        for (int i = 0; i <= _squareSize - width; i++)
                        {
                            Rectangle[] feature = new Rectangle[3];
                            feature[0] = new Rectangle { C = i, R = j, Columns = width, Rows = height };
                            feature[1] = new Rectangle { C = i, R = j + height, Columns = width, Rows = height };
                            feature[2] = new Rectangle { C = i, R = j + height + height, Columns = width, Rows = height };
                            features.Add(feature);
                        }
            }

            // 4 rectangle features
            for (int width = 1; width <= _squareSize / 2; width++)
            {
                int total_width = width + width;
                for (int i = 0; i <= _squareSize - total_width; i++)
                    for (int height = 1; height <= _squareSize / 2; height++)
                    {
                        int total_height = height + height;
                        for (int j = 0; j <= _squareSize - total_height; j++)
                        {
                            Rectangle[] feature = new Rectangle[4];
                            feature[0] = new Rectangle { C = i, R = j, Columns = width, Rows = height };
                            feature[1] = new Rectangle { C = i + width, R = j, Columns = width, Rows = height };
                            feature[2] = new Rectangle { C = i + width, R = j + height, Columns = width, Rows = height };
                            feature[3] = new Rectangle { C = i, R = j + height, Columns = width, Rows = height };
                            features.Add(feature);
                        }
                    }
            }
            int mid = _squareSize/2;
            foreach(Rectangle[] feature in features)
                for(int i=0; i<feature.Length; i++)
                    feature[i] = new Rectangle { C = feature[i].C - mid, R = feature[i].R - mid, Columns = feature[i].Columns, Rows = feature[i].Rows };
            FEATURES = features.ToArray();
        }
    }
}
