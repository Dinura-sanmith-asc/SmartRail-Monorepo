import 'package:flutter/material.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

class StationPicker extends StatelessWidget {
  const StationPicker({
    required this.label,
    required this.stations,
    required this.selectedStation,
    required this.enabled,
    required this.onSelected,
    super.key,
  });

  final String label;
  final List<Station> stations;
  final Station? selectedStation;
  final bool enabled;
  final ValueChanged<String?> onSelected;

  @override
  Widget build(BuildContext context) => SizedBox(
    width: double.infinity,
    child: OutlinedButton.icon(
      onPressed: enabled ? () => _openPicker(context) : null,
      icon: const Icon(Icons.train_outlined),
      label: Align(
        alignment: Alignment.centerLeft,
        child: Text(
          selectedStation == null
              ? 'Select $label station'
              : '$label station: ${selectedStation!.name}',
        ),
      ),
      style: OutlinedButton.styleFrom(
        alignment: Alignment.centerLeft,
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
      ),
    ),
  );

  Future<void> _openPicker(BuildContext context) async {
    final selected = await showModalBottomSheet<Station>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) => _StationPickerSheet(
        label: label,
        stations: stations,
        selectedStation: selectedStation,
      ),
    );
    if (selected != null) onSelected(selected.id);
  }
}

class _StationPickerSheet extends StatefulWidget {
  const _StationPickerSheet({
    required this.label,
    required this.stations,
    required this.selectedStation,
  });

  final String label;
  final List<Station> stations;
  final Station? selectedStation;

  @override
  State<_StationPickerSheet> createState() => _StationPickerSheetState();
}

class _StationPickerSheetState extends State<_StationPickerSheet> {
  String _query = '';

  @override
  Widget build(BuildContext context) {
    final matchingStations = widget.stations
        .where(
          (station) =>
              station.name.toLowerCase().contains(_query.trim().toLowerCase()),
        )
        .toList(growable: false);
    final height = MediaQuery.sizeOf(context).height * 0.72;

    return SafeArea(
      child: SizedBox(
        height: height,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
              child: Text(
                'Choose ${widget.label} station',
                style: Theme.of(context).textTheme.titleLarge,
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: TextField(
                autofocus: true,
                onChanged: (value) => setState(() => _query = value),
                decoration: const InputDecoration(
                  labelText: 'Search stations',
                  prefixIcon: Icon(Icons.search),
                ),
              ),
            ),
            const SizedBox(height: 8),
            Expanded(
              child: matchingStations.isEmpty
                  ? Center(
                      child: Text(
                        widget.stations.isEmpty
                            ? 'No published stations available.'
                            : 'No matching stations.',
                      ),
                    )
                  : ListView.builder(
                      itemCount: matchingStations.length,
                      itemBuilder: (context, index) {
                        final station = matchingStations[index];
                        final details = [
                          ...?station.locationDetails,
                          ...?station.serviceDetails,
                        ];
                        final selected =
                            station.id == widget.selectedStation?.id;
                        return Semantics(
                          selected: selected,
                          button: true,
                          child: ListTile(
                            selected: selected,
                            title: Text(station.name),
                            subtitle: details.isEmpty
                                ? null
                                : Text(details.join(' · ')),
                            trailing: selected ? const Icon(Icons.check) : null,
                            onTap: () => Navigator.of(context).pop(station),
                          ),
                        );
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }
}
