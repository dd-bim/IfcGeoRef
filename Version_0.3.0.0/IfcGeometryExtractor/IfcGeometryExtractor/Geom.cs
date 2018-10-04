//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using BimGisCad.Representation.Geometry.Elementary;
//using BimGisCad;

//namespace IfcGeometryExtractor
//{
//    class Geom
//    {
//        public static bool CreateSectSegmentPt(Line2 ray, Point2 segmentBeg, Point2 segmentEnd, out Point2 point)
//        {




//            double det = Direction2.Det(a.Direction, b.Direction);
//            var diff = a.Position - b.Position;
//            if(det > TRIGTOL || det < -TRIGTOL)
//            {
//                var pa = a.Position + (Direction2.Det(b.Direction, diff) / det * a.Direction);
//                var pb = b.Position + (Direction2.Det(a.Direction, diff) / det * b.Direction);
//                point = new Point2((pa.X + pb.X) / 2.0, (pa.Y + pb.Y) / 2.0);
//                return true;
//            }
//            else
//            {
//                point = default(Point2);
//                return false;
//            }
//        }

//        public static bool Create(Line2 a, Line2 b, out Point2 point)
//        {
//            double det = Direction2.Det(a.Direction, b.Direction);
//            var diff = a.Position - b.Position;
//            if(det > TRIGTOL || det < -TRIGTOL)
//            {
//                var pa = a.Position + (Direction2.Det(b.Direction, diff) / det * a.Direction);
//                var pb = b.Position + (Direction2.Det(a.Direction, diff) / det * b.Direction);
//                point = new Point2((pa.X + pb.X) / 2.0, (pa.Y + pb.Y) / 2.0);
//                return true;
//            }
//            else
//            {
//                point = default(Point2);
//                return false;
//            }
//        }

//        public void Test()
//        {
//            double direction(Point2 ) 

//        }


//    }
//}
