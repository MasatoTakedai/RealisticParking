using Colossal.Serialization.Entities;
using Unity.Entities;


namespace RealisticParking
{
    public struct ParkingPathfindLimit : IComponentData, IQueryTypeParameter, ISerializable
    {
        public short limitValue;

        public ParkingPathfindLimit(short limitValue) { this.limitValue = limitValue; }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(limitValue);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out limitValue);
        }
    }
}
