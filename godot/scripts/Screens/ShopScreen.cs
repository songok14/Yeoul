using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Hanjeopsi.App.Ui;
using Hanjeopsi.Core;
using Hanjeopsi.Core.Data;

namespace Hanjeopsi.App.Screens
{
    /// <summary>
    /// 노씨 미끼가게 (웹 ShopScene). 탭 셋: 낚시(낚싯대·릴·줄·바늘·찌, 대나무대), 보관·편의(쿨러·미끼통·뜰채·헤드랜턴·의자·장화·통발), 미끼 열 가지.
    /// 장비는 10단계: 1~4단계는 상점, 5~7단계는 보상(여울호 등급·단골·의뢰·노씨·1장), 8~10단계는 다음 지역.
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
            GreetNow();
        }

        private void GreetNow()
        {
            var says = G.Data.Dialogue.NossiSays;
            var gifts = G.CheckGifts();
            if (gifts.Count > 0)
            {
                Nossi(says["gift"]);
                Modal(T["giftTitle"], F("giftBody", string.Join("\n", gifts.Select(x => x.Name))), new[] { new ModalButton("좋아", Palette.Green, Redraw) });
                return;
            }
            Nossi(G.Save.Flag("nossiFavor") ? says["favor"] : says["ask"]);
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

        protected override IList<string> Tabs
        {
            get { return new[] { T["tabFish"], T["tabKeep"], T["tabBait"] }; }
        }

        private static readonly string[] FishKinds = { "rod", "reel", "line", "hook", "float" };
        private static readonly string[] KeepKinds = { "cooler", "baitbox", "net", "lamp", "chair", "boots", "trap" };

        protected override Control BuildPage()
        {
            var paper = Paper(34, 24);
            var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            col.AddThemeConstantOverride("separation", 10);
            paper.AddChild(col);
            if (Tab == 2)
            {
                BaitPage(col);
                return TopAligned(paper);
            }
            var kinds = Tab == 0 ? FishKinds : KeepKinds;
            for (int i = 0; i < kinds.Length; i++)
            {
                if (i > 0) col.AddChild(Rule());
                col.AddChild(KindRow(kinds[i]));
            }
            if (Tab == 0)
            {
                // 아버지의 대나무대
                col.AddChild(Rule());
                var show = SmallButton(T["show"], Palette.Wood, () => Nossi(G.Data.Dialogue.NossiSays["bamboo"]), 200, 88);
                col.AddChild(Row(new GearIcon(GearIcon.Kind.Bamboo, 104), "", T["bamboo"], T["bambooSub"], show, Palette.WoodDark));
            }
            return TopAligned(paper);
        }

        // ───────── 장비 한 줄 ─────────

        /// <summary>장비 한 종류: 다음 단계 그림 · 지금 단계와 다음 단계 이름, 효과 · 값 또는 얻는 법</summary>
        private Control KindRow(string kind)
        {
            int have, count;
            string label;
            var kit = G.KitDef(kind);
            if (kit != null)
            {
                have = G.KitLevel(kind);
                count = kit.Levels.Count;
                label = kit.Name;
            }
            else
            {
                var slot = SlotOf(kind);
                have = G.GearLevel(slot) + 1;
                count = G.GearCount(slot);
                label = T[kind];
            }
            bool max = have >= count;
            int show = max ? have : have + 1;
            string top = have > 0 ? F("tierNow", label, have) + " " + TierName(kind, have) : label + " · " + T["none"];
            string name = F("tier", show) + " " + TierName(kind, show);
            if (max) return Row(GearArt(kind, show), label, name, Stat(kind, show), Pill(T["best"], Palette.GreenDark, Colors.White), Palette.GreenDark);
            string unlock = TierUnlock(kind, show);
            Control right;
            // 새 장비는 노씨 첫 부탁을 들어준 뒤에 판다
            if (kit != null && !G.KitsOpen() && Unlock.IsShop(unlock)) right = UnlockTag(unlock, T["kitsLocked"]);
            else if (Unlock.IsShop(unlock))
            {
                int price = TierPrice(kind, show);
                right = PriceButton(price, Palette.Blue, () => Buy(kind));
            }
            else right = UnlockTag(unlock);
            return Row(GearArt(kind, show), top, name, Stat(kind, show), right, Palette.Ink);
        }

        private static GearSlot SlotOf(string kind)
        {
            return kind == "rod" ? GearSlot.Rod : kind == "reel" ? GearSlot.Reel : GearSlot.Tackle;
        }

        private string TierName(string kind, int tier)
        {
            var kit = G.KitDef(kind);
            if (kit != null) return kit.Levels[tier - 1].Name;
            var g = G.Data.Gear;
            return kind == "rod" ? g.Rods[tier - 1].Name : kind == "reel" ? g.Reels[tier - 1].Name : g.Tackles[tier - 1].Name;
        }

        private string TierUnlock(string kind, int tier)
        {
            var kit = G.KitDef(kind);
            return kit != null ? kit.Levels[tier - 1].Unlock : G.GearUnlock(SlotOf(kind), tier - 1);
        }

        private int TierPrice(string kind, int tier)
        {
            var kit = G.KitDef(kind);
            return kit != null ? kit.Levels[tier - 1].Price : G.GearPrice(SlotOf(kind), tier - 1);
        }

        /// <summary>효과 글</summary>
        private string Stat(string kind, int tier)
        {
            var kit = G.KitDef(kind);
            if (kit != null) return string.Format(kit.Effect, kit.Levels[tier - 1].Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            var g = G.Data.Gear;
            switch (kind)
            {
                case "rod":
                    var r = g.Rods[tier - 1];
                    return F("rodStat", G.Reel.CastMeters(r.Reach).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), Mathf.RoundToInt((float)r.Absorb * 100), r.HookBonus);
                case "reel":
                    var l = g.Reels[tier - 1];
                    var rb = G.Data.Balance.Reel;
                    return F("reelStat", l.Speed, l.Slack > 0 ? l.Slack : rb.Slack, l.Danger > 0 ? l.Danger : rb.Danger, l.Drag);
                default:
                    var c = g.Tackles[tier - 1];
                    return F("coolerStat", c.Capacity, c.Decay);
            }
        }

        /// <summary>보상·다음 지역 장비는 값 대신 얻는 법</summary>
        private Control UnlockTag(string unlock, string lockedText = null)
        {
            string text = lockedText ?? UnlockText(unlock);
            var box = new PanelContainer { CustomMinimumSize = new Vector2(200, 88), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
            bool later = unlock == "later";
            box.AddThemeStyleboxOverride("panel", UiKit.Box(later ? new Color(Palette.Ink, 0.12f) : new Color(Palette.Gold, 0.22f), 22, 14, 8).WithBorder(later ? new Color(Palette.Ink, 0.25f) : Palette.GoldDark, 3));
            var v = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            v.AddThemeConstantOverride("separation", 0);
            if (!later && lockedText == null) v.AddChild(UiKit.Text(T["gift"], TextSize.Tiny, Palette.GoldDark, true, false, 0, null, HorizontalAlignment.Center));
            var l = UiKit.Text(text, TextSize.Tiny, Palette.InkSoft, false, true, 0, null, HorizontalAlignment.Center);
            l.CustomMinimumSize = new Vector2(172, 0);
            v.AddChild(l);
            box.AddChild(v);
            return box;
        }

        private string UnlockText(string u)
        {
            var parts = u.Split(':');
            switch (parts[0])
            {
                case "rank": return F("unlockRank", G.Data.Diner.Ranks[int.Parse(parts[1])].Name);
                case "regulars": return F("unlockRegulars", parts[1]);
                case "requests": return F("unlockRequests", parts[1]);
                case "favor": return T["unlockFavor"];
                case "clear": return T["unlockClear"];
                default: return T["unlockLater"];
            }
        }

        private static TextureRect GearArt(string kind, int tier, float size = 104)
        {
            return new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://art/gear/" + kind + "-" + tier + ".png"),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(size, size),
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }

        /// <summary>그림 · (작은 윗줄, 큰 이름, 설명) · 오른쪽</summary>
        private Control Row(Control icon, string top, string name, string desc, Control right, Color nameColor)
        {
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 20);
            row.AddChild(icon);
            var text = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 0);
            if (!string.IsNullOrEmpty(top)) text.AddChild(UiKit.Text(top, TextSize.Tiny, Palette.InkSoft).Ellipsis());
            text.AddChild(UiKit.Text(name, TextSize.Small + 2, nameColor, true).Ellipsis());
            text.AddChild(UiKit.Text(desc, TextSize.Tiny, Palette.InkSoft, false, true));
            row.AddChild(text);
            if (right != null) row.AddChild(right);
            return row;
        }

        // ───────── 미끼 ─────────

        /// <summary>미끼 열 가지를 두 줄로. 고르면 그 미끼를 쓰고, 값을 누르면 한 봉지 산다</summary>
        private void BaitPage(VBoxContainer col)
        {
            var grid = new GridContainer { Columns = 2, MouseFilter = MouseFilterEnum.Ignore };
            grid.AddThemeConstantOverride("h_separation", 16);
            grid.AddThemeConstantOverride("v_separation", 14);
            col.AddChild(grid);
            var baits = G.Data.Gear.Baits;
            for (int i = 0; i < baits.Count; i++) grid.AddChild(BaitCard(baits[i], i + 1));
        }

        private Control BaitCard(Hanjeopsi.Core.Data.BaitKind b, int art)
        {
            bool using_ = G.Save.Kit != null && G.Save.Kit.Pick == b.Id;
            int have = G.BaitCount(b.Id);
            var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            card.AddThemeStyleboxOverride("panel", using_
                ? UiKit.Box(new Color(Palette.Gold, 0.2f), 24, 14, 10).WithBorder(Palette.GoldDark, 4)
                : UiKit.Box(new Color(Palette.Ink, 0.06f), 24, 14, 10).WithBorder(new Color(Palette.Ink, 0.12f), 3));
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 10);
            card.AddChild(row);
            row.AddChild(GearArt("bait", art, 84));
            var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 0);
            text.AddChild(UiKit.Text(b.Name + "  " + F("baitHave", have), TextSize.Tiny + 2, using_ ? Palette.GoldDark : Palette.Ink, true).Ellipsis());
            string likes = b.Likes.Count == 0 ? T["baitAll"] : string.Join("·", b.Likes.Take(3).Select(id => G.Data.SpeciesById(id).Name));
            if (b.WaitMul < 1) likes = T["chum"];
            text.AddChild(UiKit.Text(likes, TextSize.Tiny - 2, Palette.InkSoft).Ellipsis());
            var buttons = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            buttons.AddThemeConstantOverride("separation", 8);
            var use = SmallButton(using_ ? T["baitUsing"] : T["baitUse"], using_ ? Palette.GoldDark : Palette.Teal, () => PickBait(b.Id), 120, 64);
            use.SetEnabled(!using_ && have > 0);
            buttons.AddChild(use);
            var buy = SmallButton(UiKit.Won(b.Price), Palette.Green, () => BuyBait(b.Id), 150, 64);
            buy.SetEnabled(G.Save.Money >= b.Price);
            buttons.AddChild(buy);
            text.AddChild(buttons);
            row.AddChild(text);
            return card;
        }

        private GameButton PriceButton(int price, Color color, Action onPress)
        {
            var b = SmallButton(UiKit.Won(price), color, onPress, 230, 100);
            b.SetEnabled(G.Save.Money >= price);
            return b;
        }

        private void BuyBait(string id)
        {
            var b = G.BaitDef(id);
            if (!G.BuyBaitKind(id))
            {
                Toast(T["poor"], Palette.RedText);
                return;
            }
            HudBar.Refresh();
            int plus = (id == "krill" ? G.Data.Gear.Bait.Amount : b.Amount) + (int)G.KitValue("baitbox");
            FloatText(F("baitPlus", plus), new Vector2(GetViewportRect().Size.X / 2, SafeTop + 560), Palette.GreenText);
            Sound.Play("coin");
            Redraw();
        }

        private void PickBait(string id)
        {
            G.PickBait(id);
            Sound.Play("tick", -6, 1.1f);
            Redraw();
        }

        private void Buy(string kind)
        {
            if (G.KitDef(kind) == null)
            {
                Upgrade(SlotOf(kind));
                return;
            }
            int next = G.KitLevel(kind) + 1;
            if (!G.BuyKit(kind))
            {
                Toast(T["poor"], Palette.RedText);
                return;
            }
            HudBar.Refresh();
            Toast(F("bought", TierName(kind, next)), Palette.Gold);
            Sound.Play("coin");
            Sound.Play("good", -6);
            Redraw();
            GetTree().CreateTimer(0.5).Timeout += () => Nossi(G.Data.Dialogue.NossiSays["gear"]);
        }

        private void Upgrade(GearSlot slot)
        {
            int next = G.GearLevel(slot) + 1;
            string kindId = slot == GearSlot.Rod ? "rod" : slot == GearSlot.Reel ? "reel" : "cooler";
            if (!G.UpgradeGear(slot))
            {
                Toast(T["poor"], Palette.RedText);
                return;
            }
            HudBar.Refresh();
            Toast(F("bought", TierName(kindId, next + 1)), Palette.Gold);
            Sound.Play("coin");
            Sound.Play("good", -6);
            Redraw();
            var says = G.Data.Dialogue.NossiSays;
            // 노씨 감성돔대(3단계)를 사면 아끼던 거라고 한마디
            bool lastRod = slot == GearSlot.Rod && next == 2;
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
                case "keep":
                    ShowTab(1);
                    break;
                case "bait":
                    G.BuyBaitKind("shrimp");
                    G.PickBait("shrimp");
                    ShowTab(2);
                    break;
                case "gift":
                    // 동네 맛집이 되어 노씨가 챙겨 둔 장비
                    G.Save.Diner.Rep = G.Data.Diner.Ranks[1].Rep;
                    GreetNow();
                    break;
            }
        }
    }
}
