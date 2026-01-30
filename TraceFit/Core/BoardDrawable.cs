using Microsoft.Maui.Graphics;

namespace TraceFit;

public sealed class BoardDrawable : IDrawable
{
    public TraceEngine? Engine { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();

        // fundo
        canvas.FillColor = Color.FromArgb("#070910");
        canvas.FillRectangle(dirtyRect);

        // grid leve (visual bonito, mas não “guia”)
        DrawSoftGrid(canvas, dirtyRect);

        if (Engine == null)
        {
            canvas.RestoreState();
            return;
        }

        // desenha strokes centralizados na tela (pontos são relativos ao centro)
        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;

        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        foreach (var stroke in Engine.Strokes)
        {
            if (stroke.Count < 2) continue;

            // glow
            canvas.StrokeSize = 16;
            canvas.StrokeColor = Color.FromArgb("#1A2348");
            DrawPath(canvas, stroke, cx, cy);

            // tinta
            canvas.StrokeSize = 6;
            canvas.StrokeColor = Color.FromArgb("#EAF0FF");
            DrawPath(canvas, stroke, cx, cy);
        }

        canvas.RestoreState();
    }

    private static void DrawPath(ICanvas canvas, IReadOnlyList<PointF> stroke, float cx, float cy)
    {
        var p0 = stroke[0];
        var path = new PathF();
        path.MoveTo(cx + p0.X, cy + p0.Y);

        for (int i = 1; i < stroke.Count; i++)
        {
            var p = stroke[i];
            path.LineTo(cx + p.X, cy + p.Y);
        }

        canvas.DrawPath(path);
    }

    private static void DrawSoftGrid(ICanvas canvas, RectF r)
    {
        canvas.StrokeSize = 1;
        canvas.StrokeColor = Color.FromArgb("#0F1330");

        float step = 48f;

        for (float x = r.Left; x <= r.Right; x += step)
            canvas.DrawLine(x, r.Top, x, r.Bottom);

        for (float y = r.Top; y <= r.Bottom; y += step)
            canvas.DrawLine(r.Left, y, r.Right, y);

        // vinheta leve
        canvas.FillColor = Color.FromArgb("#0A0C18");
        canvas.Alpha = 0.12f;
        canvas.FillRectangle(r);
        canvas.Alpha = 1f;
    }
}
