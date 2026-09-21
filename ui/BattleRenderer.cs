using System.Drawing;
using System.Drawing.Drawing2D;
using Systemic.Engine.State;

public class BattleRenderer : IDisposable
{
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 26, FontStyle.Bold);
    private readonly Font smallFont = new(FontFamily.GenericMonospace, 11);
    private readonly Font microFont = new(FontFamily.GenericMonospace, 9);
    private readonly Font damageFont = new(FontFamily.GenericMonospace, 18, FontStyle.Bold);

    public void Draw(
        Graphics graphics,
        GameWorld world,
        PartyController party,
        ExpeditionState expedition,
        BattleSystem battle,
        string message)
    {
        BiomeType biome = BiomeCatalog.ForFloor(world.Floor);
        WorldPresentationProfile p = WorldPresentationProfile.ForBiome(biome);
        long now = AnimationClock.Now;
        BattleAnimationEvent? animation = battle.GetActiveAnimation(now);

        graphics.Clear(Color.FromArgb(8, 10, 12));

        using LinearGradientBrush bg = new(
            new Rectangle(0, 0, 1100, 700),
            p.Void,
            Color.FromArgb(42, 34, 35),
            LinearGradientMode.Vertical);
        graphics.FillRectangle(bg, 0, 0, 1100, 700);

        DrawArena(graphics, p, now);

        graphics.DrawString("TACTICAL CONTACT", titleFont, Brushes.White, 45, 30);
        using Brush biomeBrush = new SolidBrush(p.Accent);
        graphics.DrawString(BiomeCatalog.Name(biome), smallFont, biomeBrush, 48, 65);

        DrawParty(graphics, p, battle, animation, now);
        DrawEnemies(graphics, p, battle, animation, now);

        DrawTurnOrder(graphics, p, battle, now);
        DrawCommands(graphics, p, battle.SelectedCommand, now);
        DrawDescription(graphics, p, battle, animation, now);

        _ = party;
        _ = expedition;
        _ = message;
    }

    private static void DrawArena(Graphics g, WorldPresentationProfile p, long now)
    {
        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 1800);

        using Brush floor = new SolidBrush(Color.FromArgb(92, p.Floor.R, p.Floor.G, p.Floor.B));
        g.FillRectangle(floor, 60, 110, 750, 360);

        using Pen grid = new(
            Color.FromArgb(
                20 + (int)(10 * pulse),
                p.WallHighlight.R,
                p.WallHighlight.G,
                p.WallHighlight.B),
            1);

        for (int x = 60; x <= 810; x += 38)
            g.DrawLine(grid, x, 110, x, 470);

        for (int y = 110; y <= 470; y += 38)
            g.DrawLine(grid, 60, y, 810, y);

        using Pen border = new(
            Color.FromArgb(160, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B),
            2);
        g.DrawRectangle(border, 60, 110, 750, 360);

        int glowAlpha = 18 + (int)(12 * pulse);
        using Brush glow = new SolidBrush(Color.FromArgb(glowAlpha, p.Accent.R, p.Accent.G, p.Accent.B));
        g.FillEllipse(glow, 250, 150, 320, 270);
    }

    private void DrawParty(
        Graphics g,
        WorldPresentationProfile p,
        BattleSystem battle,
        BattleAnimationEvent? animation,
        long now)
    {
        int index = 0;
        foreach (PartyMember member in battle.Party.Take(4))
        {
            Point basePoint = GetPartyPoint(index);
            Point targetPoint = ResolveTargetPoint(battle, animation?.TargetId, basePoint);
            AnimationPose pose = GetPose(animation, member.Id, basePoint, targetPoint, now);

            bool hit = animation.HasValue &&
                       animation.Value.TargetId == member.Id &&
                       IsImpactWindow(animation.Value, now) &&
                       animation.Value.Kind is BattleAnimationKind.EnemyAttack or BattleAnimationKind.PoisonTick;

            float alpha = member.HP > 0 ? 1f : 0.58f;
            if (animation is { Kind: BattleAnimationKind.Defeat } &&
                animation.Value.TargetId == member.Id)
                alpha *= 1f - AnimationClock.AttackProgress(now, animation.Value.StartedAt, (int)animation.Value.DurationMs);

            bool selected = battle.SelectedActor?.Id == member.Id;

            DrawActor(
                g,
                p,
                member.SpriteId,
                member.Name,
                member.HP,
                member.MaxHP,
                basePoint.X + pose.OffsetX,
                basePoint.Y + pose.OffsetY,
                selected,
                member.Id,
                alpha,
                hit,
                now);

            index++;
        }
    }

    private void DrawEnemies(
        Graphics g,
        WorldPresentationProfile p,
        BattleSystem battle,
        BattleAnimationEvent? animation,
        long now)
    {
        int index = 0;

        foreach (Character enemy in battle.Enemies.Take(3))
        {
            Point basePoint = GetEnemyPoint(index);
            Point targetPoint = ResolveTargetPoint(battle, animation?.TargetId, basePoint);
            string enemyId = battle.GetEnemyPresentationId(enemy);
            AnimationPose pose = GetPose(animation, enemyId, basePoint, targetPoint, now);

            bool hit = animation.HasValue &&
                       animation.Value.TargetId == enemyId &&
                       IsImpactWindow(animation.Value, now) &&
                       animation.Value.Kind is
                           BattleAnimationKind.Attack or
                           BattleAnimationKind.Skill or
                           BattleAnimationKind.Expose or
                           BattleAnimationKind.PoisonTick;

            float alpha = enemy.HP > 0 ? 1f : 0.48f;
            if (animation is { Kind: BattleAnimationKind.Defeat } &&
                animation.Value.TargetId == enemyId)
                alpha *= 1f - AnimationClock.AttackProgress(now, animation.Value.StartedAt, (int)animation.Value.DurationMs);

            DrawEnemy(
                g,
                p,
                enemy,
                basePoint.X + pose.OffsetX,
                basePoint.Y + pose.OffsetY,
                battle.SelectedTarget == enemy,
                alpha,
                hit,
                now,
                battle);

            index++;
        }

        if (animation.HasValue)
            DrawBattleEffect(g, p, battle, animation.Value, now);
    }

    private void DrawActor(
        Graphics g,
        WorldPresentationProfile p,
        string spriteId,
        string name,
        int hp,
        int maxHp,
        int x,
        int y,
        bool selected,
        string id,
        float alpha,
        bool hit,
        long now,
        BattleSystem battle)
    {
        using Brush shadow = new SolidBrush(Color.FromArgb((int)(100 * alpha), 0, 0, 0));
        g.FillEllipse(shadow, x - 3, y + 46, 58, 13);

        Color accent = id == "arden"
            ? p.Accent
            : spriteId.ToLowerInvariant() switch
            {
                "lyra" => Color.FromArgb(116, 176, 170),
                "marek" => Color.FromArgb(173, 132, 89),
                "sera" => Color.FromArgb(154, 120, 177),
                _ => Color.FromArgb(122, 152, 157)
            };

        if (hit)
            accent = Mix(accent, Color.White, 0.65f);

        using Brush cloak = new SolidBrush(WithAlpha(hp > 0 ? accent : Color.FromArgb(55, 60, 62), alpha));
        Point[] body =
        {
            new Point(x + 8, y + 13), new Point(x + 18, y + 5),
            new Point(x + 40, y + 5), new Point(x + 50, y + 13),
            new Point(x + 42, y + 49), new Point(x + 15, y + 49)
        };
        g.FillPolygon(cloak, body);

        using Brush face = new SolidBrush(WithAlpha(Color.FromArgb(192, 162, 138), alpha));
        g.FillEllipse(face, x + 20, y - 7, 18, 19);

        using Pen gear = new(WithAlpha(Color.FromArgb(205, 210, 207), alpha), 2);
        if (spriteId.Equals("marek", StringComparison.OrdinalIgnoreCase))
            g.DrawLine(gear, x + 44, y + 28, x + 57, y + 16);
        else if (spriteId.Equals("lyra", StringComparison.OrdinalIgnoreCase))
            g.DrawLine(gear, x + 7, y + 10, x + 7, y + 48);

        using Brush hpBack = new SolidBrush(WithAlpha(Color.FromArgb(80, 20, 20, 20), alpha));
        g.FillRectangle(hpBack, x, y + 58, 60, 6);

        using Brush hpFill = new SolidBrush(
            WithAlpha(hp > 0 ? Color.FromArgb(195, 78, 165, 95) : Color.FromArgb(160, 72, 72, 72), alpha));
        int width = maxHp <= 0 ? 0 : 60 * hp / maxHp;
        g.FillRectangle(hpFill, x, y + 58, width, 6);

        using Brush text = new SolidBrush(WithAlpha(Color.White, alpha));
        using Brush subText = new SolidBrush(WithAlpha(Color.Gainsboro, alpha));
        g.DrawString(name, smallFont, text, x - 4, y + 69);
        g.DrawString($"HP {hp}/{maxHp}", microFont, subText, x - 4, y + 83);

        if (selected && hp > 0)
        {
            float pulse = 0.75f + 0.25f * AnimationClock.PingPong(now, 900, 175);
            int markerAlpha = (int)(130 + 95 * pulse);
            int pad = (int)MathF.Round(1 + pulse);

            using Pen marker = new(
                Color.FromArgb(markerAlpha, p.Accent.R, p.Accent.G, p.Accent.B),
                2);
            g.DrawEllipse(marker, x - 7 - pad, y - 10 - pad, 64 + pad * 2, 76 + pad * 2);
        }
    }

    private void DrawEnemy(
        Graphics g,
        WorldPresentationProfile p,
        Character enemy,
        int x,
        int y,
        bool selected,
        float alpha,
        bool hit,
        long now)
    {
        using Brush shadow = new SolidBrush(Color.FromArgb((int)(100 * alpha), 0, 0, 0));
        g.FillEllipse(shadow, x - 3, y + 48, 58, 13);

        Color bodyColor = enemy.HP > 0 ? p.Hazard : Color.FromArgb(65, 60, 60);
        if (hit)
            bodyColor = Mix(bodyColor, Color.White, 0.7f);

        using Brush body = new SolidBrush(WithAlpha(bodyColor, alpha));
        Point[] silhouette =
        {
            new Point(x + 25, y),
            new Point(x + 42, y + 10),
            new Point(x + 48, y + 31),
            new Point(x + 37, y + 48),
            new Point(x + 13, y + 48),
            new Point(x + 2, y + 31),
            new Point(x + 8, y + 10)
        };
        g.FillPolygon(body, silhouette);

        using Brush eye = new SolidBrush(WithAlpha(Color.FromArgb(235, 220, 185), alpha));
        if (enemy.HP > 0)
        {
            g.FillEllipse(eye, x + 14, y + 22, 6, 6);
            g.FillEllipse(eye, x + 32, y + 22, 6, 6);
        }

        if (selected && enemy.HP > 0)
        {
            float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 650);
            int markerAlpha = (int)(115 + 100 * pulse);

            using Pen marker = new(
                Color.FromArgb(markerAlpha, p.Accent.R, p.Accent.G, p.Accent.B),
                2);
            g.DrawEllipse(marker, x - 6, y - 6, 60, 60);
        }

        using Brush nameBrush = new SolidBrush(WithAlpha(Color.White, alpha));
        using Brush hpBrush = new SolidBrush(WithAlpha(Color.Gainsboro, alpha));
        g.DrawString(enemy.Name, smallFont, nameBrush, x - 12, y + 54);
        g.DrawString($"HP {enemy.HP}/{enemy.MAXHP}", microFont, hpBrush, x - 12, y + 68);

        if (enemy.HP > 0 && battle.HasStatus(enemy, "Poisoned"))
        {
            using Brush poison = new SolidBrush(WithAlpha(Color.FromArgb(92, 174, 130), alpha));
            g.DrawString("POISON", microFont, poison, x - 12, y + 81);
        }
    }

    private static AnimationPose GetPose(
        BattleAnimationEvent? animation,
        string combatantId,
        Point actorPoint,
        Point targetPoint,
        long now)
    {
        if (!animation.HasValue || animation.Value.ActorId != combatantId)
            return AnimationPose.Zero;

        BattleAnimationEvent eventData = animation.Value;
        float t = AnimationClock.AttackProgress(now, eventData.StartedAt, (int)eventData.DurationMs);

        if (eventData.Kind is
            BattleAnimationKind.Attack or
            BattleAnimationKind.Skill or
            BattleAnimationKind.Expose or
            BattleAnimationKind.EnemyAttack)
        {
            float lunge = t < 0.45f
                ? AnimationClock.EaseOutCubic(t / 0.45f)
                : 1f - AnimationClock.EaseInOutSine((t - 0.45f) / 0.55f);

            float strength = eventData.Kind == BattleAnimationKind.Skill ? 20f : 30f;
            if (eventData.Kind == BattleAnimationKind.EnemyAttack)
                strength = 24f;

            float dx = targetPoint.X - actorPoint.X;
            float dy = targetPoint.Y - actorPoint.Y;
            float length = MathF.Max(1f, MathF.Sqrt(dx * dx + dy * dy));

            return new AnimationPose(
                (int)MathF.Round(dx / length * strength * lunge),
                (int)MathF.Round(dy / length * strength * lunge));
        }

        return eventData.Kind switch
        {
            BattleAnimationKind.Defend => new AnimationPose(
                0,
                (int)MathF.Round(2f * MathF.Sin(t * MathF.PI))),
            BattleAnimationKind.Item => new AnimationPose(
                0,
                (int)MathF.Round(-4f * MathF.Sin(t * MathF.PI))),
            BattleAnimationKind.Defeat => new AnimationPose(
                0,
                (int)MathF.Round(14f * t)),
            BattleAnimationKind.PoisonTick => new AnimationPose(
                (int)MathF.Round(MathF.Sin(t * MathF.PI * 8f) * 3f),
                0),
            _ => AnimationPose.Zero
        };
    }

    private static Point ResolveTargetPoint(
        BattleSystem battle,
        string? targetId,
        Point fallback)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return fallback;

        PartyMember? member = battle.GetPartyMember(targetId);
        if (member != null)
        {
            int index = battle.Party.Take(4).ToList().FindIndex(candidate => candidate.Id == member.Id);
            return index >= 0 ? GetPartyPoint(index) : fallback;
        }

        Character? enemy = battle.GetEnemyByPresentationId(targetId);
        if (enemy != null)
        {
            int index = battle.Enemies.Take(3).ToList().FindIndex(candidate => candidate == enemy);
            return index >= 0 ? GetEnemyPoint(index) : fallback;
        }

        return fallback;
    }

    private static Point GetPartyPoint(int index) =>
        new(120 + (index % 2) * 145, 185 + (index / 2) * 120);

    private static Point GetEnemyPoint(int index) =>
        new(515 + (index % 2) * 120, 190 + (index / 2) * 120);

    private static bool IsImpactWindow(BattleAnimationEvent animation, long now)
    {
        float t = AnimationClock.AttackProgress(now, animation.StartedAt, (int)animation.DurationMs);
        return t >= 0.42f && t <= 0.72f;
    }

    private void DrawBattleEffect(
        Graphics g,
        WorldPresentationProfile p,
        BattleSystem battle,
        BattleAnimationEvent animation,
        long now)
    {
        Point center = ResolveTargetPoint(battle, animation.TargetId, new Point(600, 280));
        float t = AnimationClock.AttackProgress(now, animation.StartedAt, (int)animation.DurationMs);
        float impact = Math.Clamp((t - 0.35f) / 0.65f, 0f, 1f);

        Color baseColor = animation.Kind switch
        {
            BattleAnimationKind.Expose => Color.FromArgb(154, 120, 177),
            BattleAnimationKind.PoisonTick => Color.FromArgb(88, 174, 130),
            BattleAnimationKind.Defend => p.Accent,
            BattleAnimationKind.Item => p.WarmLight,
            BattleAnimationKind.EnemyAttack => p.Hazard,
            _ => p.Accent
        };

        if (animation.Kind == BattleAnimationKind.Defeat)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = i * MathF.PI * 2f / 10f;
                float radius = 10f + impact * 42f + (i % 3) * 4f;
                float fade = 1f - impact;
                using Brush particle = new SolidBrush(Color.FromArgb(
                    (int)(135 * fade),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B));
                float size = 3f + (i % 2);
                g.FillEllipse(
                    particle,
                    center.X + MathF.Cos(angle) * radius - size,
                    center.Y + MathF.Sin(angle) * radius - size,
                    size * 2,
                    size * 2);
            }
            return;
        }

        if (animation.Kind is
            BattleAnimationKind.Attack or
            BattleAnimationKind.Skill or
            BattleAnimationKind.Expose or
            BattleAnimationKind.EnemyAttack)
        {
            int radius = 10 + (int)(impact * 32f);
            int alpha = 150 - (int)(110 * impact);

            using Pen ring = new(Color.FromArgb(
                Math.Max(15, alpha),
                baseColor.R,
                baseColor.G,
                baseColor.B), 3);
            g.DrawEllipse(ring, center.X - radius, center.Y - radius, radius * 2, radius * 2);

            if (animation.Kind is BattleAnimationKind.Skill or BattleAnimationKind.Expose)
            {
                using Pen inner = new(Color.FromArgb(
                    Math.Max(10, alpha / 2),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B), 2);
                g.DrawEllipse(inner, center.X - radius / 2, center.Y - radius / 2, radius, radius);
            }
        }

        if (animation.Kind == BattleAnimationKind.Defend)
        {
            int radius = 20 + (int)(12 * MathF.Sin(t * MathF.PI));
            using Pen guard = new(Color.FromArgb(
                125,
                baseColor.R,
                baseColor.G,
                baseColor.B), 2);
            g.DrawEllipse(guard, center.X - radius, center.Y - radius, radius * 2, radius * 2);
        }

        if (animation.Kind == BattleAnimationKind.Item)
        {
            int radius = 18 + (int)(22 * MathF.Sin(t * MathF.PI));
            using Pen heal = new(Color.FromArgb(
                125,
                baseColor.R,
                baseColor.G,
                baseColor.B), 2);
            g.DrawEllipse(heal, center.X - radius, center.Y - radius, radius * 2, radius * 2);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * MathF.PI * 2f / 6f;
                float distance = 12f + t * 18f;
                using Brush spark = new SolidBrush(Color.FromArgb(
                    (int)(140 * (1f - t)),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B));
                g.FillEllipse(
                    spark,
                    center.X + MathF.Cos(angle) * distance - 2,
                    center.Y + MathF.Sin(angle) * distance - 2,
                    4,
                    4);
            }
        }

        if (animation.Kind == BattleAnimationKind.PoisonTick)
        {
            for (int i = 0; i < 7; i++)
            {
                float phase = AnimationClock.Phase(now, 520, animation.Sequence + i * 23);
                float angle = phase * MathF.PI * 2f;
                float distance = 8f + (t * 24f) + i * 2f;

                using Brush poison = new SolidBrush(Color.FromArgb(
                    (int)(120 * (1f - t)),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B));

                g.FillEllipse(
                    poison,
                    center.X + MathF.Cos(angle) * distance - 2,
                    center.Y + MathF.Sin(angle) * distance - 2,
                    4,
                    4);
            }
        }

        if (animation.Damage > 0 && IsImpactWindow(animation, now))
        {
            float rise = (AnimationClock.AttackProgress(now, animation.StartedAt, (int)animation.DurationMs) - 0.42f) * 44f;
            float fade = Math.Clamp(1f - Math.Abs(AnimationClock.AttackProgress(now, animation.StartedAt, (int)animation.DurationMs) - 0.62f) / 0.38f, 0f, 1f);

            using Brush damage = new SolidBrush(Color.FromArgb(
                (int)(235 * fade),
                baseColor.R,
                baseColor.G,
                baseColor.B));

            g.DrawString(
                animation.Damage.ToString(),
                damageFont,
                damage,
                center.X - 8,
                center.Y - 34 - rise);
        }
    }

    private void DrawTurnOrder(
        Graphics g,
        WorldPresentationProfile p,
        BattleSystem battle,
        long now)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(220, 11, 14, 18));
        g.FillRectangle(panel, 835, 95, 240, 110);

        using Pen border = new(Color.FromArgb(160, p.WallHighlight.R, p.WallHighlight.G, p.WallHighlight.B), 1);
        g.DrawRectangle(border, 835, 95, 240, 110);

        g.DrawString("TURN", smallFont, Brushes.White, 850, 110);

        using Brush activeBrush = new SolidBrush(p.Accent);
        g.DrawString(
            battle.SelectedActor == null ? "NO ACTIVE PARTY MEMBER" : $"ACTIVE  {battle.SelectedActor.Name}",
            microFont,
            activeBrush,
            850,
            132);

        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 750);
        g.DrawString(
            $"ROUND INDEX {battle.State.TurnIndex + 1:00}",
            microFont,
            Brushes.Gainsboro,
            850,
            152);

        g.DrawString(
            $"TARGET  {battle.SelectedTarget?.Name ?? "NONE"}",
            microFont,
            battle.SelectedTarget == null ? Brushes.Gray : Brushes.White,
            850,
            172);

        using Pen activeLine = new(Color.FromArgb(
            100 + (int)(80 * pulse),
            p.Accent.R,
            p.Accent.G,
            p.Accent.B), 2);
        g.DrawLine(activeLine, 850, 190, 1035, 190);
    }

    private void DrawCommands(
        Graphics g,
        WorldPresentationProfile p,
        BattleCommand selected,
        long now)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(230, 13, 16, 20));
        g.FillRectangle(panel, 35, 500, 340, 155);

        using Pen border = new(p.WallHighlight, 1);
        g.DrawRectangle(border, 35, 500, 340, 155);

        BattleCommand[] commands =
        {
            BattleCommand.Attack,
            BattleCommand.Skill,
            BattleCommand.Item,
            BattleCommand.Interact,
            BattleCommand.Defend,
            BattleCommand.Run
        };

        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 850);

        for (int i = 0; i < commands.Length; i++)
        {
            bool active = commands[i] == selected;
            Color color = active
                ? Mix(p.Accent, Color.White, 0.08f + pulse * 0.10f)
                : Color.FromArgb(190, 200, 200, 205);

            string prefix = active ? "▶ " : "  ";
            using Brush commandBrush = new SolidBrush(color);
            g.DrawString(
                prefix + commands[i].ToString().ToUpperInvariant(),
                smallFont,
                commandBrush,
                55,
                508 + i * 23);
        }
    }

    private void DrawDescription(
        Graphics g,
        WorldPresentationProfile p,
        BattleSystem battle,
        BattleAnimationEvent? animation,
        long now)
    {
        using Brush panel = new SolidBrush(Color.FromArgb(230, 13, 16, 20));
        g.FillRectangle(panel, 395, 500, 415, 155);

        using Pen border = new(p.WallHighlight, 1);
        g.DrawRectangle(border, 395, 500, 415, 155);

        using Brush actionBrush = new SolidBrush(p.Accent);
        g.DrawString("ACTION", smallFont, actionBrush, 415, 515);

        string description = animation.HasValue && !AnimationClock.IsComplete(
            now,
            animation.Value.StartedAt,
            (int)animation.Value.DurationMs)
            ? AnimationDescription(animation.Value)
            : battle.CommandMessage;

        g.DrawString(
            description,
            microFont,
            Brushes.White,
            new RectangleF(415, 545, 375, 54));

        using Brush controls = new SolidBrush(Color.FromArgb(165, 205, 210, 210));
        g.DrawString(
            "W/S or ↑/↓ COMMAND   A/< PREV TARGET   D/> NEXT TARGET   E/ENTER/SPACE EXECUTE   ESC RETREAT",
            microFont,
            controls,
            415,
            632);
    }

    private static string AnimationDescription(BattleAnimationEvent animation) =>
        animation.Kind switch
        {
            BattleAnimationKind.Attack => "Strike lands.",
            BattleAnimationKind.Skill => "Signature skill resolves.",
            BattleAnimationKind.Expose => "Expose marks the target.",
            BattleAnimationKind.EnemyAttack => "The hostile strikes back.",
            BattleAnimationKind.Defend => "Guard is raised.",
            BattleAnimationKind.Item => "Field treatment applied.",
            BattleAnimationKind.PoisonTick => "Poison pulses through the target.",
            BattleAnimationKind.Defeat => "The target collapses.",
            BattleAnimationKind.Victory => "The hostile formation collapses.",
            _ => "Action resolves."
        };

    private static Color WithAlpha(Color color, float alpha) =>
        Color.FromArgb(
            Math.Clamp((int)(color.A * alpha), 0, 255),
            color.R,
            color.G,
            color.B);

    private static Color Mix(Color a, Color b, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            AnimationClock.Lerp(a.R, b.R, amount),
            AnimationClock.Lerp(a.G, b.G, amount),
            AnimationClock.Lerp(a.B, b.B, amount));
    }

    private readonly record struct AnimationPose(int OffsetX, int OffsetY)
    {
        public static AnimationPose Zero => new(0, 0);
    }

    public void Dispose()
    {
        titleFont.Dispose();
        smallFont.Dispose();
        microFont.Dispose();
        damageFont.Dispose();
    }
}
