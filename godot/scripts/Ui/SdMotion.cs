using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>
    /// SD 캐릭터의 작은 움직임. 그림은 걷기 두 장뿐이라 크기와 기울기로 살린다.
    /// 노드의 축(PivotOffset)이 발밑이라 발과 그림자는 제자리에 있다
    /// </summary>
    public static class SdMotion
    {
        /// <summary>걷는 중: 걸음마다 발이 닿을 때 살짝 눌렸다 펴지고 좌우로 기우뚱한다. step 은 걸음 수(소수)</summary>
        public static void Walk(Control n, float step, float amount = 1)
        {
            float lift = Mathf.Abs(Mathf.Sin(step * Mathf.Pi));
            n.Scale = new Vector2(1 + 0.03f * amount * (1 - lift), 1 - 0.03f * amount + 0.06f * amount * lift);
            n.Rotation = Mathf.Sin(step * Mathf.Pi) * 0.04f * amount;
        }

        /// <summary>서 있을 때 숨 쉬기. seed 로 사람마다 박자를 어긋나게 한다</summary>
        public static void Breathe(Control n, float time, float seed = 0, float speed = 2.2f, float amount = 0.012f)
        {
            n.Scale = new Vector2(1, 1 + Mathf.Sin(time * speed + seed) * amount);
            n.Rotation = 0;
        }

        /// <summary>고개 까딱: 0~1 진행에 따라 앞으로 숙였다 돌아온다 (얼굴 쪽으로)</summary>
        public static void Nod(Control n, float t, bool faceLeft)
        {
            float a = Mathf.Sin(Mathf.Clamp(t, 0, 1) * Mathf.Pi) * 0.06f;
            n.Rotation = faceLeft ? -a : a;
            n.Scale = new Vector2(1, 1 - a * 0.3f);
        }
    }
}
