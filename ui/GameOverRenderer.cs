using System.Drawing;

public class GameOverRenderer : IDisposable
{
    private readonly Font titleFont = new(FontFamily.GenericMonospace, 32);
    private readonly Font messageFont = new(FontFamily.GenericMonospace, 16);

    public void Draw(Graphics graphics, string message)
    {
        graphics.Clear(Color.Black);
        const int frameX = 180;
        const int frameY = 180;
        const int frameWidth = 740;
        const int frameHeight = 280;

        graphics.DrawRectangle(Pens.White, frameX, frameY, frameWidth, frameHeight);
        graphics.DrawString("GAME OVER", titleFont, Brushes.Red, 390, 235);

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
