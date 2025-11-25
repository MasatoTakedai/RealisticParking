/// <summary>
/// Keeps track of the garage vehicle count with demand applied
/// </summary>

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RealisticParking
{
    public struct GarageCount : IComponentData, IQueryTypeParameter, ISerializable
    {
        public int countWithDemand;

        public GarageCount(int countWithDemand) { this.countWithDemand = countWithDemand; }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(countWithDemand);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out countWithDemand);
        }
    }
}
