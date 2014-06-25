using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Registration
{
    /// <summary>
    /// A registration model for transforming points from one 2D space to another.
    /// </summary>
    /// <typeparam name="T">The type of the data points</typeparam>
    public interface IModel<T>
    {
        /// <summary>
        /// Fits the model to the provided data.
        /// </summary>
        /// <param name="data">The data to use in fitting the model</param>
        void Fit(List<Tuple<T,T>> data);
        /// <summary>
        /// Transforms the point using the fitted model.
        /// </summary>
        /// <param name="point">The point to transform.</param>
        /// <returns>The transformed point.</returns>
        T Transform(T point);
        /// <summary>
        /// The number of points which adhere to the fitted model.
        /// </summary>
        int Consensus { get; set; }
        /// <summary>
        /// The minimum number of points needed to fit this model.
        /// </summary>
        int MinFitCount { get; }
    }
}
