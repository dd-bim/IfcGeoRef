using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfcGeoRefChecker.IO
{
    public class InputGroup
    {
        public List<JsonInput> InputObjects { get; set; }
        public string outputDirectory { get; set; }
        public bool outJson { get; set; }
        public bool outLog { get; set; }
    }
}
