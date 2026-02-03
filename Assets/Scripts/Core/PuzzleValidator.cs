using UnityEngine;
using System;
using System.Collections.Generic;

namespace Ubongo.Core
{
    /// <summary>
    /// Types of validation errors that can occur.
    /// </summary>
    public enum ValidationError
    {
        None,
        IncompleteFill,
        ExceedsHeight,
        OutOfBounds,
        Collision
    }

    /// <summary>
    /// Represents a validation error at a specific position.
    /// </summary>
    [Serializable]
    public struct ValidationErrorInfo
    {
        public ValidationError ErrorType;
        public Vector3Int Position;
        public string Message;

        public ValidationErrorInfo(ValidationError error, Vector3Int position, string message = null)
        {
            ErrorType = error;
            Position = position;
            Message = message ?? GetDefaultMessage(error);
        }

        private static string GetDefaultMessage(ValidationError error)
        {
            return error switch
            {
                ValidationError.IncompleteFill => "Cell not filled to required height",
                ValidationError.ExceedsHeight => "Block exceeds maximum height",
                ValidationError.OutOfBounds => "Block placed outside target area",
                ValidationError.Collision => "Block collision detected",
                _ => "Unknown error"
            };
        }
    }

    /// <summary>
    /// Result of puzzle validation.
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        private List<ValidationErrorInfo> errors;

        /// <summary>
        /// True if validation passed with no errors.
        /// </summary>
        public bool IsValid => errors == null || errors.Count == 0;

        /// <summary>
        /// True if the puzzle is completely solved.
        /// </summary>
        public bool IsSolved => IsValid;

        /// <summary>
        /// List of all validation errors.
        /// </summary>
        public IReadOnlyList<ValidationErrorInfo> Errors => errors ?? (IReadOnlyList<ValidationErrorInfo>)Array.Empty<ValidationErrorInfo>();

        /// <summary>
        /// Number of errors found.
        /// </summary>
        public int ErrorCount => errors?.Count ?? 0;

        /// <summary>
        /// Number of incomplete fill errors.
        /// </summary>
        public int IncompleteFillCount { get; private set; }

        /// <summary>
        /// Number of height exceeded errors.
        /// </summary>
        public int ExceedsHeightCount { get; private set; }

        /// <summary>
        /// Number of out of bounds errors.
        /// </summary>
        public int OutOfBoundsCount { get; private set; }

        public ValidationResult()
        {
            errors = new List<ValidationErrorInfo>();
            IncompleteFillCount = 0;
            ExceedsHeightCount = 0;
            OutOfBoundsCount = 0;
        }

        /// <summary>
        /// Adds an error to the validation result.
        /// </summary>
        public void AddError(ValidationError errorType, int x, int z, int y = 0)
        {
            AddError(errorType, new Vector3Int(x, y, z));
        }

        /// <summary>
        /// Adds an error to the validation result.
        /// </summary>
        public void AddError(ValidationError errorType, Vector3Int position, string message = null)
        {
            if (errors == null)
            {
                errors = new List<ValidationErrorInfo>();
            }

            errors.Add(new ValidationErrorInfo(errorType, position, message));

            switch (errorType)
            {
                case ValidationError.IncompleteFill:
                    IncompleteFillCount++;
                    break;
                case ValidationError.ExceedsHeight:
                    ExceedsHeightCount++;
                    break;
                case ValidationError.OutOfBounds:
                    OutOfBoundsCount++;
                    break;
            }
        }

        /// <summary>
        /// Checks if a specific error type exists.
        /// </summary>
        public bool HasError(ValidationError errorType)
        {
            if (errors == null)
            {
                return false;
            }

            foreach (var error in errors)
            {
                if (error.ErrorType == errorType)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all errors of a specific type.
        /// </summary>
        public IEnumerable<ValidationErrorInfo> GetErrors(ValidationError errorType)
        {
            if (errors == null)
            {
                yield break;
            }

            foreach (var error in errors)
            {
                if (error.ErrorType == errorType)
                {
                    yield return error;
                }
            }
        }

        /// <summary>
        /// Clears all errors.
        /// </summary>
        public void Clear()
        {
            errors?.Clear();
            IncompleteFillCount = 0;
            ExceedsHeightCount = 0;
            OutOfBoundsCount = 0;
        }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ValidationResult Success()
        {
            return new ValidationResult();
        }
    }

    /// <summary>
    /// Validates puzzle solutions according to Ubongo 3D rules.
    /// </summary>
    public class PuzzleValidator
    {
        private const int MaxHeight = 2;

        /// <summary>
        /// Validates a complete puzzle solution.
        /// Checks that all target area cells are filled exactly 2 layers high.
        /// </summary>
        /// <param name="occupancyGrid">3D array indicating occupied cells [x, y, z]</param>
        /// <param name="target">The target area that must be filled</param>
        /// <returns>Validation result with any errors found</returns>
        public ValidationResult ValidateSolution(bool[,,] occupancyGrid, TargetArea target)
        {
            var result = new ValidationResult();

            if (target == null)
            {
                result.AddError(ValidationError.OutOfBounds, Vector3Int.zero, "No target area defined");
                return result;
            }

            if (occupancyGrid == null)
            {
                result.AddError(ValidationError.IncompleteFill, Vector3Int.zero, "No occupancy grid provided");
                return result;
            }

            int gridWidth = occupancyGrid.GetLength(0);
            int gridHeight = occupancyGrid.GetLength(1);
            int gridDepth = occupancyGrid.GetLength(2);

            // Check each column in the target area
            foreach (var column in target.GetColumnPositions())
            {
                int x = column.x;
                int z = column.y;

                // Check if column is within grid bounds
                if (x < 0 || x >= gridWidth || z < 0 || z >= gridDepth)
                {
                    result.AddError(ValidationError.OutOfBounds, x, z, 0);
                    continue;
                }

                // Check layer 0 (y=0)
                bool layer0Occupied = gridHeight > 0 && occupancyGrid[x, 0, z];
                if (!layer0Occupied)
                {
                    result.AddError(ValidationError.IncompleteFill, new Vector3Int(x, 0, z),
                        $"Layer 0 at ({x}, {z}) is not filled");
                }

                // Check layer 1 (y=1)
                bool layer1Occupied = gridHeight > 1 && occupancyGrid[x, 1, z];
                if (!layer1Occupied)
                {
                    result.AddError(ValidationError.IncompleteFill, new Vector3Int(x, 1, z),
                        $"Layer 1 at ({x}, {z}) is not filled");
                }

                // Check no blocks above layer 1 (y >= 2)
                for (int y = MaxHeight; y < gridHeight; y++)
                {
                    if (occupancyGrid[x, y, z])
                    {
                        result.AddError(ValidationError.ExceedsHeight, new Vector3Int(x, y, z),
                            $"Block at ({x}, {y}, {z}) exceeds maximum height of {MaxHeight}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Validates a single piece placement.
        /// </summary>
        /// <param name="pieceBlocks">World positions of the piece's blocks</param>
        /// <param name="occupancyGrid">Current occupancy state</param>
        /// <param name="target">The target area</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidatePlacement(
            IEnumerable<Vector3Int> pieceBlocks,
            bool[,,] occupancyGrid,
            TargetArea target)
        {
            var result = new ValidationResult();

            if (pieceBlocks == null)
            {
                return result;
            }

            int gridWidth = occupancyGrid?.GetLength(0) ?? 0;
            int gridHeight = occupancyGrid?.GetLength(1) ?? 0;
            int gridDepth = occupancyGrid?.GetLength(2) ?? 0;

            foreach (var block in pieceBlocks)
            {
                // Check grid bounds
                if (block.x < 0 || block.x >= gridWidth ||
                    block.y < 0 || block.y >= gridHeight ||
                    block.z < 0 || block.z >= gridDepth)
                {
                    result.AddError(ValidationError.OutOfBounds, block,
                        $"Block at ({block.x}, {block.y}, {block.z}) is outside grid bounds");
                    continue;
                }

                // Check height constraint
                if (block.y >= MaxHeight)
                {
                    result.AddError(ValidationError.ExceedsHeight, block,
                        $"Block at ({block.x}, {block.y}, {block.z}) exceeds height limit of {MaxHeight}");
                }

                // Check target area
                if (target != null && !target.Contains(block.x, block.z))
                {
                    result.AddError(ValidationError.OutOfBounds, block,
                        $"Block at ({block.x}, {block.y}, {block.z}) is outside target area");
                }

                // Check collision
                if (occupancyGrid != null && occupancyGrid[block.x, block.y, block.z])
                {
                    result.AddError(ValidationError.Collision, block,
                        $"Block at ({block.x}, {block.y}, {block.z}) collides with existing block");
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the target area is completely filled with exactly 2 layers.
        /// </summary>
        public bool IsAreaCompletelyFilled(bool[,,] occupancyGrid, TargetArea target)
        {
            if (target == null || occupancyGrid == null)
            {
                return false;
            }

            foreach (var cell in target.GetAllCells())
            {
                if (cell.x < 0 || cell.x >= occupancyGrid.GetLength(0) ||
                    cell.y < 0 || cell.y >= occupancyGrid.GetLength(1) ||
                    cell.z < 0 || cell.z >= occupancyGrid.GetLength(2))
                {
                    return false;
                }

                if (!occupancyGrid[cell.x, cell.y, cell.z])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculates the current fill state of the target area.
        /// </summary>
        public FillState CalculateFillState(bool[,,] occupancyGrid, TargetArea target)
        {
            if (target == null || occupancyGrid == null)
            {
                return new FillState(0, 0, 0);
            }

            int layer0Filled = 0;
            int layer1Filled = 0;

            foreach (var column in target.GetColumnPositions())
            {
                int x = column.x;
                int z = column.y;

                if (x >= 0 && x < occupancyGrid.GetLength(0) &&
                    z >= 0 && z < occupancyGrid.GetLength(2))
                {
                    if (occupancyGrid.GetLength(1) > 0 && occupancyGrid[x, 0, z])
                    {
                        layer0Filled++;
                    }

                    if (occupancyGrid.GetLength(1) > 1 && occupancyGrid[x, 1, z])
                    {
                        layer1Filled++;
                    }
                }
            }

            return new FillState(layer0Filled, layer1Filled, target.TotalCells);
        }

        /// <summary>
        /// Checks if a piece can be placed at the given position without violating rules.
        /// </summary>
        public bool CanPlacePiece(
            Vector3Int[] pieceBlocks,
            Vector3Int position,
            bool[,,] occupancyGrid,
            TargetArea target)
        {
            if (pieceBlocks == null || occupancyGrid == null)
            {
                return false;
            }

            int gridWidth = occupancyGrid.GetLength(0);
            int gridHeight = occupancyGrid.GetLength(1);
            int gridDepth = occupancyGrid.GetLength(2);

            foreach (var block in pieceBlocks)
            {
                Vector3Int worldPos = position + block;

                // Check bounds
                if (worldPos.x < 0 || worldPos.x >= gridWidth ||
                    worldPos.y < 0 || worldPos.y >= gridHeight ||
                    worldPos.z < 0 || worldPos.z >= gridDepth)
                {
                    return false;
                }

                // Check height constraint
                if (worldPos.y >= MaxHeight)
                {
                    return false;
                }

                // Check target area
                if (target != null && !target.Contains(worldPos.x, worldPos.z))
                {
                    return false;
                }

                // Check collision
                if (occupancyGrid[worldPos.x, worldPos.y, worldPos.z])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if any blocks would protrude outside the target area.
        /// </summary>
        public bool CheckNoProtrusion(
            Vector3Int[] pieceBlocks,
            Vector3Int placement,
            TargetArea target)
        {
            if (pieceBlocks == null || target == null)
            {
                return false;
            }

            foreach (var block in pieceBlocks)
            {
                Vector3Int worldPos = placement + block;

                if (!target.Contains(worldPos.x, worldPos.z))
                {
                    return false;
                }

                if (worldPos.y < 0 || worldPos.y >= MaxHeight)
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Placement validity status enumeration.
    /// </summary>
    public enum PlacementValidity
    {
        Valid,
        Collision,
        OutOfBounds,
        OutsideTarget,
        HeightExceeded
    }
}
