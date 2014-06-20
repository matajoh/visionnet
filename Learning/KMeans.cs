using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra.Double;

namespace VisionNET.Learning
{
    /// <summary>
    /// Class to compute KMeans.  Uses the Scalable-KMeans++ technique from (http://theory.stanford.edu/~sergei/papers/vldb12-kmpar.pdf) for initialization,
    /// and computes all values at an adjustable level of parallelism to take advantage of parallel architectures.
    /// </summary>
    public class KMeans
    {
        private class Job
        {
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public List<float[]> Centers { get; set; }
        }

        private int _k;
        private List<LabelVector> _centers;
        private List<LabelVector> _data;
        private float[] _minDist;
        private double _phi;
        private List<Job> _jobs;
        private int PARALLEL_THRESHOLD = 500000;

        /// <summary>
        /// The size of job to use
        /// </summary>
        public int JobSize { get; set; }

        /// <summary>
        /// The oversampling rate to use when building a list of candidate centers
        /// </summary>
        public double OversampleRate { get; set; }

        /// <summary>
        /// The maximum number of iterations to use when building the list of center candidates
        /// </summary>
        public int MaxIterations { get; set; }

        /// <summary>
        /// The fitted centers
        /// </summary>
        public List<LabelVector> Centers
        {
            get
            {
                return _centers;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="k">The number of centers to fit</param>
        /// <param name="jobSize">The size of chunks the data should be broken into for parallelism</param>
        /// <param name="oversampleRate">The rate to oversample when selecting candidate centers</param>
        /// <param name="maxIterations">The maximum number of iterations.  If set to 0, will use a default value.</param>
        public KMeans(int k, int jobSize=1000, float oversampleRate = 1, int maxIterations = 0)
        {
            _k = k;
            JobSize = jobSize;
            OversampleRate = oversampleRate * _k;
            MaxIterations = maxIterations;
        }

        /// <summary>
        /// Fit the centers to the provided data.
        /// </summary>
        /// <param name="data">The data to fit</param>
        /// <param name="weights">Weights for the data.  If not provided, all points are equally weighted.</param>
        public void Fit(List<LabelVector> data, float[] weights = null)
        {
            UpdateManager.WriteLine("Fitting centers using k-means clustering...");
            _data = data;
            _minDist = new float[data.Count];

            _jobs = new List<Job>();
            if (data.Count > JobSize)
            {
                int jobCount = _data.Count / JobSize;
                if (_data.Count % JobSize != 0)
                    jobCount++;
                for (int i = 0; i < _data.Count; i += JobSize)
                    _jobs.Add(new Job { StartIndex = i, EndIndex = Math.Min(i + JobSize, _data.Count) });
            }
            else
            {
                _jobs.Add(new Job { StartIndex = 0, EndIndex = data.Count });
            }

            UpdateManager.WriteLine("Data partitioned into {0} job{1}.", _jobs.Count, _jobs.Count == 1 ? "" : "s");

            _centers = new List<LabelVector>();

            UpdateManager.WriteLine("Initializing centers...");
            UpdateManager.AddIndent();
            if (data.Count > PARALLEL_THRESHOLD)
                seedCentersParallel();
            else seedCenters();
            UpdateManager.RemoveIndent();

            for (short label = 0; label < _centers.Count; label++)
                _centers[label].Label = label;

            UpdateManager.WriteLine("Running Lloyd's algorithm...");
            Stopwatch sw = new Stopwatch();
            UpdateManager.Write("Building initial clusters...");
            sw.Start();
            while (runJobs(o => assignCenters(o)))
            {
                sw.Stop();
                UpdateManager.WriteLine("Done [{0}ms]", sw.ElapsedMilliseconds);

                for (int i = 0; i < _centers.Count; i++)
                    _centers[i] = new LabelVector(_data[0].Count);
                sw.Reset();
                UpdateManager.Write("Recomputing cluster centers...");
                sw.Start();
                for(int i=0; i<data.Count; i++)
                {
                    LabelVector x = data[i];
                    if (weights != null)
                    {
                        float featureValue = _centers[x.Label].FeatureValue;
                        _centers[x.Label] += weights[i] * x;
                        _centers[x.Label].FeatureValue += weights[i];
                    }
                    else
                    {
                        float featureValue = _centers[x.Label].FeatureValue;
                        _centers[x.Label] += x;
                        _centers[x.Label].FeatureValue += 1;
                    }
                }
                for (short i = 0; i < _centers.Count; i++)
                {
                    _centers[i] /= _centers[i].FeatureValue;
                    _centers[i].Label = i;
                }
                sw.Stop();
                UpdateManager.WriteLine("Done [{0}ms]", sw.ElapsedMilliseconds);
                sw.Reset();
                UpdateManager.Write("Rebuilding clusters...");
                sw.Start();
            }
            sw.Stop();
            UpdateManager.WriteLine("Done and done [{0}ms]", sw.ElapsedMilliseconds);

            UpdateManager.WriteLine("Fitting complete.");
        }

        private void seedCenters()
        {
            UpdateManager.WriteLine("Performing K-Means++ initialization...");
            LabelVector center = _data.SelectRandom().Clone() as LabelVector;
            center.Label = 0;
            _centers.Add(center);

            Stopwatch sw = new Stopwatch();            
            for (int i = 1; i < _k; i++)
            {
                sw.Reset();
                UpdateManager.Write("Recalculating phi...");
                sw.Start();
                runJobs(o => calculatePhi(o));
                sw.Stop();
                UpdateManager.WriteLine("Done [{0}ms]", sw.ElapsedMilliseconds);
                                
                float[] dist = _data.Select(o => o.FeatureValue).Normalize().ToArray();
                Vector x = _data[dist.Sample()];
                _centers.Add(x.Clone() as LabelVector);
            }
        }

        private void seedCentersParallel()
        {
            UpdateManager.WriteLine("Performing scalable K-Means++ initialization...");
            LabelVector center = _data.SelectRandom().Clone() as LabelVector;
            center.Label = 0;
            _centers.Add(center);
            runJobs(o => calculatePhi(o));
            _phi = _data.Sum(o => o.FeatureValue);

            int N = (int)Math.Log(_phi);
            if (MaxIterations != 0)
                N = MaxIterations;

            Stopwatch sw = new Stopwatch();
            for (int i = 0; i < N; i++)
            {
                sw.Reset();
                UpdateManager.Write("Gathering center candidates ({0} of {1})...", i + 1, N);
                sw.Start();
                List<LabelVector> newCenters = runJobs(new Func<object, List<LabelVector>>(findCenterCandidates));
                _centers.AddRange(newCenters);
                for (short s = 0; s < _centers.Count; s++)
                    _centers[s].Label = s;
                sw.Stop();
                UpdateManager.WriteLine("Done [{0}ms]", sw.ElapsedMilliseconds);

                sw.Reset();
                UpdateManager.Write("Recalculating phi...");
                sw.Start();
                runJobs(o => calculatePhi(o));
                _phi = _data.Sum(o => o.FeatureValue);
                sw.Stop();
                UpdateManager.WriteLine("Done [{0}ms]", sw.ElapsedMilliseconds);
            }

            sw.Reset();
            UpdateManager.Write("Assigning centers...");
            sw.Start();
            for (short s = 0; s < _centers.Count; s++)
                _centers[s].Label = s;
            runJobs(o => assignCenters(o));
            sw.Stop();
            UpdateManager.WriteLine("Done [{0}ms]", sw.ElapsedMilliseconds);
            float[] weights = new float[_centers.Count];
            foreach (LabelVector x in _data)
                weights[x.Label] += 1;
            KMeans clusterCenters = new KMeans(_k);

            UpdateManager.AddIndent();
            clusterCenters.Fit(_centers, weights);
            UpdateManager.RemoveIndent();

            _centers = clusterCenters.Centers;            
        }

        private void runJobs(Action<object> action)
        {
            List<Task> tasks = new List<Task>();
            foreach (Job job in _jobs)
            {
                tasks.Add(Task.Factory.StartNew(action, job));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private List<LabelVector> runJobs(Func<object, List<LabelVector>> func)
        {
            List<Task<List<LabelVector>>> tasks = new List<Task<List<LabelVector>>>();
            foreach (Job job in _jobs)
            {
                tasks.Add(Task<List<LabelVector>>.Factory.StartNew(func, job));
            }
            Task.WaitAll(tasks.ToArray());
            List<LabelVector> result = new List<LabelVector>();
            foreach (Task<List<LabelVector>> task in tasks)
                result.AddRange(task.Result);
            return result;
        }

        private bool runJobs(Func<object, bool> func)
        {
            List<Task<bool>> tasks = new List<Task<bool>>();
            foreach (Job job in _jobs)
                tasks.Add(Task<bool>.Factory.StartNew(func, job));
            Task.WaitAll(tasks.ToArray());
            bool result = tasks[0].Result;
            for (int i = 1; i < tasks.Count; i++)
                result = result || tasks[i].Result;

            return result;
        }

        private void calculatePhi(object arg)
        {
            List<LabelVector> centers = _centers.Select(o => (LabelVector)o.Clone()).ToList();
            Job job = arg as Job;
            for (int i = job.StartIndex; i < job.EndIndex; i++)
            {
                LabelVector x = _data[i];
                LabelVector center = findNearest(x, centers);
                LabelVector dx = x - center;
                x.FeatureValue = (float)dx.DotProduct(dx);
            }
        }

        private List<LabelVector> findCenterCandidates(object arg)
        {
            Job job = arg as Job;
            List<LabelVector> result = new List<LabelVector>();            
            double norm = OversampleRate / _phi;
            for (int i = job.StartIndex; i < job.EndIndex; i++)
            {
                LabelVector x = _data[i];
                double sample = x.FeatureValue * norm;
                if (ThreadsafeRandom.Test(sample)){
                    LabelVector center = x.Clone() as LabelVector;
                    result.Add(center);                    
                }
            }
            return result;
        }

        private LabelVector findNearest(LabelVector x, List<LabelVector> centers)
        {
            LabelVector nearest = centers[0];
            double minDist = x.SquaredDistance(nearest);
            for (int i = 1; i < centers.Count; i++)
            {
                double test = x.SquaredDistance(centers[i], minDist);
                if (test < minDist)
                {
                    minDist = test;
                    nearest = centers[i];
                }
            }
            return nearest;
        }

        private bool assignCenters(object arg)
        {
            List<LabelVector> centers = _centers.Select(o => (LabelVector)o.Clone()).ToList();
            bool change = false;
            Job job = arg as Job;
            for (int i = job.StartIndex; i < job.EndIndex; i++)
            {
                LabelVector x = _data[i];
                LabelVector center = findNearest(x, centers);
                if (x.Label != center.Label)
                {
                    x.Label = center.Label;
                    change = true;
                }
            }
            return change;
        }       
    }
}
