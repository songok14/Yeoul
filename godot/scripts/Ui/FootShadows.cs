using System.Collections.Generic;
using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// 땅에 선 SD 발밑의 옅은 그림자 타원. 배경 바로 위(캐릭터보다 아래)에 깔고, 매 프레임 주인 발밑을 따라간다.
    /// 주인이 톡 뛰어도 그림자는 땅에 남는다
    /// </summary>
    public partial class FootShadows : Control
    {
        private readonly ArtStage _stage;
        private readonly List<(Control Owner, float Width)> _owners = new List<(Control, float)>();

        public FootShadows() { }

        public FootShadows(ArtStage stage)
        {
            _stage = stage;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        /// <summary>그림자를 붙인다. width 는 논리 좌표 너비</summary>
        public void Add(Control owner, float width)
        {
            _owners.Add((owner, width));
        }

        public override void _Process(double delta)
        {
            _owners.RemoveAll(o => !IsInstanceValid(o.Owner));
            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_stage == null) return;
            foreach (var (owner, width) in _owners)
            {
                if (!IsInstanceValid(owner) || !owner.Visible) continue;
                var foot = _stage.FootOf(owner);
                if (foot == null) continue;
                // 발바닥 바로 아래에 걸치게 (발 뒤에 숨지 않게)
                var c = _stage.ToLocal(foot.Value) + new Vector2(0, 1.5f * _stage.K);
                float rx = width * 0.55f * _stage.K;
                // 가장자리가 부드럽게 겹쳐 칠한다
                for (int i = 0; i < 3; i++)
                {
                    float k = 1 - i * 0.24f;
                    DrawSetTransform(c, 0, new Vector2(1, 0.3f));
                    DrawCircle(Vector2.Zero, rx * k, new Color(0.1f, 0.07f, 0.12f, 0.17f * owner.Modulate.A));
                }
                DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            }
        }
    }
}
