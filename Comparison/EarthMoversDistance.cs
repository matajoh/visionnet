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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisionNET.Comparison
{
    /// <summary>
    /// Implementation of the Earth Mover's Distance, based upon Rubner et al. 1998 "A Metric for Distributions with Applications to Image Databases".
    /// This is heavily based upon Rubner's publically available C implementation of this algorithm.
    /// </summary>
    /// <typeparam name="T">Underlying type of the bin centers</typeparam>
    public static class EarthMoversDistance<T>
    {
        private class Node
        {
            public int I;
            public double Value;
            public Node Next;
        }

        private class Node2D
        {
            public int Index;
            public int I;
            public int J;
            public double Value;
            public Node2D Next;
            public Node2D NextR;
            public Node2D NextC;
        }

        /// <summary>
        /// Level of debugging information output by the algorithm during its computation.
        /// </summary>
        public enum DebugLevelType
        {
            /// <summary>
            /// No messages.
            /// </summary>
            None = 0,
            /// <summary>
            /// The final solution is output.
            /// </summary>
            Low = 1,
            /// <summary>
            /// Each iteration is output, in addition to the final solution.
            /// </summary>
            Medium = 2,
            /// <summary>
            /// Lots of detailed information about each stage of the process.
            /// </summary>
            High = 3,
            /// <summary>
            /// Tons of information used during debugging that you probably do not need.
            /// </summary>
            Full = 4
        }

        private static DebugLevelType _debugLevel;

        /// <summary>
        /// Level of debugging feedback provided during computation.
        /// </summary>
        public static DebugLevelType DebugLevel
        {
            get { return EarthMoversDistance<T>._debugLevel; }
            set { EarthMoversDistance<T>._debugLevel = value; }
        }

        private static double _epsilon = 1e-6;

        /// <summary>
        /// A value close to zero used during equality comparisons for floating point numbers.  Should
        /// be appropriate for your problem set.
        /// </summary>
        public static double Epsilon
        {
            get
            {
                return _epsilon;
            }
            set
            {
                _epsilon = value;
            }
        }

        private static int _maxIterations = 500;

        /// <summary>
        /// Maximum number of iterations for the algorithm.
        /// </summary>
        public static int MaxIterations
        {
            get
            {
                return _maxIterations;
            }
            set
            {
                _maxIterations = value;
            }
        }

        private static int _signature1Length, _signature2Length;                          
        private static float[,] _distanceMatrix;
        private static Node2D[] _exchanges; 
        private static Node2D _endExchange, _enterExchange;
        private static bool[,] _isExchange;
        private static Node2D[] _exchangeRows, _exchangeColumns;
        private static double _maximumWeight;
        private static float _maximumDistance;

        /// <summary>
        /// Computes the Earth Mover's Distance for the provided signatures.  A "signature" is special form of a histogram
        /// in which the key is the bin center and the value is the weight concentrated at that bin.  In addition to the
        /// signatures, a function which computes the distance between bin centers is required.
        /// </summary>
        /// <param name="signature1">The first signature to compare</param>
        /// <param name="signature2">The second signature to compare</param>
        /// <param name="binDistance">A function which determines the distance between two bin centers</param>
        /// <returns>The Earth Mover's Distance</returns>
        public static float Compute(Dictionary<T, float> signature1, Dictionary<T, float> signature2, Func<T, T, float> binDistance)
        {
            int iteration;
            double totalCost;
            float w;
            Node2D exchange;
            Node[] U, V;

            iteration = 0;

            w = (float)init(signature1, signature2, binDistance);

            if (_debugLevel > DebugLevelType.Low)
            {
                UpdateManager.WriteLine("\nInitial Solution:");
                printSolution();
            }

            U = new Node[_signature1Length];
            V = new Node[_signature2Length];
            for (int i = 0; i < _signature1Length; i++)
                U[i] = new Node();
            for (int j = 0; j < _signature2Length; j++)
                V[j] = new Node();

            if (_signature1Length > 1 && _signature2Length > 1)
            {
                for (iteration = 1; iteration < _maxIterations; iteration++)
                {
                    findBasicVariables(U, V);

                    if (isOptimal(U, V))
                        break;

                    newSol();

                    if (_debugLevel > DebugLevelType.Low)
                    {
                        UpdateManager.WriteLine("\nIteration # {0}", iteration);
                        printSolution();
                    }
                }

                if (iteration == _maxIterations)
                    throw new Exception("Maximum number of iterations has been reached");
            }

            totalCost = 0;
            for (exchange = _exchanges[0]; exchange != _endExchange; exchange = exchange.Next)
            {
                if (exchange == _enterExchange) 
                    continue;
                if (exchange.I == signature1.Count || exchange.J == signature2.Count)
                    continue;

                if (exchange.Value == 0)
                    continue;

                totalCost += (double)exchange.Value * _distanceMatrix[exchange.I, exchange.J];
            }

            if(_debugLevel > DebugLevelType.None)
                UpdateManager.WriteLine("\n*** Optimal Solution ({0} Iterations): {1} ***", iteration, totalCost);

            return (float)(totalCost / w);
        }

        private static double init(Dictionary<T, float> signature1, Dictionary<T, float> signature2, Func<T, T, float> binDistance)
        {
            int i, j;
            double supplySum, demandSum, diff;
            double[] supply = new double[signature1.Count + 1];
            double[] demand = new double[signature2.Count + 1];

            _signature1Length = signature1.Count;
            _signature2Length = signature2.Count;

            T[] features1 = signature1.Keys.ToArray();
            T[] features2 = signature2.Keys.ToArray();

            _distanceMatrix = new float[supply.Length, demand.Length];

            _maximumDistance = 0;
            for (i = 0; i < _signature1Length; i++)
                for (j = 0; j < _signature2Length; j++)
                {
                    _distanceMatrix[i, j] = binDistance(features1[i], features2[j]);
                    if (_distanceMatrix[i, j] > _maximumDistance)
                        _maximumDistance = _distanceMatrix[i, j];
                }

            for (i = 0; i < _signature1Length; i++)
            {
                supply[i] = signature1[features1[i]];
            }
            supplySum = supply.Sum();

            for (j = 0; j < _signature2Length; j++)
            {
                demand[j] = signature2[features2[j]];
            }
            demandSum = demand.Sum();

            diff = supplySum - demandSum;
            if (Math.Abs(diff) >= _epsilon * supplySum)
            {
                if (diff < 0.0)
                {
                    for (j = 0; j < _signature2Length; j++)
                        _distanceMatrix[_signature1Length, j] = 0;
                    supply[_signature1Length] = -diff;
                    _signature1Length++;
                }
                else
                {
                    for (i = 0; i < _signature1Length; i++)
                        _distanceMatrix[i, _signature2Length] = 0;
                    demand[_signature2Length] = diff;
                    _signature2Length++;
                }
            }

            _exchangeRows = new Node2D[_signature1Length];
            _exchangeColumns = new Node2D[_signature2Length];
            _isExchange = new bool[_signature1Length, _signature2Length];
            _exchanges = new Node2D[_signature1Length + _signature2Length];
            for (i = 0; i < _exchanges.Length; i++)
                _exchanges[i] = new Node2D { Index = i };
            for (i = 0; i < _exchanges.Length - 1; i++)
                _exchanges[i].Next = _exchanges[i + 1];
            _endExchange = _exchanges[0];

            _maximumWeight = supplySum > demandSum ? supplySum : demandSum;

            // Find the initial solution
            russel(supply, demand);

            _enterExchange = _endExchange;
            _endExchange = _endExchange.Next;

            return supplySum > demandSum ? demandSum : supplySum;
        }

        private static void findBasicVariables(Node[] U, Node[] V)
        {
            int i, j;
            bool found;
            int UfoundNum, VfoundNum;
            Node u0Head, u1Head, currentU, previousU;
            Node v0Head, v1Head, currentV, previousV;

            u0Head = new Node();
            v0Head = new Node();
            u1Head = new Node();
            v1Head = new Node();

            u0Head.Next = U[0];
            for (i = 0; i < _signature1Length; i++)
            {
                currentU = U[i];
                currentU.I = i;
                if (i < _signature1Length - 1)
                    currentU.Next = U[i + 1];
            }
            U[_signature1Length - 1].Next = null;

            v0Head.Next = _signature2Length > 1 ? V[1] : null;
            for (j = 1; j < _signature2Length; j++)
            {
                currentV = V[j];
                currentV.I = j;
                if (j < _signature2Length - 1)
                    currentV.Next = V[j + 1];
            }
            V[_signature2Length - 1].Next = null;

            V[0].I = 0;
            V[0].Value = 0;
            v1Head.Next = V[0];
            v1Head.Next.Next = null;

            UfoundNum = VfoundNum = 0;
            while (UfoundNum < _signature1Length || VfoundNum < _signature2Length)
            {
                if (_debugLevel == DebugLevelType.Full)
                {
                    UpdateManager.Write("UfoundNum={0}/{1},VfoundNum={2}/{3}\n", UfoundNum, _signature1Length, VfoundNum, _signature2Length);
                    UpdateManager.Write("U0=");
                    for (currentU = u0Head.Next; currentU != null; currentU = currentU.Next)
                        UpdateManager.Write("[{0}]", currentU.I);
                    UpdateManager.Write("\n");
                    UpdateManager.Write("U1=");
                    for (currentU = u1Head.Next; currentU != null; currentU = currentU.Next)
                        UpdateManager.Write("[{0}]", currentU.I);
                    UpdateManager.Write("\n");
                    UpdateManager.Write("V0=");
                    for (currentV = v0Head.Next; currentV != null; currentV = currentV.Next)
                        UpdateManager.Write("[{0}]", currentV.I);
                    UpdateManager.Write("\n");
                    UpdateManager.Write("V1=");
                    for (currentV = v1Head.Next; currentV != null; currentV = currentV.Next)
                        UpdateManager.Write("[{0}]", currentV.I);
                    UpdateManager.Write("\n\n");
                }

                found = false;
                if (VfoundNum < _signature2Length)
                {
                    previousV = v1Head;
                    for (currentV = v1Head.Next; currentV != null; currentV = currentV.Next)
                    {
                        j = currentV.I;
                        previousU = u0Head;
                        for (currentU = u0Head.Next; currentU != null; currentU = currentU.Next)
                        {
                            i = currentU.I;
                            if (_isExchange[i, j])
                            {
                                currentU.Value = _distanceMatrix[i, j] - currentV.Value;
                                previousU.Next = currentU.Next;
                                currentU.Next = u1Head.Next != null ? u1Head.Next : null;
                                u1Head.Next = currentU;
                                currentU = previousU;
                            }
                            else
                                previousU = currentU;
                        }
                        previousV.Next = currentV.Next;
                        VfoundNum++;
                        found = true;
                    }
                }
                if (UfoundNum < _signature1Length)
                {
                    previousU = u1Head;
                    for (currentU = u1Head.Next; currentU != null; currentU = currentU.Next)
                    {
                        i = currentU.I;
                        previousV = v0Head;
                        for (currentV = v0Head.Next; currentV != null; currentV = currentV.Next)
                        {
                            j = currentV.I;
                            if (_isExchange[i, j])
                            {
                                currentV.Value = _distanceMatrix[i, j] - currentU.Value;
                                previousV.Next = currentV.Next;
                                currentV.Next = v1Head.Next != null ? v1Head.Next : null;
                                v1Head.Next = currentV;
                                currentV = previousV;
                            }
                            else
                                previousV = currentV;
                        }
                        previousU.Next = currentU.Next;
                        UfoundNum++;
                        found = true;
                    }
                }
                if (!found)
                {
                    throw new Exception("Unexpected error in findBasicVariables!  This typically happens when Epsilon is not right for the scale of the problem");
                }
            }
        }

        private static bool isOptimal(Node[] U, Node[] V)
        {
            double delta, deltaMin;
            int i, j, minI, minJ;

            minI = minJ = 0;

            deltaMin = double.PositiveInfinity;
            for (i = 0; i < _signature1Length; i++)
                for (j = 0; j < _signature2Length; j++)
                    if (!_isExchange[i, j])
                    {
                        delta = _distanceMatrix[i, j] - U[i].Value - V[j].Value;
                        if (deltaMin > delta)
                        {
                            deltaMin = delta;
                            minI = i;
                            minJ = j;
                        }
                    }

            if (_debugLevel == DebugLevelType.Full)
                UpdateManager.Write("deltaMin={0}\n", deltaMin);

            if (deltaMin == double.PositiveInfinity)
            {
                throw new Exception("Unexpected error in isOptimal.");
            }

            _enterExchange.I = minI;
            _enterExchange.J = minJ;

            return deltaMin >= -_epsilon * _maximumDistance;
        }

        private static void newSol()
        {
            int i, j, k;
            double xMin;
            int steps;
            Node2D[] loop = new Node2D[_signature1Length + _signature2Length];
            Node2D currentExchange, leaveExchange;

            if (_debugLevel == DebugLevelType.Full)
                UpdateManager.Write("EnterX = ({0},{1})\n", _enterExchange.I, _enterExchange.J);

            currentExchange = leaveExchange = null;

            i = _enterExchange.I;
            j = _enterExchange.J;
            _isExchange[i, j] = true;
            _enterExchange.NextC = _exchangeRows[i];
            _enterExchange.NextR = _exchangeColumns[j];
            _enterExchange.Value = 0;
            _exchangeRows[i] = _enterExchange;
            _exchangeColumns[j] = _enterExchange;

            steps = findLoop(loop);

            xMin = double.PositiveInfinity;
            for (k = 1; k < steps; k += 2)
            {
                if (loop[k].Value < xMin)
                {
                    leaveExchange = loop[k];
                    xMin = loop[k].Value;
                }
            }

            for (k = 0; k < steps; k += 2)
            {
                loop[k].Value += xMin;
                loop[k + 1].Value -= xMin;
            }

            if (_debugLevel == DebugLevelType.Full)
                UpdateManager.Write("LeaveX = ({0},{1})\n", leaveExchange.I, leaveExchange.J);

            i = leaveExchange.I;
            j = leaveExchange.J;
            _isExchange[i, j] = false;
            if (_exchangeRows[i] == leaveExchange)
                _exchangeRows[i] = leaveExchange.NextC;
            else
                for (currentExchange = _exchangeRows[i]; currentExchange != null; currentExchange = currentExchange.NextC)
                    if (currentExchange.NextC == leaveExchange)
                    {
                        currentExchange.NextC = currentExchange.NextC.NextC;
                        break;
                    }
            if (_exchangeColumns[j] == leaveExchange)
                _exchangeColumns[j] = leaveExchange.NextR;
            else
                for (currentExchange = _exchangeColumns[j]; currentExchange != null; currentExchange = currentExchange.NextR)
                    if (currentExchange.NextR == leaveExchange)
                    {
                        currentExchange.NextR = currentExchange.NextR.NextR;
                        break;
                    }

            _enterExchange = leaveExchange;
        }

        private static int findLoop(Node2D[] Loop)
        {
            int i, steps;
            int currentExchange = 0;
            Node2D newExchange;
            bool[] IsUsed = new bool[_signature1Length + _signature2Length];

            for (i = 0; i < _signature1Length + _signature2Length; i++)
                IsUsed[i] = false;

            newExchange = Loop[currentExchange] = _enterExchange;
            IsUsed[_enterExchange.Index] = true;
            steps = 1;

            do
            {
                if (steps % 2 == 1)
                {
                    newExchange = _exchangeRows[newExchange.I];
                    while (newExchange != null && IsUsed[newExchange.Index])
                        newExchange = newExchange.NextC;
                }
                else
                {
                    newExchange = _exchangeColumns[newExchange.J];
                    while (newExchange != null && IsUsed[newExchange.Index] && newExchange != _enterExchange)
                        newExchange = newExchange.NextR;
                    if (newExchange == _enterExchange)
                        break;
                }

                if (newExchange != null)
                {
                    Loop[++currentExchange] = newExchange;
                    IsUsed[newExchange.Index] = true;
                    steps++;

                    if (_debugLevel == DebugLevelType.Full)
                        UpdateManager.Write("steps={0}, NewX=({1},{2})\n", steps, newExchange.I, newExchange.J);
                }
                else 
                {
                    do
                    {
                        newExchange = Loop[currentExchange];
                        do
                        {
                            if (steps % 2 == 1)
                                newExchange = newExchange.NextR;
                            else
                                newExchange = newExchange.NextC;
                        } while (newExchange != null && IsUsed[newExchange.Index]);

                        if (newExchange == null)
                        {
                            IsUsed[Loop[currentExchange].Index] = false;
                            currentExchange--;
                            steps--;
                        }
                    } while (newExchange == null && currentExchange >= 0);

                    if (_debugLevel == DebugLevelType.Full)
                        UpdateManager.Write("BACKTRACKING TO: steps={0}, NewX=({1},{2})\n", steps, newExchange.I, newExchange.J);

                    IsUsed[Loop[currentExchange].Index] = false;
                    Loop[currentExchange] = newExchange;
                    IsUsed[newExchange.Index] = true;
                }
            } while (currentExchange >= 0);

            if (currentExchange == 0)
            {
                throw new Exception("Unexpected error in findLoop");
            }

            if (_debugLevel == DebugLevelType.Full)
            {
                UpdateManager.Write("FOUND LOOP:\n");
                for (i = 0; i < steps; i++)
                    UpdateManager.Write("{0}: ({1},{2})\n", i, Loop[i].I, Loop[i].J);
            }

            return steps;
        }

        private static void russel(double[] S, double[] D)
        {
            int i, j, minI, minJ;
            bool found;
            double deltaMin, oldVal, diff;
            double[,] delta = new double[_signature1Length, _signature2Length];
            Node[] Ur = new Node[_signature1Length];
            for (i = 0; i < Ur.Length; i++)
                Ur[i] = new Node();
            Node[] Vr = new Node[_signature2Length];
            for (j = 0; j < Vr.Length; j++)
                Vr[j] = new Node();
            Node uHead, currentU, previousU;
            Node vHead, currentV, previousV;
            Node previousUMinI, previousVMinJ, remember;

            previousUMinI = previousVMinJ = null;
            minI = minJ = 0;

            uHead = new Node();
            vHead = new Node();

            uHead.Next = Ur[0];
            for (i = 0; i < _signature1Length; i++)
            {
                currentU = Ur[i];
                currentU.I = i;
                currentU.Value = double.NegativeInfinity;
                if (i < _signature1Length - 1)
                    currentU.Next = Ur[i + 1];
            }

            vHead.Next = Vr[0];
            for (j = 0; j < _signature2Length; j++)
            {
                currentV = Vr[j];
                currentV.I = j;
                currentV.Value = double.NegativeInfinity;
                if (j < _signature2Length - 1)
                    currentV.Next = Vr[j + 1];
            }

            for (i = 0; i < _signature1Length; i++)
                for (j = 0; j < _signature2Length; j++)
                {
                    float v;
                    v = _distanceMatrix[i, j];
                    if (Ur[i].Value <= v)
                        Ur[i].Value = v;
                    if (Vr[j].Value <= v)
                        Vr[j].Value = v;
                }

            for (i = 0; i < _signature1Length; i++)
                for (j = 0; j < _signature2Length; j++)
                    delta[i, j] = _distanceMatrix[i, j] - Ur[i].Value - Vr[j].Value;

            do
            {
                if (_debugLevel == DebugLevelType.Full)
                {
                    UpdateManager.Write("Ur=");
                    for (currentU = uHead.Next; currentU != null; currentU = currentU.Next)
                        UpdateManager.Write("[{0}]", currentU.I);
                    UpdateManager.Write("\n");
                    UpdateManager.Write("Vr=");
                    for (currentV = vHead.Next; currentV != null; currentV = currentV.Next)
                        UpdateManager.Write("[{0}]", currentV.I);
                    UpdateManager.Write("\n");
                    UpdateManager.Write("\n\n");
                }

                found = false;
                deltaMin = double.PositiveInfinity;
                previousU = uHead;
                for (currentU = uHead.Next; currentU != null; currentU = currentU.Next)
                {
                    i = currentU.I;
                    previousV = vHead;
                    for (currentV = vHead.Next; currentV != null; currentV = currentV.Next)
                    {
                        j = currentV.I;
                        if (deltaMin > delta[i, j])
                        {
                            deltaMin = delta[i, j];
                            minI = i;
                            minJ = j;
                            previousUMinI = previousU;
                            previousVMinJ = previousV;
                            found = true;
                        }
                        previousV = currentV;
                    }
                    previousU = currentU;
                }

                if (!found)
                    break;

                remember = previousUMinI.Next;
                addBasicVariable(minI, minJ, S, D, previousUMinI, previousVMinJ, uHead);

                if (remember == previousUMinI.Next)
                {
                    for (currentV = vHead.Next; currentV != null; currentV = currentV.Next)
                    {
                        j = currentV.I;
                        if (currentV.Value == _distanceMatrix[minI, j])
                        {
                            oldVal = currentV.Value;
                            currentV.Value = double.NegativeInfinity;
                            for (currentU = uHead.Next; currentU != null; currentU = currentU.Next)
                            {
                                i = currentU.I;
                                if (currentV.Value <= _distanceMatrix[i, j])
                                    currentV.Value = _distanceMatrix[i, j];
                            }

                            diff = oldVal - currentV.Value;
                            if (Math.Abs(diff) < _epsilon * _maximumDistance)
                                for (currentU = uHead.Next; currentU != null; currentU = currentU.Next)
                                    delta[currentU.I, j] += diff;
                        }
                    }
                }
                else
                {
                    for (currentU = uHead.Next; currentU != null; currentU = currentU.Next)
                    {
                        i = currentU.I;
                        if (currentU.Value == _distanceMatrix[i, minJ])
                        {
                            oldVal = currentU.Value;
                            currentU.Value = double.NegativeInfinity;
                            for (currentV = vHead.Next; currentV != null; currentV = currentV.Next)
                            {
                                j = currentV.I;
                                if (currentU.Value <= _distanceMatrix[i, j])
                                    currentU.Value = _distanceMatrix[i, j];
                            }

                            diff = oldVal - currentU.Value;
                            if (Math.Abs(diff) < _epsilon * _maximumDistance)
                                for (currentV = vHead.Next; currentV != null; currentV = currentV.Next)
                                    delta[i, currentV.I] += diff;
                        }
                    }
                }
            } while (uHead.Next != null || vHead.Next != null);
        }

        static void addBasicVariable(int minI, int minJ, double[] supply, double[] demand, Node previousUMinI, Node previousVMinJ, Node UHead)
        {
            double T;

            if (Math.Abs(supply[minI] - demand[minJ]) <= _epsilon * _maximumWeight)
            {
                T = supply[minI];
                supply[minI] = 0;
                demand[minJ] -= T;
            }
            else if (supply[minI] < demand[minJ])
            {
                T = supply[minI];
                supply[minI] = 0;
                demand[minJ] -= T;
            }
            else
            {
                T = demand[minJ];
                demand[minJ] = 0;
                supply[minI] -= T;
            }

            _isExchange[minI, minJ] = true;

            _endExchange.Value = T;
            _endExchange.I = minI;
            _endExchange.J = minJ;
            _endExchange.NextC = _exchangeRows[minI];
            _endExchange.NextR = _exchangeColumns[minJ];
            _exchangeRows[minI] = _endExchange;
            _exchangeColumns[minJ] = _endExchange;
            _endExchange = _endExchange.Next;

            if (supply[minI] == 0 && UHead.Next.Next != null)
                previousUMinI.Next = previousUMinI.Next.Next;
            else
                previousVMinJ.Next = previousVMinJ.Next.Next;
        }

        static void printSolution()
        {
            Node2D P;
            double totalCost;

            totalCost = 0;

            if(_debugLevel > DebugLevelType.Medium)
                UpdateManager.WriteLine("Sig1\tSig2\tFlow\tCost");
            for (P = _exchanges[0]; P != _endExchange; P = P.Next)
                if (P != _enterExchange && _isExchange[P.I, P.J])
                {
                    if(_debugLevel > DebugLevelType.Medium)
                        UpdateManager.WriteLine("{0}\t{1}\t{2}\t{3}", P.I, P.J, (float)P.Value, _distanceMatrix[P.I, P.J]);
                    totalCost += (double)P.Value * _distanceMatrix[P.I, P.J];
                }

            UpdateManager.Write("Cost = {0}\n", (float)totalCost);
        }
    }
}
