using System.Drawing;


public class BattleRenderer : IDisposable
{
    private readonly Font titleFont;
    private readonly Font uiFont;
    private readonly BattleMenuRenderer menuRenderer;

    private const int FrameX = 30;
    private const int FrameY = 30;
    private const int FrameWidth = 1040;
    private const int FrameHeight = 580;

    private const int TitleX = 60;
    private const int TitleY = 55;

    private const int PlayerX = 80;
    private const int EnemyX = 700;

    private const int NameY = 140;
    private const int HpY = 175;

    private const int PlayerSpriteX = 150;
    private const int PlayerSpriteY = 235;

    private const int EnemySpriteX = 760;
    private const int EnemySpriteY = 235;

    private const int CommandX = 60;
    private const int CommandY = 430;

    private const int MessageX = 550;
    private const int MessageY = 430;

    private const int BarWidth = 250;
    private const int BarHeight = 18;


    public BattleRenderer()
    {
        titleFont =
            new Font(
                FontFamily.GenericMonospace,
                26);

        uiFont =
            new Font(
                FontFamily.GenericMonospace,
                16);

        menuRenderer = new BattleMenuRenderer();
    }


    public void Draw(
        Graphics graphics,
        GameWorld world,
        Character? enemy,
        string message,
        BattleCommand selectedCommand)
    {
        graphics.Clear(
            Color.Black);

        DrawBattleFrame(
            graphics);

        DrawTitle(
            graphics);

        if (enemy == null)
        {
            return;
        }

        DrawCombatantInfo(
            graphics,
            world.Player,
            PlayerX,
            Brushes.White);

        DrawCombatantInfo(
            graphics,
            enemy,
            EnemyX,
            Brushes.Red);

        DrawPlayerSprite(
            graphics);

        DrawGoblinSprite(
            graphics);

        menuRenderer.DrawCommands(graphics, selectedCommand);

        DrawMessage(
            graphics,
            message);
    }


    private void DrawBattleFrame(
        Graphics graphics)
    {
        graphics.DrawRectangle(
            Pens.White,
            FrameX,
            FrameY,
            FrameWidth,
            FrameHeight);
    }


    private void DrawTitle(
        Graphics graphics)
    {
        graphics.DrawString(
            "BATTLE",
            titleFont,
            Brushes.White,
            TitleX,
            TitleY);
    }


    private void DrawCombatantInfo(
        Graphics graphics,
        Character character,
        int x,
        Brush nameBrush)
    {
        graphics.DrawString(
            character.Name,
            uiFont,
            nameBrush,
            x,
            NameY);

        graphics.DrawString(
            $"HP {character.HP}/{character.MAXHP}",
            uiFont,
            Brushes.White,
            x,
            HpY);

        DrawHpBar(
            graphics,
            character,
            x,
            HpY + 28);

        menuRenderer.DrawStatusBadges(graphics, character, x, HpY + 52);
    }


    private void DrawHpBar(
        Graphics graphics,
        Character character,
        int x,
        int y)
    {
        graphics.DrawRectangle(
            Pens.White,
            x,
            y,
            BarWidth,
            BarHeight);

        if (character.MAXHP <= 0)
        {
            return;
        }

        float percentage =
            (float)character.HP /
            character.MAXHP;

        int fillWidth =
            (int)(
                (BarWidth - 2) *
                percentage);

        if (fillWidth <= 0)
        {
            return;
        }

        graphics.FillRectangle(
            Brushes.LimeGreen,
            x + 1,
            y + 1,
            fillWidth,
            BarHeight - 2);
    }


    private void DrawPlayerSprite(
        Graphics graphics)
    {
        int x =
            PlayerSpriteX;

        int y =
            PlayerSpriteY;

        // Shadow

        graphics.FillRectangle(
            Brushes.DarkGray,
            x,
            y + 135,
            110,
            12);

        // Cape

        graphics.FillRectangle(
            Brushes.DarkBlue,
            x + 20,
            y + 50,
            55,
            80);

        // Body armor

        graphics.FillRectangle(
            Brushes.SteelBlue,
            x + 35,
            y + 55,
            45,
            65);

        // Head

        graphics.FillRectangle(
            Brushes.PeachPuff,
            x + 42,
            y + 15,
            35,
            40);

        // Hair

        graphics.FillRectangle(
            Brushes.MidnightBlue,
            x + 38,
            y + 10,
            43,
            15);

        // Legs

        graphics.FillRectangle(
            Brushes.SaddleBrown,
            x + 35,
            y + 115,
            15,
            25);

        graphics.FillRectangle(
            Brushes.SaddleBrown,
            x + 65,
            y + 115,
            15,
            25);

        // Sword

        using Pen sword =
            new Pen(
                Brushes.LightGray,
                8);

        graphics.DrawLine(
            sword,
            x + 80,
            y + 75,
            x + 125,
            y + 30);
    }


    private void DrawGoblinSprite(
        Graphics graphics)
    {
        int x =
            EnemySpriteX;

        int y =
            EnemySpriteY;

        // Shadow

        graphics.FillRectangle(
            Brushes.DarkGray,
            x,
            y + 135,
            110,
            12);

        // Body

        graphics.FillRectangle(
            Brushes.DarkGreen,
            x + 30,
            y + 60,
            50,
            65);

        // Head

        graphics.FillRectangle(
            Brushes.YellowGreen,
            x + 25,
            y + 20,
            60,
            50);

        // Ears

        graphics.FillRectangle(
            Brushes.YellowGreen,
            x + 10,
            y + 30,
            20,
            20);

        graphics.FillRectangle(
            Brushes.YellowGreen,
            x + 80,
            y + 30,
            20,
            20);

        // Eyes

        graphics.FillRectangle(
            Brushes.Red,
            x + 40,
            y + 38,
            8,
            8);

        graphics.FillRectangle(
            Brushes.Red,
            x + 63,
            y + 38,
            8,
            8);

        // Arms

        graphics.FillRectangle(
            Brushes.DarkGreen,
            x + 10,
            y + 70,
            25,
            15);

        graphics.FillRectangle(
            Brushes.DarkGreen,
            x + 80,
            y + 70,
            25,
            15);

        // Legs

        graphics.FillRectangle(
            Brushes.DarkGreen,
            x + 35,
            y + 120,
            15,
            25);

        graphics.FillRectangle(
            Brushes.DarkGreen,
            x + 65,
            y + 120,
            15,
            25);

        // Club

        using Pen club =
            new Pen(
                Brushes.SaddleBrown,
                10);

        graphics.DrawLine(
            club,
            x + 95,
            y + 90,
            x + 130,
            y + 125);
    }


    private void DrawCommands(
        Graphics graphics,
        BattleCommand selectedCommand)
    {
        graphics.DrawRectangle(
            Pens.White,
            45,
            405,
            400,
            175);

        BattleCommand[] commands =
        {
            BattleCommand.Attack,
            BattleCommand.Skill,
            BattleCommand.Item,
            BattleCommand.Defend,
            BattleCommand.Run
        };

        foreach (BattleCommand command
            in commands)
        {
            int index =
                (int)command;

            int y =
                CommandY +
                index * 28;

            string prefix =
                command == selectedCommand
                    ? "▶ "
                    : "  ";

            Brush brush =
                command == BattleCommand.Attack
                    ? Brushes.White
                    : Brushes.Gray;

            graphics.DrawString(
                prefix + command,
                uiFont,
                brush,
                CommandX,
                y);
        }
    }


    private void DrawMessage(
        Graphics graphics,
        string message)
    {
        const int boxX = 465;
        const int boxY = 405;
        const int boxWidth = 565;
        const int boxHeight = 175;

        const int padding = 18;

        graphics.DrawRectangle(
            Pens.White,
            boxX,
            boxY,
            boxWidth,
            boxHeight);

        string displayMessage =
            string.IsNullOrWhiteSpace(message)
                ? "Choose an action."
                : message;

        RectangleF textArea =
            new RectangleF(
                boxX + padding,
                boxY + padding,
                boxWidth - padding * 2,
                boxHeight - padding * 2);

        using StringFormat format =
            new StringFormat();

        format.Alignment =
            StringAlignment.Near;

        format.LineAlignment =
            StringAlignment.Near;

        format.Trimming =
            StringTrimming.None;

        graphics.DrawString(
            displayMessage,
            uiFont,
            Brushes.White,
            textArea,
            format);
    }


    public void Dispose()
    {
        titleFont.Dispose();
        uiFont.Dispose();
        menuRenderer.Dispose();
    }
}
