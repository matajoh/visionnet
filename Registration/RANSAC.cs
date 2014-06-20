using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Registration
{
    public static class RANSAC
    {
        /// <summary>
        /// Fit a model using random sampling and consensus
        /// </summary>
        /// <typeparam name="M"></typeparam>
        /// <typeparam name="D"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataset">Dataset containing the points to fit</param>
        /// <param name="numInterations">Maximum iterations</param>
        /// <param name="errorThreshold">The error threshold (used to determine consensus)</param>
        /// <param name="minConsensus">The minimum consensus required for a model to be considered successful</param>
        /// <returns>The fitted model</returns>
        public static M Fit<M, D, T>(D dataset, int numInterations, double errorThreshold, int minConsensus) where M:IModel<T>,new() where D:IDataSet<T>
        {
            M bestModel = default(M);
            int maxConsensus = 0;
            for (int i = 0; i < numInterations; i++)
            {
                if (i % 10 == 0)
                    Console.WriteLine("Iteration " + i);
                M maybeModel = new M();
                List<Tuple<T, T>> maybeInliers = dataset.SampleInliers(maybeModel.MinFitCount);
                maybeModel.Fit(maybeInliers);

                List<Tuple<T,T>> consensus = dataset.GetConsensus(maybeInliers, maybeModel, errorThreshold);
                if (consensus.Count > maxConsensus)
                {
                    maxConsensus = consensus.Count;
                    bestModel = maybeModel;
                    bestModel.Fit(consensus);
                    Console.WriteLine("{0}: New best model with consensus {1}", i, maxConsensus);
                }
            }
            return bestModel;
        }
    }
}
