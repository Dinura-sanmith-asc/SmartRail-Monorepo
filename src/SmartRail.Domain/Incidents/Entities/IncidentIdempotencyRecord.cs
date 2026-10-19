namespace SmartRail.Domain.Incidents.Entities;

public sealed class IncidentIdempotencyRecord
{
    private IncidentIdempotencyRecord() { }

    public Guid Id { get; private set; }
    public string ActorSubject { get; private set; } = string.Empty;
    public string Operation { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public Guid IncidentId { get; private set; }
    public int HttpStatusCode { get; private set; }
    public string ResponseJson { get; private set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; private set; }

    public IncidentIdempotencyRecord(Guid id, string actorSubject, string operation, string idempotencyKey,
        string payloadHash, Guid incidentId, int httpStatusCode, string responseJson, DateTimeOffset recordedAt)
    {
        if (id == Guid.Empty || incidentId == Guid.Empty) throw new ArgumentException("Record and incident IDs are required.");
        if (httpStatusCode is < 200 or > 599) throw new ArgumentOutOfRangeException(nameof(httpStatusCode));
        Id = id;
        ActorSubject = Incident.Required(actorSubject, nameof(actorSubject), 300);
        Operation = Incident.Required(operation, nameof(operation), 100);
        IdempotencyKey = Incident.Required(idempotencyKey, nameof(idempotencyKey), 200);
        PayloadHash = Incident.Required(payloadHash, nameof(payloadHash), 128);
        IncidentId = incidentId;
        HttpStatusCode = httpStatusCode;
        ResponseJson = Incident.Required(responseJson, nameof(responseJson), 10000);
        RecordedAt = Incident.Utc(recordedAt, nameof(recordedAt));
    }
}
