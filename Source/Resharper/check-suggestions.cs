#:package Sarif.Sdk@4.6.0
using System;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif;

var log = SarifLog.Load("./resharper-results.json");
var results = log.Runs.SelectMany(run => run.Results);
var suggestions = results.Where(result => result.Level == FailureLevel.Warning || result.Level == FailureLevel.Note);
bool hasSuggestions = false;

using (StreamWriter sw = new StreamWriter("./resharper-logs.txt"))
{
    foreach(var suggestion in suggestions){
        var location = suggestion.Locations.First();
        var level = "Suggestion";
        if (suggestion.Level != FailureLevel.Note) {
           level = suggestion.Level.ToString();
        }
        string logMessage = $"{level}: {suggestion.Message.Text} ('{suggestion.RuleId}' on line {location.PhysicalLocation.Region.StartLine} in {location.PhysicalLocation.ArtifactLocation.Uri})";
        sw.WriteLine(logMessage);
        hasSuggestions = true;
    }
}
