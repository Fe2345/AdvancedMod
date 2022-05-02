using System.IO;

namespace AdvancedMod.MutationMode.Net.Strategies
{
    public static class IntStrategies
    {
        public static CompoundStrategy CompoundStrategy = new CompoundStrategy(new SendInt(), new RecieveInt());

        public class SendInt : ISendStrategy
        {
            public void Send(object value, BinaryWriter writer)
            {
                writer.Write((int)value);
            }
        }

        public class RecieveInt : IRecieveStrategy
        {
            public void Recieve(ref object value, BinaryReader writer)
            {
                value = writer.ReadInt32();
            }
        }
    }
}