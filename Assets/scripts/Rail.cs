using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Put this on the Tilemap that has ONLY your rail tiles.
// When the game starts, it finds every line of touching rail tiles
// and turns each line into a Rail path automatically.
[RequireComponent(typeof(Tilemap))]
public class Rail : MonoBehaviour
{
    [Tooltip("Moves the path up from the tile centers. 0.5 = top of a 1-unit tile.")]
    public float pathYOffset = 0.5f;

    [Tooltip("Stretch the ends out to the edge of the last tile.")]
    public bool extendEnds = true;

    Tilemap map;
    Transform container;
    readonly HashSet<Vector3Int> cells = new HashSet<Vector3Int>();

    static readonly Vector3Int[] Ortho =
    {
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
    };
    static readonly Vector3Int[] Diag =
    {
        new Vector3Int(1, 1, 0), new Vector3Int(-1, 1, 0),
        new Vector3Int(1, -1, 0), new Vector3Int(-1, -1, 0)
    };

    void Awake()
    {
        map = GetComponent<Tilemap>();
        Build();
    }

    // Call again if you add/remove rail tiles while the game runs.
    public void Build()
    {
        if (container) Destroy(container.gameObject);
        container = new GameObject("Generated Rails").transform;
        container.SetParent(transform, false);

        cells.Clear();
        map.CompressBounds();
        foreach (var pos in map.cellBounds.allPositionsWithin)
            if (map.HasTile(pos)) cells.Add(pos);

        var used = new HashSet<(Vector3Int, Vector3Int)>();

        // 1. Start lines at ends and splits.
        foreach (var c in cells)
        {
            var ns = Neighbors(c);
            if (ns.Count != 2)
                foreach (var n in ns) Trace(c, n, used);
        }

        // 2. Whatever is left is a loop.
        foreach (var c in cells)
            foreach (var n in Neighbors(c))
                Trace(c, n, used);
    }

    void Trace(Vector3Int start, Vector3Int next, HashSet<(Vector3Int, Vector3Int)> used)
    {
        if (used.Contains(Edge(start, next))) return;

        var chain = new List<Vector3Int> { start };
        Vector3Int prev = start, cur = next;
        used.Add(Edge(prev, cur));

        while (true)
        {
            chain.Add(cur);
            var ns = Neighbors(cur);
            if (ns.Count != 2) break; // end or split

            Vector3Int nxt = ns[0] == prev ? ns[1] : ns[0];
            if (used.Contains(Edge(cur, nxt))) break; // loop closed
            used.Add(Edge(cur, nxt));
            prev = cur;
            cur = nxt;
        }

        MakeRail(chain);
    }

    void MakeRail(List<Vector3Int> chain)
    {
        if (chain.Count < 2) return;

        var pts = new List<Vector2>();
        foreach (var c in chain)
            pts.Add((Vector2)map.GetCellCenterWorld(c) + Vector2.up * pathYOffset);

        bool loop = chain[0] == chain[chain.Count - 1];
        if (extendEnds && !loop)
        {
            float half = map.cellSize.x * 0.5f;
            int last = pts.Count - 1;
            pts[0] += (pts[0] - pts[1]).normalized * half;
            pts[last] += (pts[last] - pts[last - 1]).normalized * half;
        }

        var go = new GameObject("Rail Path");
        go.transform.SetParent(container, false);
        go.AddComponent<Rail>().SetPoints(pts);
    }

    // Touching tiles. Diagonals count only when there's no square corner,
    // so stair-step slopes work but L-corners don't make extra links.
    List<Vector3Int> Neighbors(Vector3Int c)
    {
        var list = new List<Vector3Int>();
        foreach (var d in Ortho)
            if (cells.Contains(c + d)) list.Add(c + d);

        foreach (var d in Diag)
        {
            bool cornerA = cells.Contains(c + new Vector3Int(d.x, 0, 0));
            bool cornerB = cells.Contains(c + new Vector3Int(0, d.y, 0));
            if (cells.Contains(c + d) && !cornerA && !cornerB) list.Add(c + d);
        }
        return list;
    }

    static (Vector3Int, Vector3Int) Edge(Vector3Int a, Vector3Int b)
        => (a.x < b.x || (a.x == b.x && a.y < b.y)) ? (a, b) : (b, a);
}