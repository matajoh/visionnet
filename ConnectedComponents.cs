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
using VisionNET.Learning;

namespace VisionNET
{
    /// <summary>
    /// Performs connected components analysis on a binary image.
    /// </summary>
    public static class ConnectedComponents
    {
        private struct LabelPair
        {
            private int _label1;

            public int Label1
            {
                get { return _label1; }
                set { _label1 = value; }
            }
            private int _label2;

            public int Label2
            {
                get { return _label2; }
                set { _label2 = value; }
            }
        }

        private static LabelPair[] _nodes = new LabelPair[4096];
        private static int _current = 0;

        private static void list_add(LabelPair node)
        {
            if (_current == _nodes.Length)
            {
                LabelPair[] tmp = new LabelPair[_current * 2];
                Array.Copy(_nodes, tmp, _current);
                _nodes = tmp;
            }
            _nodes[_current++] = node;
        }

        /// <summary>
        /// Performs connected components analysis.
        /// </summary>
        /// <param name="image">The image to analyze</param>
        /// <returns>The connected components image</returns>
        public static unsafe LabelImage Compute(BinaryImage image)
        {
            _current = 0;
            int r, c;
            short currentLabel, currentSet;
            short label0, label1, label2, label3, set0, set1;
            short tl, t, tr, l;
            int rows = image.Rows;
            int columns = image.Columns;
            int count, labelCount;
            short[] equiv = new short[rows*columns];
            LabelImage labels = new LabelImage(rows, columns);

            fixed (bool* imageBuf = image.RawArray)
            {
                fixed (short* labelsBuf = labels.RawArray, equivBuf = equiv)
                {
                    bool* imagePtr = imageBuf + columns + 1;
                    short* tlPtr = labelsBuf;
                    short* tPtr = tlPtr + 1;
                    short* trPtr = tPtr + 1;
                    short* lPtr = tlPtr + columns;
                    short* labelsPtr = lPtr + 1;
                    short* equivPtr = equivBuf;
                    short* set0Ptr, set1Ptr;

                    label0 = label1 = label2 = label3 = 0;
                    currentLabel = 1;
                    r = rows - 2;
                    while (r-- != 0)
                    //for (r = 1; r < rows - 1; r++)
                    {
                        c = columns - 2;
                        // we are already at c+1
                        while (c-- != 0)
                        //for (c = 1; c < columns - 1; c++)
                        {
                            if (*imagePtr)
                            {
                                tl = *tlPtr;
                                t = *tPtr;
                                tr = *trPtr;
                                l = *lPtr;

                                labelCount = 0;

                                if (tl != 0)
                                {
                                    label0 = tl;
                                    labelCount++;
                                }

                                if (t != 0)
                                {
                                    if (labelCount == 0)
                                    {
                                        labelCount++;
                                        label0 = t;
                                    }
                                    else if (labelCount == 1 && t != label0)
                                    {
                                        labelCount++;
                                        label1 = t;
                                    }
                                }

                                if (tr != 0)
                                {
                                    if (labelCount == 0)
                                    {
                                        labelCount++;
                                        label0 = tr;
                                    }

                                    else if (labelCount == 1 && tr != label0)
                                    {
                                        labelCount++;
                                        label1 = tr;
                                    }
                                    else if (labelCount == 2 && tr != label0 && tr != label1)
                                    {
                                        labelCount++;
                                        label2 = tr;
                                    }
                                }

                                if (l != 0)
                                {
                                    if (labelCount == 0)
                                    {
                                        labelCount++;
                                        label0 = l;
                                    }
                                    else if (labelCount == 1 && l != label0)
                                    {
                                        labelCount++;
                                        label1 = l;
                                    }
                                    else if (labelCount == 2 && l != label0 && l != label1)
                                    {
                                        labelCount++;
                                        label2 = l;
                                    }
                                    else if (labelCount == 3 && l != label0 && l != label1 && l != label2)
                                    {
                                        labelCount++;
                                        label3 = l;
                                    }
                                }

                                if (labelCount == 0)
                                    *labelsPtr = currentLabel++;
                                else if (labelCount == 1)
                                    *labelsPtr = label0;
                                else if (labelCount == 2)
                                {
                                    *labelsPtr = label0;
                                    list_add(new LabelPair { Label1 = label0, Label2 = label1 });
                                }
                                else if (labelCount == 3)
                                {
                                    *labelsPtr = label0;
                                    list_add(new LabelPair { Label1 = label0, Label2 = label1 });
                                    list_add(new LabelPair { Label1 = label0, Label2 = label2 });
                                    list_add(new LabelPair { Label1 = label1, Label2 = label2 });
                                }
                                else if (labelCount == 4)
                                {
                                    *labelsPtr = label0;
                                    list_add(new LabelPair { Label1 = label0, Label2 = label1 });
                                    list_add(new LabelPair { Label1 = label0, Label2 = label2 });
                                    list_add(new LabelPair { Label1 = label0, Label2 = label3 });
                                    list_add(new LabelPair { Label1 = label1, Label2 = label2 });
                                    list_add(new LabelPair { Label1 = label1, Label2 = label3 });
                                    list_add(new LabelPair { Label1 = label2, Label2 = label3 });
                                }
                            }
                            imagePtr++;
                            tlPtr++;
                            tPtr++;
                            trPtr++;
                            lPtr++;
                            labelsPtr++;
                        }
                        // get to c+1
                        imagePtr += 2;
                        tlPtr += 2;
                        tPtr += 2;
                        trPtr += 2;
                        lPtr += 2;
                        labelsPtr += 2;
                    }

                    // resolve equivalencies
                    count = currentLabel;
                    while (count-- != 0)
                    {
                        *equivPtr++ = 0;
                    }
                    currentSet = 1;
                    fixed (LabelPair* nodeBuf = _nodes)
                    {
                        LabelPair* nodePtr = nodeBuf;

                        while (_current-- != 0)
                        {
                            LabelPair current = *nodePtr++;

                            set0Ptr = equivBuf + current.Label1;
                            set1Ptr = equivBuf + current.Label2;
                            bool contain1 = *set0Ptr != 0;
                            bool contain2 = *set1Ptr != 0;
                            if (contain1 && contain2)
                            {
                                set0 = *set0Ptr;
                                set1 = *set1Ptr;
                                if (set0 != set1)
                                {
                                    equivPtr = equivBuf;
                                    count = currentLabel;
                                    while (count-- != 0)
                                    {
                                        if (*equivPtr == set1)
                                            *equivPtr = set0;
                                        equivPtr++;
                                    }

                                }
                            }
                            else if (contain1)
                            {
                                *set1Ptr = *set0Ptr;
                            }
                            else if (contain2)
                            {
                                *set0Ptr = *set1Ptr;
                            }
                            else
                            {
                                *set0Ptr = currentSet;
                                *set1Ptr = currentSet;
                                currentSet++;
                            }
                        }
                    }

                    labelsPtr = labelsBuf;
                    count = rows * columns;
                    while (count-- != 0)
                    {
                        currentLabel = *labelsPtr;

                        if (currentLabel == 0)
                        {
                            labelsPtr++;
                            continue;
                        }

                        equivPtr = equivBuf + currentLabel;
                        if (*equivPtr == 0)
                            *equivPtr = currentSet++;
                        *labelsPtr++ = *equivPtr;
                    }
                }
            }

            return labels;
        }
    }
}
