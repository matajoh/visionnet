using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;

namespace VisionNET.Learning
{
    public class LabelVector : DenseVector, IDataPoint<double[]>
    {
        public LabelVector(params double[] values) : base(values) { }

        public LabelVector(int size) : base(new double[size]) { }

        public double[] Data
        {
            get
            {
                return ToArray();
            }
            set
            {
                SetValues(value);
            }
        }               

        public int Label { get; set; }

        public float FeatureValue { get; set; }

        public new object Clone()
        {
            double[] values = new double[Count];
            Array.Copy(Values, values, Count);
            LabelVector result = new LabelVector(values);
            result.Label = Label;
            result.FeatureValue = FeatureValue;
            return result;
        }

        public static LabelVector operator *(LabelVector x, double scalar)
        {
            LabelVector result = x.Clone() as LabelVector;
            x.DoMultiply(scalar, result);
            return result;
        }

        public static LabelVector operator *(double scalar, LabelVector x)
        {
            LabelVector result = x.Clone() as LabelVector;
            x.DoMultiply(scalar, result);
            return result;
        }

        public static LabelVector operator +(LabelVector lhs, LabelVector rhs)
        {
            LabelVector result = lhs.Clone() as LabelVector;
            lhs.DoAdd(rhs, result);
            return result;
        }

        public static LabelVector operator -(LabelVector lhs, LabelVector rhs)
        {
            LabelVector result = lhs.Clone() as LabelVector;
            lhs.DoSubtract(rhs, result);
            return result;
        }

        public static LabelVector operator /(LabelVector x, double scalar)
        {
            double norm = 1 / scalar;
            return x * norm;
        }


    }
}
