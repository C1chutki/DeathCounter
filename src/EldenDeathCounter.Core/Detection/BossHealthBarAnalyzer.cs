namespace EldenDeathCounter.Core.Detection;

public sealed class BossHealthBarAnalyzer
{
    public IReadOnlyList<BossHealthBarRegion> Analyze(int width, int height, Func<int, int, RgbPixel> getPixel)
    {
        if (width <= 0 || height <= 0)
        {
            return [];
        }

        var left = (int)(width * 0.16);
        var right = (int)(width * 0.86);
        var isLowerScreenCrop = height <= width * 0.35;
        var top = isLowerScreenCrop ? 0 : (int)(height * 0.70);
        var bottom = isLowerScreenCrop ? (int)(height * 0.92) : (int)(height * 0.91);
        var minimumSpan = (int)(width * 0.32);
        var minimumVisibleHealthSpan = (int)(width * 0.10);
        var inferredBossBarSpan = (int)(width * 0.52);
        var candidates = new List<RowCandidate>();

        for (var y = top; y < bottom; y += 2)
        {
            var row = FindRedRunOnRow(left, right, y, getPixel);
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
            .Where(cluster => cluster.Bottom - cluster.Top >= 4)
            .OrderBy(cluster => cluster.Top)
            .Take(3)
            .Select(cluster => ToRegion(cluster, width, height))
            .ToList();
    }

    // Tolerate a small horizontal gap of non-red pixels (anti-aliasing, noise, a cursor/effect crossing
    // the bar) so a dim or slightly noisy bar stays a single run instead of fragmenting below the span
    // thresholds. currentRight only advances on real red pixels, so a trailing gap never inflates a run.
    private const int MaxRedRunGap = 12;

    private static (int Left, int Right)? FindRedRunOnRow(int left, int right, int y, Func<int, int, RgbPixel> getPixel)
    {
        var bestLeft = 0;
        var bestRight = 0;
        var currentLeft = -1;
        var currentRight = -1;
        var gap = 0;

        for (var x = left; x < right; x += 2)
        {
            if (IsBossBarRed(getPixel(x, y)))
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

    private static BossHealthBarRegion ToRegion(RowCluster cluster, int width, int height)
    {
        var bar = new PixelRect(
            Math.Clamp(cluster.Left - 8, 0, width),
            Math.Clamp(cluster.Top - 2, 0, height),
            Math.Clamp(cluster.Right + 8, 0, width),
            Math.Clamp(cluster.Bottom + 4, 0, height));
        var nameRegion = new PixelRect(
            bar.Left,
            Math.Clamp(bar.Top - 58, 0, height),
            Math.Clamp(Math.Min(bar.Right, bar.Left + Math.Max(420, bar.Width / 2)), 0, width),
            Math.Clamp(bar.Top - 4, 0, height));

        return new BossHealthBarRegion(bar, nameRegion);
    }

    // Boss HP bars are a saturated dark crimson. We gate on red strongly dominating green/blue using
    // *relative* ceilings (a fraction of R) rather than absolute ones, so the same bar is recognised
    // across machines/brightness/gamma: a bright (180,40,30) and a dim (70,8,5) red both qualify,
    // while orange fire (220,150,60) and grey (120,120,120) are rejected because green sits too close
    // to red. The downstream boss-list OCR match (0.82 threshold) is the final guard against any
    // non-bar red that still slips through.
    private static bool IsBossBarRed(RgbPixel pixel)
    {
        return pixel.R >= 60 &&
               pixel.G <= pixel.R * 0.55 &&
               pixel.B <= pixel.R * 0.60;
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
