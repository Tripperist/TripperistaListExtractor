# TripperistaListExtractor
A utility for Tripperistas! Exports Google Maps Saved Lists to CSV and KML files.

## Building the solution

```bash
dotnet build TripperistaListExtractor.sln
```

## Running the extractor

```bash
dotnet run --project Console.TripperistaListExtractor -- \
    --inputSavedListUrl "https://maps.app.goo.gl/yourSavedList" \
    --outputKmlFile "output.kml" \
    --outputCsvFile "output.csv" \
    --googlePlacesApiKey "YOUR_API_KEY" \
    --verbose
```

The Google Places API key argument is optional. When it is not supplied on the command line the application attempts to read the value from the `GooglePlaces:ApiKey` configuration section or the `GOOGLE_PLACES_API_KEY` environment variable.
