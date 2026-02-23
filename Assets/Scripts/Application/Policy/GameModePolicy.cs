namespace Ubongo.Application.Policy
{
    public static class GameModePolicy
    {
        public static global::Ubongo.GameMode Normalize(global::Ubongo.GameMode requestedMode)
        {
            return requestedMode == global::Ubongo.GameMode.Multiplayer
                ? global::Ubongo.GameMode.Classic
                : requestedMode;
        }

        public static bool GetDefaultHintsEnabled(global::Ubongo.GameMode mode)
        {
            return mode == global::Ubongo.GameMode.Zen;
        }
    }
}
