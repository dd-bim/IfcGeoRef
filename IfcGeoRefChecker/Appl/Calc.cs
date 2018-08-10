using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Media3D;

namespace IfcGeoRefChecker.Appl
{
    //class with methods for calculating between selectable units or for correct updating of IfcModels

    internal class Calc
    {
        private const double DegToRad = Math.PI / 180;
        private Vector xy_TN_def = new Vector(0, 1);
        private Vector3D xyz_xAxis_def = new Vector3D(1, 0, 0);
        private Vector3D xyz_zAxis_def = new Vector3D(0, 0, 1);

        //convert length unit (Level 20, 30 and 40)

        public List<double> ConvertLengthUnit(string unitA, string unitB, List<double> length)
        {
            List<double> lengthConv = new List<double>();

            for(var i = 0; i < length.Count; i++)
            {
                if(unitA.Equals(unitB))
                    lengthConv.Add(length[i]);
                if(unitA == "m" && unitB == "mm")
                    lengthConv.Add(length[i] * 1000);
                if(unitA == "mm" && unitB == "m")
                    lengthConv.Add(length[i] / 1000);
                if(unitA == "ft" && unitB == "in")
                    lengthConv.Add(length[i] * 12);
                if(unitA == "in" && unitB == "ft")
                    lengthConv.Add(length[i] / 12);
                if(unitA == "m" && unitB == "ft")
                    lengthConv.Add(length[i] * 3.280839895);
                if(unitA == "ft" && unitB == "m")
                    lengthConv.Add(length[i] * 0.3048);
                if(unitA == "mm" && unitB == "in")
                    lengthConv.Add(length[i] * 0.0393701);
                if(unitA == "in" && unitB == "mm")
                    lengthConv.Add(length[i] * 25.4);
                if(unitA == "mm" && unitB == "ft")
                    lengthConv.Add(length[i] * 0.00328084);
                if(unitA == "ft" && unitB == "mm")
                    lengthConv.Add(length[i] * 304.8);
                if(unitA == "m" && unitB == "in")
                    lengthConv.Add(length[i] * 39.37007874);
                if(unitA == "in" && unitB == "m")
                    lengthConv.Add(length[i] * 0.0254);

                lengthConv[i] = Math.Round(lengthConv[i], 3);
            }

            return lengthConv;
        }

        //deg (dd) to deg (dms) for Level 20

        public double[] DDtoDMS(double angleDD)
        {
            // set decimal_degrees value here

            double[] dms = new double[3];

            dms[0] = ((angleDD < 0) == true) ? -Math.Ceiling(angleDD) : Math.Floor(angleDD);
            double minutes = (Math.Abs(angleDD) - dms[0]) * 60.0;
            dms[1] = Math.Floor(minutes);
            dms[2] = Math.Round(((Math.Abs(minutes) - dms[1]) * 60.0), 3);

            return dms;
        }

        //deg (dd) to deg (dms) for Level 20 updating (IfcCompoundPlaneAngleMeasure)

        public double[] DDtoCompound(double angleDD)
        {
            // set decimal_degrees value here
            var angle = angleDD;

            if(angleDD < 0)
            {
                angle = angleDD * (-1);
            };

            double[] dms = new double[4];

            dms[0] = Math.Floor(angle);
            double minutes = (angle - dms[0]) * 60.0;
            dms[1] = Math.Floor(minutes);
            double seconds = (minutes - dms[1]) * 60.0;
            dms[2] = Math.Floor(seconds);
            dms[3] = Math.Round((seconds - dms[2]) * 1000000);

            if(angleDD < 0)
            {
                dms[0] = -dms[0];
                dms[1] = -dms[1];
                dms[2] = -dms[2];
                dms[3] = -dms[3];
            };

            return dms;
        }

        //deg (dms) to deg (dd) for Level 20

        public double DMStoDD(string angleDMS)
        {
            string[] separators = { "°", "'", "''", " " };
            string[] values = angleDMS.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            double[] ddval = new double[3];

            for(var i = 0; i < values.Length; i++)
            {
                ddval[i] = double.Parse(values[i]);
            }

            if(ddval[0] < 0)
            {
                ddval[1] = -ddval[1];
                ddval[2] = -ddval[2];
            }

            double angleDD = ddval[0] + ddval[1] / 60 + ddval[2] / 3600;

            return angleDD;
        }

        //rotation matrices for Level 30 and 40

        //rotation matrix x-axis

        private static Matrix3D NewRotateAroundX(double radians)
        {
            var matrix = new Matrix3D();
            matrix.M11 = Math.Cos(radians);
            matrix.M12 = Math.Sin(radians);
            matrix.M21 = -(Math.Sin(radians));
            matrix.M22 = Math.Cos(radians);
            matrix.M33 = 1;
            return matrix;
        }

        //rotation matrix z-axis

        private static Matrix3D NewRotateAroundZ(double radians)
        {
            var matrix = new Matrix3D();
            matrix.M22 = Math.Cos(radians);
            matrix.M23 = Math.Sin(radians);
            matrix.M32 = -(Math.Sin(radians));
            matrix.M33 = Math.Cos(radians);
            matrix.M11 = 1;
            return matrix;
        }

        //rotation calculation for Level 30 and 40

        public Vector3D GetVector3DForXAxis(double angleX)
        {
            double rad = angleX * DegToRad;

            Vector3D xyz_xAxis = NewRotateAroundX(rad).Transform(xyz_xAxis_def);

            return xyz_xAxis;
        }

        public Vector3D GetVector3DForZAxis(double angleZ)
        {
            double rad = angleZ * DegToRad;

            Vector3D xyz_zAxis = NewRotateAroundZ(rad).Transform(xyz_zAxis_def);

            return xyz_zAxis;
        }

        //calc vector from given angle in 2D for Level 40 (True North) and 50 (Plane rotation)

        public Vector GetVectorInXYplane(double angleTN)
        {
            double rad = angleTN * DegToRad;

            Vector dir = new Vector();

            dir.X = xy_TN_def.X * (Math.Cos(rad)) - xy_TN_def.Y * (Math.Sin(rad));
            dir.Y = -(xy_TN_def.X * (Math.Sin(rad))) + xy_TN_def.Y * (Math.Cos(rad));

            return dir;
        }

        //calc angle from given vector

        //2D-True North(40), 2D-Rotation Plane(50)

        public double GetAngleBetweenForXYplane(Vector xy_TN)
        {
            double angle = Vector.AngleBetween(xy_TN_def, xy_TN);

            return angle;
        }

        //3D-X-Axis (30,40)

        public double GetAngleBetweenForXAxis(Vector3D xyz_xAxis)
        {
            double angle = Vector3D.AngleBetween(xyz_xAxis_def, xyz_xAxis);

            return angle;
        }

        //3D-Z-Axis (30,40)

        public double GetAngleBetweenForZAxis(Vector3D xyz_zAxis)
        {
            double angle = Vector3D.AngleBetween(xyz_zAxis_def, xyz_zAxis);

            return angle;
        }
    }
}