using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VisionNET.MatLab
{
    /// <summary>
    /// Class representing the data in a MatLab 5.0 file
    /// </summary>
    public class MAT5
    {
        /// <summary>
        /// Description of the file format
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Version of the MatLab format used by the file
        /// </summary>
        public short Version { get; set; }

        /// <summary>
        /// The elements in the file
        /// </summary>
        public List<DataElement> Elements { get; set; }

        /// <summary>
        /// Load a MAT5 file from disk.
        /// </summary>
        /// <param name="inputFile">The location of the file on disk</param>
        /// <returns>A MAT5 file</returns>
        public static MAT5 Load(string inputFile)
        {
            MAT5 result = new MAT5();

            Stream stream = File.OpenRead(inputFile);
            byte[] buff = new byte[128];
            stream.Read(buff, 0, 128);
            result.Description = Encoding.ASCII.GetString(buff, 0, 124);

            result.Version = BitConverter.ToInt16(buff, 124);

            bool swapBytes = false;
            char test = (char)(BitConverter.ToInt16(buff, 126) >> 8);
            if(test == 'I')
                swapBytes = true;

            result.Elements = new List<DataElement>();
            
            while (stream.Position < stream.Length)
            {
                result.Elements.Add(DataElement.Read(stream, swapBytes));
            }

            return result;
        }

        /// <summary>
        /// Returns a representation of the contents of the MAT5 file.
        /// </summary>
        /// <returns>String representation of file contents</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var element in Elements)
                sb.AppendLine(element.ToString());
            return sb.ToString();
        }

    }
}
