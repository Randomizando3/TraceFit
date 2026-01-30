using Microsoft.Maui.Graphics;

namespace TraceFit;

public sealed class GaugeDrawable : IDrawable
{
    public float Percent { get; set; } // 0..100

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();

        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;

        var stroke = size * 0.10f;
        var radius = (size - stroke) / 2f;
        var arcRect = new RectF(cx - radius, cy - radius, radius * 2, radius * 2);

        // base ring
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeColor = Color.FromArgb("#1E2A50");
        canvas.DrawCircle(cx, cy, radius);

        float p = Math.Clamp(Percent, 0, 100);
        float sweep = 360f * (p / 100f);

        // glow
        canvas.StrokeSize = stroke + 8;
        canvas.StrokeColor = Color.FromArgb("#101A3A");
        canvas.DrawArc(arcRect, -90, sweep, false, false);

        // arc
        canvas.StrokeSize = stroke;
        canvas.StrokeColor = PickArcColor(p);
        canvas.DrawArc(arcRect, -90, sweep, false, false);

        // inner disc
        canvas.FillColor = Color.FromArgb("#070910");
        canvas.FillCircle(cx, cy, radius - stroke * 0.60f);

        canvas.RestoreState();
    }

    private static Color PickArcColor(float pct)
    {
        if (pct < 10) return Color.FromArgb("#FF4D6D");
        if (pct < 30) return Color.FromArgb("#FF7A59");
        if (pct < 55) return Color.FromArgb("#FFD166");
        if (pct < 80) return Color.FromArgb("#4ADE80");
        if (pct < 95) return Color.FromArgb("#31C5FF");
        return Color.FromArgb("#7A5CFF");
    }
}
