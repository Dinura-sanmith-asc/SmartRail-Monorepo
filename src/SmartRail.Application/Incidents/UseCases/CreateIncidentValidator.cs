using SmartRail.Application.Incidents.References;

namespace SmartRail.Application.Incidents.UseCases;

public sealed record CreateIncidentInput(
    string? CategoryCode,
    string? Description,
    DateTimeOffset? OccurredAt,
    IReadOnlyCollection<IncidentReference>? Associations);

public sealed record IncidentValidationResult(IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Validates caller-controlled report fields before any authority or persistence call.</summary>
public static class CreateIncidentValidator
{
    public static IncidentValidationResult Validate(CreateIncidentInput? input)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Add(string key, string message)
        {
            if (!errors.TryGetValue(key, out var messages)) errors[key] = messages = [];
            messages.Add(message);
        }

        if (input is null)
        {
            Add("request", "A request body is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(input.CategoryCode))
                Add("categoryCode", "A category code is required.");
            if (string.IsNullOrWhiteSpace(input.Description))
                Add("description", "A description is required.");
            else if (input.Description.Trim().Length > 4000)
                Add("description", "The description exceeds the supported length.");

            var associations = input.Associations;
            if (associations is null || associations.Count == 0)
            {
                Add("associations", "At least one station or service reference is required.");
            }
            else
            {
                var hasRequiredAnchor = false;
                var seen = new HashSet<(IncidentReferenceKind Kind, Guid ResourceId, Guid? UpdateId, int? Revision)>();
                foreach (var reference in associations)
                {
                    if (reference is null || !Enum.IsDefined(reference.Kind) || reference.ResourceId == Guid.Empty)
                    {
                        Add("associations", "Each reference must contain a recognized kind and non-empty identifier.");
                        continue;
                    }

                    if (reference.Kind is IncidentReferenceKind.Station or IncidentReferenceKind.Service)
                        hasRequiredAnchor = true;

                    if (reference.Kind == IncidentReferenceKind.Disruption)
                    {
                        if (!reference.UpdateId.HasValue || reference.UpdateId.Value == Guid.Empty || reference.Revision is null or <= 0)
                            Add("associations", "A disruption reference requires a valid update identifier and positive revision.");
                    }
                    else if (reference.UpdateId.HasValue || reference.Revision.HasValue)
                    {
                        Add("associations", "Update identifier and revision are valid only for a disruption reference.");
                    }

                    if (!seen.Add((reference.Kind, reference.ResourceId, reference.UpdateId, reference.Revision)))
                        Add("associations", "Duplicate references are not allowed.");
                }

                if (!hasRequiredAnchor)
                    Add("associations", "At least one station or service reference is required.");
            }
        }

        return new IncidentValidationResult(errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
    }
}
