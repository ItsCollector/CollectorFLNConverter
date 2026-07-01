using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectorFLN.Logging
{
    public class BeatmapFormatException : Exception
    {
        public BeatmapFormatException(string message) : base(message) { }
    }
}
