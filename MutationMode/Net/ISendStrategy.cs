using System.IO;

namespace AdvancedMod.MutationMode.Net
{
    public interface ISendStrategy
    {
        void Send(object value, BinaryWriter writer);
    }
}