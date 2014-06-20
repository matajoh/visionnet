using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Registration
{
    public interface IDataSet<T>
    {
        List<Tuple<T,T>> SampleInliers(int n);
        List<Tuple<T,T>> GetConsensus<M>(List<Tuple<T,T>> inliers, M fittedModel, double errorThreshold) where M:IModel<T>;
    }
}
