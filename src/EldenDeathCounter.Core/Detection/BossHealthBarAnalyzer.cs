namespace EldenDeathCounter.Core.Detection;

public sealed class BossHealthBarAnalyzer
{
    public IReadOnlyList<BossHealthBarRegion> Analyze(int width, int height, Func<int, int, RgbPixel> getPixel)
        => Analyze(width, height, "EldenRing", getPixel);

    public IReadOnlyList<BossHealthBarRegion> Analyze(int width, int height, string? gameId, Func<int, int, RgbPixel> getPixel)
        => Analyze(width, height, gameId, BossHealthBarStyles.Vanilla, getPixel);

    public IReadOnlyList<BossHealthBarRegion> Analyze(
        int width,
        int height,
        string? gameId,
        string? bossHealthBarStyle,
        Func<int, int, RgbPixel> getPixel)
    {
        if (width <= 0 || height <= 0)
        {
            return [];
        }

        var tuning = GameTuning.For(gameId, bossHealthBarStyle);
        var left = (int)(width * tuning.LeftFraction);
        var right = (int)(width * tuning.RightFraction);
        var isBandCrop = height <= width * 0.35;
        var top = isBandCrop ? 0 : (int)(height * tuning.TopFraction);
        var bottom = isBandCrop ? (int)(height * tuning.BottomFractionForLowerCrop) : (int)(height * tuning.BottomFraction);
        var minimumSpan = (int)(width * tuning.MinimumSpanFraction);
        var minimumVisibleHealthSpan = (int)(width * tuning.MinimumVisibleSpanFraction);
        var inferredBossBarSpan = (int)(width * tuning.InferredSpanFraction);
        var candidates = new List<RowCandidate>();

        for (var y = top; y < bottom; y += 2)
        {
            var row = FindRedRunOnRow(left, right, y, tuning, getPixel);
            if (row is null)
            {
                continue;
            }

            var rowLeft = row.Value.Left;
            var rowRight = row.Value.Right;
            var visibleHealthSpan = rowRight - rowLeft;
            if (visibleHealthSpan < minimumSpan)
            {
                if (visibleHealthSpan < minimumVisibleHealthSpan)
                {
                    continue;
                }

                rowRight = Math.Min(right, rowLeft + Math.Max(minimumSpan, inferredBossBarSpan));
            }

            candidates.Add(new RowCandidate(rowLeft, rowRight, y));
        }

        if (candidates.Count == 0)
        {
            return [];
        }

        var clusters = ClusterRows(candidates);
        return clusters
            .Where(cluster =>
                cluster.Bottom - cluster.Top >= 4 &&
                cluster.Bottom - cluster.Top <= tuning.MaxClusterHeight(height))
            .OrderBy(cluster => cluster.Top)
            .Take(3)
            .Select(cluster => ToRegion(cluster, width, height, tuning))
            .ToList();
    }

    // Tolerate a small horizontal gap of non-red pixels (anti-aliasing, noise, a cursor/effect crossing
    // the bar) so a dim or slightly noisy bar stays a single run instead of fragmenting below the span
    // thresholds. currentRight only advances on real red pixels, so a trailing gap never inflates a run.
    private const int MaxRedRunGap = 12;

    private static (int Left, int Right)? FindRedRunOnRow(int left, int right, int y, GameTuning tuning, Func<int, int, RgbPixel> getPixel)
    {
        var bestLeft = 0;
        var bestRight = 0;
        var currentLeft = -1;
        var currentRight = -1;
        var gap = 0;

        for (var x = left; x < right; x += 2)
        {
            if (IsBossBarRed(getPixel(x, y), tuning))
            {
                if (currentLeft < 0)
                {
                    currentLeft = x;
                }

                currentRight = x;
                gap = 0;
                continue;
            }

            if (currentLeft < 0)
            {
                continue;
            }

            gap += 2;
            if (gap <= MaxRedRunGap)
            {
                continue;
            }

            if (currentRight - currentLeft > bestRight - bestLeft)
            {
                bestLeft = currentLeft;
                bestRight = currentRight;
            }

            currentLeft = -1;
            currentRight = -1;
            gap = 0;
        }

        if (currentLeft >= 0 && currentRight - currentLeft > bestRight - bestLeft)
        {
            bestLeft = currentLeft;
            bestRight = currentRight;
        }

        return bestRight > bestLeft ? (bestLeft, bestRight) : null;
    }

    private static IReadOnlyList<RowCluster> ClusterRows(IReadOnlyList<RowCandidate> rows)
    {
        var clusters = new List<RowCluster>();
        RowCluster? current = null;

        foreach (var row in rows)
        {
            if (current is null || row.Y - current.Bottom > 8)
            {
                current = new RowCluster(row.Left, row.Right, row.Y, row.Y);
                clusters.Add(current);
                continue;
            }

            current.Left = Math.Min(current.Left, row.Left);
            current.Right = Math.Max(current.Right, row.Right);
            current.Bottom = row.Y;
        }

        return clusters;
    }

    private static BossHealthBarRegion ToRegion(RowCluster cluster, int width, int height, GameTuning tuning)
    {
        var bar = new PixelRect(
            Math.Clamp(cluster.Left - 8, 0, width),
            Math.Clamp(cluster.Top - 2, 0, height),
            Math.Clamp(cluster.Right + 8, 0, width),
            Math.Clamp(cluster.Bottom + 4, 0, height));

        // The boss name is left-anchored above the bar. Elden Ring's bright bar is detected at its true
        // left edge, so the name region starts there. Dark Souls III's bar is dimmer and its name sits
        // further left than the detected red fill (the bar frame extends left of the visible health), so
        // the name region is shifted left by a game-specific fraction of the screen width.
        // Sekiro is the exception: its name plate sits *below* the bar, so the band is mirrored.
        var nameLeft = Math.Clamp(bar.Left - (int)(width * tuning.NameLeftInsetFraction), 0, width);
        var nameTop = tuning.NameBelowBar ? bar.Bottom + 4 : bar.Top - 58;
        var nameBottom = tuning.NameBelowBar ? bar.Bottom + 58 : bar.Top - 4;
        var nameRegion = new PixelRect(
            nameLeft,
            Math.Clamp(nameTop, 0, height),
            Math.Clamp(Math.Min(bar.Right, nameLeft + Math.Max(420, bar.Width / 2)), 0, width),
            Math.Clamp(nameBottom, 0, height));

        return new BossHealthBarRegion(bar, nameRegion);
    }

    // Boss HP bars are a saturated dark crimson. We gate on red strongly dominating green/blue using
    // *relative* ceilings (a fraction of R) rather than absolute ones, so the same bar is recognised
    // across machines/brightness/gamma: a bright (180,40,30) and a dim (70,8,5) red both qualify,
    // while orange fire (220,150,60) and grey (120,120,120) are rejected because green sits too close
    // to red. The downstream boss-list OCR match (0.82 threshold) is the final guard against any
    // non-bar red that still slips through.
    private static bool IsBossBarRed(RgbPixel pixel, GameTuning tuning)
    {
        return pixel.R >= tuning.MinRed &&
               pixel.G <= pixel.R * tuning.MaxGreenFraction &&
               pixel.B <= pixel.R * tuning.MaxBlueFraction;
    }

    // Per-game heuristics for the otherwise identical bar scan. Dark Souls III's bar is a dim, thin
    // crimson line whose red channel sits right at Elden Ring's 60 floor, so it needs a lower minimum
    // and a name region shifted left of the visible health (see ToRegion). All other games keep the
    // Elden-Ring-tuned values, so their detection is unchanged.
    private readonly record struct GameTuning(
        int MinRed,
        double NameLeftInsetFraction,
        double BottomFraction,
        double BottomFractionForLowerCrop,
        double MinimumSpanFraction,
        double MaxClusterHeightFraction,
        double MaxGreenFraction = 0.55,
        double MaxBlueFraction = 0.60,
        double LeftFraction = 0.16,
        double RightFraction = 0.86,
        double TopFraction = 0.70,
        double MinimumVisibleSpanFraction = 0.10,
        double InferredSpanFraction = 0.52,
        bool NameBelowBar = false)
    {
        public int MaxClusterHeight(int height) => Math.Max(24, (int)(height * MaxClusterHeightFraction));

        public static GameTuning For(string? gameId, string? bossHealthBarStyle)
        {
            if (string.Equals(gameId?.Trim(), "DarkSouls3", StringComparison.OrdinalIgnoreCase))
            {
                return new GameTuning(
                    MinRed: 48,
                    NameLeftInsetFraction: 0.075,
                    BottomFraction: 0.91,
                    BottomFractionForLowerCrop: 0.92,
                    MinimumSpanFraction: 0.32,
                    MaxClusterHeightFraction: 1.0);
            }

            // Dark Souls II's boss bar is a dark, desaturated crimson: its red channel sits well below
            // Elden Ring's 60 floor and its green/blue sit closer to red than the vanilla ceilings allow
            // (~G 0.55R, B 0.5R), so it needs a lower red minimum and slightly looser colour ceilings.
            // Grey UI (G≈R) and orange fire (G>0.62R) are still rejected. Its name also starts slightly
            // left of the visible red fill, so the name region is inset left by a small screen fraction.
            if (string.Equals(gameId?.Trim(), "DarkSouls2", StringComparison.OrdinalIgnoreCase))
            {
                return new GameTuning(
                    MinRed: 35,
                    NameLeftInsetFraction: 0.03,
                    BottomFraction: 0.91,
                    BottomFractionForLowerCrop: 0.92,
                    MinimumSpanFraction: 0.32,
                    MaxClusterHeightFraction: 1.0,
                    MaxGreenFraction: 0.62,
                    MaxBlueFraction: 0.62);
            }

            // Sekiro's bar is a short crimson strip at the top-left (x ~0.06..0.29, y ~0.09 of the
            // screen) with the name plate below it, so the whole scan window moves to the top-left and
            // the span thresholds shrink: a full Sekiro bar is only ~0.22 of the screen width, far under
            // the 0.32 every bottom-centre game uses.
            if (string.Equals(gameId?.Trim(), "Sekiro", StringComparison.OrdinalIgnoreCase))
            {
                return new GameTuning(
                    MinRed: 45,
                    NameLeftInsetFraction: 0.0,
                    BottomFraction: 0.22,
                    BottomFractionForLowerCrop: 1.0,
                    MinimumSpanFraction: 0.06,
                    MaxClusterHeightFraction: 1.0,
                    LeftFraction: 0.03,
                    RightFraction: 0.60,
                    TopFraction: 0.02,
                    MinimumVisibleSpanFraction: 0.04,
                    InferredSpanFraction: 0.22,
                    NameBelowBar: true);
            }

            if (BossHealthBarStyles.Normalize(bossHealthBarStyle) == BossHealthBarStyles.Reforged)
            {
                return new GameTuning(
                    MinRed: 55,
                    NameLeftInsetFraction: 0.0,
                    BottomFraction: 0.93,
                    BottomFractionForLowerCrop: 0.96,
                    MinimumSpanFraction: 0.28,
                    MaxClusterHeightFraction: 0.03);
            }

            return new GameTuning(
                MinRed: 60,
                NameLeftInsetFraction: 0.0,
                BottomFraction: 0.91,
                BottomFractionForLowerCrop: 0.92,
                MinimumSpanFraction: 0.32,
                MaxClusterHeightFraction: 1.0);
        }
    }

    private readonly record struct RowCandidate(int Left, int Right, int Y);

    private sealed class RowCluster
    {
        public RowCluster(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public int Left { get; set; }

        public int Right { get; set; }

        public int Top { get; }

        public int Bottom { get; set; }
    }
}
