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
using System.IO;
using System.Linq;
using System;
using MathNet.Numerics.LinearAlgebra.Single;

namespace VisionNET.Learning
{
    /// <summary>
    /// Class encapsulating tools for evaluating categorization algorithms.  In this case, they are curve based metrics, the Receiver Operating Characteristic
    /// Curve and the Precision/Recall curve.  For both, there is also a summary method, area under the curve and average precision respectively.
    /// </summary>
    public class Evaluation
    {
        private class ChangePoint
        {
            public ChangePoint(int tp, int fp, int tn, int fn, float rank)
            {
                TP = tp;
                FP = fp;
                TN = tn;
                FN = fn;
                Rank = rank;
            }

            public int TP, FP, TN, FN;
            public float Rank;

            public override string ToString()
            {
                return string.Format("{0}:{1}:{2}:{3}", TP, FP, TN, FN);
            }
        }

        private List<Vector> _prCurve;
        private double _ap;

        private List<Vector> _rocCurve;
        private double _auc;

        private List<RankPair<bool>> _data;
        private List<ChangePoint> _changes;
        private int _positives;
        private int _negatives;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">List of results, where a label of "true" indicates that the data point is a positive result (a member of the target category)</param>
        /// <param name="isAscending">Whether the rank values in the pairs should be sorted in ascending or descending order</param>
        public Evaluation(IEnumerable<RankPair<bool>> data, bool isAscending)
        {
            _data = data.ToList();
            _data.Sort();

            if(!isAscending)
                _data.Reverse();

            findChanges();
            computePR();
            computeRoC();
        }

        private void findChanges()
        {
            int tp, fp, tn, fn;
            tp = fp = tn = fn = 0;
            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i].Label)
                {
                    _positives++;
                    fn++;
                }
                else
                {
                    tn++;
                    _negatives++;
                }
            }
            _changes = new List<ChangePoint>();
            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i].Label)
                {
                    tp++;
                    fn--;
                }
                else
                {
                    fp++;
                    tn--;
                }
                _changes.Add(new ChangePoint(tp, fp, tn, fn, _data[i].Rank));
            }
        }

        private float computePrecision(ChangePoint p)
        {
            return (float)p.TP / (p.TP + p.FP);
        }

        private float computeRecall(ChangePoint p)
        {
            return (float)p.TP / _positives;
        }

        private void computePR()
        {
            _prCurve = new List<Vector>();
            if (_positives == 0)
            {
                _prCurve.Add(new DenseVector(new float[]{0, 0}));
                _prCurve.Add(new DenseVector(new float[]{1, 0}));
                _ap = 0;
                return;
            }
            else if (_negatives == 0)
            {
                _prCurve.Add(new DenseVector(new float[]{0, 1}));
                _prCurve.Add(new DenseVector(new float[]{1, 1}));
                _ap = 1;
                return;
            }
            float precision = computePrecision(_changes[0]);
            float recall = computeRecall(_changes[0]);
            float precisionSum = precision;
            _prCurve.Add(new DenseVector(new float[]{recall, precision}));
            for (int i = 1; i < _changes.Count; i++)
            {
                float newPrecision = computePrecision(_changes[i]);
                float newRecall = computeRecall(_changes[i]);
                if (_changes[i].TP > _changes[i - 1].TP)
                {
                    precision = newPrecision;
                    recall = newRecall;
                    precisionSum += precision;
                    _prCurve.Add(new DenseVector(new float[]{recall, precision}));
                }
            }
            _ap = precisionSum / (_changes[0].FN + _changes[0].TP);
        }

        /// <summary>
        /// Writes the precision/recall curve to a file.  This is a tab-delimited file with a data point per line, recall first and then precision.
        /// </summary>
        /// <param name="filename">File to write the graph to</param>
        public void WritePRCurve(string filename)
        {
            StreamWriter output = new StreamWriter(filename);
            output.WriteLine(_ap);
            foreach(var point in _prCurve)
                output.WriteLine("{0}\t{1}", point[0], point[1]);
            output.Close();
        }

        /// <summary>
        /// Writes the precision/recall curve to a file.  This is a tab-delimited file with a data point per line, recall first and then precision.
        /// </summary>
        /// <param name="filename">File to write the graph to</param>
        /// <param name="numPoints">Number of points to include on the curve</param>
        public void WritePRCurve(string filename, int numPoints)
        {
            StreamWriter output = new StreamWriter(filename);
            int interval = _prCurve.Count / numPoints;
            output.WriteLine(_ap);
            for (int i = 0; i < _prCurve.Count; i += interval)
                output.WriteLine("{0}\t{1}", _prCurve[i][0], _prCurve[i][1]);
            output.WriteLine("{0}\t{1}", _prCurve.Last()[0], _prCurve.Last()[1]);
            output.Close();
        }

        /// <summary>
        /// Writes the receiver operator characteristic curve to a file.  This is a tab-delimited file with one data point per line, the false positive rate first and
        /// then the true positive rate.
        /// </summary>
        public void WriteROCCurve(string filename)
        {
            WriteROCCurve(filename, 0, 1, _rocCurve.Count);
        }

        /// <summary>
        /// Writes the receiver operator characteristic curve to a file.  This is a tab-delimited file with one data point per line, the false positive rate first and
        /// then the true positive rate.
        /// </summary>
        /// <param name="filename">File to write the graph to</param>
        /// <param name="maxFPR">Maximum value on the x-axis</param>
        /// <param name="minTPR">Minimum value on the Y-axis</param>
        /// <param name="numPoints">Number of points to show on the graph</param>
        public void WriteROCCurve(string filename, float minTPR, float maxFPR, int numPoints)
        {
            StreamWriter output = new StreamWriter(filename);
            var points = (from point in _rocCurve
                          where point[1] > minTPR && point[0] < maxFPR
                          select point).ToList();
            float interval = (float)points.Count / numPoints;
            output.WriteLine(_auc);
            for (float i = 0; i < points.Count; i += interval)
            {
                int index = (int)i;
                output.WriteLine("{0}\t{1}", points[index][0], points[index][1]);
            }
            output.WriteLine("{0}\t{1}", points.Last()[0], points.Last()[1]);
            output.Close();
        }

        /// <summary>
        /// Find the rank for the given false positive rate.
        /// </summary>
        /// <param name="falsePositiveRate">The false positive rate</param>
        /// <returns>Rank which produces the provided false positive rate</returns>
        public float FindRank(float falsePositiveRate)
        {
            int index = 0;
            while (index < _changes.Count)
            {
                if (computeFPR(_changes[index]) > falsePositiveRate)
                    break;
                index++;
            }
            if (index == 0)
                return _changes[index].Rank;
            if (index == _changes.Count)
                return _changes[index - 1].Rank;
            return (_changes[index - 1].Rank + _changes[index].Rank) / 2;
        }

        /// <summary>
        /// Returns the rank at the equal error rate.
        /// </summary>
        /// <returns>The rank at the equal error rate</returns>
        public float RankAtEqualErrorRate()
        {
            int index = 0;
            while (index < _changes.Count)
            {
                float fpr = computeFPR(_changes[index]);
                float tpr = computeTPR(_changes[index]);
                if (fpr + tpr < 1)
                    index++;
                else break;
            }
            if (index == 0)
                return _changes[index].Rank;
            if (index == _changes.Count)
                return _changes[index - 1].Rank;
            return (_changes[index - 1].Rank + _changes[index].Rank) / 2;
        }

        /// <summary>
        /// The ordered list of graph points for the ROC Curve.
        /// </summary>
        public List<Vector> ROCCurve
        {
            get
            {
                return _rocCurve;
            }
        }

        /// <summary>
        /// The area under the ROC curve.
        /// </summary>
        public double AuC
        {
            get
            {
                return _auc;
            }
        }

        /// <summary>
        /// The order list of graph points for the Precision/Recall curve.
        /// </summary>
        public List<Vector> PRCurve
        {
            get
            {
                return _prCurve;
            }
        }

        /// <summary>
        /// The average precision of the Precision/Recall curve.
        /// </summary>
        public double AP
        {
            get
            {
                return _ap;
            }
        }

        private float computeTPR(ChangePoint cp)
        {
            return computeRecall(cp);
        }

        private float computeFPR(ChangePoint cp)
        {
            return (float)cp.FP / _negatives;
        }

        private void computeRoC()
        {
            _rocCurve = new List<Vector>();
            if (_positives == 0)
            {
                _rocCurve.Add(new DenseVector(new float[] { 1, 0 }));
                _rocCurve.Add(new DenseVector(new float[] { 1, 1 }));
                _auc = 0;
                return;
            }
            else if (_negatives == 0)
            {
                _rocCurve.Add(new DenseVector(new float[] { 0, 1 }));
                _rocCurve.Add(new DenseVector(new float[] { 1, 1 }));
                _auc = 1;
                return;
            }
            float tpr = computeTPR(_changes[0]);
            float fpr = computeFPR(_changes[0]);
            _rocCurve.Add(new DenseVector(new float[] { fpr, tpr }));
            _auc = 0;
            for (int i = 1; i < _changes.Count; i++)
            {
                float newTPR = computeTPR(_changes[i]);
                float newFPR = computeFPR(_changes[i]);
                if (_changes[i].TP > _changes[i - 1].TP)
                {
                    _auc += (newFPR - fpr) * ((tpr + newTPR) / 2);
                    tpr = newTPR;
                    fpr = newFPR;
                    _rocCurve.Add(new DenseVector(new float[] { fpr, tpr }));
                }
            }
            _rocCurve.Add(new DenseVector(new float[] { 1, 1 }));
            _auc += (1 - fpr);
        }

    }
}
