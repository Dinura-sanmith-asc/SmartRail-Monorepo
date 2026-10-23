import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:uuid/uuid.dart';

import '../../../app/router.dart';
import '../../authentication/application/app_controller.dart';

class ObservationScreen extends ConsumerStatefulWidget {
  const ObservationScreen({super.key, required this.journeyId});
  final String journeyId;
  @override
  ConsumerState<ObservationScreen> createState() => _ObservationScreenState();
}

class _ObservationScreenState extends ConsumerState<ObservationScreen> {
  final _form = GlobalKey<FormState>();
  final _text = TextEditingController();
  bool _sent = false;
  String? _requestId;
  String? _requestPayload;
  @override
  void dispose() {
    _text.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final s = ref.watch(appControllerProvider);
    return pageShell(
      'Report observation',
      ListView(
        padding: const EdgeInsets.all(20),
        children: [
          Container(
            padding: const EdgeInsets.all(15),
            decoration: BoxDecoration(
              color: const Color(0xffe8f2fc),
              borderRadius: BorderRadius.circular(14),
            ),
            child: const Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(Icons.info_outline, color: Color(0xff1769aa)),
                SizedBox(width: 10),
                Expanded(
                  child: Text(
                    'Your report is sent to Control for review. It does not publish or change official journey status.',
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 22),
          if (s.success != null && _sent)
            Card(
              color: const Color(0xffe8f6ee),
              child: Padding(
                padding: const EdgeInsets.all(18),
                child: Column(
                  children: [
                    const Icon(
                      Icons.check_circle_outline,
                      color: Color(0xff18794e),
                      size: 42,
                    ),
                    const SizedBox(height: 10),
                    Text(s.success!, textAlign: TextAlign.center),
                    if (s.observation != null) ...[
                      const SizedBox(height: 8),
                      Text(
                        'Reference ${s.observation!.id}',
                        style: const TextStyle(
                          fontSize: 14,
                          color: Color(0xff526b83),
                        ),
                      ),
                      Text(
                        'Received ${DateFormat('d MMM yyyy, HH:mm').format(s.observation!.reportedAtUtc.toLocal())}',
                        style: const TextStyle(
                          fontSize: 14,
                          color: Color(0xff526b83),
                        ),
                      ),
                    ],
                    const SizedBox(height: 12),
                    FilledButton(
                      onPressed: () => context.pop(),
                      child: const Text('Done'),
                    ),
                  ],
                ),
              ),
            )
          else
            Form(
              key: _form,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Observation',
                    style: TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 9),
                  TextFormField(
                    controller: _text,
                    minLines: 5,
                    maxLines: 9,
                    maxLength: 4000,
                    textCapitalization: TextCapitalization.sentences,
                    decoration: const InputDecoration(
                      hintText: 'Describe what you observed…',
                      alignLabelWithHint: true,
                    ),
                    validator: (value) {
                      final v = value?.trim() ?? '';
                      if (v.isEmpty) return 'Enter an observation.';
                      if (v.length > 4000) {
                        return 'Keep the observation under 4,000 characters.';
                      }
                      return null;
                    },
                  ),
                  if (s.error != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Text(
                        s.error!,
                        style: const TextStyle(color: Color(0xffb42318)),
                      ),
                    ),
                  SizedBox(
                    width: double.infinity,
                    height: 52,
                    child: FilledButton.icon(
                      onPressed: s.loading
                          ? null
                          : () async {
                              if (!_form.currentState!.validate()) {
                                return;
                              }
                              final payload = _text.text.trim();
                              if (_requestPayload != payload) {
                                _requestPayload = payload;
                                _requestId = const Uuid().v4();
                              }
                              await ref
                                  .read(appControllerProvider.notifier)
                                  .submitObservation(
                                    widget.journeyId,
                                    _requestId!,
                                    payload,
                                  );
                              if (mounted &&
                                  ref.read(appControllerProvider).success !=
                                      null) {
                                setState(() => _sent = true);
                              }
                            },
                      icon: s.loading
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : const Icon(Icons.send_rounded),
                      label: Text(s.loading ? 'Sending…' : 'Send for review'),
                    ),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
