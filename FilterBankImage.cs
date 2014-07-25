using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using VisionNET.Learning;
using VisionNET.Texture;

namespace VisionNET
{
    /// <summary>
    /// An image of filter bank responses, where the channels are the different filter responses at each pixel.
    /// </summary>
    public class FilterBankImage : IMultichannelImage<float>
    {
        private FloatArrayHandler _handler;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FilterBankImage()
        {
            _handler = new FloatArrayHandler();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rows">The number of rows in the image</param>
        /// <param name="columns">The number of columns in the image</param>
        /// <param name="filters">The number of filters (i.e. channels) in the image</param>
        public FilterBankImage(int rows, int columns, int filters)
        {
            _handler = new FloatArrayHandler(rows, columns, filters);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="data">The raw image data</param>
        public FilterBankImage(float[, ,] data)
        {
            _handler = new FloatArrayHandler(data, false);
        }

        /// <summary>
        /// Create a new Filter Bank Image.  The input image is run through the provided filter banks and their responses are concatenated together to form the channels of the image.
        /// </summary>
        /// <param name="input">The input image</param>
        /// <param name="filterBanks">The filter banks to apply</param>
        /// <returns>The image</returns>
        public static unsafe FilterBankImage Create(IMultichannelImage<float> input, params FilterBank[] filterBanks)
        {
            FilterBankImage image = new FilterBankImage();
            image.SetDimensions(input.Rows, input.Columns, filterBanks.Sum(o => o.DescriptorLength));
            fixed (float* dataSrc = image.RawArray)
            {
                float* dataPtr = dataSrc;
                for (short r = 0; r < input.Rows; r++)
                {
                    for (short c = 0; c < input.Columns; c++)
                    {
                        ImageDataPoint<float> point = new ImageDataPoint<float>(input, r, c, 0);
                        foreach (var fb in filterBanks)
                        {
                            float[] values = fb.Compute(point);
                            for (int i = 0; i < values.Length; i++, dataPtr++)
                                *dataPtr = values[i];
                        }
                    }
                }
            }
            return image;
        }

        /// <summary>
        /// Read a filter bank image from a stream.
        /// </summary>
        /// <param name="stream">The stream containing the filter bank image</param>
        /// <returns>The filter bank image</returns>
        public static unsafe FilterBankImage Read(Stream stream)
        {
            FilterBankImage image = new FilterBankImage();

            BinaryReader input = new BinaryReader(stream);
            int rows = input.ReadInt32();
            int columns = input.ReadInt32();
            int channels = input.ReadInt32();
            image.SetDimensions(rows, columns, channels);
            fixed (float* dataSrc = image.RawArray)
            {
                float* dataPtr = dataSrc;
                for (int r = 0; r < image.Rows; r++)
                    for (int c = 0; c < image.Columns; c++)
                        for (int i = 0; i < image.Channels; i++, dataPtr++)
                            *dataPtr = input.ReadSingle();
            }
            return image;
        }

        /// <summary>
        /// Write a FilterBankImage to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="image">The image to write</param>
        public static unsafe void Write(Stream stream, FilterBankImage image)
        {
            BinaryWriter output = new BinaryWriter(stream);
            output.Write(image.Rows);
            output.Write(image.Columns);
            output.Write(image.Channels);
            fixed (float* dataSrc = image.RawArray)
            {
                float* dataPtr = dataSrc;
                for (int r = 0; r < image.Rows; r++)
                {
                    for (int c = 0; c < image.Columns; c++)
                    {
                        for (int i = 0; i < image.Channels; i++, dataPtr++)
                        {
                            output.Write(*dataPtr);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The Width of the image in pixels
        /// </summary>
        public int Width
        {
            get { return _handler.Columns; }
        }

        /// <summary>
        /// The height of the image in pixels
        /// </summary>
        public int Height
        {
            get { return _handler.Rows; }
        }

        /// <summary>
        /// Converts this image to a bitmap (not implemented)
        /// </summary>
        /// <returns>Not implemented</returns>
        public BitmapSource ToBitmap()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The ID for the image
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// The number of rows in the image
        /// </summary>
        public int Rows
        {
            get { return _handler.Rows; }
        }

        /// <summary>
        /// The number of columns in the image
        /// </summary>
        public int Columns
        {
            get { return _handler.Columns; }
        }

        /// <summary>
        /// The number of channels in the image.  This will be the total number of filter reponses across all filter banks.
        /// </summary>
        public int Channels
        {
            get { return _handler.Channels; }
        }

        /// <summary>
        /// Clears all data from the image.
        /// </summary>
        public void Clear()
        {
            _handler.Clear();
        }

        /// <summary>
        /// Sets the pixel data of the image.
        /// </summary>
        /// <param name="data">The pixel data</param>
        public void SetData(float[, ,] data)
        {
            _handler.SetData(data);
        }

        /// <summary>
        /// Sets the dimensions of the image.  This will also clear the data.
        /// </summary>
        /// <param name="rows">The desired rows</param>
        /// <param name="columns">The desired columns</param>
        /// <param name="channels">The desired channels</param>
        public void SetDimensions(int rows, int columns, int channels)
        {
            _handler.SetDimensions(rows, columns, channels);
        }
        
        /// <summary>
        /// Whether this image is an integral image (used to facilitate the computation of rectangular sums)
        /// </summary>
        public bool IsIntegral
        {
            get
            {
                return _handler.IsIntegral;
            }
            set
            {
                _handler.IsIntegral = value;
            }
        }

        /// <summary>
        /// Compute a sum of values in a rectangle.
        /// </summary>
        /// <param name="startRow">Top of the rectangle</param>
        /// <param name="startColumn">Left of the rectangle</param>
        /// <param name="rows">Height of the rectangle</param>
        /// <param name="columns">Width of the rectangle</param>
        /// <param name="channel">The channel to use when computing the sum</param>
        /// <returns>The sum of all the values in a rectangle</returns>
        public float ComputeRectangleSum(int startRow, int startColumn, int rows, int columns, int channel)
        {
            return _handler.ComputeRectangleSum(startRow, startColumn, rows, columns, channel);
        }

        /// <summary>
        /// Extracts a single channel as a 2D array.
        /// </summary>
        /// <param name="channel">The channel to extract</param>
        /// <returns>The extracted channel</returns>
        public float[,] ExtractChannel(int channel)
        {
            return _handler.ExtractChannel(channel);
        }

        /// <summary>
        /// Extracts a sub-image as a 3D array.
        /// </summary>
        /// <param name="startRow">Top of the rectangle</param>
        /// <param name="startColumn">Left of the rectangle</param>
        /// <param name="rows">Height of the rectangle</param>
        /// <param name="columns">Width of the rectangle</param>
        /// <returns>A sub-image as a 3D array</returns>
        public float[, ,] ExtractRectangle(int startRow, int startColumn, int rows, int columns)
        {
            return _handler.ExtractRectangle(startRow, startColumn, rows, columns);
        }

        /// <summary>
        /// Returns a reference to the raw image pixels.
        /// </summary>
        public float[, ,] RawArray
        {
            get { return _handler.RawArray; }
        }

        /// <summary>
        /// Returns the pixel value at the desired index.
        /// </summary>
        /// <param name="row">Row in the image</param>
        /// <param name="column">Column in the image</param>
        /// <param name="channel">Channel, i.e. the filter response</param>
        /// <returns>The value</returns>
        public float this[int row, int column, int channel]
        {
            get
            {
                return _handler[row, column, channel];
            }
            set
            {
                _handler[row, column, channel] = value;
            }
        }
    }
}
