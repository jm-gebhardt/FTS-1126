namespace Gebhardt.Acc.DeviceCommunication.Common.Configuration;

/// <summary>
/// Provides a configuration reader.
/// </summary>
public interface IConfigurationReader
{
    /// <summary>
    /// Read the configuration.
    /// </summary>
    /// <returns>A text reader.</returns>
    public TextReader? Read();
}