using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Registration
{
    /// <summary>
    /// A registration dataset.
    /// </summary>
    /// <typeparam name="T">The type of the data points</typeparam>
    public interface IRegistrationDataSet<T>
    {
        /// <summary>
        /// Sample a random subset of points.
        /// </summary>
        /// <param name="n">The number of points to sample</param>
        /// <returns>A random subset of size n</returns>
        List<Tuple<T,T>> SampleInliers(int n);
        /// <summary>
        /// Gets the number of points in the dataset which adhere to the provided model within an accepted error threshold.
        /// </summary>
        /// <typeparam name="M">The model type</typeparam>
        /// <param name="fittedModel">The model to test</param>
        /// <param name="errorThreshold">The error threshold</param>
        /// <returns>The consensus set</returns>
        List<Tuple<T,T>> GetConsensus<M>(M fittedModel, double errorThreshold) where M:IModel<T>;
    }
}
