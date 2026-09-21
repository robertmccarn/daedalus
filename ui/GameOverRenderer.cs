using System.Drawing;

public class GameOverRenderer : IDisposable
{
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 32);
    private readonly Font messageFont = new(FontFamily.GenericMonospace, 16);

    public void Draw(Graphics graphics, string message)
    {
        long now = AnimationClock.Now;
        graphics.Clear(Color.Black);
        const int frameX = 180;
        const int frameY = 180;
        const int frameWidth = 740;
        const int frameHeight = 280;

        float pulse = 0.5f + 0.5f * AnimationClock.Sine(now, 1200);
        using Pen frame = new(Color.FromArgb(150 + (int)(80 * pulse), 160, 45, 45), 2);
        graphics.DrawRectangle(frame, frameX, frameY, frameWidth, frameHeight);

        using Brush gameOver = new SolidBrush(Color.FromArgb(
            185 + (int)(70 * pulse),
            220,
            65,
            65));
        graphics.DrawString("GAME OVER", titleFont, gameOver, 390, 235);

        string displayMessage = string.IsNullOrWhiteSpace(message)
            ? "Your expedition has ended."
            : message;

        RectangleF textArea = new(frameX + 40, frameY + 130, frameWidth - 80, 100);
        graphics.DrawString(displayMessage, messageFont, Brushes.White, textArea);
    }

    public void Dispose()
    {
        titleFont.Dispose();
        messageFont.Dispose();
    }
}
