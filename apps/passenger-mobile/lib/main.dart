import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/app/app.dart';
import 'package:smartrail_passenger/app/app_config.dart';

void main() {
  runApp(
    ProviderScope(child: SmartRailApp(config: AppConfig.fromEnvironment())),
  );
}
