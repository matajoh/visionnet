using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Double;

namespace VisionNET.Registration
{
    public class CorrelatedDataset : IDataSet<Vector>
    {
        private List<Tuple<Vector, Vector>> _data;

        public CorrelatedDataset(List<Tuple<Vector,Vector>> data)
        {
            _data = data;
        }

        public List<Tuple<Vector, Vector>> SampleInliers(int n)
        {
            return _data.RandomSubset(n);
        }

        public List<Tuple<Vector, Vector>> GetConsensus<M>(List<Tuple<Vector, Vector>> inliers, M fittedModel, double errorThreshold) where M : IModel<Vector>
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
