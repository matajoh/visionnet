using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;
using System.IO;
using MathNet.Numerics.LinearAlgebra.Double.Factorization;

namespace VisionNET.Registration
{
    /// <summary>
    /// Model of a transformation in two dimensional space using homogenous coordinates.
    /// </summary>
    public class HomographyModel : IModel<Vector>
    {
        private Matrix _transform;

        /// <summary>
        /// Fits the model to the provided data.
        /// </summary>
        /// <param name="data">Each tuple represents a data point before and after transformation</param>
        public void Fit(List<Tuple<Vector, Vector>> data)
        {
            double[,] XValues = new double[data.Count * 2, 9];
            double[] YValues = new double[data.Count * 2];
            for (int i = 0; i < data.Count; i++)
            {
                Vector x = data[i].Item1;
                Vector y = data[i].Item2;
                int row = i * 2;
                XValues[row, 0] = x[0];
                XValues[row, 1] = x[1];
                XValues[row, 2] = 1;
                XValues[row, 6] = -x[0] * y[0];
                XValues[row, 7] = -x[1] * y[0];
                XValues[row, 8] = -y[0];
                row++;
                XValues[row, 3] = x[0];
                XValues[row, 4] = x[1];
                XValues[row, 5] = 1;
                XValues[row, 6] = -x[0] * y[1];
                XValues[row, 7] = -x[1] * y[1];
                XValues[row, 8] = -y[1];
            }

            Matrix X = DenseMatrix.OfArray(XValues);
            var XTX = X.Transpose().Multiply(X);
            Evd evd = XTX.Evd();
            var eigenvector = evd.EigenVectors().Column(0);

            _transform = (Matrix)(new DenseMatrix(3, 3, eigenvector.ToArray()).Transpose());
            Consensus = data.Count;
        }

        /// <summary>
        /// Transforms the point according to the fitted model.
        /// </summary>
        /// <param name="point">The point to transform</param>
        /// <returns>The transformed point</returns>
        public Vector Transform(Vector point)
        {
            double[] values = new double[3];
            Array.Copy(point.ToArray(), values, 2);
            values[2] = 1;
            Vector result = (Vector)_transform.Multiply(new DenseVector(new double[]{point[0], point[1], 1}));
            return new DenseVector(new double[] { result[0] / result[2], result[1] / result[2] });
        }

        /// <summary>
        /// The number of points which concur with this model.
        /// </summary>
        public int Consensus { get; set; }

        /// <summary>
        /// The minimum number of points needed to fit the model.
        /// </summary>
        public int MinFitCount
        {
            get { return 4; }
        }
        
        /// <summary>
        /// Saves this model to a file.
        /// </summary>
        /// <param name="filename">The path to the file</param>
        public void Save(string filename)
        {
            StreamWriter output = new StreamWriter(filename);
            double[,] values = _transform.ToArray();
            output.WriteLine("{0}\t{1}\t{2}\n{3}\t{4}\t{5}\n{6}\t{7}\t{8}", values[0, 0], values[0, 1], values[0, 2], values[1, 0], values[1, 1], values[1, 2], values[2,0], values[2,1], values[2,2]);
            output.Close();
        }

        /// <summary>
        /// Reads a model from a file.
        /// </summary>
        /// <param name="filename">The path to the file</param>
        /// <returns>The model</returns>
        public static HomographyModel Load(string filename)
        {
            StreamReader input = new StreamReader(filename);
            List<double[]> rows = new List<double[]>();
            rows.Add(input.ReadLine().Split().Select(o => Convert.ToDouble(o)).ToArray());
            rows.Add(input.ReadLine().Split().Select(o => Convert.ToDouble(o)).ToArray());
            rows.Add(input.ReadLine().Split().Select(o => Convert.ToDouble(o)).ToArray());
            input.Close();

            HomographyModel model = new HomographyModel();
            model._transform = DenseMatrix.OfRows(3, 3, rows);
            return model;
        }
    }
}
