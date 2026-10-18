import 'package:flutter/material.dart';

class ServiceDetailScreen extends StatelessWidget {
  const ServiceDetailScreen({required this.serviceId, super.key});

  final String serviceId;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Service details')),
    body: Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Text(
          'Temporary placeholder for service $serviceId. Planned details will be added in a later phase.',
          textAlign: TextAlign.center,
        ),
      ),
    ),
  );
}
