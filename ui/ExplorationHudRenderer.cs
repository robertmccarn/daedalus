using System.Drawing;
using Systemic.Engine.State;

public class ExplorationHudRenderer : IDisposable
{
    private readonly Font uiFont = new(FontFamily.GenericMonospace, 12);
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 18, FontStyle.Bold);
    private readonly Font smallFont = new(FontFamily.GenericMonospace, 10);
    private readonly MinimapRenderer minimap = new();

    public void Draw(Graphics graphics, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;

        using Brush panel = new SolidBrush(Color.FromArgb(232, 12, 15, 18));
        graphics.FillRectangle(panel, 860, 0, 240, 700);
        using Pen border = new(Color.FromArgb(180, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        graphics.DrawLine(border, 860, 0, 860, 700);

        graphics.DrawString("DAEDALUS", titleFont, new SolidBrush(p.Bone()), 882, 18);
        graphics.DrawString(BiomeCatalog.Name(context.Biome), smallFont, new SolidBrush(p.Accent), 882, 47);
        graphics.DrawString($"DEPTH {context.World.Floor:00}", uiFont, Brushes.White, 882, 64);

        minimap.Draw(graphics, context, new Rectangle(882, 92, 195, 135));

        PartyMember? leader = context.Expedition.Party
            .FirstOrDefault(member => member.Id == context.Party.LeaderId);

        if (leader != null)
        {
            graphics.DrawString("EXPEDITION", titleFont, Brushes.White, 882, 244);
            int y = 276;
            foreach (PartyMember member in context.Expedition.Party.Take(4))
            {
                bool selected = member.Id == context.Party.LeaderId;
                Color accent = selected ? p.Accent : Color.FromArgb(180, 220, 220, 220);
                using Brush accentBrush = new SolidBrush(accent);
                graphics.FillRectangle(accentBrush, 882, y + 3, 5, 31);
                graphics.DrawString(member.Name, uiFont, Brushes.White, 895, y);
                graphics.DrawString($"HP {member.HP,2}/{member.MaxHP,2}  M {member.Morale,3}", smallFont, Brushes.Gainsboro, 895, y + 17);
                y += 40;
            }

            graphics.DrawString($"FORMATION  {context.Expedition.Formation.ToString().ToUpperInvariant()}", smallFont, Brushes.Gainsboro, 882, 448);
            graphics.DrawString($"UPKEEP     {context.Expedition.Upkeep}", smallFont, Brushes.Gainsboro, 882, 466);
            graphics.DrawString($"LOOT GOLD  {context.Expedition.CarriedGold}", smallFont, Brushes.Gainsboro, 882, 484);
        }

        DungeonNode? node = context.World.GetNodeAt(
            context.Party.LeaderPosition.X,
            context.Party.LeaderPosition.Y);

        if (node != null)
        {
            using Brush prompt = new SolidBrush(Color.FromArgb(225, 24, 28, 32));
            graphics.FillRectangle(prompt, 875, 520, 210, 48);
            graphics.DrawRectangle(border, 875, 520, 210, 48);
            graphics.DrawString($"[{PromptFor(node)}]", uiFont, Brushes.White, 886, 530);
            graphics.DrawString(node.Name, smallFont, new SolidBrush(p.Accent), 886, 548);
        }

        graphics.DrawString("WASD MOVE   E INTERACT   X EXTRACT", smallFont, Brushes.Gainsboro, 875, 610);
        graphics.DrawString("C STATS     B BATTLE", smallFont, Brushes.Gainsboro, 875, 628);
    }

    private static string PromptFor(DungeonNode node) => node.Type switch
    {
        DungeonNodeType.Chest => "E OPEN",
        DungeonNodeType.Terminal => "E ACTIVATE",
        DungeonNodeType.Extraction => "X EXTRACT",
        DungeonNodeType.Combat => "CONTACT",
        _ => "E INSPECT"
    };

    public void Dispose()
    {
        uiFont.Dispose();
        titleFont.Dispose();
        smallFont.Dispose();
    }
}

internal static class ColorExtensions
{
    public static Color Bone(this WorldPresentationProfile profile) =>
        Color.FromArgb(238, 240, 244);
}
