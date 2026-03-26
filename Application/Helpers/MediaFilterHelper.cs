namespace Core.Application.Helpers
{
    /// <summary>
    /// Defines the visual parameters for each image filter.
    /// These values match the frontend React Native FILTERS constants exactly.
    /// </summary>
    public class SkiaFilterDefinition
    {
        public string GradientStart { get; init; }
        public string GradientEnd { get; init; }
        public string TintColor { get; init; }
        public float Opacity { get; init; }
    }

    public static class MediaFilterHelper
    {
        /// <summary>
        /// Returns the SkiaSharp filter definition for the given filter name,
        /// or null for Original / unknown filters (no processing needed).
        /// </summary>
        public static SkiaFilterDefinition GetSkiaFilter(string filterType)
        {
            if (string.IsNullOrEmpty(filterType) || filterType == "Original")
                return null;

            return filterType switch
            {
                "Sunfade"  => new SkiaFilterDefinition { GradientStart = "#fceabb", GradientEnd = "#f8b500", TintColor = "#f8b500", Opacity = 0.2f },
                "Edge"     => new SkiaFilterDefinition { GradientStart = "#ffffff", GradientEnd = "#000000", TintColor = "#000000", Opacity = 0.3f },
                "Glow"     => new SkiaFilterDefinition { GradientStart = "#ffffff", GradientEnd = "#ffccaa", TintColor = "#ffccaa", Opacity = 0.4f },
                "Color"    => new SkiaFilterDefinition { GradientStart = "#ff8a00", GradientEnd = "#e52e71", TintColor = "#e52e71", Opacity = 0.45f },
                "Tone"     => new SkiaFilterDefinition { GradientStart = "#0f0f7f", GradientEnd = "#7f0f0f", TintColor = "#7f0f0f", Opacity = 0.4f },
                "Inverse"  => new SkiaFilterDefinition { GradientStart = "#000000", GradientEnd = "#ffffff", TintColor = "#888888", Opacity = 0.5f },
                "Mono"     => new SkiaFilterDefinition { GradientStart = "#bbbbbb", GradientEnd = "#dddddd", TintColor = "#aaaaaa", Opacity = 0.35f },
                "Nocturne" => new SkiaFilterDefinition { GradientStart = "#001122", GradientEnd = "#334455", TintColor = "#001122", Opacity = 0.5f },
                "Amber"    => new SkiaFilterDefinition { GradientStart = "#ffbf00", GradientEnd = "#ff8000", TintColor = "#ff8000", Opacity = 0.45f },
                "Frost"    => new SkiaFilterDefinition { GradientStart = "#99ccff", GradientEnd = "#ccffff", TintColor = "#66ccff", Opacity = 0.45f },
                "Dream"    => new SkiaFilterDefinition { GradientStart = "#ff99cc", GradientEnd = "#cc99ff", TintColor = "#cc99ff", Opacity = 0.45f },
                "Retro"    => new SkiaFilterDefinition { GradientStart = "#f4e2d8", GradientEnd = "#ba9d6f", TintColor = "#ba9d6f", Opacity = 0.35f },
                "Analog"   => new SkiaFilterDefinition { GradientStart = "#e0c3fc", GradientEnd = "#8ec5fc", TintColor = "#8ec5fc", Opacity = 0.4f },
                "Mocha"    => new SkiaFilterDefinition { GradientStart = "#5a3f37", GradientEnd = "#8d6e63", TintColor = "#5a3f37", Opacity = 0.5f },
                "Aged"     => new SkiaFilterDefinition { GradientStart = "#987654", GradientEnd = "#c0b283", TintColor = "#c0b283", Opacity = 0.35f },
                "Trippy"   => new SkiaFilterDefinition { GradientStart = "#ff00ff", GradientEnd = "#00ffff", TintColor = "#ff00ff", Opacity = 0.5f },
                "Crimson"  => new SkiaFilterDefinition { GradientStart = "#b22222", GradientEnd = "#8b0000", TintColor = "#8b0000", Opacity = 0.2f },
                "Lush"     => new SkiaFilterDefinition { GradientStart = "#228b22", GradientEnd = "#32cd32", TintColor = "#228b22", Opacity = 0.45f },
                "Mood"     => new SkiaFilterDefinition { GradientStart = "#666699", GradientEnd = "#9999cc", TintColor = "#666699", Opacity = 0.4f },
                _          => null
            };
        }
    }
}
