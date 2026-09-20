using Systemic.Engine.State;

public abstract class InteractiveProp : GameObject
{
    public bool IsBlocking { get; }

    protected InteractiveProp(
        string name,
        int x,
        int y,
        bool isBlocking)
        : base(name, x, y)
    {
        IsBlocking = isBlocking;
    }

    public abstract string Interact();
}

public class Chest : InteractiveProp
{
    public bool IsOpen { get; private set; }
    public RewardBundle Reward { get; }

    public Chest(int x, int y, RewardBundle? reward = null)
        : base("Chest", x, y, false)
    {
        Reward = reward ?? new RewardBundle();
    }

    public override string Interact()
    {
        if (IsOpen)
            return "The chest is empty.";

        IsOpen = true;
        return "You open the chest.";
    }
}

public class Terminal : InteractiveProp
{
    public bool IsActivated { get; private set; }
    public CoreReward CoreReward { get; }
    public int HealAmount { get; }

    public Terminal(
        int x,
        int y,
        CoreReward? coreReward = null,
        int healAmount = 8)
        : base("Energy Terminal", x, y, false)
    {
        CoreReward = coreReward ?? new CoreReward
        {
            Type = "Standard",
            Charge = 8,
            Quantity = 1
        };

        HealAmount = healAmount;
    }

    public override string Interact()
    {
        if (IsActivated)
            return "The energy terminal is already active.";

        IsActivated = true;
        return "The energy terminal hums to life.";
    }
}

public class Rubble : InteractiveProp
{
    public Rubble(int x, int y)
        : base("Rubble", x, y, true)
    {
    }

    public override string Interact() =>
        "The rubble blocks the way.";
}
