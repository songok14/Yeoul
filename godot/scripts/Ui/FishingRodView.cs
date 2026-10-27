using Godot;

namespace Hanjeopsi.App.Ui
{
    /// <summary>낚싯대 몸통 (FishingField 가 계산한 모양을 여울 그림 아래 층에 그린다)</summary>
    public partial class FishingRodView : Control
    {
        private readonly FishingField _field;

        public FishingRodView() { }

        public FishingRodView(FishingField field)
        {
            _field = field;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Process(double delta)
        {
            QueueRedraw();
        }

        public override void _Draw()
        {
            _field?.PaintRod(this);
        }
    }
}
