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
        renderTimer.Tick += (_, _) =>
        {
            session.Party.AdvanceAnimation();
            Invalidate();
        };
        renderTimer.Start();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;

        if (key == Keys.F1)
        {
            session.ToggleDevMenu();
            Invalidate();
            return true;
        }

        if (session.DevMenuOpen)
        {
            if (HandleDevMenuInput(key))
            {
                Invalidate();
                return true;
            }

            return true;
        }

        switch (session.State)
        {
            case GameState.Battle:
                if (HandleBattleInput(key))
                {
                    Invalidate();
                    return true;
                }
                break;

            case GameState.ExtractionResults:
                if (key is Keys.Enter or Keys.Space)
                {
                    session.ReturnToCampaign();
                    Invalidate();
                    return true;
                }
                break;

            case GameState.Campaign:
            case GameState.GameOver:
                if (key is Keys.Enter or Keys.Space)
                {
                    session.StartNewExpeditionFromCampaign();
                    Invalidate();
                    return true;
                }
                break;

            case GameState.Exploration:
                if (HandleExplorationInput(key))
                {
                    Invalidate();
                    return true;
                }
                break;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private bool HandleDevMenuInput(Keys key)
    {
        if (key == Keys.Escape)
        {
            session.CloseDevMenu();
            return true;
        }

        if (key is Keys.W or Keys.Up)
        {
            session.AdjustDevFloor(-1);
            return true;
        }

        if (key is Keys.S or Keys.Down)
        {
            session.AdjustDevFloor(1);
            return true;
        }

        if (key is Keys.A or Keys.Left)
        {
            session.AdjustDevFloor(-5);
            return true;
        }

        if (key is Keys.D or Keys.Right)
        {
            session.AdjustDevFloor(5);
            return true;
        }

        if (key is Keys.E or Keys.Enter or Keys.Space)
        {
            session.JumpToDevFloor();
            return true;
        }

        return false;
    }

    private bool HandleExplorationInput(Keys key)
    {
        if (key == Keys.C)
        {
            ShowStats();
            return true;
        }

        if (key == Keys.X)
        {
            session.ExtractExpedition();
            return true;
        }

        if (key == Keys.E)
        {
            session.Interact();
            return true;
        }

        CharacterDirection? direction = key switch
        {
            Keys.W or Keys.Up => CharacterDirection.Up,
            Keys.S or Keys.Down => CharacterDirection.Down,
            Keys.A or Keys.Left => CharacterDirection.Left,
            Keys.D or Keys.Right => CharacterDirection.Right,
            _ => null
        };

        if (!direction.HasValue)
            return false;

        session.MoveLeader(direction.Value);
        return true;
    }

    private bool HandleBattleInput(Keys key)
    {
        if (key == Keys.Escape)
        {
            session.CancelBattle();
            return true;
        }

        // Command selection: W/S or Up/Down.
        if (key is Keys.W or Keys.Up)
        {
            session.SelectPreviousBattleCommand();
            return true;
        }

        if (key is Keys.S or Keys.Down)
        {
            session.SelectNextBattleCommand();
            return true;
        }

        // Target selection: A = previous (<), D = next (>).
        if (key == Keys.A)
        {
            session.SelectPreviousBattleTarget();
            return true;
        }

        if (key == Keys.D)
        {
            session.SelectNextBattleTarget();
            return true;
        }

        // Confirm: E, Enter, or Space.
        if (key is Keys.E or Keys.Enter or Keys.Space)
        {
            session.PerformBattleCommand();
            return true;
        }

        return false;
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
