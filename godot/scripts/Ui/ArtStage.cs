using System;
using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 배경 그림 한 장을 화면에 꽉 차게(넘치는 쪽은 잘라) 깔고, 그림 위 좌표로 물건을 세운다.
    /// 좌표는 웹 논리 좌표(450×800)를 그대로 쓴다. 화면 비율이 달라도 간판이 건물 위에 붙어 있다.
    /// </summary>
    public partial class ArtStage : Control
    {
        public static readonly Vector2 Logical = new Vector2(450, 800);

        private readonly TextureRect _bg;
        private readonly List<(Control Node, Vector2 At, Vector2 Origin, float Height)> _items = new List<(Control, Vector2, Vector2, float)>();
        private readonly List<(Control Node, Vector2 At)> _anchored = new List<(Control, Vector2)>();
        private float _zoom = 1;

        /// <summary>논리 좌표 1이 화면 몇 픽셀인지</summary>
        public float K { get; private set; } = 2.4f;
        public Vector2 Origin { get; private set; }

        public ArtStage()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            ClipContents = true;
            _bg = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(_bg);
            Resized += Layout;
        }

        public Texture2D Background
        {
            get { return _bg.Texture; }
            set
            {
                _bg.Texture = value;
                Layout();
            }
        }

        /// <summary>배경 그림에만 색을 살짝 입힌다 (시간대 분위기). 위에 세운 그림은 그대로</summary>
        public Color Tint
        {
            get { return _tint; }
            set
            {
                _tint = value;
                ApplyTint();
            }
        }

        /// <summary>
        /// 시간대 색을 입혀도 하늘의 흰 구름은 하얗게 둔다 (낚시·트럭).
        /// 그림 위쪽(하늘)의 밝고 채도가 낮은 곳만 원래 색을 지킨다. 이야기 밤 장면처럼 전체를 물들일 때는 끈다
        /// </summary>
        public bool KeepClouds
        {
            get { return _keepClouds; }
            set
            {
                _keepClouds = value;
                ApplyTint();
            }
        }

        private Color _tint = Colors.White;
        private bool _keepClouds;
        private static Shader _cloudShader;

        private const string CloudShaderCode = @"shader_type canvas_item;
uniform vec4 tint : source_color = vec4(1.0);
void fragment() {
    vec4 c = texture(TEXTURE, UV);
    float mx = max(c.r, max(c.g, c.b));
    float mn = min(c.r, min(c.g, c.b));
    float sat = mx > 0.001 ? (mx - mn) / mx : 0.0;
    // 밝고 하얀 곳 × 하늘(위쪽 30~40% 아래로는 서서히 빠진다)
    float cloud = smoothstep(0.66, 0.84, mx) * (1.0 - smoothstep(0.14, 0.3, sat)) * (1.0 - smoothstep(0.30, 0.40, UV.y));
    COLOR = vec4(mix(c.rgb * tint.rgb, c.rgb, cloud), c.a);
}";

        private void ApplyTint()
        {
            if (_keepClouds)
            {
                _cloudShader ??= new Shader { Code = CloudShaderCode };
                if (_bg.Material is not ShaderMaterial m)
                {
                    m = new ShaderMaterial { Shader = _cloudShader };
                    _bg.Material = m;
                }
                m.SetShaderParameter("tint", _tint);
                _bg.SelfModulate = Colors.White;
            }
            else
            {
                _bg.Material = null;
                _bg.SelfModulate = _tint;
            }
        }

        /// <summary>배경을 아주 천천히 숨 쉬듯 확대한다 (1이면 그대로)</summary>
        public float Zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value;
                Layout();
            }
        }

        /// <summary>배경 그림을 읽는다. 최적화한 WebP 가 있으면 그것을, 없으면 PNG 를 쓴다</summary>
        public static Texture2D LoadBackground(string name)
        {
            string path = "res://art/backgrounds/" + name;
            if (ResourceLoader.Exists(path + ".webp")) return GD.Load<Texture2D>(path + ".webp");
            return GD.Load<Texture2D>(path + ".png");
        }

        /// <summary>그림(SD, 소품)을 발밑 기준으로 세운다. height 는 논리 좌표 높이</summary>
        public TextureRect Place(Texture2D tex, Vector2 at, float height, Vector2? origin = null)
        {
            var r = new TextureRect { Texture = tex, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(r);
            _items.Add((r, at, origin ?? new Vector2(0.5f, 1f), height));
            Layout();
            return r;
        }

        /// <summary>크기를 스스로 정하는 Control(간판 등)을 그림 위 점에 가운데 맞춰 단다</summary>
        public void Pin(Control c, Vector2 at)
        {
            AddChild(c);
            _anchored.Add((c, at));
            c.Resized += Layout;
            Layout();
        }

        /// <summary>세운 그림이나 단 것을 뗀다</summary>
        public void Remove(Control c)
        {
            _items.RemoveAll(it => it.Node == c);
            _anchored.RemoveAll(it => it.Node == c);
            if (IsInstanceValid(c)) c.QueueFree();
        }

        /// <summary>세운 그림의 텍스처를 바꾼다 (포즈 바꾸기). 높이 기준은 그대로</summary>
        public void Swap(TextureRect r, Texture2D tex)
        {
            r.Texture = tex;
            Layout();
        }

        /// <summary>포즈가 바뀌어 그림 크기와 기준점이 다를 때 (높이와 기준점도 새로)</summary>
        public void Swap(TextureRect r, Texture2D tex, float height, Vector2 origin)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Node != r) continue;
                var it = _items[i];
                _items[i] = (it.Node, it.At, origin, height);
            }
            r.Texture = tex;
            Layout();
        }

        /// <summary>세운 그림을 다른 자리로 (발밑 기준)</summary>
        public void Move(Control node, Vector2 at)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Node != node) continue;
                var it = _items[i];
                _items[i] = (it.Node, at, it.Origin, it.Height);
            }
            for (int i = 0; i < _anchored.Count; i++)
                if (_anchored[i].Node == node) _anchored[i] = (node, at);
            Layout();
        }

        /// <summary>화면 픽셀(이 Control 안) → 논리 좌표</summary>
        public Vector2 ToLogical(Vector2 local)
        {
            return (local - Origin) / K;
        }

        /// <summary>논리 좌표 → 이 Control 안 픽셀</summary>
        public Vector2 ToLocal(Vector2 logical)
        {
            return Origin + logical * K;
        }

        private void Layout()
        {
            if (Size.X <= 0 || Size.Y <= 0) return;
            K = Mathf.Max(Size.X / Logical.X, Size.Y / Logical.Y) * _zoom;
            var drawn = Logical * K;
            Origin = (Size - drawn) / 2;
            _bg.Position = Origin;
            _bg.Size = drawn;

            foreach (var it in _items)
            {
                var t = it.Node is TextureRect tr ? tr.Texture : null;
                if (t == null) continue;
                float h = it.Height * K;
                var size = new Vector2(t.GetWidth() * h / t.GetHeight(), h);
                it.Node.Size = size;
                it.Node.PivotOffset = new Vector2(size.X * it.Origin.X, size.Y * it.Origin.Y);
                it.Node.Position = ToLocal(it.At) - it.Node.PivotOffset;
            }
            foreach (var a in _anchored)
            {
                var size = a.Node.GetCombinedMinimumSize();
                a.Node.Size = size;
                a.Node.PivotOffset = size / 2;
                // 화면이 길어 그림 양옆이 잘려도 간판은 화면 안에 남긴다
                var pos = ToLocal(a.At) - size / 2;
                pos.X = Mathf.Clamp(pos.X, 14, Mathf.Max(14, Size.X - size.X - 14));
                a.Node.Position = pos;
            }
        }
    }
}
