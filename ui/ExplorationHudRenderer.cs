using System.Drawing;
using Systemic.Engine.State;

public class ExplorationHudRenderer : IDisposable
{
    private readonly Font uiFont = new(FontFamily.GenericMonospace, 12);
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 18, FontStyle.Bold);
    private readonly Font smallFont = new(FontFamily.GenericMonospace, 10);
    private readonly Font microFont = new(FontFamily.GenericMonospace, 9);
    private readonly MinimapRenderer minimap = new();

    public void Draw(Graphics graphics, ExplorationRenderContext context)
    {
        WorldPresentationProfile p = context.Profile;

        using Brush panel = new SolidBrush(Color.FromArgb(232, 12, 15, 18));
        graphics.FillRectangle(panel, 860, 0, 240, 700);

        using Pen divider = new(
            Color.FromArgb(180, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B),
            1);
        graphics.DrawLine(divider, 860, 0, 860, 700);

        using Brush title = new SolidBrush(p.Bone());
        using Brush accent = new SolidBrush(p.Accent);
        graphics.DrawString("DAEDALUS", titleFont, title, 882, 18);
        graphics.DrawString(BiomeCatalog.Name(context.Biome), smallFont, accent, 882, 47);
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
                Color memberColor = selected
                    ? p.Accent
                    : Color.FromArgb(180, 220, 220, 220);

                using Brush memberAccent = new SolidBrush(memberColor);
                graphics.FillRectangle(memberAccent, 882, y + 3, 5, 31);
                graphics.DrawString(member.Name, uiFont, Brushes.White, 895, y);
                graphics.DrawString(
                    $"HP {member.HP,2}/{member.MaxHP,2}  M {member.Morale,3}",
                    smallFont,
                    Brushes.Gainsboro,
                    895,
                    y + 17);

                y += 40;
            }

            graphics.DrawString(
                $"FORMATION  {context.Expedition.Formation.ToString().ToUpperInvariant()}",
                smallFont,
                Brushes.Gainsboro,
                882,
                438);
            graphics.DrawString(
                $"UPKEEP     {context.Expedition.Upkeep}",
                smallFont,
                Brushes.Gainsboro,
                882,
                456);
            graphics.DrawString(
                $"LOOT GOLD  {context.Expedition.CarriedGold}",
                smallFont,
                Brushes.Gainsboro,
                882,
                474);
        }

        DungeonNode? node = context.World.GetNodeAt(
            context.Party.LeaderPosition.X,
            context.Party.LeaderPosition.Y);

        if (node != null)
        {
            DrawPanel(graphics, divider, 875, 510, 210, 48);

            using Brush promptText = new SolidBrush(Color.White);
            using Brush promptAccent = new SolidBrush(p.Accent);
            graphics.DrawString($"[{PromptFor(node)}]", uiFont, promptText, 886, 519);
            graphics.DrawString(node.Name, smallFont, promptAccent, 886, 538);
        }

        DrawObjective(graphics, context, p, 566);
        DrawMessage(graphics, context, p, 624);

        using Brush controls = new SolidBrush(Color.FromArgb(190, 205, 208, 212));
        graphics.DrawString(
            "WASD MOVE  E INTERACT  X EXTRACT  C STATS",
            microFont,
            controls,
            875,
            689);
    }

    private void DrawObjective(
        Graphics graphics,
        ExplorationRenderContext context,
        WorldPresentationProfile profile,
        int top)
    {
        DrawPanel(graphics, null, 875, top, 210, 52);

        using Brush heading = new SolidBrush(profile.Accent);
        using Brush text = new SolidBrush(Color.White);

        graphics.DrawString("OBJECTIVE", microFont, heading, 886, top + 6);
        graphics.DrawString(
            context.CurrentObjective,
            microFont,
            text,
            new RectangleF(886, top + 20, 186, 28));
    }

    private void DrawMessage(
        Graphics graphics,
        ExplorationRenderContext context,
        WorldPresentationProfile profile,
        int top)
    {
        DrawPanel(graphics, null, 875, top, 210, 61);

        using Brush heading = new SolidBrush(profile.WarmLight);
        using Brush text = new SolidBrush(Color.FromArgb(235, 238, 240, 244));

        graphics.DrawString("FIELD REPORT", microFont, heading, 886, top + 6);

        string message = string.IsNullOrWhiteSpace(context.Message)
            ? "No new report."
            : context.Message;

        graphics.DrawString(
            message,
            microFont,
            text,
            new RectangleF(886, top + 20, 186, 36));
    }

    private static void DrawPanel(
        Graphics graphics,
        Pen? border,
        int x,
        int y,
        int width,
        int height)
    {
        using Brush background = new SolidBrush(Color.FromArgb(225, 18, 22, 27));
        graphics.FillRectangle(background, x, y, width, height);

        if (border != null)
            graphics.DrawRectangle(border, x, y, width, height);
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
        microFont.Dispose();
    }
}

internal static class ColorExtensions
{
    public static Color Bone(this WorldPresentationProfile profile) =>
        Color.FromArgb(238, 240, 244);
}
