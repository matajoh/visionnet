using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionNET.Texture
{
    /// <summary>
    /// Class which random selects filters from preset Filter Banks.
    /// </summary>
    public class FilterBankFactory : IFilterFactory
    {
        private FilterBank[] _banks;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="banks">Filter banks to use when generating filters</param>
        public FilterBankFactory(FilterBank[] banks)
        {
            _banks = banks;
        }
        
        /// <summary>
        /// Selects a random filter from the set of filter banks.
        /// </summary>
        /// <returns>A filter</returns>
        public Filter Create()
        {
            return _banks.SelectRandom().SelectRandom();
        }
    }
}
