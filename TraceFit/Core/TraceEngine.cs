using Microsoft.Maui.Graphics;

namespace TraceFit;

public enum CircleGameState
{
    Idle,
    Drawing,
    TooSmall,
    NotEnoughCoverage,
    ScribblePenalty,
    Good
}

public sealed class TraceEngine
{
    private readonly List<List<PointF>> _strokes = new();
    private List<PointF>? _currentStroke;

    public IReadOnlyList<IReadOnlyList<PointF>> Strokes => _strokes;

    public double ScorePercent { get; private set; }
    public CircleGameState State { get; private set; } = CircleGameState.Idle;

    public bool HasAnyDrawing => _strokes.Sum(s => s.Count) > 6;

    // Regras
    public int MinPoints { get; set; } = 80;
    public float MinRadiusPx { get; set; } = 26f;

    // Cobertura angular (0..1)
    public int CoverageBins { get; set; } = 180;

    // Antes você "zerava" se coverage < MinCoverage. Agora:
    // - MinCoverage vira apenas um marcador para mensagem
    // - Pontuação passa a ser proporcional à cobertura, SEM zerar
    public double MinCoverage { get; set; } = 0.55;

    // Cobertura mínima para começar a pontuar (evita 2 risquinhos darem nota)
    public double MinCoverageToScore { get; set; } = 0.18; // ~65 graus

    // Suavização enquanto desenha
    public double SmoothAlpha { get; set; } = 0.22;

    public void NewSession()
    {
        ClearDrawing();
        State = CircleGameState.Idle;
    }

    public void ClearDrawing()
    {
        _strokes.Clear();
        _currentStroke = null;
        ScorePercent = 0;
        State = CircleGameState.Idle;
    }

    public void BeginStroke()
    {
        _currentStroke = new List<PointF>(1024);
        _strokes.Add(_currentStroke);
        State = CircleGameState.Drawing;
    }

    public void EndStroke()
    {
        // ✅ Ao soltar: cálculo final (sem suavização)
        Evaluate(final: true);

        if (!HasAnyDrawing)
            State = CircleGameState.Idle;
    }

    public void AddPoint(PointF centeredPoint)
    {
        if (_currentStroke == null) return;

        // filtro simples (evita ruído)
        if (_currentStroke.Count > 0)
        {
            var last = _currentStroke[^1];
            var dx = centeredPoint.X - last.X;
            var dy = centeredPoint.Y - last.Y;
            if ((dx * dx + dy * dy) < 1.2f) // ~1px
                return;
        }

        _currentStroke.Add(centeredPoint);

        // ✅ Enquanto desenha: calcula em tempo real (com suavização)
        Evaluate(final: false);
    }

    private void Evaluate(bool final)
    {
        var points = Flatten();
        if (points.Count < MinPoints)
        {
            ScorePercent = final ? 0 : Smooth(ScorePercent, 0, SmoothAlpha);
            State = CircleGameState.Drawing;
            return;
        }

        if (!TryFitCircleKasa(points, out var center, out var radius))
        {
            ScorePercent = final ? 0 : Smooth(ScorePercent, 0, SmoothAlpha);
            State = CircleGameState.Drawing;
            return;
        }

        if (radius < MinRadiusPx)
        {
            ScorePercent = final ? 0 : Smooth(ScorePercent, 0, SmoothAlpha);
            State = CircleGameState.TooSmall;
            return;
        }

        var coverage = ComputeCoverage(points, center, CoverageBins);

        // ✅ Agora não zera, mas evita pontuar com cobertura ridiculamente baixa
        if (coverage < MinCoverageToScore)
        {
            ScorePercent = final ? 0 : Smooth(ScorePercent, 0, SmoothAlpha);
            State = CircleGameState.NotEnoughCoverage;
            return;
        }

        var (meanR, stdR) = RadialStats(points, center);
        var radialCv = (meanR <= 1e-6) ? 1.0 : (stdR / meanR);

        var length = PathLength(points);
        var circ = 2.0 * Math.PI * meanR;
        var ratio = (circ <= 1e-6) ? 999.0 : (length / circ);

        // Penalizações/ganhos em [0..1]
        var radialPenalty = Math.Exp(-radialCv * 6.0);           // redondeza
        var lengthPenalty = Math.Exp(-Math.Abs(ratio - 1.0) * 1.35); // rabisco

        // ✅ Cobertura proporcional: arco menor ainda ganha algo, mas não ganha “tanto”
        // Mapeia coverage de [MinCoverageToScore..1] => [0..1]
        var cov01 = (coverage - MinCoverageToScore) / (1.0 - MinCoverageToScore);
        cov01 = Math.Clamp(cov01, 0, 1);

        // Peso de cobertura: sublinear pra não premiar demais meia-volta
        var coverageWeight = Math.Pow(cov01, 0.85);

        var raw = 100.0 * coverageWeight * radialPenalty * lengthPenalty;
        raw = Math.Clamp(raw, 0, 100);

        if (final)
            ScorePercent = raw; // ✅ resultado final “carimbado”
        else
            ScorePercent = Smooth(ScorePercent, raw, SmoothAlpha);

        // Estado para mensagens
        if (coverage < MinCoverage)
            State = CircleGameState.NotEnoughCoverage;
        else if (ratio > 2.2 && ScorePercent < 30)
            State = CircleGameState.ScribblePenalty;
        else
            State = CircleGameState.Good;
    }

    private List<PointF> Flatten()
    {
        var total = _strokes.Sum(s => s.Count);
        var list = new List<PointF>(Math.Max(256, total));
        foreach (var s in _strokes) list.AddRange(s);
        return list;
    }

    private static double PathLength(List<PointF> pts)
    {
        double sum = 0;
        for (int i = 1; i < pts.Count; i++)
        {
            var dx = pts[i].X - pts[i - 1].X;
            var dy = pts[i].Y - pts[i - 1].Y;
            sum += Math.Sqrt(dx * dx + dy * dy);
        }
        return sum;
    }

    private static (double mean, double std) RadialStats(List<PointF> pts, PointF c)
    {
        double sum = 0, sum2 = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var dx = pts[i].X - c.X;
            var dy = pts[i].Y - c.Y;
            var r = Math.Sqrt(dx * dx + dy * dy);
            sum += r;
            sum2 += r * r;
        }

        var n = pts.Count;
        var mean = sum / n;
        var var = Math.Max(0, (sum2 / n) - (mean * mean));
        var std = Math.Sqrt(var);
        return (mean, std);
    }

    private static double ComputeCoverage(List<PointF> pts, PointF c, int bins)
    {
        var hit = new bool[bins];
        int count = 0;

        for (int i = 0; i < pts.Count; i++)
        {
            var dx = pts[i].X - c.X;
            var dy = pts[i].Y - c.Y;

            var ang = Math.Atan2(dy, dx);
            var norm = (ang + Math.PI) / (2.0 * Math.PI);
            var idx = (int)Math.Floor(norm * bins);
            if (idx < 0) idx = 0;
            if (idx >= bins) idx = bins - 1;

            if (!hit[idx])
            {
                hit[idx] = true;
                count++;
            }
        }

        return (double)count / bins;
    }

    /// <summary>
    /// Ajuste de círculo (Kåsa):
    /// x^2 + y^2 = a x + b y + c
    /// center = (a/2, b/2), r = sqrt(c + center^2)
    /// </summary>
    private static bool TryFitCircleKasa(List<PointF> pts, out PointF center, out double radius)
    {
        center = default;
        radius = 0;

        int n = pts.Count;
        if (n < 3) return false;

        double Sx = 0, Sy = 0, Sxx = 0, Syy = 0, Sxy = 0;
        double Sz = 0, Sxz = 0, Syz = 0;

        for (int i = 0; i < n; i++)
        {
            double x = pts[i].X;
            double y = pts[i].Y;
            double z = x * x + y * y;

            Sx += x;
            Sy += y;
            Sxx += x * x;
            Syy += y * y;
            Sxy += x * y;

            Sz += z;
            Sxz += x * z;
            Syz += y * z;
        }

        double[,] A =
        {
            { Sxx, Sxy, Sx },
            { Sxy, Syy, Sy },
            { Sx,  Sy,  n  }
        };

        double[] B = { Sxz, Syz, Sz };

        if (!Solve3x3(A, B, out var X))
            return false;

        double a = X[0];
        double b = X[1];
        double c = X[2];

        double cx = a / 2.0;
        double cy = b / 2.0;
        double r2 = c + cx * cx + cy * cy;

        if (r2 <= 1e-6) return false;

        center = new PointF((float)cx, (float)cy);
        radius = Math.Sqrt(r2);
        return true;
    }

    private static bool Solve3x3(double[,] A, double[] B, out double[] X)
    {
        X = new double[3];

        double[,] m = new double[3, 4]
        {
            { A[0,0], A[0,1], A[0,2], B[0] },
            { A[1,0], A[1,1], A[1,2], B[1] },
            { A[2,0], A[2,1], A[2,2], B[2] },
        };

        for (int col = 0; col < 3; col++)
        {
            int pivot = col;
            double max = Math.Abs(m[col, col]);
            for (int row = col + 1; row < 3; row++)
            {
                var v = Math.Abs(m[row, col]);
                if (v > max) { max = v; pivot = row; }
            }

            if (max < 1e-12) return false;

            if (pivot != col)
            {
                for (int k = col; k < 4; k++)
                    (m[col, k], m[pivot, k]) = (m[pivot, k], m[col, k]);
            }

            double div = m[col, col];
            for (int k = col; k < 4; k++) m[col, k] /= div;

            for (int row = 0; row < 3; row++)
            {
                if (row == col) continue;
                double factor = m[row, col];
                for (int k = col; k < 4; k++)
                    m[row, k] -= factor * m[col, k];
            }
        }

        X[0] = m[0, 3];
        X[1] = m[1, 3];
        X[2] = m[2, 3];
        return true;
    }

    private static double Smooth(double prev, double next, double alpha)
        => (alpha * next) + ((1 - alpha) * prev);
}
