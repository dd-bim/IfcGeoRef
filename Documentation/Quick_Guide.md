# ![Icon LoGeoRef](pic/icon_img.png) IFCGeoRefChecker

# Quick Guide

[TOC]

## The -IFCGeoRefChecker- tool (Version: 0.3.1.0)

![GUI after checking an IFC-file](pic/GeoRefChecker_GUI_2_v3.png)

### GeoRefChecker functionality

1. **"Set working directory" section**
-- optional, but recommended via Change... button
-- some export files will be stored there
-- you need permission to write in there

2. **"Check IFC-file(s)..." button**
-- main functionality
-- automatically export of Log- and/or json-file to working directory possible (if Checkboxes are checked)
-- check one or more IFC-files per import regarding LoGeoRef concept (see Documentation)
-- imported and checked models will be displayed in Listbox

3. **"Check overview" section**
-- short results of check against Level of Georeferencing
-- switch between results via selecting models in Listbox above
-- (if checked before) buttons for displaying of Log- and/or JSON-file

4. **"--> GeoRefComparer" button**
-- comparing of georeferencing attribute values of two or more IFC files
-- see [GeoRefComparer](#Comparing georef)

5. **Update GeoRef" section**
-- possibility to update/change the georeferencing of your IFC file
-- first step: update georef attributes manually OR via map (external web app)
-- second step: Export Updates to IFC file
-- see [GeoRefUpdater](#Updating georef)

<a name="Comparing georef"></a>
### Comparing georef

![GeoRefComparer](pic/GeoRefComparer_GUI1.png)

1. **"Reference model" section**
-- choose the IFC file which should be the reference for the comparison
-- normally that is the coordination model (with correct georef data)

2. **"Comparison models" section**
-- select the models which should be compared with the reference model
-- please note: they will be not compared with each other
-- normally that are the models of participating disciplines

3. **"Start Comparison" button**
-- start of comparison
-- internally export of compare log file to working directory
-- quick view on compare file via click on "See compare file" button

<a name="Updating georef"></a>
### GeoRefUpdate functionality

#### First option: edit "...via manual setup"

![GeoRefUpdater](pic/GeoRefUpdater_GUI1_v3.png)

Possibility to edit manually address, Geolocation (Geographic site coordinates) and Geodetic transformation.
Via "Save and Close" the new attribute values will be stored as json in a user defined directory.
The json file will be needed for further IFC export.

#### Second option: edit "...via browser map"

1. **Calculating building perimeter**
-- for later display at the map the application will internally calculate the building perimeter out of the wall geometry in IFC file
-- because of the wide variety of storing geometry in IFC this is prone to error
-- please contact us if calculation will fail or will be incorrect

2. **Saving of json with building perimeter**
-- the application requests to save the json file with the calculated building perimeter
-- please save the file in the working directory or any other known folder

3. **"Building Locator" will be opened**
-- start of the Building Locator via opening your local setted browser
-- on the left side a map is displayed
-- if not check your Internet connection and/or try another browser
-![Building Locator](pic/GeoRefChecker_Locator.png)

5. **Updating georef via "Building Locator"**
- **Select File:** 
-- import the json file with the calculated building perimeter (WKTRep-attribute in json file)

- **Show Existing Georef Information**
-- display of the georef content in json file

- **Select Map Projection**
-- choose one of the offered projections

- **Position Building**
-- displaying of the WKT-string representing the building perimeter
-- via **Draw Building** display of the building and its project base point in the map
-- the positioning is orientated to the Level50 attributes or if they are not given, ob the LatLon coordinate of Level20
-- user defined positioning via **move** and **rotate** functionality

- **Query Buildng Address**
-- get the address of the choosen location
-- source: [Nominatim](https://nominatim.org/release-docs/develop/api/Overview/)

- **Save Position and Download File**
-- updating of the georef json file
-- Download to working directory or user-defined folder

#### Export changes to IFC

![GeoRefUpdater](pic/GeoRefUpdater_GUI3_v3.png)

1. **Export Updates to IFC via IfcGeoRefChecker**
-- import of updated json file from file system
-- choosing of the required export options (dependent to your BIM-software)
-- possibility to edit manually IFC attributes, see [IFC export for experts](#Extended export)

<a name="Extended export"></a>

#### Extended export to IFC (only for IFC experts recommended)

![GeoRefUpdater](pic/GeoRefUpdater_GUI4_v3.png)

- Updating georef via Level of Georeferencing concept
- displaying of attribute values of certain levels and their corresponding IFC entities

##Built with

- [xBIM Toolkit](http://docs.xbim.net/) - Main functionality used to read IFC-files
- [Json.NET](https://www.newtonsoft.com/json) - Functionality for exporting JSON-files
- [Pixabay](https://pixabay.com/) - Graphics used to design LoGeoRef-Icons

##Contributors

The concept together with the tool was developed within the scope of the following sponsorship projects:

| 3D-Punktwolke - CityBIM  | Digitalisierung des Bauwesens - DD BIM |
|--------|--------|
|   <img src="pic/BMWi_4C_Gef_en.jpg" align=center style="width: 200px;"/>    |   Supported by: <br> <img src="pic/DD-BIM-LOGO.png" style="width: 250px;"/> <br> Landeshauptstadt Dresden <br>Amt für Wirtschaftsförderung |

##Contact

 <img src="pic/logo_htwdd.jpg" align=center style="width: 300px;"/>  

**HTW Dresden**
**Fakultät Geoinformation**
Friedrich-List-Platz 1
01069 Dresden

Project head:

- Prof. Dr.-Ing. Christian Clemen (<christian.clemen@htw-dresden.de>)

Project staff:

- Hendrik Görne, M.Eng. (<hendrik.goerne@htw-dresden.de>)
- Tim Kaiser, M.Eng.
- Enrico Romanschek, M.Eng.

##License

This project is licensed under the MIT License:

```
Copyright (c) 2019 HTW Dresden

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

```










