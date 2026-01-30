using System.Diagnostics;
using Microsoft.Maui.Graphics;

namespace TraceFit;

public partial class MainPage : ContentPage
{
    private readonly TraceEngine _engine = new();
    private readonly GaugeDrawable _gaugeDrawable = new();
    private readonly BoardDrawable _boardDrawable = new();

    private DateTime _lastUiUpdate = DateTime.MinValue;

    public MainPage()
    {
        InitializeComponent();

        GaugeView.Drawable = _gaugeDrawable;

        _boardDrawable.Engine = _engine;
        BoardView.Drawable = _boardDrawable;

        // ✅ Coordenadas precisas do GraphicsView (mouse/toque)
        BoardView.StartInteraction += BoardView_StartInteraction;
        BoardView.DragInteraction += BoardView_DragInteraction;
        BoardView.EndInteraction += BoardView_EndInteraction;

        // ✅ Aqui é EventHandler (EventArgs), não TouchEventArgs
        BoardView.CancelInteraction += BoardView_CancelInteraction;

        _engine.NewSession();
        SyncUI(force: true);
    }

    private void BoardView_StartInteraction(object? sender, TouchEventArgs e)
    {
        try
        {
            // ✅ Ao começar de novo, limpa e inicia um desenho novo
            if (_engine.HasAnyDrawing)
                _engine.ClearDrawing();

            _engine.BeginStroke();

            var p = GetFirstPoint(e);
            if (p.HasValue)
                _engine.AddPoint(ToCentered(p.Value));

            BoardView.Invalidate();
            SyncUI(force: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void BoardView_DragInteraction(object? sender, TouchEventArgs e)
    {
        try
        {
            var p = GetFirstPoint(e);
            if (p.HasValue)
                _engine.AddPoint(ToCentered(p.Value));

            BoardView.Invalidate();

            if ((DateTime.UtcNow - _lastUiUpdate).TotalMilliseconds >= 45)
            {
                _lastUiUpdate = DateTime.UtcNow;
                SyncUI(force: false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void BoardView_EndInteraction(object? sender, TouchEventArgs e)
    {
        try
        {
            var p = GetFirstPoint(e);
            if (p.HasValue)
                _engine.AddPoint(ToCentered(p.Value));

            _engine.EndStroke();

            BoardView.Invalidate();
            SyncUI(force: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    // ✅ Assinatura correta
    private void BoardView_CancelInteraction(object? sender, EventArgs e)
    {
        try
        {
            _engine.EndStroke();
            BoardView.Invalidate();
            SyncUI(force: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private static PointF? GetFirstPoint(TouchEventArgs e)
    {
        if (e.Touches == null || e.Touches.Length == 0)
            return null;

        return e.Touches[0];
    }

    private PointF ToCentered(PointF p)
    {
        float cx = (float)(BoardView.Width / 2.0);
        float cy = (float)(BoardView.Height / 2.0);

        return new PointF(p.X - cx, p.Y - cy);
    }

    private void SyncUI(bool force)
    {
        var pct = _engine.ScorePercent;

        _gaugeDrawable.Percent = (float)pct;
        GaugeView.Invalidate();

        PercentText.Text = $"{pct:0.0}%";

        var (headline, sub, badge) = Motivation.CopyForCircleGame(pct, _engine.State);
        HeadlineText.Text = headline;
        SubText.Text = sub;
        MsgBadge.Text = badge;

        if (force)
        {
            this.Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await PercentText.ScaleTo(1.04, 80, Easing.CubicOut);
                    await PercentText.ScaleTo(1.00, 120, Easing.CubicIn);
                }
                catch { }
            });
        }
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        _engine.ClearDrawing();
        BoardView.Invalidate();
        SyncUI(force: true);
    }

    private void OnNewClicked(object sender, EventArgs e)
    {
        _engine.NewSession();
        BoardView.Invalidate();
        SyncUI(force: true);
    }
}
