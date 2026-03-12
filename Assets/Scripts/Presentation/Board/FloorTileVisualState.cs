namespace Ubongo
{
    public readonly struct FloorTileVisualState
    {
        public bool IsTarget { get; }
        public bool IsOccupied { get; }
        public FloorTileHighlightMode HighlightMode { get; }

        public FloorTileVisualState(bool isTarget, bool isOccupied, FloorTileHighlightMode highlightMode)
        {
            IsTarget = isTarget;
            IsOccupied = isOccupied;
            HighlightMode = highlightMode;
        }
    }
}
