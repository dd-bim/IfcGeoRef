using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Serilog;
using Xbim.Ifc;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.RepresentationResource;

namespace IfcGeoRefChecker.IO
{
    internal class IfcWriter
    {
        /// <summary>
        /// Writes updated IFC-file (old file will be opened again)
        /// </summary>
        public IfcWriter(string ifcPath, string fileName, string jsonObj)
        {
            try
            {
                var json = new Appl.GeoRefChecker(jsonObj);

                var editor = new XbimEditorCredentials
                {
                    ApplicationDevelopersName = "HTW Dresden",
                    ApplicationFullName = "IfcGeoRefChecker",
                    ApplicationIdentifier = "IfcGeoRef",
                    ApplicationVersion = "0.3.0.0",
                    EditorsFamilyName = Environment.UserName,
                };

                var fileIFC = ifcPath + ".ifc";

                using(var model = IfcStore.Open(fileIFC, editor))
                {
                    using(var txn = model.BeginTransaction(model.FileName + "_transedit"))
                    {
                        //Level 10

                        try
                        {
                            foreach(var lev10 in json.LoGeoRef10)
                            {
                                var refObj = GetRefObj(model, lev10.Reference_Object[2]);
                                var creation = GetCreationDate(refObj);

                                IIfcPostalAddress p;

                                if(json.IFCSchema == "Ifc2X3")
                                {
                                    p = model.Instances.New<Xbim.Ifc2x3.ActorResource.IfcPostalAddress>();
                                }
                                else
                                {
                                    p = model.Instances.New<Xbim.Ifc4.ActorResource.IfcPostalAddress>();
                                }

                                foreach(var addLine in lev10.AddressLines)
                                {
                                    p.AddressLines.Add(addLine);
                                }

                                p.PostalCode = lev10.Postalcode;
                                p.Town = lev10.Town;
                                p.Region = lev10.Region;
                                p.Country = lev10.Country;

                                if(refObj is IIfcSite)
                                {
                                    (refObj as IIfcSite).SiteAddress = p;
                                }
                                else
                                {
                                    (refObj as IIfcBuilding).BuildingAddress = p;
                                }

                                refObj = UpdateOwnerHistory(refObj, creation);
                            }

                            Log.Information("IFC Writer: Level 10 updated.");
                        }
                        catch
                        {
                            Log.Error("IFC Writer: Error while updating Level 10 occured.");
                        }

                        try
                        {
                            foreach(var lev20 in json.LoGeoRef20)
                            {
                                var refObj = GetRefObj(model, lev20.Reference_Object[2]);
                                var creation = GetCreationDate(refObj);

                                var dmsLat = new Appl.Calc().DDtoCompound((double)lev20.Latitude);
                                var dmsLon = new Appl.Calc().DDtoCompound((double)lev20.Longitude);

                                var listLat = new List<long>
                            {
                                { Convert.ToInt64(dmsLat[0]) },
                                { Convert.ToInt64(dmsLat[1]) },
                                { Convert.ToInt64(dmsLat[2]) },
                                { Convert.ToInt64(dmsLat[3]) }
                            };

                                var listLon = new List<long>
                            {
                                { Convert.ToInt64(dmsLon[0]) },
                                { Convert.ToInt64(dmsLon[1]) },
                                { Convert.ToInt64(dmsLon[2]) },
                                { Convert.ToInt64(dmsLon[3]) }
                            };

                                (refObj as IIfcSite).RefLatitude = new IfcCompoundPlaneAngleMeasure(listLat);
                                (refObj as IIfcSite).RefLongitude = new IfcCompoundPlaneAngleMeasure(listLon);

                                (refObj as IIfcSite).RefElevation = lev20.Elevation;

                                refObj = UpdateOwnerHistory(refObj, creation);
                            }
                            Log.Information("IFC Writer: Level 20 updated.");
                        }
                        catch
                        {
                            Log.Error("IFC Writer: Error while updating Level 20 occured.");
                        }

                        try
                        {
                            foreach(var lev30 in json.LoGeoRef30)
                            {
                                var refObj = GetRefObj(model, lev30.Reference_Object[2]);
                                var creation = GetCreationDate(refObj);

                                var elemPlcm = (IIfcLocalPlacement)(refObj as IIfcProduct).ObjectPlacement;
                                var plcm3D = (IIfcAxis2Placement3D)elemPlcm.RelativePlacement;

                                if(json.IFCSchema == "Ifc2X3")
                                {
                                    plcm3D.Location = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint>(p => p.SetXYZ(lev30.ObjectLocationXYZ[0], lev30.ObjectLocationXYZ[1], lev30.ObjectLocationXYZ[2]));
                                    plcm3D.RefDirection = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationX[0], lev30.ObjectRotationX[1], lev30.ObjectRotationX[2]));
                                    plcm3D.Axis = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationZ[0], lev30.ObjectRotationZ[1], lev30.ObjectRotationZ[2]));
                                }
                                else
                                {
                                    plcm3D.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(lev30.ObjectLocationXYZ[0], lev30.ObjectLocationXYZ[1], lev30.ObjectLocationXYZ[2]));
                                    plcm3D.RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationX[0], lev30.ObjectRotationX[1], lev30.ObjectRotationX[2]));
                                    plcm3D.Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev30.ObjectRotationZ[0], lev30.ObjectRotationZ[1], lev30.ObjectRotationZ[2]));
                                }

                                refObj = UpdateOwnerHistory(refObj, creation);
                            }

                            Log.Information("IFC Writer: Level 30 updated.");
                        }
                        catch
                        {
                            Log.Error("IFC Writer: Error while updating Level 30 occured.");
                        }

                        try
                        {
                            foreach(var lev40 in json.LoGeoRef40)
                            {
                                var project = GetRefObj(model, lev40.Reference_Object[2]);
                                var creation = GetCreationDate(project);

                                var contexts = (project as IIfcProject).RepresentationContexts;

                                foreach(var context in contexts)
                                {
                                    var refObj = context as IIfcGeometricRepresentationContext;

                                    var plcm = refObj.WorldCoordinateSystem;

                                    if(json.IFCSchema == "Ifc2X3")
                                    {
                                        refObj.TrueNorth = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXY(lev40.TrueNorthXY[0], lev40.TrueNorthXY[1]));

                                        if(plcm is IIfcAxis2Placement3D)
                                        {
                                            (plcm as IIfcAxis2Placement3D).Location = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint>(p => p.SetXYZ(lev40.ProjectLocation[0], lev40.ProjectLocation[1], lev40.ProjectLocation[2]));
                                            (plcm as IIfcAxis2Placement3D).RefDirection = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1], lev40.ProjectRotationX[2]));
                                            (plcm as IIfcAxis2Placement3D).Axis = model.Instances.New<Xbim.Ifc2x3.GeometryResource.IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationZ[0], lev40.ProjectRotationZ[1], lev40.ProjectRotationZ[2]));
                                        }
                                        else
                                        {
                                            (plcm as IIfcAxis2Placement2D).Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(lev40.ProjectLocation[0], lev40.ProjectLocation[1]));
                                            (plcm as IIfcAxis2Placement2D).RefDirection = model.Instances.New<IfcDirection>(d => d.SetXY(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1]));
                                        }
                                    }
                                    else //IFC4 or higher
                                    {
                                        refObj.TrueNorth = model.Instances.New<IfcDirection>(d => d.SetXY(lev40.TrueNorthXY[0], lev40.TrueNorthXY[1]));

                                        if(plcm is IIfcAxis2Placement3D)
                                        {
                                            (plcm as IIfcAxis2Placement3D).Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(lev40.ProjectLocation[0], lev40.ProjectLocation[1], lev40.ProjectLocation[2]));
                                            (plcm as IIfcAxis2Placement3D).RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1], lev40.ProjectRotationX[2]));
                                            (plcm as IIfcAxis2Placement3D).Axis = model.Instances.New<IfcDirection>(d => d.SetXYZ(lev40.ProjectRotationZ[0], lev40.ProjectRotationZ[1], lev40.ProjectRotationZ[2]));
                                        }
                                        else
                                        {
                                            (plcm as IIfcAxis2Placement2D).Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(lev40.ProjectLocation[0], lev40.ProjectLocation[1]));
                                            (plcm as IIfcAxis2Placement2D).RefDirection = model.Instances.New<IfcDirection>(d => d.SetXY(lev40.ProjectRotationX[0], lev40.ProjectRotationX[1]));
                                        }
                                    }
                                }
                                //(refObj as IIfcGeometricRepresentationContext).WorldCoordinateSystem = plcm;

                                var proj = (IIfcObjectDefinition)project;

                                proj = UpdateOwnerHistory((project as IIfcObjectDefinition), creation);
                            }

                            Log.Information("IFC Writer: Level 40 updated.");
                        }
                        catch
                        {
                            Log.Error("IFC Writer: Error while updating Level 40 occured.");
                        }

                        try
                        {
                            foreach(var lev50 in json.LoGeoRef50)
                            {
                                //var refObj = GetRefCtx(model, lev50.Instance_Object[0]);

                                var project = model.Instances.OfType<IIfcProject>().SingleOrDefault();

                                var refCtx = project.RepresentationContexts.Where(c => c is IIfcGeometricRepresentationContext).Where(ct => ct.ContextType == "Model").SingleOrDefault();

                                var creation = GetCreationDate(project);

                                if(json.IFCSchema == "Ifc2X3")
                                {
                                    var pSetMapCRS = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcRelDefinesByProperties>(r =>
                                    {
                                        r.RelatingPropertyDefinition = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcPropertySet>(pSet =>
                                        {
                                            pSet.Name = "ePset_ProjectedCRS";
                                            pSet.Description = "Definition of the coordinate reference system";                                                                   //all collections are always initialized

                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "Name";
                                                p.Description = "Name by which the coordinate reference system is defined, shall be EPSG-Code";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcLabel(lev50.CRS_Name);
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "Description";
                                                p.Description = "Informal description";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcText(lev50.CRS_Description);
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "GeodeticDatum";
                                                p.Description = "Name by which this datum is identified.";

                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(lev50.CRS_Geodetic_Datum);
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "VerticalDatum";
                                                p.Description = "Name by which the vertical datum is identified.";

                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(lev50.CRS_Vertical_Datum);
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "MapProjection";
                                                p.Description = "Name by which the map projection is identified";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(lev50.CRS_Projection_Name);
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "MapZone";
                                                p.Description = "Name by which the map zone is identified";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcIdentifier(lev50.CRS_Projection_Zone);
                                            }));
                                        });
                                    });

                                    pSetMapCRS.RelatedObjects.Add((Xbim.Ifc2x3.Kernel.IfcObject)project);

                                    var unit = model.Instances.New<Xbim.Ifc2x3.MeasureResource.IfcSIUnit>();

                                    unit.UnitType = Xbim.Ifc2x3.MeasureResource.IfcUnitEnum.LENGTHUNIT;
                                    unit.Name = Xbim.Ifc2x3.MeasureResource.IfcSIUnitName.METRE;

                                    var pSetMapConv = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcRelDefinesByProperties>(r =>
                                    {
                                        //r.GlobalId = Guid.NewGuid();
                                        r.RelatingPropertyDefinition = model.Instances.New<Xbim.Ifc2x3.Kernel.IfcPropertySet>(pSet =>
                                        {
                                            pSet.Name = "ePset_MapConversion";
                                            pSet.Description = "Specification of the transformation between the local grid coordinate system and a map coordinate system";                                                                   //all collections are always initialized

                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "Eastings";
                                                p.Description = "The translation in X between the two coordinate systems";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcLengthMeasure((double)lev50.Translation_Eastings);
                                                p.Unit = unit;
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "Northings";
                                                p.Description = "The translation in Y between the two coordinate systems";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcLengthMeasure((double)lev50.Translation_Northings);
                                                p.Unit = unit;
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "OrthogonalHeight";
                                                p.Description = "The translation in Z between the two coordinate systems";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcLengthMeasure((double)lev50.Translation_Orth_Height);
                                                p.Unit = unit;
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "XAxisAbscissa";
                                                p.Description = "The X component of the rotation between the two coordinate systems";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcReal(lev50.RotationXY[0]);
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "XAxisOrdinate";
                                                p.Description = "The Y component of the rotation between the two coordinate systems";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcReal(lev50.RotationXY[1]);
                                            }));
                                            pSet.HasProperties.Add(model.Instances.New<Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue>(p =>
                                            {
                                                p.Name = "Scale";
                                                p.Description = "The scale in X, Y between the two coordinate systems";
                                                p.NominalValue = new Xbim.Ifc2x3.MeasureResource.IfcReal(lev50.Scale);
                                            }));
                                        });
                                    });

                                    pSetMapConv.RelatedObjects.Add((Xbim.Ifc2x3.Kernel.IfcObject)project);
                                }
                                else //IFC4 --> keine PropertySets
                                {
                                    //Verzweigung
                                    IIfcMapConversion mapConv;
                                    IIfcProjectedCRS mapCRS;

                                    if(lev50.Instance_Object.Count == 0)       //keine MapConversion bisher vorhanden
                                    {
                                        mapConv = model.Instances.New<IfcMapConversion>(m =>
                                        {
                                            m.SourceCRS = (refCtx as IIfcGeometricRepresentationContext);
                                            m.TargetCRS = model.Instances.New<IfcProjectedCRS>();
                                        });

                                        mapCRS = (IfcProjectedCRS)mapConv.TargetCRS;
                                    }
                                    else                                    //MapConversion vorhanden
                                    {
                                        mapConv = GetRefMap(model, lev50.Instance_Object[0]);

                                        mapCRS = (IfcProjectedCRS)mapConv.TargetCRS;

                                        if(mapCRS == null)
                                        {
                                            mapConv.TargetCRS = model.Instances.New<IfcProjectedCRS>();
                                        }
                                        mapCRS = (IfcProjectedCRS)mapConv.TargetCRS;
                                    }
                                    //var mapUnit = mapCRS.MapUnit;

                                    mapConv.Eastings = (double)lev50.Translation_Eastings;
                                    mapConv.Northings = (double)lev50.Translation_Northings;
                                    mapConv.OrthogonalHeight = (double)lev50.Translation_Orth_Height;
                                    mapConv.XAxisAbscissa = lev50.RotationXY[0];
                                    mapConv.XAxisOrdinate = lev50.RotationXY[1];
                                    mapConv.Scale = lev50.Scale;

                                    mapCRS.Name = lev50.CRS_Name;
                                    mapCRS.Description = lev50.CRS_Description;
                                    mapCRS.GeodeticDatum = lev50.CRS_Geodetic_Datum;
                                    mapCRS.VerticalDatum = lev50.CRS_Vertical_Datum;
                                    mapCRS.MapProjection = lev50.CRS_Projection_Name;
                                    mapCRS.MapZone = lev50.CRS_Projection_Zone;

                                    var unit = model.Instances.New<IfcSIUnit>(u =>
                                    {
                                        u.UnitType = IfcUnitEnum.LENGTHUNIT;
                                        u.Name = IfcSIUnitName.METRE;
                                    });

                                    mapCRS.MapUnit = unit;
                                }

                                var proj = (IIfcObjectDefinition)project;

                                proj = UpdateOwnerHistory((project as IIfcObjectDefinition), creation);
                            }
                            Log.Information("IFC Writer: Level 50 updated.");
                        }
                        catch
                        {
                            Log.Error("IFC Writer: Error while updating Level 50 occured.");
                        }

                        txn.Commit();
                    }

                    var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog();

                    saveFileDialog1.InitialDirectory = ifcPath;        //Pfad, der zunächst angeboten wird
                    saveFileDialog1.DefaultExt = "ifc";
                    saveFileDialog1.Filter = "IFC-files (*.ifc)|*.ifc";
                    saveFileDialog1.FilterIndex = 1;
                    saveFileDialog1.Title = "Save updated IFC-file";
                    saveFileDialog1.RestoreDirectory = true;
                    saveFileDialog1.FileName = fileName + "_edit.ifc";
                    saveFileDialog1.ShowDialog();

                    var text = saveFileDialog1.FileName;

                    model.SaveAs(text);

                    var str = "IFC Writer: Updating IFC-file was successful.";
                    Log.Information(str);
                    MessageBox.Show(str);
                }
            }
            catch(Exception e)
            {
                var str = "IFC Writer: Error occured while writing updated IFC-file. Error:" + e.Message;

                MessageBox.Show(str);
                //Log.Error(str);
            }
        }

        /// <summary>
        /// Find correct IfcObject in IfcStore
        /// </summary>
        private IIfcObjectDefinition GetRefObj(IfcStore model, string refId)
        {
            var refObj = model.Instances.OfType<IIfcObjectDefinition>().Where(o => (o as IIfcObjectDefinition).GlobalId.Value.ToString().Equals(refId)).SingleOrDefault();

            return refObj;
        }

        /// <summary>
        /// Find correct conversion in IfcStore
        /// </summary>
        private IIfcMapConversion GetRefMap(IfcStore model, string refNo)
        {
            var refMap = (IIfcMapConversion)model.Instances.Where(o => ("#" + o.GetHashCode()).Equals(refNo)).Single();

            return refMap;
        }

        /// <summary>
        /// Create Timestamp for OwnerHistory (if necessary) or keep old one
        /// </summary>
        private Xbim.Ifc4.DateTimeResource.IfcTimeStamp GetCreationDate(IIfcObjectDefinition refObj)
        {
            Xbim.Ifc4.DateTimeResource.IfcTimeStamp creation;

            if(refObj.OwnerHistory == null || refObj.OwnerHistory.CreationDate == null)
            {
                creation = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            }
            else
                creation = refObj.OwnerHistory.CreationDate;

            return creation;
        }

        /// <summary>
        /// Update OwnerHistory for instantiated IfcObjects (Timestamp, ChangeAction)
        /// </summary>
        private IIfcObjectDefinition UpdateOwnerHistory(IIfcObjectDefinition refObj, Xbim.Ifc4.DateTimeResource.IfcTimeStamp creation)
        {
            refObj.OwnerHistory.CreationDate = creation;
            long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            refObj.OwnerHistory.LastModifiedDate = new Xbim.Ifc4.DateTimeResource.IfcTimeStamp(timestamp);
            refObj.OwnerHistory.ChangeAction = IfcChangeActionEnum.MODIFIED;

            return refObj;
        }
    }
}