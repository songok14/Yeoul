using System;
using System.Collections.Generic;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 노씨 미끼가게 (웹 ShopScene). 크릴 미끼, 낚싯대·릴·쿨러 다음 단계, 채비와 아버지의 대나무대.
    /// 들어오면 노씨가 대화창(일러스트)으로 한마디 한다. 제목 줄의 노씨 얼굴을 누르면 또 이야기한다.
    /// </summary>
    public partial class ShopScreen : PlaceScreen
    {
        protected override string Music
        {
            get { return "town"; }
        }

        private int _line = -1;

        private Dictionary<string, string> T
        {
            get { return G.Data.Texts.Shop; }
        }

        private string F(string key, params object[] args)
        {
            return string.Format(T[key], args);
        }

        protected override string Backdrop
        {
            get { return "backgrounds/baitshop"; }
        }

        protected override string TitleText
        {
            get { return T["title"]; }
        }

        protected override void BuildHeader()
        {
            HeaderRight.AddChild(Avatar("nossi", 116, NossiTalk));
            Sound.Play("door", -4);
            CallDeferred(MethodName.Greet);
        }

        private void Greet()
        {
            if (GameState.I.ShotMode) return;
            var says = G.Data.Dialogue.NossiSays;
            Nossi(G.Save.Flag("nossiFavor") ? says["favor"] : NextLine());
        }

        private string NextLine()
        {
            var lines = G.Data.Dialogue.Nossi;
            if (_line < 0) _line = (int)(GameState.I.Rng.Next() * lines.Count);
            else _line = (_line + 1) % lines.Count;
            return lines[_line];
        }

        private void NossiTalk()
        {
            Nossi(NextLine());
        }

        private void Nossi(string text)
        {
            Say("nossi", "default", text);
        }

        /// <summary>둥근 얼굴 단추. 일러스트 위쪽(얼굴)만 잘라 동그랗게 보여 준다</summary>
        private Control Avatar(string speaker, float size, Action onTap)
        {
            var file = G.Data.Dialogue.SpeakerById(speaker).Face("default");
            var tex = GD.Load<Texture2D>("res://art/portraits/" + file + ".png");
            float side = tex.GetWidth();
            var face = new AtlasTexture { Atlas = tex, Region = new Rect2(side * 0.17f, 0, side * 0.62f, side * 0.62f) };

            var ring = new Panel { CustomMinimumSize = new Vector2(size, size), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Stop, MouseDefaultCursorShape = CursorShape.PointingHand };
            ring.AddThemeStyleboxOverride("panel", UiKit.Box(new Color("3f7a8a"), (int)(size / 2)).WithBorder(Palette.Paper, 6).WithShadow(0.35f, 12, 4));
            var clip = new Panel { MouseFilter = MouseFilterEnum.Ignore, ClipChildren = ClipChildrenMode.Only };
            clip.AddThemeStyleboxOverride("panel", UiKit.Box(Colors.White, (int)(size / 2 - 7)));
            clip.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 7);
            ring.AddChild(clip);
            var pic = new TextureRect { Texture = face, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore };
            pic.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            clip.AddChild(pic);

            ring.Resized += () => ring.PivotOffset = ring.Size / 2;
            ring.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
                {
                    var tw = ring.CreateTween();
                    tw.TweenProperty(ring, "scale", new Vector2(0.9f, 0.9f), 0.06);
                    tw.TweenProperty(ring, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                    onTap();
                }
            };
            // 말을 걸 수 있다는 걸 알리려고 아주 천천히 숨 쉰다
            var breathe = ring.CreateTween().SetLoops();
            breathe.TweenProperty(ring, "scale", new Vector2(1.04f, 1.04f), 1.1).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            breathe.TweenProperty(ring, "scale", Vector2.One, 1.1).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            return ring;
        }

        protected override Control BuildPage()
        {
            var paper = Paper(40, 30);
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 18);
            paper.AddChild(col);

            // 미끼
            var bait = G.Data.Gear.Bait;
            var buyBait = PriceButton(bait.Price, Palette.Green, BuyBait);
            col.AddChild(Row(GearIcon.Kind.Bait, F("bait", G.Save.Bait), bait.Name, bait.Desc, buyBait, Palette.Ink));

            // 장비 세 가지
            foreach (var slot in new[] { GearSlot.Rod, GearSlot.Reel, GearSlot.Tackle })
            {
                col.AddChild(Rule());
                col.AddChild(GearRow(slot));
            }

            // 채비와 대나무대
            col.AddChild(Rule());
            col.AddChild(Row(GearIcon.Kind.Rig, T["rig"], T["rigNow"], T["rigLater"], null, Palette.Ink));
            col.AddChild(Rule());
            var show = SmallButton(T["show"], Palette.Wood, () => Nossi(G.Data.Dialogue.NossiSays["bamboo"]), 220, 96);
            col.AddChild(Row(GearIcon.Kind.Bamboo, "", T["bamboo"], T["bambooSub"], show, Palette.WoodDark));
            return TopAligned(paper);
        }

        private Control GearRow(GearSlot slot)
        {
            int lv = G.GearLevel(slot);
            string label = T[slot == GearSlot.Rod ? "rod" : slot == GearSlot.Reel ? "reel" : "tackle"];
            var kind = slot == GearSlot.Rod ? GearIcon.Kind.Rod : slot == GearSlot.Reel ? GearIcon.Kind.Reel : GearIcon.Kind.Tackle;
            string curName = GearName(slot, lv);
            string top = label + " · " + F("now", curName);
            if (lv + 1 >= G.GearCount(slot))
                return Row(kind, label, curName, T["best"], null, Palette.GreenDark);
            int price = G.GearPrice(slot, lv + 1);
            var btn = PriceButton(price, Palette.Blue, () => Upgrade(slot));
            return Row(kind, top, GearName(slot, lv + 1), GearDesc(slot, lv + 1), btn, Palette.Ink);
        }

        private string GearName(GearSlot slot, int lv)
        {
            var g = G.Data.Gear;
            return slot == GearSlot.Rod ? g.Rods[lv].Name : slot == GearSlot.Reel ? g.Reels[lv].Name : g.Tackles[lv].Name;
        }

        private string GearDesc(GearSlot slot, int lv)
        {
            var g = G.Data.Gear;
            return slot == GearSlot.Rod ? g.Rods[lv].Desc : slot == GearSlot.Reel ? g.Reels[lv].Desc : g.Tackles[lv].Desc;
        }

        /// <summary>그림 · (작은 윗줄, 큰 이름, 설명) · 버튼</summary>
        private Control Row(GearIcon.Kind kind, string top, string name, string desc, Control button, Color nameColor)
        {
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 24);
            row.AddChild(new GearIcon(kind, 128));
            var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 2);
            if (!string.IsNullOrEmpty(top)) text.AddChild(UiKit.Text(top, TextSize.Tiny, Palette.InkSoft).Ellipsis());
            text.AddChild(UiKit.Text(name, TextSize.Body, nameColor, true, true));
            text.AddChild(UiKit.Text(desc, TextSize.Tiny + 2, Palette.InkSoft, false, true));
            row.AddChild(text);
            if (button != null) row.AddChild(button);
            return row;
        }

        private GameButton PriceButton(int price, Color color, Action onPress)
        {
            var b = SmallButton(UiKit.Won(price), color, onPress, 230, 100);
            b.SetEnabled(G.Save.Money >= price);
            return b;
        }

        private void BuyBait()
        {
            if (!G.BuyBait())
            {
                Toast(T["poor"], Palette.RedText);
                return;
            }
            HudBar.Refresh();
            FloatText(F("baitPlus", G.Data.Gear.Bait.Amount), new Vector2(GetViewportRect().Size.X / 2, SafeTop + 560), Palette.GreenText);
            Sound.Play("coin");
            Redraw();
        }

        private void Upgrade(GearSlot slot)
        {
            int next = G.GearLevel(slot) + 1;
            if (!G.UpgradeGear(slot))
            {
                Toast(T["poor"], Palette.RedText);
                return;
            }
            HudBar.Refresh();
            Toast(F("bought", GearName(slot, next)), Palette.Gold);
            Sound.Play("coin");
            Sound.Play("good", -6);
            Redraw();
            var says = G.Data.Dialogue.NossiSays;
            // 노씨 감성돔대(마지막 대)를 사면 아끼던 거라고 한마디
            bool lastRod = slot == GearSlot.Rod && next == G.GearCount(GearSlot.Rod) - 1;
            GetTree().CreateTimer(0.5).Timeout += () => Nossi(lastRod ? says["rod"] : says["gear"]);
        }

        // ───────── CI 사진 ─────────

        public override void ShotSetup(string action)
        {
            switch (action)
            {
                case "talk":
                    Nossi(G.Data.Dialogue.Nossi[2]);
                    break;
            }
        }
    }
}
