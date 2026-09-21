using System.Drawing;
using System.Windows.Forms;
using Systemic.Engine.State;

public class GameWindow : Form
{
    private readonly GameSession session;
    private readonly GameRenderer renderer;
    private readonly System.Windows.Forms.Timer renderTimer;

    public GameWindow(GameWorld world)
    {
        session = new GameSession(world);
        renderer = new GameRenderer(world);
        renderTimer = new System.Windows.Forms.Timer { Interval = 80 };

        Text = "Daedalus";
        ClientSize = new Size(1100, 700);
        BackColor = Color.Black;
        ForeColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        DoubleBuffered = true;

        Paint += DrawGame;
        KeyDown += HandleKeyDown;
        renderTimer.Tick += (_, _) =>
        {
            session.Party.AdvanceAnimation();
            Invalidate();
        };
        renderTimer.Start();
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        switch (session.State)
        {
            case GameState.Battle:
                HandleBattleInput(e);
                break;
            case GameState.ExtractionResults:
                if (e.KeyCode == Keys.Enter) session.ReturnToCampaign();
                break;
            case GameState.Campaign:
                if (e.KeyCode == Keys.Enter) session.StartNewExpeditionFromCampaign();
                break;
            case GameState.GameOver:
                if (e.KeyCode == Keys.Enter) session.StartNewExpeditionFromCampaign();
                break;
            case GameState.Exploration:
                HandleExplorationInput(e);
                break;
        }
        Invalidate();
    }

    private void HandleExplorationInput(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.C)
        {
            ShowStats();
            return;
        }

        if (e.KeyCode == Keys.X)
        {
            session.ExtractExpedition();
            return;
        }

        if (e.KeyCode == Keys.E)
        {
            session.Interact();
            return;
        }

        CharacterDirection? direction = e.KeyCode switch
        {
            Keys.W => CharacterDirection.Up,
            Keys.S => CharacterDirection.Down,
            Keys.A => CharacterDirection.Left,
            Keys.D => CharacterDirection.Right,
            _ => null
        };

        if (direction.HasValue)
            session.MoveLeader(direction.Value);
    }

    private void HandleBattleInput(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
            session.CancelBattle();
        else if (e.KeyCode == Keys.W)
            session.SelectPreviousBattleCommand();
        else if (e.KeyCode == Keys.S)
            session.SelectNextBattleCommand();
        else if (e.KeyCode == Keys.A)
            session.PerformBattleCommand();
        else if (e.KeyCode == Keys.D)
            session.SelectNextBattleTarget();
    }

    private void ShowStats()
    {
        PartyMember? leader = session.StateManager.ActiveExpedition.Party
            .FirstOrDefault(member => member.Id == session.Party.LeaderId);
        if (leader == null)
            return;

        using StatsWindow statsWindow = new(leader);
        statsWindow.ShowDialog(this);
    }

    private void DrawGame(object? sender, PaintEventArgs e)
    {
        renderer.Draw(
            e.Graphics,
            session.World,
            session.Party,
            session.StateManager.ActiveExpedition,
            session.State,
            session.Battle,
            session.LastExtraction,
            session.Feedback,
            session.Message,
            session.CurrentObjective,
            session.IsCellDiscovered,
            session.IsCellVisible,
            session.StateManager);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            renderTimer.Dispose();
            renderer.Dispose();
        }

        base.Dispose(disposing);
    }
}
