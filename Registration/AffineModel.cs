using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;
using System.IO;

namespace VisionNET.Registration
{
    public class AffineModel : IModel<Vector>
    {
        private Matrix _transform;

        public void Fit(List<Tuple<Vector, Vector>> data)
        {
            double[,] XValues = new double[data.Count * 2, 6];
            double[] YValues = new double[data.Count * 2];
            for (int i = 0; i < data.Count; i++)
            {
                Vector x = data[i].Item1;
                Vector y = data[i].Item2;
                int row = i << 1;
                XValues[row, 0] = x[0];
                XValues[row, 1] = x[1];
                XValues[row, 2] = 1;
                YValues[row] = y[0];
                row++;
                XValues[row, 3] = x[0];
                XValues[row, 4] = x[1];
                XValues[row, 5] = 1;
                YValues[row] = y[1];
            }

            Matrix X = DenseMatrix.OfArray(XValues);
            Vector Y = new DenseVector(YValues);

            double[] AValues = new double[6];
            if (data.Count == MinFitCount)
            {
                AValues = X.Inverse().Multiply(Y).ToArray();
            }
            else
            {
                var pseudoInverse = X.Transpose().Multiply(X).Inverse().Multiply(X.Transpose());
                AValues = pseudoInverse.Multiply(Y).ToArray();
            }
            _transform = (Matrix)(new DenseMatrix(3, 2, AValues).Transpose());
            Consensus = data.Count;
        }

        public Vector Transform(Vector point)
        {
            double[] values = new double[3];
            Array.Copy(point.ToArray(), values, 2);
            values[2] = 1;
            return (Vector)_transform.Multiply(new DenseVector(values));
        }

        public int Consensus { get; set; }

        public int MinFitCount
        {
            get { return 3; }
        }

        public void Save(string filename)
        {
            StreamWriter output = new StreamWriter(filename);
            double[,] values = _transform.ToArray();
            output.WriteLine("{0}\t{1}\t{2}\n{3}\t{4}\t{5}", values[0, 0], values[0, 1], values[0, 2], values[1, 0], values[1, 1], values[1, 2]);
            output.Close();
        }

        public static AffineModel Load(string filename)
        {
            StreamReader input = new StreamReader(filename);
            List<double[]> rows = new List<double[]>();
            rows.Add(input.ReadLine().Split().Select(o => Convert.ToDouble(o)).ToArray());
            rows.Add(input.ReadLine().Split().Select(o => Convert.ToDouble(o)).ToArray());            
            input.Close();

            AffineModel model = new AffineModel();
            model._transform = DenseMatrix.OfRows(2, 3, rows);
            return model;
        }
    }
}
