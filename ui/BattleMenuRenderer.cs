using System.Drawing;

public class BattleMenuRenderer : IDisposable
{
    private readonly AtlasSlicer atlas = new();
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
            AtlasCommandIcon icon = command switch
            {
                BattleCommand.Attack => AtlasCommandIcon.Attack,
                BattleCommand.Skill => AtlasCommandIcon.Tech,
                BattleCommand.Item => AtlasCommandIcon.Item,
                BattleCommand.Interact => AtlasCommandIcon.Interact,
                BattleCommand.Defend => AtlasCommandIcon.Defend,
                BattleCommand.Run => AtlasCommandIcon.Retreat,
                _ => AtlasCommandIcon.Interact
            };

            if (isSelected)
            {
                graphics.FillRectangle(Brushes.DarkSlateBlue, 52, y - 2, 380, 27);
            }

            AtlasRegion region = atlas.GetCommandIconRegion(icon);
            atlas.Draw(graphics, region, new Rectangle(60, y, 24, 24));
            graphics.DrawString($"{(isSelected ? "▶" : " ")} {command}", font,
                isSelected ? Brushes.White : Brushes.LightGray, 95, y + 3);
        }
    }

    public void DrawStatusBadges(Graphics graphics, Character character, int x, int y)
    {
        int offsetX = 0;
        foreach (StatusEffect effect in character.StatusEffects)
        {
            AtlasRegion region = atlas.GetStatusIconRegion((AtlasStatusIcon)effect);
            atlas.Draw(graphics, region, new Rectangle(x + offsetX, y, 16, 16));
            offsetX += 18;
        }
    }

    public void Dispose()
    {
        atlas.Dispose();
        font.Dispose();
    }
}
