using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;

namespace VisionNET.Registration
{
    /// <summary>
    /// Representation of a dataset of correlated vectors under transformation.
    /// </summary>
    public class CorrelatedDataset : IRegistrationDataSet<Vector>
    {
        private List<Tuple<Vector, Vector>> _data;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">Each tuple represents the same point before and after transformation</param>
        public CorrelatedDataset(List<Tuple<Vector,Vector>> data)
        {
            _data = data;
        }

        /// <summary>
        /// Returns a random subset of data of the provided size.
        /// </summary>
        /// <param name="n">Size of random subset</param>
        /// <returns>The random subset</returns>
        public List<Tuple<Vector, Vector>> SampleInliers(int n)
        {
            return _data.RandomSubset(n);
        }

        /// <summary>
        /// Returns the set of points which are correctly modeled the provided model, within an error threshold.
        /// </summary>
        /// <typeparam name="M">The type of the model</typeparam>
        /// <param name="fittedModel">The model to test</param>
        /// <param name="errorThreshold">The acceptable error threshold</param>
        /// <returns>The list of vectors which adhere to the model</returns>
        public List<Tuple<Vector, Vector>> GetConsensus<M>(M fittedModel, double errorThreshold) where M : IModel<Vector>
        {
            List<Tuple<Vector, Vector>> consensus = new List<Tuple<Vector, Vector>>();
            foreach (Tuple<Vector, Vector> pair in _data)
            {
                Vector y = fittedModel.Transform(pair.Item1);
                if ((y - pair.Item2).Norm(2) < errorThreshold)
                {
                    consensus.Add(pair);
                }
            }
            return consensus;
        }
    }
}
