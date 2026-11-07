import 'package:flutter/material.dart';

/// PlayaAutos brand palette: cool "ocean" tones (negro/azulOscuro/teal) paired
/// with warm "sand" tones (taupe/arena/beigeClaro) — fitting a beachside car
/// dealership ("playa" = beach).
abstract final class AppColors {
  static const Color negro = Color(0xFF000101);
  static const Color azulOscuro = Color(0xFF0C2E3D);
  static const Color tealPetroleo = Color(0xFF186784);
  static const Color taupe = Color(0xFF9A7C65);
  static const Color arena = Color(0xFFC0A088);
  static const Color beigeClaro = Color(0xFFDAC6B6);

  /// Not part of the raw brand palette: a lightened tint of beigeClaro used
  /// as the light-theme page background so long content (catalog grids,
  /// forms) stays readable instead of sitting on a saturated brand color.
  static const Color offWhiteBeige = Color(0xFFFAF6F3);
}

class AppTheme {
  AppTheme._();

  static final ColorScheme _lightScheme = ColorScheme.fromSeed(
    seedColor: AppColors.tealPetroleo,
    brightness: Brightness.light,
  ).copyWith(
    primary: AppColors.tealPetroleo,
    onPrimary: Colors.white,
    primaryContainer: AppColors.azulOscuro,
    onPrimaryContainer: AppColors.beigeClaro,
    secondary: AppColors.taupe,
    onSecondary: AppColors.negro,
    secondaryContainer: AppColors.beigeClaro,
    onSecondaryContainer: AppColors.azulOscuro,
    tertiary: AppColors.azulOscuro,
    onTertiary: AppColors.beigeClaro,
    tertiaryContainer: AppColors.arena,
    onTertiaryContainer: AppColors.azulOscuro,
    surface: AppColors.offWhiteBeige,
    onSurface: AppColors.azulOscuro,
    outline: AppColors.taupe,
  );

  static final ColorScheme _darkScheme = ColorScheme.fromSeed(
    seedColor: AppColors.tealPetroleo,
    brightness: Brightness.dark,
  ).copyWith(
    primary: AppColors.tealPetroleo,
    onPrimary: Colors.white,
    primaryContainer: AppColors.azulOscuro,
    onPrimaryContainer: AppColors.beigeClaro,
    secondary: AppColors.arena,
    onSecondary: AppColors.negro,
    secondaryContainer: AppColors.taupe,
    onSecondaryContainer: AppColors.negro,
    tertiary: AppColors.beigeClaro,
    onTertiary: AppColors.azulOscuro,
    tertiaryContainer: AppColors.taupe,
    onTertiaryContainer: AppColors.negro,
    surface: AppColors.negro,
    onSurface: AppColors.beigeClaro,
    outline: AppColors.taupe,
  );

  static ThemeData get light => _themeFrom(_lightScheme, AppColors.azulOscuro);

  static ThemeData get dark => _themeFrom(_darkScheme, AppColors.negro);

  /// Radio usado en AppBar (esquinas inferiores), cards, inputs y botones
  /// para el look moderno y redondeado pedido.
  static const double roundedRadius = 22;

  static ThemeData _themeFrom(ColorScheme scheme, Color barColor) {
    final fillColor = Color.alphaBlend(
      scheme.onSurface.withValues(alpha: 0.05),
      scheme.surface,
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: scheme.surface,
      appBarTheme: AppBarTheme(
        backgroundColor: barColor,
        foregroundColor: AppColors.beigeClaro,
        elevation: 0,
        centerTitle: false,
        shape: const RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(bottom: Radius.circular(roundedRadius)),
        ),
      ),
      // NavigationBar (M3) en vez de BottomNavigationBar: la barra queda
      // rectangular a lo ancho completo (sin flotar/redondear, como pidió el
      // usuario después de ver la versión redondeada), pero el indicador de
      // pill brillante detrás del ítem activo la hace más "llamativa" que
      // antes en vez de quedar apagada.
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: barColor,
        indicatorColor: AppColors.tealPetroleo,
        surfaceTintColor: Colors.transparent,
        elevation: 8,
        labelTextStyle: WidgetStateProperty.resolveWith((states) {
          final selected = states.contains(WidgetState.selected);
          return TextStyle(
            fontSize: 11,
            fontWeight: selected ? FontWeight.bold : FontWeight.w600,
            color: selected ? Colors.white : AppColors.arena,
          );
        }),
        iconTheme: WidgetStateProperty.resolveWith((states) {
          final selected = states.contains(WidgetState.selected);
          return IconThemeData(
            size: 22,
            color: selected ? Colors.white : AppColors.arena,
          );
        }),
      ),
      cardTheme: CardThemeData(
        elevation: 3,
        shadowColor: scheme.onSurface.withValues(alpha: 0.18),
        surfaceTintColor: Colors.transparent,
        color: scheme.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(roundedRadius),
        ),
        clipBehavior: Clip.antiAlias,
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: fillColor,
        contentPadding: const EdgeInsets.symmetric(horizontal: 18, vertical: 16),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide.none,
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide(color: scheme.primary, width: 1.5),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide(color: scheme.error, width: 1.5),
        ),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.tealPetroleo,
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: AppColors.tealPetroleo,
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(foregroundColor: AppColors.tealPetroleo),
      ),
    );
  }
}
