using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RealisticParking
{
    public struct GarageCount : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int actualCount;

        public GarageCount(int actualCount) { this.actualCount = actualCount; }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(actualCount);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out actualCount);
        }
    }
}
