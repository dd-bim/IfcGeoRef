using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace IFCGeoRefChecker
{
    public class GeoRefChecker
    {
        public string Result { get; set; }  //String-object for output stream
        public bool GeoRef { get; set; } //auxiliary variable for decision in LoGeoref 30 and 40
        public bool Error { get; set; } //auxiliary variable for error notification

        //GeoRef 10: read all IfcPostalAddress-objects which are referenced by IfcSite or IfcBuilding
        //--------------------------------------------------------------------------------------------
        //CultureInfo culture = CultureInfo.InvariantCulture;

        //CurrentThread.CurrentCulture = culture;

        public void GetAddress(IfcStore model)
        {
            try
            {
                     

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                this.Result += "\r\n \r\nExisting addresses referenced by IfcSite or IfcBuilding" +
                "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

                //get all IfcPostalAddress-objects, referenced by IfcSite:
                var siteAddr = model.Instances.Where<IIfcSite>(a => a.SiteAddress != null).Select(a => a.SiteAddress);

                //loop for output of every address referenced by IfcSite:
                foreach(var address in siteAddr)
                {
                    this.Result += "\r\nFound address referenced by IfcSite:";

                    //function for address reading
                    WriteAdress(address);
                }

                //get all IfcPostalAddress-objects, referenced by IfcBuilding:
                var bldgAddr = model.Instances.Where<IIfcBuilding>(a => a.BuildingAddress != null).Select(a => a.BuildingAddress);

                //loop for output of every address referenced by IfcBuilding
                foreach(var address in bldgAddr)
                {
                    this.Result += "\r\nFound address referenced by IfcBuilding:";

                    //function for address reading
                    WriteAdress(address);
                }

                //statement for LoGeoRef_10 decision
                if(siteAddr.Count() == 0 && bldgAddr.Count() == 0)
                {
                    this.Result += "\r\nNo referenced addresses existent." +
                        "\r\n \r\nLoGeoRef 10 = false";

                    this.GeoRef = false;
                }
                else
                {
                    this.Result += "\r\n \r\nLoGeoRef 10 = true";

                    this.GeoRef = true;
                }

                this.Result += "\r\n________________________________________________________________________________________________________________________________________";

                //function for reading of all address elements
                void WriteAdress(IIfcPostalAddress address)
                {
                    string empty = "-";

                    string pcode = (address.PostalCode.HasValue == true) ? (string)address.PostalCode.Value : empty;
                    string town = (address.Town.HasValue == true) ? (string)address.Town.Value : empty;
                    string reg = (address.Region.HasValue == true) ? (string)address.Region.Value : empty;
                    string ctry = (address.Country.HasValue == true) ? (string)address.Country.Value : empty;

                    this.Result += $"\r\n #{address.GetHashCode()}= {address.GetType().Name}" +
                        $"\r\n  Address: {GetAdressLines(address)}" +
                        $"\r\n  Postal code: {pcode}" +
                        $"\r\n  Town: {town}" +
                        $"\r\n  Region: {reg}" +
                        $"\r\n  Country: {ctry}";
                }

                //String-function for reading of every single address line
                string GetAdressLines(IIfcPostalAddress address)
                {
                    string addressLines = "-";

                    for(int i = 0; i < address.AddressLines.Count; i++)
                    {
                        if(i == 0)
                        { addressLines = address.AddressLines[i]; }
                        else
                        { addressLines += ", " + address.AddressLines[i]; }
                    }
                    return addressLines;
                }

                this.Error = false;
            }
            catch(Exception e)
            {
                this.Result += "\r\n \r\nThe following exception occured while checking LoGeoRef_10: " +
                    $"\r\n Message: { e.Message}" +
                    $"\r\n StackTrace { e.StackTrace}";

                this.Error = true;
            }
        }

        //GeoRef 20: read Latitude, Longitude and Elevation from IfcSite-object
        //----------------------------------------------------------------------

        public void GetLatLon(IfcStore model)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                this.Result += "\r\n \r\nGeographic coordinates referenced by IfcSite (Latitude / Longitude / Elevation)" +
                "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

                //read alle IfcSite-objects where Latitude and Longitude is set:
                var ctSite = model.Instances.Where<IIfcSite>(lat => lat.RefLatitude != null && lat.RefLongitude != null);

                //loop for output of lat, lon and elevation of every site object
                foreach(var site in ctSite)
                {
                    this.Result += $"\r\n Referenced in #{site.GetHashCode()}= {site.GetType().Name}" +
                        $"\r\n  Latitude: {site.RefLatitude.Value.AsDouble}" +
                        $"\r\n  Longitude: {site.RefLongitude.Value.AsDouble}" +
                        $"\r\n  Elevation: {site.RefElevation.Value}";
                }

                //statement for LoGeoRef_20 decision
                if(ctSite.Count() == 0)
                {
                    this.Result += "\r\n No geographic coordinates existent." +
                        "\r\n \r\nLoGeoRef 20 = false";

                    this.GeoRef = false;
                }
                else
                {
                    this.Result += "\r\n \r\nLoGeoRef 20 = true";

                    this.GeoRef = true;
                }

                this.Result += "\r\n________________________________________________________________________________________________________________________________________";

                this.Error = false;
            }
            catch(Exception e)
            {
                this.Result += "\r\n \r\nThe following exception occured while checking LoGeoRef_20: " +
                    $"\r\n Message: { e.Message}" +
                    $"\r\n StackTrace { e.StackTrace}";

                this.Error = true;
            }
        }

        //GeoRef 30: read all Spatial Structure Elements with the "highest" Local Placement --> that means their placment is not relative to an other elements placement
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void GetSitePlcm(IfcStore model)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                this.Result += "\r\n \r\nLocal placement for the uppermost IfcSpatialStructureElement (usually an instance of IfcSite)" +
                        "\r\nThe placement of those elements is only relative to the WorldCoordinateSystem (see LoGeoRef 40) but not to other IFC-Elements" +
                        "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

                //default value: false, gets true if locations X,Y or Z value is greater than 0 (see function writeAxis2Plcm)
                this.GeoRef = false;

                //variable for all IfcProduct objects whose placement is not relative to an other elements placment
                var elem = model.Instances.Where<IIfcLocalPlacement>(e => e.PlacementRelTo == null)
                    .SelectMany(e => e.PlacesObject);

                this.Result += "\r\n The following elements fulfill this condition:" +
                    "\r\n  For purpose of information the output contains also IFC-Elements " +
                    "\r\n  which are not belonging to the SpatialStructure (if present in file)";

                //loop for output of every product which was found
                foreach(var e in elem)
                {
                    this.Result += $"\r\n \r\n  Element: #{e.GetHashCode()}= {e.GetType().Name}";

                    //statement for LoGeoref_30 decision and output of the location and rotation of only the founded SpatialStructureElements (Site, Building, BuildingStorey, Space)
                    if(e is IIfcSpatialStructureElement)
                    {
                        //variables for navigation through the placement entities/attributes
                        var elemPlcm = (IIfcLocalPlacement)e.ObjectPlacement;
                        var elAxis2Plcm = (IIfcAxis2Placement3D)elemPlcm.RelativePlacement;

                        //function for reading of location and rotation
                        writeAxis2Plcm(elAxis2Plcm);
                    }
                    else
                    {
                        this.Result += "\r\n  This element is not a kind of IfcSpatialStructureElement.";
                    }
                }

                //statement for GeoRef_30 decision
                if(this.GeoRef == true)
                {
                    this.Result += "\r\n \r\nLoGeoRef 30 = true";

                    this.GeoRef = true;
                }
                else
                {
                    this.Result += "\r\n \r\nLoGeoRef 30 = false";

                    this.GeoRef = false;
                }

                this.Result += "\r\n________________________________________________________________________________________________________________________________________";

                this.Error = false;
            }
            catch(Exception e)
            {
                this.Result += "\r\n \r\nThe following exception occured while checking LoGeoRef_30: " +
                    $"\r\n Message: { e.Message}" +
                    $"\r\n StackTrace { e.StackTrace}";

                this.Error = true;
            }
        }

        //GeoRef 40: read the WorldCoordinateSystem and TrueNorth attribute of IfcGeometricRepresentationContext
        //-------------------------------------------------------------------------------------------------------

        public void GetWorldCoordinateSystem(IfcStore model)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                this.Result += "\r\n \r\nProject context attributes for georeferencing (Location: WorldCoordinateSystem / Rotation: TrueNorth)" +
                "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

                //by default
                this.GeoRef = false;

                //gets the IfcGeometricRepresentationContext of the 3D-geometry
                var prjCtx = getRepresentationContext(model);

                this.Result += $"\r\n \r\n Project Context element: #{prjCtx.GetHashCode()}= {prjCtx.GetType().Name}";

                this.Result += $"\r\n \r\n WorldCoordinateSystem:";

                //variable for the WorldCoordinatesystem attribute
                var plcCtx = (IIfcAxis2Placement3D)prjCtx.WorldCoordinateSystem;

                //function for reading of location and rotation
                writeAxis2Plcm(plcCtx);

                //variable for the TrueNorth attribute
                var dir = prjCtx.TrueNorth;

                this.Result += $"\r\n \r\n TrueNorth:" +
                        $"\r\n  If present there is a rotation of the XY-plane towards the True North direction mentioned." +
                        $"\r\n  (Caution: IFC-schema does not define an attribute for Grid North.)";

                if(dir != null)
                {
                    this.Result +=
                        $"\r\n \r\n  Rotation towards True North: #{dir.GetHashCode()} = {dir.GetType().Name}" +
                            $"\r\n   X-component = {dir.DirectionRatios[0]}" +
                            $"\r\n   Y-component = {dir.DirectionRatios[1]}";
                }
                else
                {
                    this.Result += "\r\n \r\n  There is no value for TrueNorth mentioned.";
                }

                //statement for GeoRef_40 decision
                if(this.GeoRef == true)
                {
                    this.Result += "\r\n \r\nLoGeoRef 40 = true";

                    this.GeoRef = true;
                }
                else
                {
                    this.Result += "\r\n \r\nLoGeoRef 40 = false";

                    this.GeoRef = false;
                }

                this.Result += "\r\n________________________________________________________________________________________________________________________________________";

                this.Error = false;
            }
            catch(Exception e)
            {
                this.Result += "\r\n \r\nThe following exception occured while checking LoGeoRef_40: " +
                    $"\r\n Message: { e.Message}" +
                    $"\r\n StackTrace { e.StackTrace}";

                this.Error = true;
            }
        }

        //GeoRef 50: read MapConversion, if referenced by IfcGeometricRepresentationContext (only in scope of IFC4 schema)
        //-----------------------------------------------------------------------------------------------------------------

        public void GetMapConversion(IfcStore model)

        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                this.Result += "\r\n \r\nSpecific entities for georeferencing (only in scope of IFC4; IfcMapConversion references IfcGeometricRepresenationContext)" +
                "\r\n----------------------------------------------------------------------------------------------------------------------------------------";

                //gets the IfcGeometricRepresentationContext of the 3D-geometry
                var prjCtx = getRepresentationContext(model);

                //restriction on IfcMapConversion objects which references (or inverse referenced by) IfcGeometricRepresentationContext
                if(prjCtx.HasCoordinateOperation.Count() != 0)
                {
                    this.Result += "\r\n There is a conversion of the world coordinate system (WCS) in a coordinate reference system (CRS) applicable:";
                    this.Result += $"\r\n \r\n Project Context element which is referenced by IfcMapConversion: #{prjCtx.GetHashCode()}= {prjCtx.GetType().Name}";

                    //restriction on IfcMapConversion with SourceCRS = project context and TargetCRS = CRS
                    //theoretically it is also possible to describe an conversion between two distinct CRS (not in scope of this application)
                    var mapCvs = model.Instances
                            .Where<IIfcMapConversion>(map => map.SourceCRS is IIfcGeometricRepresentationContext && map.TargetCRS is IIfcCoordinateReferenceSystem).First();

                    this.Result +=
                                $"\r\n \r\n Conversion parameteres (WCS to CRS): #{mapCvs.GetHashCode()}= {mapCvs.GetType().Name}" +
                                    $"\r\n  Translation:" +
                                        $"\r\n  Eastings: {mapCvs.Eastings}" +
                                        $"\r\n  Northings: {mapCvs.Northings}" +
                                        $"\r\n  Orthogonal height: {mapCvs.OrthogonalHeight}" +
                                    $"\r\n Rotation of the XY-plane:" +
                                        $"\r\n  Abscissa of the X-axis (vector component): {mapCvs.XAxisAbscissa}" +
                                        $"\r\n  Ordinate of the X-axis (vector component): {mapCvs.XAxisOrdinate}" +
                                    $"\r\n Scale: {mapCvs.Scale}"
                                ;

                    this.Result += "\r\n \r\n Definition of the CRS:";

                    var mapCRS = (IIfcProjectedCRS)mapCvs.TargetCRS;

                    this.Result +=
                            $"\r\n Target system (CRS): #{mapCRS.GetHashCode()}= {mapCRS.GetType().Name}" +
                                $"\r\n  Name: {mapCRS.Name}" +
                                $"\r\n  Description: {mapCRS.Description}" +
                                $"\r\n  Geodetic Datum: { mapCRS.GeodeticDatum}" +
                                $"\r\n  Vertical Datum: { mapCRS.VerticalDatum}" +
                            $"\r\n Projection: \n" +
                                $"\r\n  Name: {mapCRS.MapProjection}" +
                                $"\r\n  Zone: {mapCRS.MapZone}" +
                                $"\r\n  Unit:{mapCRS.MapUnit.Symbol} ({mapCRS.MapUnit.FullName})"
                            ;

                    this.Result += "\r\n \r\nLoGeoRef 50 = true";

                    this.GeoRef = true;
                }
                else
                {
                    this.Result += "\r\n No conversion of the world coordinate system (WCS) in a coordinate reference system (CRS) applicable." +
                        "\r\n \r\nLoGeoRef 50 = false";

                    this.GeoRef = false;
                }

                this.Result += "\r\n________________________________________________________________________________________________________________________________________";

                this.Error = false;
            }
            catch(Exception e)
            {
                this.Result += "\r\n \r\nThe following exception occured while checking LoGeoRef_50: " +
                    $"\r\n Message: { e.Message}" +
                    $"\r\n StackTrace { e.StackTrace}";

                this.Error = true;
            }
        }

        //functions, which are necessary for more than one GeoRef-level
        //--------------------------------------------------------------

        //function for reading of location and rotation (needed for LoGeoRef 30 and 40)
        private void writeAxis2Plcm(IIfcAxis2Placement3D plcm)
        {
            this.Result += $"\r\n  Placement referenced in #{plcm.GetHashCode()}= {plcm.GetType().Name}" +
                $"\r\n   X = {plcm.Location.X}" +
                $"\r\n   Y = {plcm.Location.Y}" +
                $"\r\n   Z = {plcm.Location.Z}";

            if((plcm.Location.X > 0) || (plcm.Location.Y > 0) || (plcm.Location.Z > 0))
            {
                //by definition: ONLY in this case there could be an georeferencing
                this.GeoRef = true;
            }

            //statement for occurence of direction attributes (rotation of X-axis)
            if(plcm.RefDirection != null)
            {
                this.Result +=
                    $"\r\n  Rotation X-axis = ({plcm.RefDirection.DirectionRatios[0]}/{plcm.RefDirection.DirectionRatios[1]}/{plcm.RefDirection.DirectionRatios[2]})";
            }
            else
            {
                this.Result += "\r\n  No rotation of the X-axis defined.";
            }

            //statement for occurence of direction attributes (rotation of Z-axis)
            if(plcm.Axis != null)
            {
                this.Result +=
                $"\r\n  Rotation Z-axis = ({plcm.Axis.DirectionRatios[0]}/{plcm.Axis.DirectionRatios[1]}/{plcm.Axis.DirectionRatios[2]})";
            }
            else
            {
                this.Result += "\r\n  No rotation of the Z-axis defined.";
            }
        }

        //function for reading of the IfcGeometricRepresentationContext of the project
        private IIfcGeometricRepresentationContext getRepresentationContext(IfcStore model)
        {
            //read only the context where the type is a model (not plan) and the dimension is 3D (not 2D) --> context for the 3D model/geometry of the project
            var repCtx = model.Instances
            .Where<IIfcGeometricRepresentationContext>(ctx => ctx.ContextType == "Model" && ctx.CoordinateSpaceDimension == 3).First();

            return repCtx;
        }
    }
}