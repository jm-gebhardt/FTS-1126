using System.Diagnostics.CodeAnalysis;
using Gebhardt.Core.NotificationCenter.Contracts.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Notification.Models;

/// <summary>
/// Data transfer object wrapper for notification links.
/// </summary>
[PublicAPI]
public record NotificationLink
{
    /// <summary>
    /// Landing page for internal resource links.
    /// </summary>
    private const string ResourcePrefix = "resources";

    /// <summary>
    /// If this is an external link or a link to an internal resource.
    /// </summary>
    public bool IsExternal
    {
        private init;

        [MemberNotNullWhen(false, nameof(Resource), nameof(ResourceId))]
        [MemberNotNullWhen(true, nameof(ExternalUrl))]
        get;
    }

    /// <summary>
    /// The display title of the link.
    /// </summary>
    public string Title { private init; get; }

    /// <summary>
    /// The resource name.
    /// </summary>
    public NotificationLinkResource? Resource { private init; get; }

    /// <summary>
    /// The resource id.
    /// </summary>
    public Guid? ResourceId { private init; get; }

    /// <summary>
    /// The external url.
    /// </summary>
    public string? ExternalUrl { private init; get; }

    /// <summary>
    /// Creates a link to use in a notification that links to an internal resource.
    /// </summary>
    /// <param name="title">The display title of the link.</param>
    /// <param name="resource">The resource name.</param>
    /// <param name="resourceId">The resource id.</param>
    public NotificationLink(string title, NotificationLinkResource resource, Guid resourceId)
    {
        IsExternal = false;
        Title = title;
        Resource = resource;
        ResourceId = resourceId;
    }

    /// <summary>
    /// Creates link to use in a notification that links to an external url.
    /// </summary>
    /// <param name="title">The display title of the link.</param>
    /// <param name="externalUrl">The external url.</param>
    public NotificationLink(string title, string externalUrl)
    {
        IsExternal = true;
        Title = title;
        ExternalUrl = externalUrl;
    }

    /// <summary>
    /// Converts the Notification link into the external dto.
    /// </summary>
    internal LinkDto ToDto()
    {
        return IsExternal ? 
            new LinkDto{ Title=Title, Url=ExternalUrl } :
            new LinkDto { Title = Title, Url = $"https://127.0.0.1/{ResourcePrefix}/{Resource.Value.ToString()}/{ResourceId.Value.ToString()}" };
    }
}

/// <summary>
/// Types of resources available for links.
/// </summary>
[PublicAPI]
public enum NotificationLinkResource
{
    /// <summary>
    /// Agv.
    /// </summary>
    Agv,

    /// <summary>
    /// Station.
    /// </summary>
    Station,

    /// <summary>
    /// Action.
    /// </summary>
    Action,

    /// <summary>
    /// ActionGroup.
    /// </summary>
    ActionGroup
}