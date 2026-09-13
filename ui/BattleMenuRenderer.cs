using System.Drawing;

public class BattleMenuRenderer : IDisposable
{
    private readonly Font font = new(FontFamily.GenericMonospace, 16);

    public void DrawCommands(Graphics graphics, BattleCommand selectedCommand)
    {
        graphics.DrawRectangle(Pens.White, 45, 395, 400, 195);

        BattleCommand[] commands =
        {
            BattleCommand.Attack,
            BattleCommand.Skill,
            BattleCommand.Item,
            BattleCommand.Interact,
            BattleCommand.Defend,
            BattleCommand.Run
        };

        for (int index = 0; index < commands.Length; index++)
        {
            BattleCommand command = commands[index];
            int y = 402 + index * 29;
            bool isSelected = command == selectedCommand;

            if (isSelected)
                graphics.FillRectangle(Brushes.DarkSlateBlue, 52, y - 2, 380, 27);

            graphics.DrawString($"{(isSelected ? "▶" : " ")} {command}", font,
                isSelected ? Brushes.White : Brushes.LightGray, 65, y + 3);
        }
    }

    public void DrawStatusBadges(Graphics graphics, Character character, int x, int y)
    {
        int offsetX = 0;
        foreach (StatusEffect effect in character.StatusEffects)
        {
            graphics.FillRectangle(Brushes.DarkRed, x + offsetX, y, 16, 16);
            graphics.DrawRectangle(Pens.White, x + offsetX, y, 16, 16);
            offsetX += 18;
        }
    }

    public void Dispose()
    {
        font.Dispose();
    }
}
