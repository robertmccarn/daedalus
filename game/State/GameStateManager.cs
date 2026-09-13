namespace Systemic.Engine.State;

public class GameStateManager
{
    public CampaignState Campaign { get; } = new();
    public ExpeditionState ActiveExpedition { get; private set; } = new();

    public void StartNewExpedition()
    {
        ActiveExpedition = new ExpeditionState
        {
            PlayerGridPosition = (26, 5)
        };
    }

    public void StartNewExpedition(
        int startX,
        int startY,
        int health,
        int maxHealth)
    {
        ActiveExpedition = new ExpeditionState
        {
            Health = health,
            MaxHealth = maxHealth,
            PlayerGridPosition = (startX, startY)
        };
    }

    public void SynchronizeExpedition(int x, int y, int health, int maxHealth)
    {
        ActiveExpedition.PlayerGridPosition = (x, y);
        ActiveExpedition.Health = health;
        ActiveExpedition.MaxHealth = maxHealth;
    }
}
