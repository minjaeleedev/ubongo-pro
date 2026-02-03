using UnityEngine;

namespace Ubongo
{
    /// <summary>
    /// Ubongo 3D Pro 게임의 모든 색상 정의
    /// 디자인 요구사항 문서에 정의된 색상 팔레트 기반
    /// </summary>
    public static class GameColors
    {
        #region Block Colors (8 Primary Colors)

        /// <summary>
        /// 블록 색상 ID (1-8)
        /// </summary>
        public enum BlockColorId
        {
            SunsetOrange = 1,
            OceanBlue = 2,
            JungleGreen = 3,
            RoyalPurple = 4,
            SavannaYellow = 5,
            CoralPink = 6,
            Turquoise = 7,
            EarthBrown = 8
        }

        /// <summary>
        /// Sunset Orange - Warm, energetic orange (I-Block)
        /// </summary>
        public static readonly Color SunsetOrange = new Color(255f / 255f, 107f / 255f, 53f / 255f, 1f);

        /// <summary>
        /// Ocean Blue - Deep, calming blue (L-Block)
        /// </summary>
        public static readonly Color OceanBlue = new Color(46f / 255f, 134f / 255f, 171f / 255f, 1f);

        /// <summary>
        /// Jungle Green - Vibrant tropical green (T-Block)
        /// </summary>
        public static readonly Color JungleGreen = new Color(40f / 255f, 167f / 255f, 69f / 255f, 1f);

        /// <summary>
        /// Royal Purple - Rich, regal purple (Z-Block)
        /// </summary>
        public static readonly Color RoyalPurple = new Color(123f / 255f, 44f / 255f, 191f / 255f, 1f);

        /// <summary>
        /// Savanna Yellow - Bright, sunny yellow (S-Block)
        /// </summary>
        public static readonly Color SavannaYellow = new Color(255f / 255f, 217f / 255f, 61f / 255f, 1f);

        /// <summary>
        /// Coral Pink - Playful, warm pink (O-Block)
        /// </summary>
        public static readonly Color CoralPink = new Color(255f / 255f, 107f / 255f, 157f / 255f, 1f);

        /// <summary>
        /// Turquoise - Fresh, aquatic teal (J-Block)
        /// </summary>
        public static readonly Color Turquoise = new Color(23f / 255f, 162f / 255f, 184f / 255f, 1f);

        /// <summary>
        /// Earth Brown - Natural, grounded brown (Corner-Block)
        /// </summary>
        public static readonly Color EarthBrown = new Color(139f / 255f, 90f / 255f, 43f / 255f, 1f);

        /// <summary>
        /// 모든 블록 색상 배열 (인덱스 0부터 시작)
        /// </summary>
        public static readonly Color[] BlockColors = new Color[]
        {
            SunsetOrange,  // 0 - I-Block
            OceanBlue,     // 1 - L-Block
            JungleGreen,   // 2 - T-Block
            RoyalPurple,   // 3 - Z-Block
            SavannaYellow, // 4 - S-Block
            CoralPink,     // 5 - O-Block
            Turquoise,     // 6 - J-Block
            EarthBrown     // 7 - Corner-Block
        };

        /// <summary>
        /// 블록 ID로 색상 가져오기
        /// </summary>
        public static Color GetBlockColor(BlockColorId id)
        {
            int index = (int)id - 1;
            if (index >= 0 && index < BlockColors.Length)
            {
                return BlockColors[index];
            }
            return Color.white;
        }

        /// <summary>
        /// 인덱스로 블록 색상 가져오기 (0-7)
        /// </summary>
        public static Color GetBlockColorByIndex(int index)
        {
            if (index >= 0 && index < BlockColors.Length)
            {
                return BlockColors[index];
            }
            return Color.white;
        }

        #endregion

        #region UI Colors

        /// <summary>
        /// UI 색상 정의
        /// </summary>
        public static class UI
        {
            /// <summary>
            /// Primary Background - Warm Sand
            /// </summary>
            public static readonly Color PrimaryBackground = new Color(245f / 255f, 230f / 255f, 211f / 255f, 1f);

            /// <summary>
            /// Secondary Background - Deep Mahogany
            /// </summary>
            public static readonly Color SecondaryBackground = new Color(61f / 255f, 41f / 255f, 20f / 255f, 1f);

            /// <summary>
            /// Accent - Golden Sun
            /// </summary>
            public static readonly Color Accent = new Color(255f / 255f, 184f / 255f, 0f / 255f, 1f);

            /// <summary>
            /// Text Primary - Charcoal
            /// </summary>
            public static readonly Color TextPrimary = new Color(45f / 255f, 45f / 255f, 45f / 255f, 1f);

            /// <summary>
            /// Text Secondary - Warm Gray
            /// </summary>
            public static readonly Color TextSecondary = new Color(107f / 255f, 107f / 255f, 107f / 255f, 1f);

            /// <summary>
            /// Success - Safari Green
            /// </summary>
            public static readonly Color Success = new Color(76f / 255f, 175f / 255f, 80f / 255f, 1f);

            /// <summary>
            /// Warning - Amber Alert
            /// </summary>
            public static readonly Color Warning = new Color(255f / 255f, 152f / 255f, 0f / 255f, 1f);

            /// <summary>
            /// Error - Crimson
            /// </summary>
            public static readonly Color Error = new Color(220f / 255f, 53f / 255f, 69f / 255f, 1f);

            /// <summary>
            /// Timer Normal - Sky Blue
            /// </summary>
            public static readonly Color TimerNormal = new Color(79f / 255f, 195f / 255f, 247f / 255f, 1f);

            /// <summary>
            /// Timer Warning - Warning Orange
            /// </summary>
            public static readonly Color TimerWarning = new Color(255f / 255f, 112f / 255f, 67f / 255f, 1f);

            /// <summary>
            /// Timer Critical - Alert Red
            /// </summary>
            public static readonly Color TimerCritical = new Color(239f / 255f, 83f / 255f, 80f / 255f, 1f);
        }

        #endregion

        #region Gem Colors

        /// <summary>
        /// 보석 타입
        /// </summary>
        public enum GemType
        {
            Ruby,
            Sapphire,
            Emerald,
            Amber
        }

        /// <summary>
        /// 보석 색상 정의
        /// </summary>
        public static class Gems
        {
            /// <summary>
            /// Ruby - Crimson Red (Oval Cut, 16 facets)
            /// </summary>
            public static readonly Color Ruby = new Color(227f / 255f, 28f / 255f, 61f / 255f, 1f);

            /// <summary>
            /// Sapphire - Royal Blue (Round Brilliant, 24 facets)
            /// </summary>
            public static readonly Color Sapphire = new Color(26f / 255f, 95f / 255f, 180f / 255f, 1f);

            /// <summary>
            /// Emerald - Forest Green (Emerald Cut, 12 facets)
            /// </summary>
            public static readonly Color Emerald = new Color(46f / 255f, 125f / 255f, 50f / 255f, 1f);

            /// <summary>
            /// Amber - Golden Orange (Cushion Cut, 18 facets)
            /// </summary>
            public static readonly Color Amber = new Color(255f / 255f, 179f / 255f, 0f / 255f, 1f);

            /// <summary>
            /// 모든 보석 색상 배열
            /// </summary>
            public static readonly Color[] AllGemColors = new Color[]
            {
                Ruby,
                Sapphire,
                Emerald,
                Amber
            };

            /// <summary>
            /// 보석 타입으로 색상 가져오기
            /// </summary>
            public static Color GetGemColor(GemType type)
            {
                return type switch
                {
                    GemType.Ruby => Ruby,
                    GemType.Sapphire => Sapphire,
                    GemType.Emerald => Emerald,
                    GemType.Amber => Amber,
                    _ => Color.white
                };
            }
        }

        #endregion

        #region Board Colors

        /// <summary>
        /// 게임 보드 관련 색상
        /// </summary>
        public static class Board
        {
            /// <summary>
            /// Grid Line Color
            /// </summary>
            public static readonly Color GridLine = new Color(74f / 255f, 74f / 255f, 74f / 255f, 1f);

            /// <summary>
            /// Grid Background - Light Tan
            /// </summary>
            public static readonly Color GridBackground = new Color(232f / 255f, 220f / 255f, 200f / 255f, 1f);

            /// <summary>
            /// Target Outline - Golden Sun
            /// </summary>
            public static readonly Color TargetOutline = new Color(255f / 255f, 184f / 255f, 0f / 255f, 1f);

            /// <summary>
            /// Valid Placement - Safari Green at 30%
            /// </summary>
            public static readonly Color ValidPlacement = new Color(76f / 255f, 175f / 255f, 80f / 255f, 0.3f);

            /// <summary>
            /// Invalid Placement - Error at 50%
            /// </summary>
            public static readonly Color InvalidPlacement = new Color(220f / 255f, 53f / 255f, 69f / 255f, 0.5f);

            /// <summary>
            /// Layer 1 (Bottom) - Tan
            /// </summary>
            public static readonly Color Layer1 = new Color(139f / 255f, 115f / 255f, 85f / 255f, 1f);

            /// <summary>
            /// Layer 2 (Middle) - Light Tan
            /// </summary>
            public static readonly Color Layer2 = new Color(160f / 255f, 137f / 255f, 110f / 255f, 1f);

            /// <summary>
            /// Layer 3 (Top) - Cream
            /// </summary>
            public static readonly Color Layer3 = new Color(184f / 255f, 165f / 255f, 136f / 255f, 1f);
        }

        #endregion

        #region Color Utility Methods

        /// <summary>
        /// 색상의 밝기를 조정 (+값은 밝게, -값은 어둡게)
        /// </summary>
        public static Color AdjustBrightness(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a
            );
        }

        /// <summary>
        /// 색상의 채도를 조정 (0-1, 1이 원본)
        /// </summary>
        public static Color AdjustSaturation(Color color, float saturation)
        {
            float gray = 0.2989f * color.r + 0.5870f * color.g + 0.1140f * color.b;
            return new Color(
                Mathf.Lerp(gray, color.r, saturation),
                Mathf.Lerp(gray, color.g, saturation),
                Mathf.Lerp(gray, color.b, saturation),
                color.a
            );
        }

        /// <summary>
        /// 색상에 투명도 적용
        /// </summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// 10% 채도 감소된 블록 Albedo 색상 (머티리얼용)
        /// </summary>
        public static Color GetBlockAlbedoColor(Color baseColor)
        {
            return AdjustSaturation(baseColor, 0.9f);
        }

        /// <summary>
        /// 호버 상태 색상 (+15% 밝기)
        /// </summary>
        public static Color GetHoverColor(Color baseColor)
        {
            return AdjustBrightness(baseColor, 0.15f);
        }

        /// <summary>
        /// 배치됨 상태 색상 (-5% 채도, 0.95 알파)
        /// </summary>
        public static Color GetPlacedColor(Color baseColor)
        {
            Color desaturated = AdjustSaturation(baseColor, 0.95f);
            return WithAlpha(desaturated, 0.95f);
        }

        /// <summary>
        /// 무효 상태 색상 (빨간 틴트, 50% 투명)
        /// </summary>
        public static Color GetInvalidColor(Color baseColor)
        {
            Color tinted = Color.Lerp(baseColor, UI.Error, 0.5f);
            return WithAlpha(tinted, 0.5f);
        }

        /// <summary>
        /// 잠김 상태 색상 (그레이스케일, 70% 투명)
        /// </summary>
        public static Color GetLockedColor(Color baseColor)
        {
            Color grayscale = AdjustSaturation(baseColor, 0f);
            return WithAlpha(grayscale, 0.7f);
        }

        #endregion

        #region Debug Panel Colors

        /// <summary>
        /// 디버그 패널 색상
        /// </summary>
        public static class Debug
        {
            /// <summary>
            /// Debug Panel Background (80% opacity black)
            /// </summary>
            public static readonly Color PanelBackground = new Color(0f, 0f, 0f, 0.8f);

            /// <summary>
            /// Debug Text Color (Terminal Green)
            /// </summary>
            public static readonly Color TerminalGreen = new Color(0f, 1f, 0f, 1f);

            /// <summary>
            /// Debug Warning Color
            /// </summary>
            public static readonly Color DebugWarning = new Color(1f, 1f, 0f, 1f);

            /// <summary>
            /// Debug Error Color
            /// </summary>
            public static readonly Color DebugError = new Color(1f, 0.3f, 0.3f, 1f);
        }

        #endregion
    }
}
