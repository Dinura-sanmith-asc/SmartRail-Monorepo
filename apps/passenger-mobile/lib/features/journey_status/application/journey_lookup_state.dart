import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

enum JourneyLookupStatus {
  initial,
  loading,
  ready,
  empty,
  unavailable,
  invalid,
  accessDenied,
  notFound,
}

class JourneyLookupRequest {
  const JourneyLookupRequest({required this.choice, required this.travelDate});

  final ScheduledServiceChoice choice;
  final TravelDate travelDate;

  String get serviceId => choice.serviceId;
  String get originStationId => choice.origin.stationId;
  String get stationLocalDepartureDate => travelDate.toIsoDate();
}

class JourneyLookupState {
  JourneyLookupState({
    this.status = JourneyLookupStatus.initial,
    List<JourneyCandidate> candidates = const [],
    this.selectedCandidate,
    this.failure,
  }) : candidates = List.unmodifiable(candidates);

  final JourneyLookupStatus status;
  final List<JourneyCandidate> candidates;
  final JourneyCandidate? selectedCandidate;
  final ApiFailure? failure;

  bool get canRetry => status == JourneyLookupStatus.unavailable;
}
