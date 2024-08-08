using Colossal.Serialization.Entities;
using Unity.Entities;


namespace RealisticParking
{
    public struct ParkingDemand : IComponentData, IQueryTypeParameter, ISerializable
    {
        public short demand;
        public uint cooldownStartIndex;

        public ParkingDemand(short limitValue, uint cooldownIndex) 
        { 
            this.demand = limitValue; 
            this.cooldownStartIndex = cooldownIndex;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(demand);
            writer.Write(cooldownStartIndex);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out demand);
            reader.Read(out cooldownStartIndex);
        }
    }
}
