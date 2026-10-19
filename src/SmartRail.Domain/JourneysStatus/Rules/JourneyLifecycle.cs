using SmartRail.Domain.JourneysStatus;

namespace SmartRail.Domain.JourneysStatus.Rules;

public static class JourneyLifecycle
{
    public static bool CanTransition(JourneyStatus current, JourneyStatus next, bool isLinkedCorrection = false)
    {
        if (current == next) return false;
        if (current is JourneyStatus.Cancelled or JourneyStatus.Completed or JourneyStatus.Terminated)
            return isLinkedCorrection;
        return current switch
        {
            JourneyStatus.Scheduled => next is JourneyStatus.Running or JourneyStatus.Delayed or JourneyStatus.Interrupted or JourneyStatus.Cancelled,
            JourneyStatus.Running => next is JourneyStatus.Delayed or JourneyStatus.Interrupted or JourneyStatus.Completed or JourneyStatus.Terminated,
            JourneyStatus.Delayed => next is JourneyStatus.Running or JourneyStatus.Interrupted or JourneyStatus.Completed or JourneyStatus.Terminated,
            JourneyStatus.Interrupted => next is JourneyStatus.Running or JourneyStatus.Delayed or JourneyStatus.Completed or JourneyStatus.Terminated,
            _ => false
        };
    }

    public static void EnsureTransition(JourneyStatus current, JourneyStatus next, bool isLinkedCorrection = false)
    {
        if (!CanTransition(current, next, isLinkedCorrection))
            throw new InvalidOperationException("Journey status transition is not permitted.");
    }
}
