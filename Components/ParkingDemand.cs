/// <summary>
/// Keeps track of parking demand and the frame at which the cooldown countdown started
/// </summary>

using Colossal.Serialization.Entities;
using Unity.Entities;

namespace RealisticParking
{
    public struct ParkingDemand : IComponentData, IQueryTypeParameter, ISerializable
    {
        public short demand;
        public uint cooldownStartFrame;

        public ParkingDemand(short limitValue, uint cooldownIndex) 
        { 
            this.demand = limitValue; 
            this.cooldownStartFrame = cooldownIndex;
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(demand);
            writer.Write(cooldownStartFrame);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out demand);
            reader.Read(out cooldownStartFrame);
        }
    }
}
