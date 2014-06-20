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
using System.Linq;

namespace VisionNET.Learning
{
    /// <summary>
    /// Class encapsulating an image, its ground truth labels, and an optional mask indicating which pixels are "valid" and should be considered.
    /// </summary>
    /// <typeparam name="T">Underlying type of the source image</typeparam>
    [Serializable]
    public class LabeledImage<T>
    {
        private IMultichannelImage<T> _image;
        private LabelImage _labels;
        private bool[,] _valid;
        private SupervisionMode _supervisionMode;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LabeledImage()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="image">The source image</param>
        /// <param name="labels">The ground truth labels</param>
        public LabeledImage(IMultichannelImage<T> image, LabelImage labels)
        {
            Image = image;
            Labels = labels;
            initValid();
        }

        private void initValid()
        {
            if (_image == null)
                return;

            int rows = _image.Rows;
            int columns = _image.Columns;
            _valid = new bool[rows, columns];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    _valid[r, c] = true;
        }

        /// <summary>
        /// Mask over the image indicating which pixels are valid.
        /// </summary>
        public bool[,] Valid
        {
            get
            {
                return _valid;
            }
            set
            {
                _valid = value;
            }
        }

        /// <summary>
        /// Source image.
        /// </summary>
        public IMultichannelImage<T> Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
                initValid();
            }
        }

        /// <summary>
        /// Ground truth labels for the image.
        /// </summary>
        public LabelImage Labels
        {
            get
            {
                return _labels;
            }
            set
            {
                _labels = value;
            }
        }

        /// <summary>
        /// ID of the image.
        /// </summary>
        public string ID
        {
            get
            {
                return _image.ID;
            }
            set
            {
                _image.ID = value;
            }
        }

        /// <summary>
        /// The supervision mode to use when creating data points (whether to label them with all labels in the image, or their ground truth label.
        /// </summary>
        public SupervisionMode SupervisionMode
        {
            get { return _supervisionMode; }
            set { _supervisionMode = value; }
        }

        private short getLabel(short label, LabelSet labels)
        {
            switch (_supervisionMode)
            {
                case SupervisionMode.Full:
                    return label;

                case SupervisionMode.Part:
                    return labels.SelectRandom();

                case SupervisionMode.None:
                    return (short)ThreadsafeRandom.Next(20);
            }
            return 0;
        }

        /// <summary>
        /// Creates a list of all pixels in the image.
        /// </summary>
        /// <returns>List of all pixels in the image</returns>
        public List<ImageDataPoint<T>> CreateAllDataPoints()
        {
            return CreateAllDataPoints(BackgroundSampleMode.Ignore);
        }

        /// <summary>
        /// Creates a list of pixels sampled according to <paramref name="mode"/>.
        /// </summary>
        /// <param name="mode">Mode to use when sampling pixels marked with the "background label" as defined by <see cref="LabelImage.BackgroundLabel"/></param>
        /// <returns>List of sampled pixels</returns>
        public List<ImageDataPoint<T>> CreateAllDataPoints(BackgroundSampleMode mode)
        {
            if (_labels != null)
                return createAllDataPointsLabels(mode);
            else return createAllDataPointsRaw();
        }

        private unsafe List<ImageDataPoint<T>> createAllDataPointsLabels(BackgroundSampleMode mode)
        {
            List<ImageDataPoint<T>> points = new List<ImageDataPoint<T>>();
            int rows = _image.Rows;
            int columns = _image.Columns;
            LabelSet set = _labels.Labels;
            fixed (short* labelsSrc = _labels.RawArray)
            {
                fixed (bool* validSrc = _valid)
                {
                    short* labelsPtr = labelsSrc;
                    bool* validPtr = validSrc;
                    for (short r = 0; r < rows; r++)
                        for (short c = 0; c < columns; c++)
                        {
                            short label = getLabel(*labelsPtr++, set);
                            bool sample = *validPtr++;
                            if (sample && label == LabelImage.BackgroundLabel)
                            {
                                switch (mode)
                                {
                                    case BackgroundSampleMode.Ignore:
                                        sample = false;
                                        break;

                                    case BackgroundSampleMode.Half:
                                        sample = ThreadsafeRandom.Test(.5);
                                        break;
                                }
                            }
                            if (sample)
                                points.Add(new ImageDataPoint<T>(_image, r, c, label));
                        }
                }
            }
            return points;
        }

        private unsafe List<ImageDataPoint<T>> createAllDataPointsRaw()
        {
            List<ImageDataPoint<T>> points = new List<ImageDataPoint<T>>();
            int rows = _image.Rows;
            int columns = _image.Columns;
            fixed (bool* validSrc = _valid)
            {
                bool* validPtr = validSrc;
                for (short r = 0; r < rows; r++)
                    for (short c = 0; c < columns; c++)
                    {
                        bool sample = *validPtr++;
                        if (sample)
                            points.Add(new ImageDataPoint<T>(_image, r, c, -1));
                    }
            }
            return points;
        }
    }

    /// <summary>
    /// Enumeration of modes for sampling pixels marked with the "background label" as defined by <see cref="LabelImage.BackgroundLabel"/>.
    /// </summary>
    public enum BackgroundSampleMode { 
        /// <summary>
        /// Ignore all background pixels.
        /// </summary>
        Ignore, 
        /// <summary>
        /// Sample half of the background pixels.
        /// </summary>
        Half, 
        /// <summary>
        /// Sample all background pixels.
        /// </summary>
        Full 
    };

    /// <summary>
    /// Enumeration of modes for supervision in the creation of training data.
    /// </summary>
    public enum SupervisionMode { 
        /// <summary>
        /// All pixels are marked with their ground truth labels.
        /// </summary>
        Full, 
        /// <summary>
        /// Pixels are marked with a set of labels corresponding to all the labels present in the ground truth image
        /// </summary>
        Part, 
        /// <summary>
        /// Pixels have no training labels associated with them.
        /// </summary>
        None 
    };

    /// <summary>
    /// This class encapsulates the training data for image-based recognition algorithms based on pixel-level object labels.
    /// </summary>
    /// <typeparam name="T">Type of the underlying images in the training set</typeparam>
    public class LabeledDataSet<T> : IEnumerable<LabeledImage<T>>
    {
        private List<LabeledImage<T>> _images;
        private BackgroundSampleMode _backgroundSampleMode;
        private SupervisionMode _supervisionMode;
        private double _dataPercentage;
        private double _imagePercentage;
        private bool _byImage;

        /// <summary>
        /// Supervision mode of the dataset.
        /// </summary>
        public SupervisionMode SupervisionMode
        {
            get { return _supervisionMode; }
            set { _supervisionMode = value; }
        }
        
        /// <summary>
        /// Percentage of data from each image to be included in each data split.
        /// </summary>
        public double DataPercentage
        {
            get { return _dataPercentage; }
            set { _dataPercentage = value; }
        }

        /// <summary>
        /// Percentage of images to include in each data split.
        /// </summary>
        public double ImagePercentage
        {
            get { return _imagePercentage; }
            set { _imagePercentage = value; }
        }

        /// <summary>
        /// Whether to sample by image first for a split before sampling pixels.
        /// </summary>
        public bool ByImage
        {
            get { return _byImage; }
            set { _byImage = value; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="images">Training images</param>
        /// <param name="dataPercentage">Percentage of data per split</param>
        /// <param name="imagePercentage">Percentage of images per split</param>
        public LabeledDataSet(IEnumerable<LabeledImage<T>> images, double dataPercentage, double imagePercentage)
        {
            _dataPercentage = dataPercentage;
            _imagePercentage = imagePercentage;
            _byImage = true;
            _images = images.ToList();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="images">Training images</param>
        /// <param name="dataPercentage">Percentage of data per split</param>
        public LabeledDataSet(IEnumerable<LabeledImage<T>> images, double dataPercentage)
        {
            _images = images.ToList();
            _dataPercentage = dataPercentage;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dataPercentage">Percentage of data per split</param>
        public LabeledDataSet(double dataPercentage)
            : this(new List<LabeledImage<T>>(), dataPercentage)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dataPercentage">Percentage of data per split</param>
        /// <param name="imagePercentage">Percentage of images per split</param>
        public LabeledDataSet(double dataPercentage, double imagePercentage)
            : this(new List<LabeledImage<T>>(), dataPercentage, imagePercentage)
        {
        }

        /// <summary>
        /// Adds <paramref name="image"/> to the dataset.
        /// </summary>
        /// <param name="image">Image to add to the dataset</param>
        public void AddImage(LabeledImage<T> image)
        {
            _images.Add(image);
        }

        /// <summary>
        /// Removes <paramref name="image"/> from the dataset.
        /// </summary>
        /// <param name="image">Image to remove</param>
        public void RemoveImage(LabeledImage<T> image)
        {
            _images.Remove(image);
        }

        /// <summary>
        /// Clears all images from the dataset.
        /// </summary>
        public void Clear()
        {
            _images.Clear();
        }

        private short getLabel(short label, LabelSet labels)
        {
            switch (_supervisionMode)
            {
                case SupervisionMode.Full:
                    return label;

                case SupervisionMode.Part:
                    return labels.SelectRandom();

                case SupervisionMode.None:
                    return (short)ThreadsafeRandom.Next(20);
            }
            return 0;
        }

        /// <summary>
        /// Computes the inverse label frequency array for the image.  This is an array in which each index holds a value equal
        /// to the total number of image labels divided by the total number of that particular label.
        /// </summary>
        /// <param name="numLabels">Total number of labels</param>
        /// <returns>Inverse label frequency</returns>
        public unsafe float[] ComputeInverseLabelFrequency(int numLabels)
        {
            int[] counts = new int[numLabels];
            foreach (LabeledImage<T> image in _images)
            {
                LabelImage labels = image.Labels;
                LabelSet set = labels.Labels;
                fixed (short* labelsSrc = labels.RawArray)
                {
                    int count = labels.Rows * labels.Columns;
                    short* labelsPtr = labelsSrc;
                    while (count-- > 0)
                    {
                        short index = getLabel(*labelsPtr++, set);
                        bool sample = true;
                        if (index == LabelImage.BackgroundLabel)
                        {
                            switch (_backgroundSampleMode)
                            {
                                case BackgroundSampleMode.Ignore:
                                    sample = false;
                                    break;

                                case BackgroundSampleMode.Half:
                                    sample = ThreadsafeRandom.Test(.5);
                                    break;
                            }
                        }
                        if (!sample)
                            continue;
                        counts[index]++;
                    }
                }
            }
            float[] frequency = new float[numLabels];
            float sum = 0;
            for (short i = 0; i < frequency.Length; i++)
            {
                frequency[i] = 1 + counts[i];
                sum += frequency[i];
            }
            for (int i = 0; i < frequency.Length; i++)
                frequency[i] = sum / frequency[i];
            return frequency;
        }

        /// <summary>
        /// Sampling mode for background pixels.
        /// </summary>
        public BackgroundSampleMode BackgroundSampleMode
        {
            get
            {
                return _backgroundSampleMode;
            }
            set
            {
                _backgroundSampleMode = value;
            }
        }

        /// <summary>
        /// Number of images in the dataset.
        /// </summary>
        public int Count
        {
            get
            {
                return _images.Count;
            }
        }

        /// <summary>
        /// Creates several splits of the dataset.
        /// </summary>
        /// <param name="sampleFrequency">How often to sample pixels within the training images.</param>
        /// <param name="boxRows">Vertical trim around the edges of images to avoid feature tests beyond the boundary of the image</param>
        /// <param name="boxColumns">Vertical trim around the edges of images to avoid feature tests beyond the boundary of the image</param>
        /// <param name="numSplits">Number of splits to create</param>
        /// <returns>Splits of the data</returns>
        public List<ImageDataPoint<T>>[] CreateDataPoints(int sampleFrequency, int boxRows, int boxColumns, int numSplits)
        {
            List<ImageDataPoint<T>> points = new List<ImageDataPoint<T>>();
            foreach(LabeledImage<T> labelledImage in _images){
                IMultichannelImage<T> image = labelledImage.Image;
                LabelImage labels = labelledImage.Labels;
                LabelSet set = labels.Labels;
                string id = labelledImage.ID;
                bool[,] valid = labelledImage.Valid;
                int maxRows = image.Rows-boxRows;
                int maxColumns = image.Columns - boxColumns;
                for(int r=boxRows; r<maxRows; r += sampleFrequency)
                    for (int c = boxColumns; c < maxColumns; c += sampleFrequency)
                    {
                        short label = getLabel(labels[r,c], set);
                        bool sample = valid[r,c];
                        if(sample && label == LabelImage.BackgroundLabel)
                        {
                            switch (_backgroundSampleMode)
                            {
                                case BackgroundSampleMode.Ignore:
                                    sample = false;
                                    break;

                                case BackgroundSampleMode.Half:
                                    sample = ThreadsafeRandom.Test(.5);
                                    break;
                            }
                        }
                        if (sample)
                            points.Add(new ImageDataPoint<T>(image, (short)r, (short)c, label));
                    }
            }
            List<ImageDataPoint<T>>[] splits = new List<ImageDataPoint<T>>[numSplits];
            for (int i = 0; i < numSplits; i++)
                if (_byImage)
                    splits[i] = sampleByImage(points);
                else splits[i] = sample(points);

            return splits;
        }

        private List<ImageDataPoint<T>> sampleByImage(List<ImageDataPoint<T>> points)
        {
            List<ImageDataPoint<T>> result = new List<ImageDataPoint<T>>();
            var imageGroups = points.GroupBy(o => o.ImageID);
            foreach (var group in imageGroups)
            {
                if(ThreadsafeRandom.Test(_imagePercentage))
                    result.AddRange(group.Where(o=>ThreadsafeRandom.Test(_dataPercentage)));
            }

            return result;
        }

        private List<ImageDataPoint<T>> sample(List<ImageDataPoint<T>> points)
        {
            List<ImageDataPoint<T>> result = new List<ImageDataPoint<T>>();
            for (int i = 0; i < points.Count; i++)
                if (ThreadsafeRandom.Test(_dataPercentage))
                    result.Add(points[i]);
            return result;
        }

        /// <summary>
        /// Indexes the dataset.
        /// </summary>
        /// <param name="index">Desired index</param>
        /// <returns>A labeled image</returns>
        public LabeledImage<T> this[int index]
        {
            get
            {
                return _images[index];
            }
        }

        /// <summary>
        /// Returns an enumerator for the dataset.
        /// </summary>
        /// <returns>The enumerator</returns>
        public IEnumerator<LabeledImage<T>> GetEnumerator()
        {
            return _images.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator for the dataset.
        /// </summary>
        /// <returns>The enumerator</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _images.GetEnumerator();
        }
    }
}
