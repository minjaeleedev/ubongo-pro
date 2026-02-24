using Ubongo.Domain;

namespace Ubongo.Application.Policy
{
    public static class GameModePolicy
    {
        public static GameMode Normalize(GameMode requestedMode)
        {
            return requestedMode == GameMode.Multiplayer
                ? GameMode.Classic
                : requestedMode;
        }

        public static bool GetDefaultHintsEnabled(GameMode mode)
        {
            return mode == GameMode.Zen;
        }
    }
}
