using System.Drawing;
using System.Drawing.Drawing2D;
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
        Rectangle hud = context.Layout.HudViewport;
        int panelX = hud.X;
        int panelWidth = hud.Width;
        int contentX = hud.X + 22;

        using Brush panel = new SolidBrush(Color.FromArgb(232, 12, 15, 18));
        graphics.FillRectangle(panel, hud);

        using Pen divider = new(
            Color.FromArgb(180, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B),
            1);
        graphics.DrawLine(divider, panelX, hud.Top, panelX, hud.Bottom);

        using Brush title = new SolidBrush(p.Bone());
        using Brush accent = new SolidBrush(p.Accent);
        graphics.DrawString("DAEDALUS", titleFont, title, contentX, 18);
        graphics.DrawString(BiomeCatalog.Name(context.Biome), smallFont, accent, contentX, 47);
        graphics.DrawString($"DEPTH {context.World.Floor:00}", uiFont, Brushes.White, contentX, 64);

        int minimapWidth = Math.Min(195, Math.Max(120, panelWidth - 45));
        minimap.Draw(graphics, context, new Rectangle(contentX, 92, minimapWidth, 135));

        PartyMember? leader = context.Expedition.Party
            .FirstOrDefault(member => member.Id == context.Party.LeaderId);

        if (leader != null)
        {
            graphics.DrawString("EXPEDITION", titleFont, Brushes.White, contentX, 244);

            int y = 276;
            foreach (PartyMember member in context.Expedition.Party.Take(4))
            {
                bool selected = member.Id == context.Party.LeaderId;
                Color memberColor = selected
                    ? p.Accent
                    : Color.FromArgb(180, 220, 220, 220);

                using Brush memberAccent = new SolidBrush(memberColor);
                graphics.FillRectangle(memberAccent, contentX, y + 3, 5, 31);
                graphics.DrawString(member.Name, uiFont, Brushes.White, contentX + 13, y);
                graphics.DrawString(
                    $"HP {member.HP,2}/{member.MaxHP,2}  M {member.Morale,3}",
                    smallFont,
                    Brushes.Gainsboro,
                    contentX + 13,
                    y + 17);

                y += 40;
            }

            graphics.DrawString(
                $"FORMATION  {context.Expedition.Formation.ToString().ToUpperInvariant()}",
                smallFont,
                Brushes.Gainsboro,
                contentX,
                438);
            graphics.DrawString(
                $"UPKEEP     {context.Expedition.Upkeep}",
                smallFont,
                Brushes.Gainsboro,
                contentX,
                456);
            graphics.DrawString(
                $"LOOT GOLD  {context.Expedition.CarriedGold}",
                smallFont,
                Brushes.Gainsboro,
                contentX,
                474);
        }

        DungeonNode? node = context.World.GetNodeAt(
            context.Party.LeaderPosition.X,
            context.Party.LeaderPosition.Y);

        if (node != null)
        {
            DrawPanel(graphics, divider, hud.X + 15, 510, panelWidth - 30, 48);

            using Brush promptText = new SolidBrush(Color.White);
            using Brush promptAccent = new SolidBrush(p.Accent);
            graphics.DrawString($"[{PromptFor(node)}]", uiFont, promptText, 886, 519);
            graphics.DrawString(node.Name, smallFont, promptAccent, 886, 538);
        }

        DrawObjective(graphics, context, p, 566);
        DrawMessage(graphics, context, p, 622);

        using Pen footerRule = new(Color.FromArgb(70, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        graphics.DrawLine(footerRule, 875, 682, 1085, 682);

        using Brush controls = new SolidBrush(Color.FromArgb(205, 205, 208, 212));
        graphics.DrawString(
            "WASD MOVE   E USE   X EXTRACT   C STATS",
            microFont,
            controls,
            contentX - 7,
            668);
    }

    private void DrawObjective(
        Graphics graphics,
        ExplorationRenderContext context,
        WorldPresentationProfile profile,
        int top)
    {
        Rectangle hud = context.Layout.HudViewport;
        DrawPanel(graphics, null, hud.X + 15, top, hud.Width - 30, 49);

        using Brush heading = new SolidBrush(profile.Accent);
        using Brush text = new SolidBrush(Color.White);

        graphics.DrawString("OBJECTIVE", microFont, heading, 886, top + 5);
        graphics.DrawString(
            context.CurrentObjective,
            microFont,
            text,
            new RectangleF(hud.X + 26, top + 19, hud.Width - 54, 27));
    }

    private void DrawMessage(
        Graphics graphics,
        ExplorationRenderContext context,
        WorldPresentationProfile profile,
        int top)
    {
        Rectangle hud = context.Layout.HudViewport;
        DrawPanel(graphics, null, hud.X + 15, top, hud.Width - 30, 54);

        using Brush heading = new SolidBrush(profile.WarmLight);
        using Brush text = new SolidBrush(Color.FromArgb(235, 238, 240, 244));

        graphics.DrawString("FIELD REPORT", microFont, heading, 886, top + 5);

        string message = string.IsNullOrWhiteSpace(context.Message)
            ? "No new report."
            : context.Message;

        graphics.DrawString(
            message,
            microFont,
            text,
            new RectangleF(hud.X + 26, top + 19, hud.Width - 54, 31));
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
