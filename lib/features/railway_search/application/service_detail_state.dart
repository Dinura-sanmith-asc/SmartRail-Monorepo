import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/application/retrieval_metadata.dart';

enum DetailSectionStatus {
  loading,
  ready,
  empty,
  unavailable,
  accessDenied,
  notFound,
  invalid,
}

class ServiceDetailState {
  const ServiceDetailState({
    required this.choice,
    required this.travelDate,
    required this.serviceStatus,
    required this.stopsStatus,
    this.serviceDetail,
    this.routeStops,
    this.serviceMessage,
    this.stopsMessage,
    this.serviceMetadata,
    this.stopsMetadata,
  });

  final ScheduledServiceChoice choice;
  final TravelDate travelDate;
  final DetailSectionStatus serviceStatus;
  final DetailSectionStatus stopsStatus;
  final ServiceDetail? serviceDetail;
  final RouteStops? routeStops;
  final String? serviceMessage;
  final String? stopsMessage;
  final RetrievalMetadata? serviceMetadata;
  final RetrievalMetadata? stopsMetadata;

  bool get canRetryService => serviceStatus == DetailSectionStatus.unavailable;
  bool get canRetryStops => stopsStatus == DetailSectionStatus.unavailable;

  ServiceDetailState copyWith({
    DetailSectionStatus? serviceStatus,
    DetailSectionStatus? stopsStatus,
    ServiceDetail? serviceDetail,
    RouteStops? routeStops,
    String? serviceMessage,
    String? stopsMessage,
    RetrievalMetadata? serviceMetadata,
    RetrievalMetadata? stopsMetadata,
    bool clearServiceMessage = false,
    bool clearStopsMessage = false,
  }) => ServiceDetailState(
    choice: choice,
    travelDate: travelDate,
    serviceStatus: serviceStatus ?? this.serviceStatus,
    stopsStatus: stopsStatus ?? this.stopsStatus,
    serviceDetail: serviceDetail ?? this.serviceDetail,
    routeStops: routeStops ?? this.routeStops,
    serviceMessage: clearServiceMessage
        ? null
        : serviceMessage ?? this.serviceMessage,
    stopsMessage: clearStopsMessage ? null : stopsMessage ?? this.stopsMessage,
    serviceMetadata: serviceMetadata ?? this.serviceMetadata,
    stopsMetadata: stopsMetadata ?? this.stopsMetadata,
  );
}
