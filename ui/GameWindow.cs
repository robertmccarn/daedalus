using System.Drawing;
using System.Windows.Forms;

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
        renderTimer.Tick += (_, _) => Invalidate();
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
        {
            HandleExplorationInput(e);
        }
    }

    private void HandleExplorationInput(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.C)
        {
            ShowStats();
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

        (int deltaX, int deltaY) = e.KeyCode switch
        {
            Keys.W => (0, -1),
            Keys.S => (0, 1),
            Keys.A => (-1, 0),
            Keys.D => (1, 0),
            _ => (0, 0)
        };

        if (deltaX != 0 || deltaY != 0)
        {
            session.MovePlayer(deltaX, deltaY);
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
        using StatsWindow statsWindow = new(session.World.Player);
        statsWindow.ShowDialog(this);
    }

    private void DrawGame(object? sender, PaintEventArgs e)
    {
        BattleSystem? battle = session.Battle;

        renderer.Draw(
            e.Graphics,
            session.World,
            session.State,
            battle?.Enemy,
            session.Message,
            battle?.SelectedCommand ?? BattleCommand.Attack);
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
