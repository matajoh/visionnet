/*
 * Vision.NET 2.1 Computer Vision Library
 * Copyright (C) 2009 Matthew Johnson
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace VisionNET.Learning
{
    internal class LabelCounter
    {
        private int _numThresholds;
        private int _numLabels;

        public LabelCounter(int numThresholds, int numLabels)
        {
            _numThresholds = numThresholds;
            _numLabels = numLabels;
        }

        public unsafe void Count(
            float[,] leftDistributions,
            float[,] rightDistributions,
            float[] values,
            float[] weights,
            int[] labels,
            float[] thresholds,
            float[] leftCounts,
            float[] rightCounts,
            float[] labelWeights
            )
        {
            Count(
                leftDistributions,
                rightDistributions,
                values,
                weights,
                labels,
                thresholds,
                leftCounts,
                rightCounts
                );

            fixed (float* leftCountsSrc = leftCounts,
                rightCountsSrc = rightCounts,
                leftDistributionsSrc = leftDistributions,
                rightDistributionsSrc = rightDistributions,
                classWeightsSrc = labelWeights)
            {
                float* leftCountsPtr = leftCountsSrc;
                float* rightCountsPtr = rightCountsSrc;
                float* leftDistributionsPtr = leftDistributionsSrc;
                float* rightDistributionsPtr = rightDistributionsSrc;

                for (int i = 0; i < _numThresholds; i++, leftCountsPtr++, rightCountsPtr++)
                {
                    *leftCountsPtr = 0;
                    *rightCountsPtr = 0;
                    float* weightsPtr = classWeightsSrc;
                    for (int j = 0; j < _numLabels; j++, weightsPtr++, leftDistributionsPtr++, rightDistributionsPtr++)
                    {
                        float weight = *weightsPtr;
                        *leftDistributionsPtr *= weight;
                        *leftCountsPtr += *leftDistributionsPtr;
                        *rightDistributionsPtr *= weight;
                        *rightCountsPtr += *rightDistributionsPtr;
                    }
                }
            }
        }

       
        public unsafe void Count(
            float[,] leftDistributions,
            float[,] rightDistributions,
            float[] values,
            float[] weights,
            int[] labels,
            float[] thresholds,
            float[] leftCounts,
            float[] rightCounts
            )
        {
            fixed (float* leftCountsSrc = leftCounts,
                rightCountsSrc = rightCounts,
                leftDistributionsSrc = leftDistributions,
                rightDistributionsSrc = rightDistributions
                )
            {
                float* leftCountsPtr = leftCountsSrc;
                float* rightCountsPtr = rightCountsSrc;
                float* leftDistributionsPtr = leftDistributionsSrc;
                float* rightDistributionsPtr = rightDistributionsSrc;

                for (int i = 0; i < _numThresholds; i++, leftCountsPtr++, rightCountsPtr++)
                {
                    *leftCountsPtr = 0;
                    *rightCountsPtr = 0;
                    for (int j = 0; j < _numLabels; j++, leftDistributionsPtr++, rightDistributionsPtr++)
                    {
                        *leftDistributionsPtr = 0;
                        *rightDistributionsPtr = 0;
                    }
                }
            }
            float minThresh = thresholds[0];
            float maxThresh = thresholds[_numThresholds - 1];
            int length = values.Length;
            fixed (float* valuesSrc = values, weightsSrc = weights)
            {
                fixed (int* labelsSrc = labels)
                {
                    float* valuesPtr = valuesSrc;
                    float* weightsPtr = weightsSrc;
                    int* labelsPtr = labelsSrc;
                    for (int i = 0; i < length; i++, valuesPtr++, labelsPtr++, weightsPtr++)
                    {
                        int label = *labelsPtr;
                        float value = *valuesPtr;
                        float weight = *weightsPtr;
                        if (value <= minThresh)
                        {
                            leftDistributions[0,label] += weight;
                            leftCounts[0] += weight;
                        }
                        else if (value > maxThresh)
                        {
                            rightDistributions[_numThresholds - 1, label] += weight;
                            rightCounts[_numThresholds - 1] += weight;
                        }
                        else
                        {
                            int index = Array.BinarySearch<float>(thresholds, value);
                            if (index < 0)
                                index = ~index;
                            leftDistributions[index,label] += weight;
                            leftCounts[index] += weight;
                            rightDistributions[index - 1,label] += weight;
                            rightCounts[index - 1] += weight;
                        }
                    }
                }
            }
            fixed (float* leftDistributionsSrc = leftDistributions,
                rightDistributionsSrc = rightDistributions,
                leftCountsSrc = leftCounts,
                rightCountsSrc = rightCounts)
            {
                float* lastLeftDistPtr = leftDistributionsSrc;
                float* lastRightDistPtr = rightDistributionsSrc + (_numThresholds - 1) * _numLabels;
                float* lastLeftCountsPtr = leftCountsSrc;
                float* lastRightCountsPtr = rightCountsSrc + _numThresholds - 1;
                float* currLeftDistPtr = lastLeftDistPtr + _numLabels;
                float* currRightDistPtr = lastRightDistPtr - _numLabels;
                float* currLeftCountsPtr = lastLeftCountsPtr + 1;
                float* currRightCountsPtr = lastRightCountsPtr - 1;

                for (int i = 0; i < _numThresholds - 1; i++)
                {
                    *currLeftCountsPtr += *lastLeftCountsPtr;
                    *currRightCountsPtr += *lastRightCountsPtr;
                    for (int j = 0; j < _numLabels; j++)
                    {
                        currLeftDistPtr[j] += lastLeftDistPtr[j];
                        currRightDistPtr[j] += lastRightDistPtr[j];
                    }

                    //update
                    lastLeftDistPtr = currLeftDistPtr;
                    lastRightDistPtr = currRightDistPtr;
                    lastLeftCountsPtr = currLeftCountsPtr;
                    lastRightCountsPtr = currRightCountsPtr;
                    currLeftDistPtr += _numLabels;
                    currRightDistPtr -= _numLabels;
                    currLeftCountsPtr++;
                    currRightCountsPtr--;
                }
            }
        }
    }
}
