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

        /// <summary>세운 그림의 텍스처를 바꾼다 (포즈 바꾸기). 높이 기준은 그대로</summary>
        public void Swap(TextureRect r, Texture2D tex)
        {
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
                a.Node.Position = ToLocal(a.At) - size / 2;
            }
        }
    }
}
