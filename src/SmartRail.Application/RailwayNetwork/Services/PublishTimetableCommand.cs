using SmartRail.Domain.RailwayNetwork.Entities;

namespace SmartRail.Application.RailwayNetwork.Services;

public sealed record TimetableDatePublication(ServiceDateManifest Manifest, IReadOnlyCollection<ServiceOperationDate> Operations);
public sealed record TimetablePublicationRequest(
    Guid ServiceId, Guid ScheduleId, DateTimeOffset PublishedAt, string PublisherIdentity,
    IReadOnlyCollection<TimetableDatePublication> Dates);

public interface ISchedulePublicationRepository
{
    Task PublishTimetableAsync(TimetablePublicationRequest request, CancellationToken cancellationToken = default);
}

public interface ISchedulePublicationAuthorization
{
    ValueTask<bool> IsAuthorizedAsync(string publisherIdentity, CancellationToken cancellationToken = default);
}

public sealed class DenyAllSchedulePublicationAuthorization : ISchedulePublicationAuthorization
{
    public ValueTask<bool> IsAuthorizedAsync(string publisherIdentity, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(false);
}

public sealed record PublishTimetableCommand(TimetablePublicationRequest Request);
public sealed record PublishTimetableResult(bool IsPublished, bool IsUnauthorized, IReadOnlyList<string> Errors);

public sealed class PublishTimetableHandler(
    ISchedulePublicationAuthorization authorization,
    ISchedulePublicationRepository repository)
{
    public async Task<PublishTimetableResult> HandleAsync(PublishTimetableCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Request);
        if (string.IsNullOrWhiteSpace(command.Request.PublisherIdentity) ||
            !await authorization.IsAuthorizedAsync(command.Request.PublisherIdentity, cancellationToken))
            return new(false, true, ["The timetable publisher is not authorized."]);
        if (command.Request.ServiceId == Guid.Empty || command.Request.ScheduleId == Guid.Empty || command.Request.Dates is null)
            return new(false, false, ["A service, schedule and date publication collection are required."]);
        var duplicateDate = command.Request.Dates.GroupBy(item => item.Manifest.OperationDate).Any(group => group.Count() > 1);
        if (duplicateDate) return new(false, false, ["Each operation date may appear only once in a timetable publication."]);
        foreach (var date in command.Request.Dates)
        {
            if (date.Operations is null || date.Operations.Any(item => item.OperationDate != date.Manifest.OperationDate))
                return new(false, false, ["Operation rows must match their date manifest."]);
            if (!date.Manifest.IsComplete && date.Operations.Count != 0)
                return new(false, false, ["An incomplete date cannot assert operating services."]);
        }
        await repository.PublishTimetableAsync(command.Request, cancellationToken);
        return new(true, false, []);
    }
}
