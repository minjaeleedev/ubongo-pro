using Ubongo.Core;

namespace Ubongo.Domain.Board
{
    public class BoardWinConditionService
    {
        private readonly PuzzleValidator validator;
        private bool[,,] occupancyBuffer;

        public BoardWinConditionService()
        {
            validator = new PuzzleValidator();
        }

        public ValidationResult ValidateSolution(BoardState state, TargetArea targetArea)
        {
            bool[,,] snapshot = GetSnapshot(state);
            return validator.ValidateSolution(snapshot, targetArea);
        }

        public FillState CalculateFillState(BoardState state, TargetArea targetArea)
        {
            bool[,,] snapshot = GetSnapshot(state);
            return validator.CalculateFillState(snapshot, targetArea);
        }

        private bool[,,] GetSnapshot(BoardState state)
        {
            if (state == null)
            {
                return null;
            }

            EnsureBufferSize(state.Width, state.Height, state.Depth);
            state.CopyOccupancyTo(occupancyBuffer);
            return occupancyBuffer;
        }

        private void EnsureBufferSize(int width, int height, int depth)
        {
            if (occupancyBuffer != null &&
                occupancyBuffer.GetLength(0) == width &&
                occupancyBuffer.GetLength(1) == height &&
                occupancyBuffer.GetLength(2) == depth)
            {
                return;
            }

            occupancyBuffer = new bool[width, height, depth];
        }
    }
}
