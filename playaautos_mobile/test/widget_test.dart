import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'package:playaautos_mobile/main.dart';

void main() {
  testWidgets('App builds without throwing', (WidgetTester tester) async {
    await tester.pumpWidget(
      const ProviderScope(child: PlayaAutosApp()),
    );

    expect(find.byType(MaterialApp), findsOneWidget);
  });
}
