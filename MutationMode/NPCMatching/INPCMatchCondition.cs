namespace AdvancedMod.MutationMode.NPCMatching
{
    public interface INPCMatchCondition
    {
        bool Satisfies(int type);
    }
}