import 'dart:math';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/app/router.dart';
import 'package:smartrail_passenger/features/catering/data/catering_api.dart';

class CateringServicesScreen extends StatelessWidget {
  const CateringServicesScreen({super.key});
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Services')),
    body: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        const Card(
          child: ListTile(
            leading: Icon(Icons.restaurant),
            title: Text('Holy-Rice'),
            subtitle: Text(
              'Order food for an eligible dated journey and collect at a supported station.',
            ),
          ),
        ),
        const SizedBox(height: 12),
        FilledButton(
          onPressed: () => context.go(AppRoutes.explore),
          child: const Text('Find a journey to order food'),
        ),
        OutlinedButton(
          onPressed: () => context.push(AppRoutes.cateringOrders),
          child: const Text('My catering orders'),
        ),
      ],
    ),
  );
}

class CateringMenuScreen extends ConsumerStatefulWidget {
  const CateringMenuScreen({
    required this.journeyId,
    this.journeyLabel,
    super.key,
  });
  final String journeyId;
  final String? journeyLabel;
  @override
  ConsumerState<CateringMenuScreen> createState() => _CateringMenuScreenState();
}

class _CateringMenuScreenState extends ConsumerState<CateringMenuScreen> {
  CateringOptions? options;
  final cart = <String, int>{};
  String? stationId, error, pendingKey;
  bool loading = true, submitting = false;
  CateringOrder? confirmed;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final value = await ref
          .read(cateringApiProvider)
          .options(widget.journeyId);
      if (mounted) {
        setState(() {
          options = value;
          loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          error = e.toString();
          loading = false;
        });
      }
    }
  }

  String _newKey() => List.generate(
    24,
    (_) => Random.secure().nextInt(256).toRadixString(16).padLeft(2, '0'),
  ).join();
  double get total => options!.items.fold<double>(
    0,
    (sum, item) => sum + item.price * (cart[item.id] ?? 0),
  );

  Future<void> _submit() async {
    if (stationId == null || cart.isEmpty || submitting) return;
    setState(() {
      submitting = true;
      error = null;
    });
    try {
      pendingKey ??= _newKey();
      final result = await ref
          .read(cateringApiProvider)
          .create(widget.journeyId, stationId!, cart, pendingKey!);
      if (mounted) {
        setState(() {
          confirmed = result;
          submitting = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          error = e.toString();
          submitting = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Holy-Rice menu')),
    body: loading
        ? const Center(child: CircularProgressIndicator())
        : options == null
        ? Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(error ?? 'Menu unavailable'),
                TextButton(onPressed: _load, child: const Text('Retry')),
              ],
            ),
          )
        : confirmed != null
        ? _confirmation()
        : _menu(),
  );

  Widget _menu() => ListView(
    padding: const EdgeInsets.all(16),
    children: [
      const Text(
        'Choose food',
        style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
      ),
      if (options!.items.isEmpty)
        const Text('No menu items are available for this journey.'),
      for (final item in options!.items)
        Card(
          child: ListTile(
            title: Text(item.name),
            subtitle: Text(
              '${item.description}\n${item.price.toStringAsFixed(2)} ${item.currency}${item.available ? '' : ' · Unavailable'}',
            ),
            isThreeLine: true,
            trailing: item.available
                ? Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      IconButton(
                        icon: const Icon(Icons.remove),
                        tooltip: 'Remove ${item.name}',
                        onPressed: (cart[item.id] ?? 0) == 0
                            ? null
                            : () => setState(() {
                                cart.update(item.id, (v) => v - 1);
                                if (cart[item.id] == 0) cart.remove(item.id);
                                pendingKey = null;
                              }),
                      ),
                      Text('${cart[item.id] ?? 0}'),
                      IconButton(
                        icon: const Icon(Icons.add),
                        tooltip: 'Add ${item.name}',
                        onPressed: (cart[item.id] ?? 0) >= 20
                            ? null
                            : () => setState(() {
                                cart.update(
                                  item.id,
                                  (v) => v + 1,
                                  ifAbsent: () => 1,
                                );
                                pendingKey = null;
                              }),
                      ),
                    ],
                  )
                : null,
          ),
        ),
      const SizedBox(height: 18),
      const Text(
        'Cart and collection',
        style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold),
      ),
      Text(widget.journeyLabel ?? 'Selected dated journey'),
      Text('Estimated total: ${total.toStringAsFixed(2)} LKR'),
      if (options!.points.isEmpty)
        const Text('No eligible collection stations for this journey.'),
      if (options!.points.isNotEmpty)
        DropdownButtonFormField<String>(
          decoration: const InputDecoration(labelText: 'Collection station'),
          initialValue: stationId,
          items: options!.points
              .map(
                (p) => DropdownMenuItem(
                  value: p.stationId,
                  child: Text(p.stationName),
                ),
              )
              .toList(),
          onChanged: (value) => setState(() {
            stationId = value;
            pendingKey = null;
          }),
        ),
      if (stationId != null)
        Text(
          options!.points
              .firstWhere((p) => p.stationId == stationId)
              .instructions,
        ),
      if (error != null)
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 10),
          child: Text(
            error!,
            style: TextStyle(color: Theme.of(context).colorScheme.error),
          ),
        ),
      const SizedBox(height: 16),
      FilledButton(
        onPressed: cart.isEmpty || stationId == null || submitting
            ? null
            : _submit,
        child: Text(
          submitting
              ? 'Submitting…'
              : error == null
              ? 'Submit order'
              : 'Retry same order safely',
        ),
      ),
    ],
  );

  Widget _confirmation() => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      const Icon(Icons.check_circle, size: 60, color: Colors.green),
      const Text(
        'Order placed',
        style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
      ),
      Text('Reference: ${confirmed!.id.substring(0, 8)}'),
      Text('Status: ${confirmed!.status}'),
      Text(
        'Collect at: ${options!.points.firstWhere((p) => p.stationId == confirmed!.collectionStationId).stationName}',
      ),
      for (final item in confirmed!.items)
        Text('${item['quantity']} × ${item['name']}'),
      Text(
        'Total: ${confirmed!.total?.toStringAsFixed(2) ?? 'Unknown'} ${confirmed!.currency}',
      ),
      FilledButton(
        onPressed: () =>
            context.push('${AppRoutes.cateringOrders}/${confirmed!.id}'),
        child: const Text('Track order'),
      ),
    ],
  );
}

class CateringOrdersScreen extends ConsumerStatefulWidget {
  const CateringOrdersScreen({super.key});
  @override
  ConsumerState<CateringOrdersScreen> createState() =>
      _CateringOrdersScreenState();
}

class _CateringOrdersScreenState extends ConsumerState<CateringOrdersScreen> {
  List<CateringOrder>? orders;
  String? error;
  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      orders = null;
      error = null;
    });
    try {
      final value = await ref.read(cateringApiProvider).orders();
      if (mounted) setState(() => orders = value);
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('My catering orders'),
      actions: [
        IconButton(
          onPressed: _load,
          icon: const Icon(Icons.refresh),
          tooltip: 'Refresh orders',
        ),
      ],
    ),
    body: orders == null
        ? Center(
            child: error == null
                ? const CircularProgressIndicator()
                : Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(error!),
                      TextButton(onPressed: _load, child: const Text('Retry')),
                    ],
                  ),
          )
        : orders!.isEmpty
        ? const Center(child: Text('No catering orders yet.'))
        : ListView(
            children: [
              for (final order in orders!)
                ListTile(
                  title: Text('Order ${order.id.substring(0, 8)}'),
                  subtitle: Text(
                    '${order.status} · ${order.total?.toStringAsFixed(2) ?? 'Unknown'} ${order.currency}',
                  ),
                  onTap: () =>
                      context.push('${AppRoutes.cateringOrders}/${order.id}'),
                ),
            ],
          ),
  );
}

class CateringOrderDetailScreen extends ConsumerStatefulWidget {
  const CateringOrderDetailScreen({required this.id, super.key});
  final String id;
  @override
  ConsumerState<CateringOrderDetailScreen> createState() =>
      _CateringOrderDetailScreenState();
}

class _CateringOrderDetailScreenState
    extends ConsumerState<CateringOrderDetailScreen> {
  CateringOrder? order;
  String? error;
  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      order = null;
      error = null;
    });
    try {
      final value = await ref.read(cateringApiProvider).order(widget.id);
      if (mounted) setState(() => order = value);
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('Catering order'),
      actions: [
        IconButton(
          onPressed: _load,
          icon: const Icon(Icons.refresh),
          tooltip: 'Refresh status',
        ),
      ],
    ),
    body: order == null
        ? Center(
            child: error == null
                ? const CircularProgressIndicator()
                : Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(error!),
                      TextButton(onPressed: _load, child: const Text('Retry')),
                    ],
                  ),
          )
        : ListView(
            padding: const EdgeInsets.all(20),
            children: [
              Text(
                'Order ${order!.id.substring(0, 8)}',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              Text('Provider status: ${order!.status}'),
              for (final item in order!.items)
                Text('${item['quantity']} × ${item['name']}'),
              Text(
                'Total: ${order!.total?.toStringAsFixed(2) ?? 'Unknown'} ${order!.currency}',
              ),
            ],
          ),
  );
}
