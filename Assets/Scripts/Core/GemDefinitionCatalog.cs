using System.Collections.Generic;
using UnityEngine;
using Ubongo.Domain;

namespace Ubongo
{
    /// <summary>
    /// Visual style metadata used by gem icon renderers.
    /// </summary>
    public readonly struct GemIconStyle
    {
        public Color HighlightColor { get; }
        public Color GradientStart { get; }
        public Color GradientEnd { get; }
        public string Description { get; }

        public GemIconStyle(Color highlightColor, Color gradientStart, Color gradientEnd, string description)
        {
            HighlightColor = highlightColor;
            GradientStart = gradientStart;
            GradientEnd = gradientEnd;
            Description = description;
        }
    }

    /// <summary>
    /// Single gem definition shared across systems (value, color, icon style).
    /// </summary>
    public readonly struct GemDefinition
    {
        public GemType Type { get; }
        public int PointValue { get; }
        public Color Color { get; }
        public GemIconStyle IconStyle { get; }

        public GemDefinition(GemType type, int pointValue, Color color, GemIconStyle iconStyle)
        {
            Type = type;
            PointValue = pointValue;
            Color = color;
            IconStyle = iconStyle;
        }
    }

    /// <summary>
    /// Central catalog for all GemType mapping rules.
    /// </summary>
    public static class GemDefinitionCatalog
    {
        private static readonly GemIconStyle UnknownIconStyle = new GemIconStyle(
            highlightColor: Color.white,
            gradientStart: Color.white,
            gradientEnd: Color.gray,
            description: "Unknown gem");

        private static readonly Dictionary<GemType, GemDefinition> Definitions = new Dictionary<GemType, GemDefinition>
        {
            [GemType.Ruby] = new GemDefinition(
                type: GemType.Ruby,
                pointValue: 4,
                color: new Color(227f / 255f, 28f / 255f, 61f / 255f, 1f),
                iconStyle: new GemIconStyle(
                    highlightColor: Color.white,
                    gradientStart: new Color(0.9f, 0.2f, 0.3f),
                    gradientEnd: new Color(0.7f, 0.1f, 0.2f),
                    description: "Red oval with white highlight, warm gradient")),
            [GemType.Sapphire] = new GemDefinition(
                type: GemType.Sapphire,
                pointValue: 3,
                color: new Color(26f / 255f, 95f / 255f, 180f / 255f, 1f),
                iconStyle: new GemIconStyle(
                    highlightColor: Color.white,
                    gradientStart: new Color(0.2f, 0.5f, 0.9f),
                    gradientEnd: new Color(0.1f, 0.3f, 0.7f),
                    description: "Blue circle with star highlight, cool gradient")),
            [GemType.Emerald] = new GemDefinition(
                type: GemType.Emerald,
                pointValue: 2,
                color: new Color(46f / 255f, 125f / 255f, 50f / 255f, 1f),
                iconStyle: new GemIconStyle(
                    highlightColor: Color.white,
                    gradientStart: new Color(0.2f, 0.6f, 0.3f),
                    gradientEnd: new Color(0.1f, 0.4f, 0.2f),
                    description: "Green rectangle with corner cuts, natural gradient")),
            [GemType.Amber] = new GemDefinition(
                type: GemType.Amber,
                pointValue: 1,
                color: new Color(255f / 255f, 179f / 255f, 0f / 255f, 1f),
                iconStyle: new GemIconStyle(
                    highlightColor: Color.white,
                    gradientStart: new Color(1f, 0.8f, 0.3f),
                    gradientEnd: new Color(0.9f, 0.6f, 0.1f),
                    description: "Orange rounded square, warm honey gradient"))
        };

        public static readonly Color[] AllColors = new Color[]
        {
            Definitions[GemType.Ruby].Color,
            Definitions[GemType.Sapphire].Color,
            Definitions[GemType.Emerald].Color,
            Definitions[GemType.Amber].Color
        };

        public static GemDefinition Get(GemType type)
        {
            if (Definitions.TryGetValue(type, out GemDefinition definition))
            {
                return definition;
            }

            return new GemDefinition(type, 0, Color.white, UnknownIconStyle);
        }

        public static int GetPointValue(GemType type)
        {
            return Get(type).PointValue;
        }

        public static Color GetColor(GemType type)
        {
            return Get(type).Color;
        }

        public static GemIconStyle GetIconStyle(GemType type)
        {
            return Get(type).IconStyle;
        }
    }
}
