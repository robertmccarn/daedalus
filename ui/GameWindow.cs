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

        Text = "Dungeon";
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
        if (session.State == GameState.Battle)
        {
            HandleBattleInput(e);
            return;
        }

        if (session.State == GameState.Exploration)
            HandleExplorationInput(e);
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
            Invalidate();
            return;
        }

        if (e.KeyCode == Keys.B)
        {
            session.StartTestBattle();
            Invalidate();
            return;
        }

        if (e.KeyCode == Keys.E)
        {
            session.Interact();
            Invalidate();
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
        {
            session.MoveLeader(direction.Value);
            Invalidate();
        }
    }

    private void HandleBattleInput(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            session.CancelBattle();
        }
        else if (e.KeyCode == Keys.W)
        {
            session.SelectPreviousBattleCommand();
        }
        else if (e.KeyCode == Keys.S)
        {
            session.SelectNextBattleCommand();
        }
        else if (e.KeyCode == Keys.A)
        {
            session.PerformBattleCommand();
        }
        else
        {
            return;
        }

        Invalidate();
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
        BattleSystem? battle = session.Battle;

        renderer.Draw(
            e.Graphics,
            session.World,
            session.Party,
            session.StateManager.ActiveExpedition,
            session.State,
            battle?.Enemy,
            session.Message,
            battle?.SelectedCommand ?? BattleCommand.Attack,
            session.IsCellDiscovered);
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
