using Colossal.Serialization.Entities;
using Unity.Entities;


namespace RealisticParking
{
    public struct ParkingDemand : IComponentData, IQueryTypeParameter, ISerializable
    {
        public short demand;

        public ParkingDemand(short limitValue) { this.demand = limitValue; }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(demand);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out demand);
        }
    }
}
