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
        Container(
          padding: const EdgeInsets.all(24),
          decoration: BoxDecoration(
            gradient: const LinearGradient(
              colors: [Color(0xFF153F4B), Color(0xFF126C73)],
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
            ),
            borderRadius: BorderRadius.circular(28),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(
                Icons.ramen_dining_rounded,
                color: Color(0xFFFFD59E),
                size: 40,
              ),
              const SizedBox(height: 22),
              const Text(
                'Good food,\nright on your route.',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 27,
                  height: 1.12,
                  fontWeight: FontWeight.w800,
                ),
              ),
              const SizedBox(height: 12),
              const Text(
                'Holy-Rice makes your journey a little more delicious. Order ahead and collect at an eligible station.',
                style: TextStyle(color: Color(0xFFD7EFED), height: 1.45),
              ),
              const SizedBox(height: 23),
              FilledButton.icon(
                onPressed: () => context.go(AppRoutes.explore),
                style: FilledButton.styleFrom(
                  backgroundColor: Colors.white,
                  foregroundColor: const Color(0xFF14535D),
                ),
                icon: const Icon(Icons.arrow_forward_rounded),
                label: const Text('Find a journey to order food'),
              ),
            ],
          ),
        ),
        const SizedBox(height: 27),
        Text(
          'How it works',
          style: Theme.of(context).textTheme.titleLarge
              ?.copyWith(fontWeight: FontWeight.w800),
        ),
        const SizedBox(height: 10),
        const _ServiceStep(
          number: '01',
          icon: Icons.search_rounded,
          title: 'Choose your train',
          detail: 'Search by route and travel date.',
        ),
        const _ServiceStep(
          number: '02',
          icon: Icons.restaurant_menu_rounded,
          title: 'Pick your food',
          detail: 'Open a dated journey to see the menu.',
        ),
        const _ServiceStep(
          number: '03',
          icon: Icons.place_rounded,
          title: 'Collect on the way',
          detail: 'Choose a supported station.',
        ),
        const SizedBox(height: 18),
        Card(
          margin: EdgeInsets.zero,
          child: ListTile(
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 18,
              vertical: 9,
            ),
            leading: const Icon(
              Icons.receipt_long_rounded,
              color: Color(0xFF126C73),
            ),
            title: const Text(
              'My catering orders',
              style: TextStyle(fontWeight: FontWeight.w700),
            ),
            subtitle: const Text('Track an order or check its status'),
            trailing: const Icon(Icons.arrow_forward_rounded),
            onTap: () => context.push(AppRoutes.cateringOrders),
          ),
        ),
      ],
    ),
  );
}

class _ServiceStep extends StatelessWidget {
  const _ServiceStep({
    required this.number,
    required this.icon,
    required this.title,
    required this.detail,
  });
  final String number, title, detail;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 5),
    child: Row(
      children: [
        Container(
          width: 47,
          height: 47,
          decoration: BoxDecoration(
            color: const Color(0xFFE2F2EF),
            borderRadius: BorderRadius.circular(15),
          ),
          child: Icon(icon, color: const Color(0xFF126C73)),
        ),
        const SizedBox(width: 13),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: const TextStyle(fontWeight: FontWeight.w700)),
              Text(
                detail,
                style: TextStyle(
                  color: Theme.of(context).colorScheme.onSurfaceVariant,
                ),
              ),
            ],
          ),
        ),
        Text(
          number,
          style: const TextStyle(
            color: Color(0xFF91A9A5),
            fontWeight: FontWeight.w700,
          ),
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
  String? selectedCategory;

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
    padding: const EdgeInsets.fromLTRB(18, 10, 18, 26),
    children: [
      Container(
        padding: const EdgeInsets.all(21),
        decoration: BoxDecoration(
          color: const Color(0xFF153F4B),
          borderRadius: BorderRadius.circular(23),
        ),
        child: Row(
          children: [
            const Icon(
              Icons.dinner_dining_rounded,
              size: 38,
              color: Color(0xFFFFD59E),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Holy-Rice',
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 24,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  Text(
                    widget.journeyLabel ?? 'Food for your selected journey',
                    style: const TextStyle(color: Color(0xFFD7EFED)),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
      const SizedBox(height: 24),
      const Text(
        'Choose food',
        style: TextStyle(fontSize: 23, fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 4),
      const Text('Fresh picks for the journey ahead.'),
      const SizedBox(height: 14),
      SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: [
            for (final category in <String?>[
              null,
              ...options!.items.map((item) => item.category).toSet(),
            ])
              Padding(
                padding: const EdgeInsets.only(right: 8),
                child: ChoiceChip(
                  label: Text(category ?? 'All'),
                  selected: selectedCategory == category,
                  onSelected: (_) =>
                      setState(() => selectedCategory = category),
                ),
              ),
          ],
        ),
      ),
      const SizedBox(height: 6),
      if (options!.items.isEmpty)
        const Text('No menu items are available for this journey.'),
      for (final item in options!.items.where(
        (item) => selectedCategory == null || item.category == selectedCategory,
      ))
        Card(
          margin: const EdgeInsets.symmetric(vertical: 6),
          child: ListTile(
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 14,
              vertical: 10,
            ),
            leading: Container(
              width: 46,
              height: 46,
              decoration: BoxDecoration(
                color: const Color(0xFFFFF1E3),
                borderRadius: BorderRadius.circular(15),
              ),
              child: const Icon(
                Icons.restaurant_rounded,
                color: Color(0xFFB9652C),
              ),
            ),
            title: Text(
              item.name,
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
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
        style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800),
      ),
      const SizedBox(height: 10),
      Card(
        margin: EdgeInsets.zero,
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(
                    Icons.shopping_bag_outlined,
                    color: Color(0xFF126C73),
                  ),
                  const SizedBox(width: 10),
                  Text(
                    '${cart.values.fold<int>(0, (sum, value) => sum + value)} items in your cart',
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Text(
                'Estimated total: ${total.toStringAsFixed(2)} LKR',
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ],
          ),
        ),
      ),
      const SizedBox(height: 13),
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
