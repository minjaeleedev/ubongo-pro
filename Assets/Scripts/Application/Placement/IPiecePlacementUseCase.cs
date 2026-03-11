namespace Ubongo.Application.Placement
{
    public interface IPiecePlacementUseCase
    {
        PlacementResult Preview(PlacementRequest request);

        PlacementResult Place(PlacementRequest request);

        bool Remove(string pieceId, out PlacementResult result);
    }
}
