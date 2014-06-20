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
using VisionNET.Learning;
using System.Collections.Generic;
using VisionNET.Comparison;
using System;

namespace VisionNET.DecisionForests
{
    /// <summary>
    /// Extension methods for specific types of DecisionTree.
    /// </summary>
    public static class DecisionTreeExtensions
    {
        internal static void FillLeafImage<T>(this DecisionTree<ImageDataPoint<T>, T[]> tree, LeafImage<T> leafImage, IMultichannelImage<T> image)
        {
            INodeInfo<ImageDataPoint<T>, T[]>[, ,] array = leafImage.RawArray;
            int rows = image.Rows;
            int columns = image.Columns;
            List<ImageDataPoint<T>> points = new List<ImageDataPoint<T>>();
            List<int> indices = new List<int>();
            int i = 0;
            for (short r = 0; r < rows; r++)
                for (short c = 0; c < columns; c++, i++)
                {
                    points.Add(new ImageDataPoint<T>(image, r, c, -1));
                    indices.Add(i);
                }
            INodeInfo<ImageDataPoint<T>, T[]>[] info = new INodeInfo<ImageDataPoint<T>, T[]>[points.Count];
            DecisionTree<ImageDataPoint<T>, T[]>.assignLabels(tree._root, points, info, indices);
            i = 0;
            int treeLabel = tree.TreeLabel;
            for (short r = 0; r < rows; r++)
                for (short c = 0; c < columns; c++, i++)
                    array[r, c, treeLabel] = info[i];
        }

        /// <summary>
        /// Computes a tree histogram from <paramref name="image"/>.
        /// </summary>
        /// <param name="tree">The tree used for the computation</param>
        /// <param name="image">The image used to compute the tree histogram</param>
        /// <returns>A tree histogram</returns>
        public static TreeHistogram ComputeHistogram<T>(this DecisionTree<ImageDataPoint<T>, T[]> tree, IMultichannelImage<T> image)
        {
            List<ImageDataPoint<T>> points = new List<ImageDataPoint<T>>();
            for (short r = 0; r < image.Rows; r++)
                for (short c = 0; c < image.Columns; c++)
                    points.Add(new ImageDataPoint<T>(image, r, c, -1));
            return tree.ComputeHistogram(points);
        }

        /// <summary>
        /// Classifies each point from <paramref name="image"/> and trackes which nodes it visits.
        /// </summary>
        /// <param name="tree">The tree used for the computation</param>
        /// <param name="image">Image to add to the tree</param>
        /// <param name="mode">Mode to use when sampling the image</param>
        public static void Fill<T>(this DecisionTree<ImageDataPoint<T>, T[]> tree, LabeledImage<T> image, BackgroundSampleMode mode)
        {
            List<ImageDataPoint<T>> points = image.CreateAllDataPoints(mode);
            tree.Fill(points);
        }

        /// <summary>
        /// Classifies each pixel of <paramref name="image"/> and produces a corresponding <see cref="T:LabelImage" />.  The maximum likelihood label
        /// is chosen at each pixel.
        /// </summary>
        /// <param name="tree">The tree used for the computation</param>
        /// <param name="image">Image to classify</param>
        /// <returns>A label image with all of the classifications</returns>
        public static LabelImage Classify<T>(this DecisionTree<ImageDataPoint<T>, T[]> tree, IMultichannelImage<T> image)
        {
            return tree.ClassifySoft(image).ToLabelImage();
        }

        /// <summary>
        /// Classifies <paramref name="image"/>, creating a distribution over all labels at each pixel.
        /// </summary>
        /// <param name="tree">The tree used for the computation</param>
        /// <param name="image">Image to classify</param>
        /// <returns>Distributions at each pixel</returns>
        public static DistributionImage ClassifySoft<T>(this DecisionTree<ImageDataPoint<T>, T[]> tree, IMultichannelImage<T> image)
        {
            DistributionImage dist = new DistributionImage(image.Rows, image.Columns, tree.LabelCount);
            tree.ClassifySoft(image, dist);
            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Classifies points from <paramref name="labeledImage"/> (using the mask if present) and adds the distributions at each pixel to <paramref name="dist"/>.
        /// </summary>
        /// <param name="tree">The tree used for the computation</param>
        /// <param name="labeledImage">The image to classify</param>
        /// <param name="dist">Image which is used to store the distributions</param>
        public static void ClassifySoft<T>(this DecisionTree<ImageDataPoint<T>, T[]> tree, LabeledImage<T> labeledImage, DistributionImage dist)
        {
            List<ImageDataPoint<T>> points = labeledImage.CreateAllDataPoints(BackgroundSampleMode.Full);
            List<int> indices = new List<int>();
            for (int i = 0; i < points.Count; i++)
                indices.Add(i);
            INodeInfo<ImageDataPoint<T>, T[]>[] info = new INodeInfo<ImageDataPoint<T>, T[]>[points.Count];
            DecisionTree<ImageDataPoint<T>, T[]>.assignLabels(tree._root, points, info, indices);
            for (int i = 0; i < info.Length; i++)
                dist.Add(points[i].Row, points[i].Column, info[i].Distribution);
        }

        /// <summary>
        /// Classifies each pixel in <paramref name="image"/> and stores the results in <paramref name="dist"/>.
        /// </summary>
        /// <param name="tree">The tree used for the computation</param>
        /// <param name="image">Image to classify</param>
        /// <param name="dist">Image which is used to store the distributions</param>
        public static void ClassifySoft<T>(this DecisionTree<ImageDataPoint<T>, T[]> tree, IMultichannelImage<T> image, DistributionImage dist)
        {
            int rows = image.Rows;
            int columns = image.Columns;
            List<ImageDataPoint<T>> points = new List<ImageDataPoint<T>>();
            List<int> indices = new List<int>();
            int i = 0;
            for (short r = 0; r < rows; r++)
                for (short c = 0; c < columns; c++, i++)
                {
                    points.Add(new ImageDataPoint<T>(image, r, c, -1));
                    indices.Add(i);
                }
            INodeInfo<ImageDataPoint<T>, T[]>[] info = new INodeInfo<ImageDataPoint<T>, T[]>[points.Count];
            DecisionTree<ImageDataPoint<T>, T[]>.assignLabels(tree._root, points, info, indices);
            i = 0;
            for (short r = 0; r < rows; r++)
                for (short c = 0; c < columns; c++, i++)
                    dist.Add(r, c, info[i].Distribution);
        }

        /// <summary>
        /// Adds the data from <paramref name="image"/> to each tree in the forest.
        /// </summary>
        /// <param name="forest">The forest used for the computation</param>
        /// <param name="image">Image to learn from</param>
        /// <param name="mode">Mode to use when sampling the image background</param>
        public static void Fill<T>(this DecisionForest<ImageDataPoint<T>, T[]> forest, LabeledImage<T> image, BackgroundSampleMode mode)
        {
            for (int i = 0; i < forest.TreeCount; i++)
                forest[i].Fill(image, mode);
            forest.RefreshMetadata();
        }

        /// <summary>
        /// Computes a histogram for all trees from the provided image.
        /// </summary>
        /// <param name="forest">The forest used for the computation</param>
        /// <param name="image">Image to classify</param>
        /// <returns>The histogram</returns>
        public static TreeHistogram ComputeHistogram<T>(this DecisionForest<ImageDataPoint<T>, T[]> forest, IMultichannelImage<T> image)
        {
            int rows = image.Rows;
            int columns = image.Columns;
            List<ImageDataPoint<T>> points = new List<ImageDataPoint<T>>();
            for (short r = 0; r < rows; r++)
                for (short c = 0; c < columns; c++)
                    points.Add(new ImageDataPoint<T>(image, r, c, -1));
            return forest.ComputeHistogram(points);
        }

        /// <summary>
        /// Computes a histogram for all trees from the provided image.
        /// </summary>
        /// <param name="forest">The forest used for the computation</param> 
        /// <param name="image">Image to classify</param>
        /// <returns>The histogram</returns>
        public static TreeHistogram ComputeHistogram<T>(this DecisionForest<ImageDataPoint<T>, T[]> forest, LabeledImage<T> image)
        {
            return forest.ComputeHistogram(image.CreateAllDataPoints(BackgroundSampleMode.Full));
        }

        /// <summary>
        /// Classifies every pixel in the provided image with the maximum likelihood label.
        /// </summary>
        /// <param name="forest">The forest used for the computation</param>
        /// <param name="image">Image to classify</param>
        /// <returns>The classified image</returns>
        public static LabelImage Classify<T>(this DecisionForest<ImageDataPoint<T>, T[]> forest, IMultichannelImage<T> image)
        {
            return forest.ClassifySoft(image).ToLabelImage();
        }

        /// <summary>
        /// Classifies every pixel in the provided image, returning a full distribution over all labels.
        /// </summary>
        /// <param name="forest">The forest used for the computation</param>
        /// <param name="image">Image to classify</param>
        /// <returns>The classified image</returns>
        public static DistributionImage ClassifySoft<T>(this DecisionForest<ImageDataPoint<T>, T[]> forest, IMultichannelImage<T> image)
        {
            DistributionImage dist = new DistributionImage(image.Rows, image.Columns, forest.LabelCount);
            dist.ID = image.ID;
            for (int t = 0; t < forest.TreeCount; t++)
                forest[t].ClassifySoft(image, dist);
            dist.DivideThrough(forest.TreeCount);
            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Classifies every pixel in the provided image, returning a full distribution over all labels.
        /// </summary>
        /// <param name="forest">The forest used for the computation</param>
        /// <param name="image">Image to classify</param>
        /// <returns>The classified image</returns>
        public static DistributionImage ClassifySoft<T>(this DecisionForest<ImageDataPoint<T>, T[]> forest, LabeledImage<T> image)
        {
            DistributionImage dist = new DistributionImage(image.Image.Rows, image.Image.Columns, forest.LabelCount);
            dist.ID = image.ID;
            for (int t = 0; t < forest.TreeCount; t++)
                forest[t].ClassifySoft(image, dist);
            dist.DivideThrough(forest.TreeCount);
            dist.Normalize();
            return dist;
        }

        /// <summary>
        /// Classifies each pixel in the image and returns the leaf nodes which they end up in.
        /// </summary>
        /// <param name="forest">The forest used for the computation</param>
        /// <param name="image">Image to classify</param>
        /// <returns>A leaf image</returns>
        public static LeafImage<T> ComputeLeafImage<T>(this DecisionForest<ImageDataPoint<T>, T[]> forest, IMultichannelImage<T> image)
        {
            LeafImage<T> leafImage = new LeafImage<T>(image.Rows, image.Columns, forest.TreeCount);
            leafImage.ID = image.ID;
            for (int i = 0; i < forest.TreeCount; i++)
                forest[i].FillLeafImage(leafImage, image);
            return leafImage;
        }
    }
}