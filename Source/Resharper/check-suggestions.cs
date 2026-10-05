#:package Sarif.Sdk
using System;
using System.Linq;
using Microsoft.CodeAnalysis.Sarif;

var log = SarifLog.Load("./resharper-results.json");
var results = log.Runs.SelectMany(run => run.Results);
var warnings = results.Where(result => result.Level == FailureLevel.Error || result.Level == FailureLevel.Warning);

using (StreamWriter sw = new StreamWriter("./resharper-logs.txt"))
{
    foreach(var warning in warnings){
        var location = warning.Locations.First();
        var level = warning.Level.ToString();
        string logMessage = $"{level}: {warning.Message.Text} ('{warning.RuleId}' on line {location.PhysicalLocation.Region.StartLine} in {location.PhysicalLocation.ArtifactLocation.Uri})";
        sw.WriteLine(logMessage);
    }
}
