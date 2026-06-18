using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;

/// <summary>
/// Extension methods for constructing a <see cref="LifFile"/> from a <see cref="LifLayoutCollection"/>.
/// </summary>
[PublicAPI]
public static class LifToLifFileExtensions
{
    extension(LifLayoutCollection lifLayoutCollection)
    {
        /// <summary>
        /// Constructs a <see cref="LifFile"/> from the layout collection and the metadata of the uploaded file.
        /// </summary>
        /// <param name="uploadTimestamp">The timestamp at which the file was uploaded.</param>
        /// <param name="fileName">The name of the uploaded file.</param>
        /// <param name="fileSizeMb">The size of the uploaded file in megabytes.</param>
        /// <param name="uploadedBy">The identifier of the user who uploaded the file.</param>
        public LifFile ToLifFile(DateTime uploadTimestamp, string fileName, double fileSizeMb, string uploadedBy)
        {
            ICollection<Guid> layoutIds = lifLayoutCollection.Layouts
                .Select(l => l.Id)
                .ToList();

            return new LifFile(
                id: Guid.NewGuid(),
                lifCollectionMetaInformation: lifLayoutCollection.MetaInformation,
                layoutIds: layoutIds,
                metaInformation: new LifFileMetaInformation(
                    uploadTimestamp: uploadTimestamp,
                    filename: fileName,
                    fileSizeMb: fileSizeMb,
                    uploadedBy: uploadedBy
                ),
                supportedVehicleTypes: lifLayoutCollection.GetSupportedVehicleTypes()
            );
        }
    }
}
