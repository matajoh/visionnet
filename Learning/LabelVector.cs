using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;

namespace VisionNET.Learning
{
    /// <summary>
    /// Class encapsulating a MathNet DenseVector for use in learning applications as a data point.
    /// </summary>
    public class LabelVector : DenseVector, IDataPoint<double[]>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="values">The values of the vector</param>
        public LabelVector(params double[] values) : base(values) { Weight = 1; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="size">The capacity or size of the vector</param>
        public LabelVector(int size) : base(new double[size]) { Weight = 1; }

        /// <summary>
        /// The data of the vector.
        /// </summary>
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

        /// <summary>
        /// The label for this data point.
        /// </summary>
        public int Label { get; set; }

        /// <summary>
        /// The latest computed feature value of the data point.
        /// </summary>
        public float FeatureValue { get; set; }

        /// <summary>
        /// The weight of the data point.
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// Returns an exact copy of the data point in its current state.
        /// </summary>
        /// <returns>A copy of the point</returns>
        public new object Clone()
        {
            double[] values = new double[Count];
            Array.Copy(Values, values, Count);
            LabelVector result = new LabelVector(values);
            result.Label = Label;
            result.FeatureValue = FeatureValue;
            result.Weight = Weight;
            return result;
        }

        /// <summary>
        /// Multiplies all of the values of the vector by a scalar value.
        /// </summary>
        /// <param name="x">The vector</param>
        /// <param name="scalar">The scalar value</param>
        /// <returns>The product of a vector and a scalar</returns>
        public static LabelVector operator *(LabelVector x, double scalar)
        {
            LabelVector result = x.Clone() as LabelVector;
            x.DoMultiply(scalar, result);
            return result;
        }

        /// <summary>
        /// Multiplies all of the values of the vector by a scalar value.
        /// </summary>
        /// <param name="x">The vector</param>
        /// <param name="scalar">The scalar value</param>
        /// <returns>The product of a vector and a scalar</returns>
        public static LabelVector operator *(double scalar, LabelVector x)
        {
            LabelVector result = x.Clone() as LabelVector;
            x.DoMultiply(scalar, result);
            return result;
        }

        /// <summary>
        /// Adds two vectors of the same size together.
        /// </summary>
        /// <param name="lhs">The left hand vector</param>
        /// <param name="rhs">The right hand vector</param>
        /// <returns>The sum of the two vectors</returns>
        public static LabelVector operator +(LabelVector lhs, LabelVector rhs)
        {
            LabelVector result = lhs.Clone() as LabelVector;
            lhs.DoAdd(rhs, result);
            return result;
        }

        /// <summary>
        /// Subtracts two vectors of the same size together.
        /// </summary>
        /// <param name="lhs">The left hand vector</param>
        /// <param name="rhs">The right hand vector</param>
        /// <returns>The difference of the two vectors</returns>
        public static LabelVector operator -(LabelVector lhs, LabelVector rhs)
        {
            LabelVector result = lhs.Clone() as LabelVector;
            lhs.DoSubtract(rhs, result);
            return result;
        }

        /// <summary>
        /// Divides all of the values of the vector by a scalar value.
        /// </summary>
        /// <param name="x">The vector</param>
        /// <param name="scalar">The scalar value</param>
        /// <returns>The vector divided by a scalar</returns>
        public static LabelVector operator /(LabelVector x, double scalar)
        {
            double norm = 1 / scalar;
            return x * norm;
        }


    }
}
