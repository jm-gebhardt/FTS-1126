using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Messages.Responses;

/// <summary>
/// Response indicating that a test plan has been set.
/// </summary>
[PublicAPI]
public record TestPlanSet(string TestPlanName);