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
using System.IO;

namespace VisionNET.Learning
{
    /// <summary>
    /// Enumeration of the categories of data as regards a learning algorithm
    /// </summary>
    public enum DataCategory{
        /// <summary>
        /// Training data, seen by the learning algorithm during the training period
        /// </summary>
        Training,
        /// <summary>
        /// Validation data, used by the learning algorithm to set parameters or as additional training data
        /// </summary>
        Validation,
        /// <summary>
        /// Unseen data drawn from the same distribution as the training and validation data, used for evaluation.
        /// </summary>
        Test,
        /// <summary>
        /// Unseen data of a completely different nature from the training, validation and test data but still
        /// pertinent to the domain of the learning algorithm.
        /// </summary>
        Heldout
    };

    /// <summary>
    /// Encapsulates a compartmentalization of a dataset into training, validation and test data.
    /// </summary>
    [Serializable]
    public class Split
    {
        private Dictionary<DataCategory, List<string>> _data;

        private static string trim(string input)
        {
            input = input.Trim();
            if (input.IndexOf(".") > 0)
                input = input.Substring(0, input.IndexOf("."));
            return input;
        }

        /// <summary>
        /// Constructor.  Each file pointed to by the parameters has one filename per line.
        /// </summary>
        /// <param name="trainFile">File containing the training image filenames</param>
        /// <param name="valFile">File containing the validation image filenames</param>
        /// <param name="testFile">File containing the test image filenames</param>
        public Split(string trainFile, string valFile, string testFile):this(
            from id in File.ReadAllLines(trainFile)
            select trim(id),
            from id in File.ReadAllLines(valFile)
            select trim(id),
            from id in File.ReadAllLines(testFile)
            select trim(id))
        {
        }

        /// <summary>
        /// Constructor.  The collection parameters contain data file IDs without file extensions.
        /// </summary>
        /// <param name="train">Training ID collection</param>
        /// <param name="val">Validation ID collection</param>
        /// <param name="test">Test ID collection</param>
        public Split(IEnumerable<string> train, IEnumerable<string> val, IEnumerable<string> test)
        {
            _data = new Dictionary<DataCategory, List<string>>();
            _data[DataCategory.Training] = train.ToList();
            _data[DataCategory.Validation] = val.ToList();
            _data[DataCategory.Test] = test.ToList();
            removeDuplicates();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Split()
        {
            _data = new Dictionary<DataCategory, List<string>>();
            _data[DataCategory.Training] = new List<string>();
            _data[DataCategory.Validation] = new List<string>();
            _data[DataCategory.Test] = new List<string>();
        }

        private void removeDuplicates()
        {
            _data[DataCategory.Training] = _data[DataCategory.Training].Distinct().ToList();
            _data[DataCategory.Validation] = _data[DataCategory.Validation].Distinct().ToList();
            _data[DataCategory.Test] = _data[DataCategory.Test].Distinct().ToList();
        }

        /// <summary>
        /// Adds <paramref name="id"/> to the split under <paramref name="category"/>.
        /// </summary>
        /// <param name="id">Data id</param>
        /// <param name="category">Data category</param>
        public void Add(string id, DataCategory category)
        {
            _data[category].Add(id);
        }

        /// <summary>
        /// Returns all data ids.
        /// </summary>
        public List<string> All
        {
            get
            {
                List<string> all = new List<string>();
                all.AddRange(_data[DataCategory.Training]);
                all.AddRange(_data[DataCategory.Validation]);
                all.AddRange(_data[DataCategory.Test]);
                return all;
            }
        }

        /// <summary>
        /// Returns training ids.
        /// </summary>
        public List<string> Train
        {
            get
            {
                return _data[DataCategory.Training];
            }
            set
            {
                _data[DataCategory.Training] = value;
            }
        }

        /// <summary>
        /// Returns both training and validation ids.
        /// </summary>
        public List<string> TrainVal
        {
            get
            {
                List<string> trainval = new List<string>();
                trainval.AddRange(_data[DataCategory.Training]);
                trainval.AddRange(_data[DataCategory.Validation]);
                return trainval;
            }
        }

        /// <summary>
        /// Returns validation ids.
        /// </summary>
        public List<string> Val
        {
            get
            {
                return _data[DataCategory.Validation];
            }
            set
            {
                _data[DataCategory.Validation] = value;
            }
        }

        /// <summary>
        /// Returns test ids.
        /// </summary>
        public List<string> Test
        {
            get
            {
                return _data[DataCategory.Test];
            }
            set
            {
                _data[DataCategory.Test] = value;
            }
        }
    }
}
