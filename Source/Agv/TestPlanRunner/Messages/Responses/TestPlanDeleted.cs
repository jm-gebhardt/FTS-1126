using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.Messages.Responses;

/// <summary>
/// Response indicating that a test plan has been deleted.
/// </summary>
[PublicAPI]
public record TestPlanDeleted(string TestPlanName);