public abstract class InteractiveProp : GameObject
{
    public bool IsBlocking { get; }
    public abstract AtlasProp Visual { get; }

    protected InteractiveProp(string name, int x, int y, bool isBlocking)
        : base(name, x, y)
    {
        IsBlocking = isBlocking;
    }

    public abstract string Interact();
}

public class Chest : InteractiveProp
{
    public bool IsOpen { get; private set; }
    public override AtlasProp Visual => IsOpen ? AtlasProp.ChestOpen : AtlasProp.ChestClosed;

    public Chest(int x, int y) : base("Chest", x, y, false) { }

    public override string Interact()
    {
        if (IsOpen)
        {
            return "The chest is empty.";
        }

        IsOpen = true;
        return "You open the chest.";
    }
}

public class Terminal : InteractiveProp
{
    public bool IsActivated { get; private set; }
    public override AtlasProp Visual => AtlasProp.EnergyTerminal;

    public Terminal(int x, int y) : base("Energy Terminal", x, y, false) { }

    public override string Interact()
    {
        IsActivated = true;
        return "The energy terminal hums to life.";
    }
}

public class Rubble : InteractiveProp
{
    public override AtlasProp Visual => AtlasProp.RubblePile;

    public Rubble(int x, int y) : base("Rubble", x, y, true) { }

    public override string Interact() => "The rubble blocks the way.";
}
