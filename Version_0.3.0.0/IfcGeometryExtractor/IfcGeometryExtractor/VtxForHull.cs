using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MIConvexHull;

namespace IfcGeometryExtractor
{
    public class VtxForHull : IVertex
    {
        public VtxForHull(double x, double y)
        {
            Position = new double[2] { x, y };
        }

        public double[] Position { get; set; }
    }
}
