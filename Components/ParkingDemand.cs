using Colossal.Serialization.Entities;
using Unity.Entities;


namespace RealisticParking
{
    public struct ParkingDemand : IComponentData, IQueryTypeParameter, ISerializable
    {
        public short demand;
        public uint cooldownIndex;

        public ParkingDemand(short limitValue, uint cooldownIndex) 
        { 
            this.demand = limitValue; 
            this.cooldownIndex = cooldownIndex;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(demand);
            writer.Write(cooldownIndex);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out demand);
            reader.Read(out cooldownIndex);
        }
    }
}
