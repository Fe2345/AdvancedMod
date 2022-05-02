using System.IO;

namespace AdvancedMod.MutationMode.Net
{
    public interface IRecieveStrategy
    {
        void Recieve(ref object value, BinaryReader reader);
    }
}