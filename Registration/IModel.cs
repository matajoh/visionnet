using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Registration
{
    public interface IModel<T>
    {
        void Fit(List<Tuple<T,T>> data);
        T Transform(T point);
        int Consensus { get; set; }
        int MinFitCount { get; }
    }
}
