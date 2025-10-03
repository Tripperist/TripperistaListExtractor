We are going to create a .NET10 C# Console Program.
A sample command line may look like this
TripperistaListExtractor --inputSavedListUrl "https://maps.app.goo.gl/PwxB6Wrc7a7R5g889" --outputKmlFile "myOutputFilename.kml" --outputCsvFile "myOutputFilename.csv" --googlePlacesApiKey "Jfdisfjaiejf;adkuefnhfa" --verbose

The command line parameters
--inputSavedListUrl <URL> is required and is the URL of the Google Maps User's Shared List
--outputKmlFile "optionalFilename.kml" specifies the name of the output file to create. If --outputKmlFile does not specify a filename, then the Name of the List will be used for the name with an extension of .kml
--outputCsvFile "optionalFilename.csv" specifies the name of the output file to create. If --outputCsvFile does not specify a filename, then the Name of the List will be used for the name with an extension of .csv
--googlePlacesApiKey "google_places_api_key" is a Key credential used for accessing the Google Places API. If no command line arg is specified, the application will look for an Environment Variable or Configuration entry in apps.json
--verbose if this argument is specified detailed debug and information logging will be used to output information to the console. Otherwise only errors and warnings will output to the console.

The application will need to use the Microsoft.Playwright to scrape the information from the URL.
It should ensure that the page is navigated to and loaded.

The general idea for the program is as follows
* Using Microsoft.Playwright navigate to the Google Maps User's Saved List specified in the --inputSavedListUrl
* Ensure that the list is fully loaded. There will be a <div role="main"> section that contains the ListName, ListDescription, ListCreator, and a List of Places. 


The full list is encapsulated by a div similar to the following
<div class="m6QErb DxyBCb kA9KIf dS8AEf XiKgde ussYcc " tabindex="-1" style="padding-top: 4px;"> HM1g1c Jw7tL "></div>

Each Place is encapsulated in similar to the following
<div class="m6QErb XiKgde " style=""><div class="BsJqK xgHk6 BxEA8d "></div>

It may be necessary to page or scroll down using Playwright to load the entire list of Places.

Once the entire list of places has been loaded, you will look for a <script nonce> in the <head> section. This is usually the second script .Nth(1) in the header section.

Grab the Text of the function and search for the following sequence of characters 
")]}'\n

The beginning of our js payload starts with the next character in the string and continues until it reaches the following set of characters
\u003d13\"]

We need to include everything in between ( including the ending search string of \u003d13\"] )

This should give us a JavaScript string that looks something like sampleUserList.js in the root folder of our repository.

This will contain all of the detail including the ListName, ListDescription, ListCreator, and ListOfPlaces

Please load these up into a data structure that of a ListHeader and Places
Each place consists of a Name, Address, Latitude, Longitude, and potentially a Note, or image url.

Once you have loaded the information into the array please write out files as specified in the command line arguments

For the CSV file please use the dotnet package CSVHelper
For the KML file please follow the Keyhole Markup Language specification as described here: https://developers.google.com/kml

Please pay attention to the AGENTS.md file located in this repositories root folder