using Ubongo.Systems;

namespace Ubongo.Application.Formatting
{
    public static class DifficultyDisplayFormatter
    {
        public static string Format(DifficultyConfig config)
        {
            return $"{config.DisplayName} ({config.PieceCount} pieces)";
        }
    }
}
