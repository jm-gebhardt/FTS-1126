 #r "nuget: Sarif.Sdk, 4.5.4"
using System;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif;

var log = SarifLog.Load("./resharper-results.json");
var results = log.Runs.SelectMany(run => run.Results);
var suggestions = results.Where(result => result.Level == FailureLevel.Warning );
bool hasSuggestions = false;

using (StreamWriter sw = new StreamWriter("./resharper-logs.txt"))
{
    foreach(var suggestion in suggestions){
        var location = suggestion.Locations.First();
        string logMessage = $"Warning '{suggestion.RuleId}' on line {location.PhysicalLocation.Region.StartLine} in {location.PhysicalLocation.ArtifactLocation.Uri}";
        sw.WriteLine(logMessage);
        hasSuggestions = true;
    }
}

