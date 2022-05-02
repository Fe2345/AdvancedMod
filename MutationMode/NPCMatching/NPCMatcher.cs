using AdvancedMod.MutationMode.NPCMatching.Conditions;
using System.Collections.Generic;

namespace AdvancedMod.MutationMode.NPCMatching
{
    public class NPCMatcher
    {
        public List<INPCMatchCondition> Conditions;

        public NPCMatcher()
        {
            Conditions = new List<INPCMatchCondition>();
            // So that empty matches match everything
            Conditions.Add(new MatchEverythingCondition());
        }

        public NPCMatcher MatchType(int type)
        {
            Conditions.Add(new MatchTypeCondition(type));
            return this;
        }

        public NPCMatcher MatchTypeRange(params int[] types)
        {
            Conditions.Add(new MatchTypeRangeCondition(types));
            return this;
        }

        public bool Satisfies(int type) => Conditions.TrueForAll(condition => condition.Satisfies(type));
    }
}