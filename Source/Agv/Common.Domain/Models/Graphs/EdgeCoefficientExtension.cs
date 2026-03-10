namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Provides extension methods for edge coefficients.
/// </summary>
public static class EdgeCoefficientExtension
{
    extension(ICollection<EdgeCoefficient> coefficients)
    {
        /// <summary>
        /// Get the inner coefficients, i.e. without the first and last
        /// element.
        /// </summary>
        /// <returns>A collection of inner edge coefficients.</returns>
        public ICollection<EdgeCoefficient> GetInner()
        {
            return coefficients.Skip(1).Take(coefficients.Count - 2).ToList();
        }
    }
}