using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Requests.Lif;

/// <summary>
/// Represents the response to the <see cref="GetLifFiles"/> request.
/// </summary>
/// <param name="LifFiles">The lif files.</param>
public record GotLifFiles(ICollection<LifFile> LifFiles);