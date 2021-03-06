﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Texture
{
    /// <summary>
    /// Implements the MR8 or maximum response 8 filter bank from "A Statistical Approach to Texture Classification from Single Images" by Varma and Zisserman.
    /// </summary>
    [Serializable]
    public class MR8 : FilterBank
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="channel">The channel to use when computing the filter responses.</param>
        public MR8(int channel) : base(generateFilters(channel))
        {
        }

        private static void addMRFilters(List<Filter> filters, Func<float, float, Filter> create)
        {
            for (int i = 0; i < 3; i++)
            {
                float stdDev = 1 << i;
                for (int j = 1; j < 7; j++)
                {
                    float orientation = (float)((j * Math.PI) / 6);
                    filters.Add(create(stdDev, orientation));
                }
            }
        }

        private static Filter[] generateFilters(int channel)
        {
            List<Filter> filters = new List<Filter>();
            addMRFilters(filters, (stddev, orientation) => new EdgeFilter(stddev, orientation, channel));
            addMRFilters(filters, (stddev, orientation) => new BarFilter(stddev, orientation, channel));
            filters.Add(new GaussianFilter(10, channel));
            filters.Add(new LaplacianFilter(10, channel));
            return filters.ToArray();
        }

        private float max(float[] values, int startIndex, int endIndex)
        {
            float max = float.MinValue;
            for (int i = startIndex; i < endIndex; i++)
                max = values[i] > max ? values[i] : max;
            return max;
        }

        private float[] maximumResponse(float[] rawResponse)
        {
            float[] maxResponse = new float[8];
            maxResponse[0] = max(rawResponse, 0, 6);
            maxResponse[1] = max(rawResponse, 6, 12);
            maxResponse[2] = max(rawResponse, 12, 18);
            maxResponse[3] = max(rawResponse, 18, 24);
            maxResponse[4] = max(rawResponse, 24, 30);
            maxResponse[5] = max(rawResponse, 30, 36);
            maxResponse[6] = rawResponse[36];
            maxResponse[7] = rawResponse[37];
            return maxResponse;
        }

        /// <summary>
        /// For a maximum response filter, the descriptor length is not equal to the number of filters in the bank, but in this case is instead 8.
        /// </summary>
        public override int DescriptorLength
        {
            get
            {
                return 8;
            }
        }

        /// <summary>
        /// Computes the descriptor as the maximums of an array of filter responses.
        /// </summary>
        /// <param name="samples">Samples to compute the filter bank response for</param>
        /// <param name="pyramid">Pyramid to use when computing responses</param>
        /// <returns>Filter bank descriptor</returns>
        public override List<Keypoint> Compute<T>(List<ScaleSpaceSample> samples, ScaleSpacePyramid<T> pyramid)
        {
            List<Keypoint> points = base.Compute<T>(samples, pyramid);
            foreach (var point in points)
                point.Descriptor = maximumResponse(point.Descriptor);
            return points;
        }

        /// <summary>
        /// Computes the maximum response of the filter bank at the provided point.
        /// </summary>
        /// <param name="point">Desired location</param>
        /// <returns>Filter response</returns>
        public override float[] Compute(Learning.ImageDataPoint<float> point)
        {
            return maximumResponse(base.Compute(point));
        }
    }
}
