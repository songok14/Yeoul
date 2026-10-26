using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>story.json: 장면 그림이 모두 있고, 배경과 연출 이름이 화면이 아는 것인지, 아버지의 말이 그대로인지</summary>
    public class StoryTests
    {
        private static readonly string[] Bgs = { "kitchen", "ash", "phone", "harbor", "room", "notice", "storage", "memory", "breakwater", "grill", "night", "dawn", "seize" };

        private static string Portraits()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "godot", "art", "portraits"))) dir = dir.Parent;
            Assert.NotNull(dir);
            return Path.Combine(dir.FullName, "godot", "art", "portraits");
        }

        [Fact]
        public void 네_이야기가_있고_그림과_배경이_모두_있다()
        {
            var story = TestData.Full.Story;
            Assert.Equal(new[] { "clear", "fail", "p1", "p2" }, story.Parts.Keys.OrderBy(k => k));
            string dir = Portraits();
            foreach (var part in story.Parts)
            {
                Assert.NotEmpty(part.Value);
                foreach (var page in part.Value)
                {
                    Assert.Contains(page.Bg, Bgs);
                    Assert.False(string.IsNullOrWhiteSpace(page.Text));
                    Assert.DoesNotContain("—", page.Text);
                    // "fish/bollak" 처럼 폴더가 붙으면 godot/art 아래 그림
                    string file = page.Pic == null ? null : page.Pic.Contains('/') ? Path.Combine(dir, "..", page.Pic + ".png") : Path.Combine(dir, page.Pic + ".png");
                    if (file != null) Assert.True(File.Exists(file), page.Pic + ".png 이 없다");
                    if (page.Event != null) Assert.Contains(page.Event, new[] { "salt", "cat" });
                }
            }
        }

        [Fact]
        public void 대화_감정_그림이_모두_있다()
        {
            string dir = Portraits();
            foreach (var sp in TestData.Full.Dialogue.Speakers)
                foreach (var face in sp.Faces)
                    Assert.True(File.Exists(Path.Combine(dir, face.Value + ".png")), sp.Id + "." + face.Key + " → " + face.Value + ".png 이 없다");
        }

        [Fact]
        public void 아버지의_말과_1장_조건이_STORY_md_그대로다()
        {
            var p1 = TestData.Full.Story.Parts["p1"];
            Assert.Contains(p1, p => p.Text.Contains("고기는 먼저 낚은 사람 게 아니야. 제일 맛있게 먹어주는 사람 거지."));
            Assert.Contains(p1, p => p.Text.Contains("90일 뒤 차압 예정") && p.Text.Contains("30일 안에 1차 상환금 300만 원"));
            Assert.Contains(TestData.Full.Story.Parts["p2"], p => p.Event == "salt");
        }
    }
}
