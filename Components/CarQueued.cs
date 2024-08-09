/// <summary>
/// Tag component for use on parking that has been pathfinded to by a personal car
/// </summary>

using Unity.Entities;

namespace RealisticParking
{
    public struct CarQueued : IComponentData, IQueryTypeParameter
    {
    }
}
