import 'package:dio/dio.dart';

import '../../features/assignments/domain/models.dart';

class RailApi {
  RailApi(this.dio);
  final Dio dio;
  Future<DriverIdentity> me() async => DriverIdentity.fromJson(
    (await dio.get('/api/me')).data as Map<String, dynamic>,
  );
  Future<List<Assignment>> assignments() async {
    final body =
        (await dio.get('/api/driver/assignments')).data as Map<String, dynamic>;
    return (body['items'] as List)
        .map((e) => Assignment.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<DriverObservation> submitObservation(
    String journeyId,
    String requestId,
    String text,
  ) async {
    final response = await dio.post(
      '/api/driver/journeys/$journeyId/observations',
      data: {'requestId': requestId, 'text': text},
    );
    return DriverObservation.fromJson(response.data as Map<String, dynamic>);
  }
}
