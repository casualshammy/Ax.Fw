using System;
using System.Text;

namespace Ax.Fw.Extensions;

public enum DateTimeOffsetToHumanFriendlyStringMaxUnit
{
  None = 0,
  Seconds,
  Minutes,
  Hours
}

public record DateTimeOffsetToHumanFriendlyStringOptions(
  DateTimeOffsetToHumanFriendlyStringMaxUnit MaxUnit = DateTimeOffsetToHumanFriendlyStringMaxUnit.Minutes,
  string FallbackFormat = "yyyy/MM/dd HH:mm:ss",
  string HoursWord = "hours",
  string MinutesWord = "min",
  string SecondsWord = "sec")
{
  public static DateTimeOffsetToHumanFriendlyStringOptions Default { get; } = new DateTimeOffsetToHumanFriendlyStringOptions();
};

public static class DateTimeOffsetExtensions
{
  public static string ToHumanFriendlyString(this DateTimeOffset _dateTime, DateTimeOffsetToHumanFriendlyStringOptions _options)
  {
    var now = DateTimeOffset.Now;
    if (now < _dateTime)
      return _dateTime.ToString(_options.FallbackFormat);

    var delta = now - _dateTime;
    if (delta.TotalHours >= 24)
      return _dateTime.ToString(_options.FallbackFormat);

    if (delta.TotalHours >= 1 && _options.MaxUnit < DateTimeOffsetToHumanFriendlyStringMaxUnit.Hours)
      return _dateTime.ToString(_options.FallbackFormat);

    if (delta.TotalMinutes >= 1 && _options.MaxUnit < DateTimeOffsetToHumanFriendlyStringMaxUnit.Minutes)
      return _dateTime.ToString(_options.FallbackFormat);

    var sb = new StringBuilder();

    var hours = delta.Hours;
    var minutes = delta.Minutes;
    var seconds = delta.Seconds;

    if (hours > 0)
      sb.Append($"{hours} {_options.HoursWord} ");

    if (minutes > 0)
      sb.Append($"{minutes} {_options.MinutesWord} ");

    if (seconds > 0)
      sb.Append($"{seconds} {_options.SecondsWord} ");

    return sb.ToString().TrimEnd();
  }
}
