import 'package:flutter/material.dart';

/// نظام الألوان الديناميكي للتطبيق - Dynamic Color System
/// ألوان تطبيق أبحر (Abher)
class AppColors {
  AppColors._();

  // ==================== ABHER BRAND COLORS ====================
  // Primary teal: #2EC5D7
  // Dark navy: #174862
  // Accent grey: #7D7D7D

  static final ColorScheme lightColorScheme = ColorScheme.fromSeed(
    seedColor: const Color(0xFF076453),
    primary: const Color(0xFF076453),
    onPrimary: Colors.white,
    secondary: const Color(0xFF526069),
    onSecondary: Colors.white,
    surface: const Color(0xFFF8F9FA),
    onSurface: const Color(0xFF191C1D),
    surfaceContainerHighest: const Color(0xFFE1E3E4),
    onSurfaceVariant: const Color(0xFF3F4945),
    outline: const Color(0xFF6F7975),
    error: const Color(0xFFBA1A1A),
    onError: Colors.white,
    brightness: Brightness.light,
  );

  static final ColorScheme darkColorScheme = ColorScheme.fromSeed(
    seedColor: const Color(0xFF076453),
    primary: const Color(0xFF88D5C0), // Dark mode primary from CSS
    onPrimary: const Color(0xFF00382D),
    secondary: const Color(0xFFB6C9D7), // secondary fixed dim approximation
    onSecondary: const Color(0xFF24323A),
    surface: const Color(0xFF0F1412),
    onSurface: const Color(0xFFDFE4E1),
    surfaceContainerHighest: const Color(0xFF2C2C2C),
    onSurfaceVariant: const Color(0xFFBFC9C4),
    outline: const Color(0xFF89938F),
    error: const Color(0xFFFFB4AB),
    onError: const Color(0xFF690005),
    brightness: Brightness.dark,
  );

  // ==================== DYNAMIC ACCESSORS (STITCH DESIGN SYSTEM) ====================

  static ColorScheme get _currentScheme {
    return lightColorScheme;
  }

  static Color get primary => _currentScheme.primary;
  static Color get onPrimary => _currentScheme.onPrimary;
  static Color get secondary => _currentScheme.secondary;
  static Color get onSecondary => _currentScheme.onSecondary;
  static Color get background => _currentScheme.surface;
  static Color get surface => _currentScheme.surface;
  static Color get onSurface => _currentScheme.onSurface;
  static Color get error => _currentScheme.error;
  static Color get border => _currentScheme.outline;
  static Color get surfaceVariant => _currentScheme.surfaceContainerHighest;

  // ==================== STITCH COLORS (MINIMALIST CLINIC LOCATOR) ====================
  static const Color stitchPrimary = Color(0xFF076453);
  static const Color stitchSurface = Color(0xFFF8F9FA);
  static const Color stitchPrimaryFixed = Color(0xFFA4F2DB);
  static const Color stitchPrimaryContainer = Color(0xFF2E7D6B);
  static const Color stitchOnPrimary = Color(0xFFFFFFFF);
  static const Color stitchSecondary = Color(0xFF526069);
  static const Color stitchTertiary = Color(0xFF00671A);
  static const Color stitchTertiaryContainer = Color(0xFF178229);
  static const Color stitchOnTertiaryContainer = Color(0xFFDEFFD6);
  static const Color stitchSurfaceLow = Color(0xFFF3F4F5);
  static const Color stitchSurfaceLowest = Color(0xFFFFFFFF);
  static const Color stitchSurfaceBright = Color(0xFFF8FAFB);

  // ==================== ABHER FIXED COLORS (MAPPED TO STITCH TO PREVENT CONFLICTS) ====================

  /// Primary teal -> Replaced by Minimalist primary (Dark Green)
  static const Color primaryTeal = stitchPrimary;

  /// Dark navy -> Replaced by Minimalist secondary
  static const Color primaryNavy = stitchSecondary;

  /// Off-white background
  static const Color offWhite = stitchSurface;

  /// Light white background
  static const Color lightWhite = stitchSurface;

  /// Accent grey
  static const Color accentGrey = Color(0xFF7D7D7D);

  /// border grey
  static const Color borderGrey = Color(0xFFDADADA);

  /// rate
  static const Color rate = Color(0xFFD6990A);
  static const Color rateText = Color(0xFF919191);

  /// anchors
  static const Color anchorsText = Color(0xFF949494);
  static const Color anchorsIcon = Color(0xFFC9C9C9);

  /// radio
  static const Color radio = Color(0xFFCACACA);
  static const Color buttonDisabled = Color(0xFFA3A3A3);

  static const Color spicalColor = Color(0xFFEF8801);
  static const Color discountText = Color(0xFFB7B7B7);
  static const Color discountColor = Color(0xff00C907);
  static const Color genderIcon = Color(0xffC4C4C4);
  static const Color editableColor = Color(0xff3CA500);
  static const Color notEditableColor = Color(0xffE3065F);
  static const Color iconTextField = Color(0xfff6f6f6);

  static const Color chatBackground = stitchSurface;
  static const Color chatTimeText = Color(0xffBCBDBE);
  static const Color chatUserNameText = Color(0xffA9A9A9);

  /// Chat bubble sent by current user
  static const Color chatBubbleMe = stitchPrimaryContainer;

  /// Chat bubble received from other user
  static const Color chatBubbleOther = Color(0xFFF5F5F5);

  /// Chat input border color
  static const Color chatInputBorder = Color(0xFFE0E0E0);

  /// Chat timestamp text color
  static const Color chatTimestamp = Color(0xFF9E9E9E);

  /// Chat waveform inactive color
  static const Color chatWaveformInactive = Color(0xFF9E9E9E);

  static const Color shadowColor = Color(0xffA6A6A6);

  /// more
  static const Color more = Color(0xffC2C2C2);

  ///my_order
  static const Color myOrderSubTabIndicator = Color(0xffEFEFEF);

  static const Color white = Colors.white;
  static const Color black = Colors.black;
  static const Color transparent = Colors.transparent;

  // ==================== GREY PALETTE (from Android) ====================
  static const Color grey1 = Color(0xFFE6E6E6);
  static const Color grey2 = Color(0xFFCCCCCC);
  static const Color grey3 = Color(0xFFB3B3B3);
  static const Color grey4 = Color(0xFF999999);
  static const Color grey5 = Color(0xFF808080);
  static const Color grey6 = Color(0xFF666666);
  static const Color grey7 = Color(0xFF4D4D4D);
  static const Color grey8 = Color(0xFF333333);
  static const Color grey9 = Color(0xFF1A1A1A);

  // General grey shortcuts
  static const Color grey100 = Color(0xFFF5F5F5);
  static const Color grey200 = Color(0xFFEEEEEE);
  static const Color grey300 = Color(0xFFE0E0E0);
  static const Color grey400 = Color(0xFFBDBDBD);
  static const Color grey500 = Color(0xFF9E9E9E);
  static const Color grey600 = Color(0xFF757575);
  static const Color grey700 = Color(0xFF616161);
  static const Color grey800 = Color(0xFF424242);
  static const Color grey900 = Color(0xFF212121);

  // ==================== SEMANTIC COLORS ====================
  static const Color success = Color(0xFF4CAF50);
  static const Color warning = Color(0xFFFF9800);
  static const Color info = Color(0xFF2196F3);
  static const Color errorColor = Color(0xFFFF0000);

  static const Color textPrimary = Color(0xFF252525);
  static const Color textSecondary = Color(0xFF7C7D7E);
  static const Color textHint = Color(0xFF939598);

  static const Color divider = Color(0xFFDCDBDB);
  static const Color inputBorder = Color(0xFF707070);
  static const Color cardBorder = Color(0xFFD9E0E5);

  static const Color shimmerBase = Color(0xFFE0E0E0);
  static const Color shimmerHighlight = Color(0xFFF5F5F5);

  // ==================== TRANSPARENT VARIANTS ====================
  static const Color primaryTeal10 = Color(0x1A2EC5D7);
  static const Color primaryTeal20 = Color(0x332EC5D7);
  static const Color black10 = Color(0x1A000000);
  static const Color black30 = Color(0x4D000000);
  static const Color black50 = Color(0x80000000);

  // ==================== DYNAMIC ACCESSORS (WITH CONTEXT) ====================

  static Color primaryOf(BuildContext context) =>
      Theme.of(context).colorScheme.primary;
  static Color secondaryOf(BuildContext context) =>
      Theme.of(context).colorScheme.secondary;
}

/// تسهيل الوصول للألوان عبر Context
extension AppColorsX on BuildContext {
  ColorScheme get colorScheme => Theme.of(this).colorScheme;

  Color get primaryColor => colorScheme.primary;
  Color get onPrimaryColor => colorScheme.onPrimary;
  Color get secondaryColor => colorScheme.secondary;
  Color get onSecondaryColor => colorScheme.onSecondary;
  Color get backgroundColor => colorScheme.surface;
  Color get surfaceColor => colorScheme.surface;
  Color get onSurfaceColor => colorScheme.onSurface;
  Color get errorColor => colorScheme.error;
  Color get borderColor => colorScheme.outline;
}
