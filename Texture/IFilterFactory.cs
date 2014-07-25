using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionNET.Texture
{
    /// <summary>
    /// Interface for a class which creates filters.
    /// </summary>
    public interface IFilterFactory
    {
        /// <summary>
        /// Create a filter.
        /// </summary>
        /// <returns>A new filter</returns>
        Filter Create();
    }
}
