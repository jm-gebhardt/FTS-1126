namespace Gebhardt.Acc.IntegrationTest.Utils.Db.Models
{
    /// <summary>
    /// WCS writes actions for us to consume into this table
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public class ToAgv : ToFromAgvBase
    {
        /// <summary>
        /// Validate that OrderId and (Source or Destination) are set
        /// </summary>
        /// <returns>bool if all required fields are set</returns>
        public bool IsValid =>
            !string.IsNullOrEmpty(OrderId) &&
            (!string.IsNullOrEmpty(Source) || !string.IsNullOrEmpty(Destination));
    }
}