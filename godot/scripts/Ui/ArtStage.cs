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
        private TextureRect _sky;
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
                BuildSky();
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
            if (_sky != null) _sky.SelfModulate = _tint;
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

        /// <summary>
        /// 카메라 중심 (논리 좌표). 정하면 그 점이 화면 가운데에 오도록 그림을 민다(그림 밖은 보이지 않게 막는다).
        /// 걷는 마을처럼 Zoom 으로 크게 보고 여울을 따라갈 때 쓴다. null 이면 그림 가운데
        /// </summary>
        public Vector2? Focus
        {
            get { return _focus; }
            set
            {
                _focus = value;
                Layout();
            }
        }

        private Vector2? _focus;

        /// <summary>카메라가 지도 맨 위를 이만큼(화면 px) 더 내려 보일 수 있다. 위 HUD 에 가리는 높이만큼 준다</summary>
        public float PadTop { get; set; }

        /// <summary>
        /// 그림 맨 위 위로 하늘을 이만큼(논리 좌표) 이어 칠한다. 0 이면 끈다.
        /// 걷는 마을은 꼭대기(전망대)를 HUD 아래까지 내려 보여 그림 위 끝이 칼로 자른 듯 보였다.
        /// 그림 맨 위 줄 색을 가로로 크게 흐려 위로 늘이고, 꼭대기는 짙은 남색(HUD)으로 녹인다. 그림과 닿는 곳은 살짝 겹쳐 이음매를 감춘다
        /// </summary>
        public float SkyCap
        {
            get { return _skyCap; }
            set
            {
                _skyCap = value;
                BuildSky();
                Layout();
            }
        }

        private float _skyCap;
        private const float SkyFeather = 26;

        private void BuildSky()
        {
            if (_skyCap <= 0 || _bg.Texture == null)
            {
                if (_sky != null) _sky.Visible = false;
                return;
            }
            var src = _bg.Texture.GetImage();
            if (src == null) return;
            if (src.IsCompressed()) src.Decompress();
            int sw = src.GetWidth(), rows = Math.Max(2, (int)(src.GetHeight() * 0.02f));
            // 맨 위 띠의 세로 평균 → 가로로 세 번 상자 흐리기 (구름 자국이 세로 줄로 남지 않게)
            const int w = 128;
            var col = new Color[w];
            for (int x = 0; x < w; x++)
            {
                int x0 = x * sw / w, x1 = Math.Max(x0 + 1, (x + 1) * sw / w);
                float r = 0, g = 0, b = 0;
                int n = 0;
                for (int y = 0; y < rows; y += 2)
                    for (int xx = x0; xx < x1; xx += 2)
                    {
                        var c = src.GetPixel(xx, y);
                        r += c.R; g += c.G; b += c.B; n++;
                    }
                col[x] = new Color(r / n, g / n, b / n);
            }
            int rad = w / 6;
            for (int pass = 0; pass < 3; pass++)
            {
                var next = new Color[w];
                for (int x = 0; x < w; x++)
                {
                    float r = 0, g = 0, b = 0;
                    for (int k = -rad; k <= rad; k++)
                    {
                        var c = col[Math.Clamp(x + k, 0, w - 1)];
                        r += c.R; g += c.G; b += c.B;
                    }
                    int n = rad * 2 + 1;
                    next[x] = new Color(r / n, g / n, b / n);
                }
                col = next;
            }
            float lum = 0;
            foreach (var c in col) lum += (c.R + c.G + c.B) / 3;
            bool night = lum / w < 0.3f;

            // 세로: 이어 칠한 하늘(불투명) + 그림 위 끝에 겹치는 띠(점점 투명)
            int h = 128;
            int capRows = (int)(h * _skyCap / (_skyCap + SkyFeather));
            var deep = Palette.NightDeep;
            var data = new byte[w * h * 4];
            var rng = new Random(7);
            for (int y = 0; y < h; y++)
            {
                float a = 1, m = 1;
                if (y < capRows)
                {
                    float t = (float)y / Math.Max(1, capRows - 1);
                    m = Mathf.SmoothStep(0, 1, Mathf.Clamp((t - 0.3f) / 0.55f, 0, 1));
                }
                else a = 1 - Mathf.SmoothStep(0, 1, (float)(y - capRows) / Math.Max(1, h - capRows));
                for (int x = 0; x < w; x++)
                {
                    var c = deep.Lerp(col[x], m);
                    int i = (y * w + x) * 4;
                    data[i] = (byte)(c.R * 255);
                    data[i + 1] = (byte)(c.G * 255);
                    data[i + 2] = (byte)(c.B * 255);
                    data[i + 3] = (byte)(a * 255);
                }
            }
            // 밤하늘이면 늘인 하늘에도 별 몇 개
            if (night)
                for (int k = 0; k < 18; k++)
                {
                    int x = rng.Next(w), y = rng.Next((int)(capRows * 0.4f), capRows);
                    float s = 0.4f + (float)rng.NextDouble() * 0.4f;
                    int i = (y * w + x) * 4;
                    for (int ch = 0; ch < 3; ch++) data[i + ch] = (byte)(data[i + ch] + (255 - data[i + ch]) * s);
                }
            var img = Image.CreateFromData(w, h, false, Image.Format.Rgba8, data);
            if (_sky == null)
            {
                _sky = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = MouseFilterEnum.Ignore };
                AddChild(_sky);
                MoveChild(_sky, _bg.GetIndex() + 1);
            }
            _sky.Texture = ImageTexture.CreateFromImage(img);
            _sky.SelfModulate = _tint;
            _sky.Visible = true;
        }

        /// <summary>간판(Pin)을 화면 안에 붙들지 않는다 (걷는 마을은 그림을 따라 화면 밖으로 나간다)</summary>
        public bool ClampPins { get; set; } = true;

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

        /// <summary>세운 그림의 발밑 (논리 좌표). 세운 그림이 아니면 null</summary>
        public Vector2? FootOf(Control node)
        {
            foreach (var it in _items)
                if (it.Node == node) return it.At;
            return null;
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
            if (_focus.HasValue)
            {
                var o = Size / 2 - _focus.Value * K;
                Origin = new Vector2(
                    drawn.X > Size.X ? Mathf.Clamp(o.X, Size.X - drawn.X, 0) : Origin.X,
                    drawn.Y > Size.Y ? Mathf.Clamp(o.Y, Size.Y - drawn.Y, PadTop) : Origin.Y);
            }
            _bg.Position = Origin;
            _bg.Size = drawn;
            if (_sky != null && _sky.Visible)
            {
                _sky.Position = Origin - new Vector2(0, _skyCap * K);
                _sky.Size = new Vector2(drawn.X, (_skyCap + SkyFeather) * K);
            }

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
                if (ClampPins) pos.X = Mathf.Clamp(pos.X, 14, Mathf.Max(14, Size.X - size.X - 14));
                a.Node.Position = pos;
            }
        }
    }
}
