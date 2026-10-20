namespace SmartRail.Domain.DriverOperations;

public enum DriverAssignmentAuditAction { Granted = 1, Revoked = 2 }

internal static class DriverDataGuard
{
    internal static Guid Id(Guid value, string name) => value != Guid.Empty ? value : throw new ArgumentException("A nonempty identifier is required.", name);
    internal static string Subject(string value, string name) => !string.IsNullOrWhiteSpace(value) && value.Length <= 300 ? value : throw new ArgumentException("An opaque verified subject is required (maximum 300 characters).", name);
    internal static string Text(string value, string name, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximum ? value.Trim() : throw new ArgumentException($"Text is required (maximum {maximum} characters).", name);
    internal static DateTimeOffset Utc(DateTimeOffset value, string name) => value != default && value.Offset == TimeSpan.Zero ? value : throw new ArgumentException("A non-default UTC timestamp is required.", name);
}
