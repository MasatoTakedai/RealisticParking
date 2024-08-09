/// <summary>
/// Keeps track of parking entity that personal cars are trying to navigate to.  Not needed for actual gameplay but useful for testing
/// </summary>

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RealisticParking
{
    public struct ParkingTarget : IComponentData, IQueryTypeParameter, ISerializable
    {
        public Entity target;

        public ParkingTarget(Entity target) { this.target = target; }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(target);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out target);
        }
    }
}
