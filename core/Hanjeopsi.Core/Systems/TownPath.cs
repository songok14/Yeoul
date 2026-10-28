using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hanjeopsi.Core.Data;

namespace Hanjeopsi.Core.Systems
{
    /// <summary>
    /// 걷는 마을 길 찾기 (townmap.json). 길 그물 위에서 누른 곳에 가장 가까운 점을 찾고,
    /// 지금 자리에서 그 점까지 그물을 따라 가장 짧은 길(점 목록)을 낸다. 원근에 따른 SD 크기도 여기서 정한다
    /// </summary>
    public sealed class TownPath
    {
        private readonly TownMap _map;
        private readonly Dictionary<string, Vector2> _pos = new Dictionary<string, Vector2>();
        private readonly List<(string A, string B)> _edges = new List<(string, string)>();
        private readonly Dictionary<string, List<string>> _next = new Dictionary<string, List<string>>();

        public TownPath(TownMap map)
        {
            _map = map;
            foreach (var n in map.Nodes)
            {
                _pos[n.Id] = new Vector2(n.X, n.Y);
                _next[n.Id] = new List<string>();
            }
            foreach (var e in map.Edges)
            {
                if (e.Count != 2) throw new FormatException("마을 길 이음은 점 두 개여야 한다");
                string a = e[0], b = e[1];
                if (!_pos.ContainsKey(a) || !_pos.ContainsKey(b)) throw new KeyNotFoundException("없는 마을 길 점: " + a + "-" + b);
                _edges.Add((a, b));
                _next[a].Add(b);
                _next[b].Add(a);
            }
        }

        public TownMap Map
        {
            get { return _map; }
        }

        public Vector2 PosOf(string node)
        {
            return _pos[node];
        }

        /// <summary>길 위 한 점: 이음(A-B)과 그 위 비율 t (0이면 A, 1이면 B)</summary>
        public struct OnPath
        {
            public string A, B;
            public float T;
            public Vector2 At;
        }

        /// <summary>점 p 에서 가장 가까운 길 위 점</summary>
        public OnPath Nearest(Vector2 p)
        {
            var best = new OnPath();
            float bestD = float.MaxValue;
            foreach (var (a, b) in _edges)
            {
                Vector2 pa = _pos[a], pb = _pos[b];
                var ab = pb - pa;
                float len2 = ab.LengthSquared();
                float t = len2 <= 0 ? 0 : Math.Clamp(Vector2.Dot(p - pa, ab) / len2, 0, 1);
                var q = pa + ab * t;
                float d = Vector2.DistanceSquared(p, q);
                if (d < bestD)
                {
                    bestD = d;
                    best = new OnPath { A = a, B = b, T = t, At = q };
                }
            }
            return best;
        }

        /// <summary>길 위 점 from 에서 to 까지 그물을 따라 가는 점 목록 (from 자리는 빼고, to 자리로 끝난다)</summary>
        public List<Vector2> Route(Vector2 from, Vector2 to)
        {
            var s = Nearest(from);
            var g = Nearest(to);
            // 같은 이음 위면 바로 간다
            if ((s.A == g.A && s.B == g.B) || (s.A == g.B && s.B == g.A)) return new List<Vector2> { g.At };
            // 출발 이음의 두 끝과 도착 이음의 두 끝 사이 가장 짧은 길 (다익스트라)
            var dist = _pos.Keys.ToDictionary(k => k, _ => float.MaxValue);
            var prev = new Dictionary<string, string>();
            var open = new HashSet<string>(_pos.Keys);
            dist[s.A] = Vector2.Distance(s.At, _pos[s.A]);
            dist[s.B] = Math.Min(dist[s.B], Vector2.Distance(s.At, _pos[s.B]));
            while (open.Count > 0)
            {
                string u = open.OrderBy(k => dist[k]).First();
                open.Remove(u);
                if (dist[u] == float.MaxValue) break;
                foreach (var v in _next[u])
                {
                    if (!open.Contains(v)) continue;
                    float nd = dist[u] + Vector2.Distance(_pos[u], _pos[v]);
                    if (nd < dist[v])
                    {
                        dist[v] = nd;
                        prev[v] = u;
                    }
                }
            }
            float viaA = dist[g.A] + Vector2.Distance(_pos[g.A], g.At);
            float viaB = dist[g.B] + Vector2.Distance(_pos[g.B], g.At);
            string end = viaA <= viaB ? g.A : g.B;
            var nodes = new List<string>();
            for (string c = end; c != null; c = prev.TryGetValue(c, out var p) ? p : null) nodes.Insert(0, c);
            var route = nodes.Select(n => _pos[n]).ToList();
            route.Add(g.At);
            return route;
        }

        /// <summary>문 앞 점까지 가는 길</summary>
        public List<Vector2> RouteToNode(Vector2 from, string node)
        {
            return Route(from, _pos[node]);
        }

        /// <summary>원근: 지도 위쪽일수록 작게 (앞쪽 크기에 곱하는 배율)</summary>
        public float ScaleAt(float y)
        {
            var s = _map.Scale;
            if (s.BottomY <= s.TopY) return 1;
            float t = (y - s.TopY) / (s.BottomY - s.TopY);
            float k = s.Top + (s.Bottom - s.Top) * t;
            return Math.Clamp(k, Math.Min(s.Top, s.Bottom), s.Max);
        }

        /// <summary>모든 점이 한 그물로 이어져 있나 (데이터 점검)</summary>
        public bool Connected()
        {
            if (_pos.Count == 0) return true;
            var seen = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(_pos.Keys.First());
            while (stack.Count > 0)
            {
                var u = stack.Pop();
                if (!seen.Add(u)) continue;
                foreach (var v in _next[u]) stack.Push(v);
            }
            return seen.Count == _pos.Count;
        }

        /// <summary>점 목록을 따라가는 길이</summary>
        public static float Length(Vector2 from, IList<Vector2> route)
        {
            float sum = 0;
            var p = from;
            foreach (var q in route)
            {
                sum += Vector2.Distance(p, q);
                p = q;
            }
            return sum;
        }
    }
}
