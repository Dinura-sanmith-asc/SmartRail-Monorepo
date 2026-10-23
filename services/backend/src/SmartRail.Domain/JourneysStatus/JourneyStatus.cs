namespace SmartRail.Domain.JourneysStatus;

public enum JourneyStatus { Scheduled, Running, Delayed, Interrupted, Cancelled, Completed, Terminated }
public enum JourneyPublicationKind { InitializationBaseline, Status, StopChange, RouteDiversion, Correction }
public enum JourneySource { ScheduledBaseline, ControllerManual, StationStaffManual, ApprovedRailwayFeed }
public enum JourneyInitializationDecision { Accepted, IdempotentReplay, RejectedConflict, RejectedAuthorization, RejectedValidation }
