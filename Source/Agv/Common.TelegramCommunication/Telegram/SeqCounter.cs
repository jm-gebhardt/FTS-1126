using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.TelegramCommunication.Telegram;

/// <summary>
/// Provides a counter for <see cref="TelegramBase.Seq"/>
/// </summary>
public class SeqCounter
{
    private int Format { get; }

    private int _current;

    private readonly int _end;

    private readonly int _start;

    /// <summary>
    /// Current counter
    /// </summary>
    public int Current
    {
        get => _current;
        [PublicAPI]
        set => _current = (value <= _end && value >= _start) || value == 0 ? value : _current;
    }

    /// <summary>
    /// Previous counter
    /// </summary>
    public int Previous => _current - 1 < _start ? _end : _current - 1;

    /// <summary>
    /// Creates a sequence counter
    /// </summary>
    /// <param name="start">Start of the counter</param>
    /// <param name="end">End of the counter</param>
    /// <param name="format">Length for string formatting</param>
    public SeqCounter(int start, int end, int format)
    {
        _current = start;
        _start = start;
        _end = end;
        Format = format;
    }

    /// <summary>
    /// Increase the counter
    /// </summary>
    public void Increase()
    {
        _current++;
        if (_current > _end)
        {
            _current = _start;
        }
    }

    /// <summary>
    /// Reset the counter to a given value
    /// </summary>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public bool Reset(int value)
    {
        bool isValid = value <= _end && value >= _start;
        if (isValid)
        {
            _current = value;
        }
        return isValid;
    }

    /// <summary>
    /// Convert to string
    /// </summary>
    public override string ToString()
    {
        return _current.ToString($"D{Format}");
    }
}
