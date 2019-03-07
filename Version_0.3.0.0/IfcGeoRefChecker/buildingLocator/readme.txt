Please consider the following steps for IfcGeoRefChecker_v3 alpha version:

Updating via map does not yet work completely:

Please consider the following steps:

- after click on "..via browser map":
-- a new browser window should pop up displaying a map on the left and a menu on the right. If this is not the case, you can manually start the web app by opening the index.html
   in the folder XXXYYY

-- the web app is able to load the building perimeter as a WKT-String. The perimeter is extracted on start up of the the georefchecker and is saved in the resulting 
   JSON-file in the attribute WKTRep. If this attribute contains: "error" there is a error in calculating the building perimeter --> please contact us and send the IFC-file (if possible)
	
-- The georef update procedure starts with loading the JSON-File in the web app. To do so open the Select File tab and choose the appropriate JSON-File

-- Under Step 2. Show Existing Georef Information the current information is displayed for the different levels. In the current Version of the building locator only Level50 is displayed.

-- For a successfull workflow the user has to select an appropriate map projection. The app currently supports 4 CRS:
	- UTM Zone 33N
	- UTM Zone 32N
	- WGS84 LatLon
	- Web-Mercator
   For getting the Georef Info into level 50 you should not use WGS84 LatLon projection!

-- After having defined the correct map projection the user can draw the building perimter using tab 4. position building.
	- If the JSON-file contains a valid WKT-string in "WKTRep" but the visualisation is obviously wrong --> please contact us and send IFC-file (if possible)

-- The building parameter appears in the map window and can be moved and rotated to the correct place

-- As a fith step it is possible to query the building address. This feature uses Nominatim (https://wiki.openstreetmap.org/wiki/Nominatim) for retrieving the address.

-- If the building is finally placed the user must save the settings via tab 6. 

-- to save your updates you should save the updates vie button click and export them to the same directory as mentioned above:
	-- "..\IfcGeoRefChecker\buildingLocator\json\" with the name "update.json"

-- you can now continue in the windows application vie step 2 "Export updates to IFC". It will load the new information out of the "update.json" file
