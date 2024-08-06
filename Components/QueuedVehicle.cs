using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RealisticParking
{
    [InternalBufferCapacity(0)]
    public struct QueuedVehicle : IBufferElementData, ISerializable
    {
        public Entity vehicle;

        public QueuedVehicle(Entity vehicle) { this.vehicle = vehicle; }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(vehicle);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out vehicle);
        }
    }
}
