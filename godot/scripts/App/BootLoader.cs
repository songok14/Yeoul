using System;
using Godot;
using Hanjeopsi.App.Ui;

namespace Hanjeopsi.App
{
    /// <summary>
    /// 첫 장면. 데이터를 읽고 타이틀로 간다 (웹 BootScene).
    /// 사진 모드면 --screen 으로 바로 간다. lab 은 점검 화면, 그 밖의 화면은 체험 판(--demo)을 깔고 간다.
    /// </summary>
    public partial class BootLoader : Control
    {
        public override void _Ready()
        {
            Theme = UiKit.Theme;
            var gs = GameState.I;
            try
            {
                gs.LoadData();
            }
            catch (Exception e)
            {
                GD.PushError(e.ToString());
                gs.Reveal();
                var err = UiKit.Text("데이터를 읽지 못했다\n" + e.GetType().Name + ": " + e.Message, TextSize.Body, Palette.Paper, false, true);
                err.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize, 60);
                AddChild(err);
                return;
            }

            string target = gs.ShotScreen ?? "title";
            if (target == "lab")
            {
                gs.Reveal();
                CallDeferred(MethodName.Go, "core_lab");
                return;
            }
            if (gs.ShotMode && (target != "title" || gs.Demo != null)) gs.SetupDemo(gs.Demo ?? "1");
            CallDeferred(MethodName.Go, target);
        }

        private void Go(string screen)
        {
            GetTree().ChangeSceneToFile("res://scenes/" + screen + ".tscn");
        }
    }
}
