using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;

public static class FactsheetJsonGenerator
{
    public const string Manufacturer = "gebhardt";
    public const string SeriesName = "series";
    public const string AgvTypeIdentifier = $"{Manufacturer}.{SeriesName}";
    public const string FactsheetJson = $$$"""{"typeSpecification":{"seriesName":"{{{SeriesName}}}"}}""";
    public const string InvalidFactsheetJson = "{ not json";

    public static readonly Guid AgvTypeId = AgvTypeIdentifier.ToGuid();
}