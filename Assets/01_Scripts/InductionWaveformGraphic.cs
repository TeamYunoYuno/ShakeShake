using UnityEngine;

/// <summary>Scrolling signed-current history, drawn as a uGUI mesh without textures.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class InductionWaveformGraphic : UnityEngine.UI.MaskableGraphic
{
    [SerializeField, Min(1f)] private float historySeconds = 4f;
    [SerializeField] private Color gridColor = new Color(0.45f, 0.6f, 0.75f, 0.16f);
    [SerializeField] private Color waveformColor = new Color32(105, 241, 155, 255);
    private readonly float[] times = new float[2048];
    private readonly float[] currents = new float[2048];
    private int next;
    private int count;
    private float timeline;
    public float CurrentRange { get; private set; } = 0.5f;

    public void AddSample(float current, float duration)
    {
        if (duration <= 0f) return;
        Append(timeline, current);
        timeline += duration;
        Append(timeline, current);
    }

    private void Append(float time, float value)
    {
        times[next] = time;
        currents[next] = value;
        next = (next + 1) % times.Length;
        count = Mathf.Min(count + 1, times.Length);
    }

    public void Refresh() => SetVerticesDirty();

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        var r = rectTransform.rect;
        r.xMin += 12f; r.xMax -= 12f; r.yMin += 12f; r.yMax -= 12f;
        if (r.width <= 0f || r.height <= 0f) return;
        for (var i = 0; i <= 8; i++)
        {
            var x = Mathf.Lerp(r.xMin, r.xMax, i / 8f);
            Line(mesh, new Vector2(x, r.yMin), new Vector2(x, r.yMax), 1f, gridColor);
        }
        for (var i = 0; i <= 4; i++)
        {
            var y = Mathf.Lerp(r.yMin, r.yMax, i / 4f);
            var c = gridColor; if (i == 2) c.a *= 2f;
            Line(mesh, new Vector2(r.xMin, y), new Vector2(r.xMax, y), i == 2 ? 2f : 1f, c);
        }

        var cutoff = timeline - historySeconds;
        var peak = 0.5f;
        for (var i = 0; i < count; i++)
        {
            var index = (next - count + i + times.Length) % times.Length;
            if (times[index] >= cutoff) peak = Mathf.Max(peak, Mathf.Abs(currents[index]) * 1.15f);
        }
        var power = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(peak)));
        var normalized = peak / power;
        CurrentRange = (normalized <= 1f ? 1f : normalized <= 2f ? 2f : normalized <= 5f ? 5f : 10f) * power;

        // Empty history still displays a quiet zero-current trace.
        if (count == 0)
        {
            Line(mesh, new Vector2(r.xMin, r.center.y), new Vector2(r.xMax, r.center.y), 3f, waveformColor);
            return;
        }
        for (var layer = 0; layer < 3; layer++)
        {
            var c = waveformColor;
            c.a *= layer == 0 ? 0.08f : layer == 1 ? 0.22f : 1f;
            var width = layer == 0 ? 13f : layer == 1 ? 7f : 3f;
            Vector2 previous = default;
            var hasPrevious = false;
            for (var i = 0; i < count; i++)
            {
                var index = (next - count + i + times.Length) % times.Length;
                if (times[index] < cutoff) continue;
                var point = new Vector2(Mathf.Lerp(r.xMin, r.xMax, (times[index] - cutoff) / historySeconds),
                    r.center.y + Mathf.Clamp(currents[index] / CurrentRange, -1f, 1f) * r.height * 0.45f);
                if (hasPrevious) Line(mesh, previous, point, width, c);
                previous = point; hasPrevious = true;
            }
        }
    }

    private static void Line(UnityEngine.UI.VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
    {
        var delta = b - a;
        if (delta.sqrMagnitude < 0.000001f) return;
        var normal = new Vector2(-delta.y, delta.x).normalized * width * 0.5f;
        var index = mesh.currentVertCount;
        mesh.AddVert(a - normal, color, Vector2.zero); mesh.AddVert(a + normal, color, Vector2.zero);
        mesh.AddVert(b + normal, color, Vector2.zero); mesh.AddVert(b - normal, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index + 2, index + 3, index);
    }
}
