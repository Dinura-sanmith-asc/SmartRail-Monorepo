import 'package:flutter/material.dart';

class RailwaySearchScreen extends StatelessWidget {
  const RailwaySearchScreen({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('SmartRail Passenger')),
    body: const Center(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Text(
          'Temporary placeholder: railway search will be added in a later phase.',
        ),
      ),
    ),
  );
}
