namespace Ubongo.Application.Formatting
{
    public static class DifficultyDisplayFormatter
    {
        public static string Format(string displayName, int pieceCount)
        {
            return $"{displayName} ({pieceCount} pieces)";
        }
    }
}
