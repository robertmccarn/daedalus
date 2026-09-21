using Systemic.Engine.State;

public static class ProgressionSystem
{
    public static bool ApplyExperience(PartyMember member, int experience)
    {
        if (experience <= 0)
            return false;

        member.Experience += experience;
        bool leveled = false;
        int required = ExperienceForNextLevel(member.Level);

        while (member.Experience >= required)
        {
            member.Experience -= required;
            member.Level++;
            member.MaxHP += 4;
            member.HP = member.MaxHP;
            member.MaxMP += 1;
            member.MP = member.MaxMP;
            member.Stats.Strength++;
            member.Stats.Agility++;
            leveled = true;
            required = ExperienceForNextLevel(member.Level);
        }

        return leveled;
    }

    public static int ExperienceForNextLevel(int level) =>
        20 + Math.Max(0, level - 1) * 15;
}
