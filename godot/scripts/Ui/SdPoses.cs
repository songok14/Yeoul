using System.Linq;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace Hanjeopsi.App.Ui
{
    /// <summary>SD 포즈 그림의 기준점 (scripts/sd_pose.py 가 쓰는 data/poses.json). 좌표는 그림 픽셀</summary>
    public sealed class SdPose
    {
        /// <summary>발밑 가운데</summary>
        public float[] Foot;
        /// <summary>낚싯대 끝 (줄이 나가는 곳)</summary>
        public float[] Tip;
        /// <summary>머리 꼭대기부터 발바닥까지. 포즈마다 이 키로 크기를 맞춘다</summary>
        public float Body;
        /// <summary>
        /// 그림에서 낚싯대를 지운 포즈(감기·버티기)만: 손잡이 끝, 대가 뻗는 방향, 대 길이 (그림 픽셀).
        /// 대는 화면이 그리고 텐션만큼 물고기 쪽으로 휜다
        /// </summary>
        public float[] Grip;
        /// <summary>그림 속 릴 자리와 너비 [x, y, 너비]. 여기에 가진 릴 그림을 덮는다 (없으면 그림 속 릴 그대로)</summary>
        public float[] Reel;
        /// <summary>앉은 포즈만: 발바닥에서 엉덩이까지 높이. 가진 의자의 앉는 자리를 여기에 맞춘다</summary>
        public float Seat;
        public float[] RodDir;
        public float RodLen;

        public bool DrawnRod
        {
            get { return Grip != null; }
        }

        public Vector2 FootPx
        {
            get { return new Vector2(Foot[0], Foot[1]); }
        }

        public Vector2 TipPx
        {
            get { return new Vector2(Tip[0], Tip[1]); }
        }
    }

    public static class SdPoses
    {
        private static Dictionary<string, SdPose> _all;

        public static SdPose Get(string name)
        {
            if (_all == null)
            {
                string json = FileAccess.GetFileAsString("res://data/poses.json");
                _all = JsonConvert.DeserializeObject<Dictionary<string, SdPose>>(json);
            }
            return _all[name];
        }

        private static Newtonsoft.Json.Linq.JObject _look;

        /// <summary>대 밑동+릴 그림(gear/butt-단계): 맨 위 대 몸통의 가로 가운데 비율과 너비(px) (data/gearlook.json)</summary>
        public static (float TopX, float BlankPx) Butt(int level)
        {
            if (_look == null) _look = Newtonsoft.Json.Linq.JObject.Parse(FileAccess.GetFileAsString("res://data/gearlook.json"));
            var tops = _look["buttTop"].ToObject<List<float>>();
            var w = _look["buttBlank"].ToObject<List<float>>();
            int i = Mathf.Clamp(level - 1, 0, tops.Count - 1);
            return (tops[i], w[i]);
        }

        /// <summary>낚싯대 몸통 그림(gear/blank-단계): 몸통 가운데 비율, 맨 아래 굵기(px), 고리 자리 (data/gearlook.json blanks)</summary>
        public static (float Center, float Base, float[] Guides) Blank(int level)
        {
            if (_look == null) _look = Newtonsoft.Json.Linq.JObject.Parse(FileAccess.GetFileAsString("res://data/gearlook.json"));
            var list = _look["blanks"];
            if (list == null) return (0.23f, 45, null);
            var b = list[Mathf.Clamp(level - 1, 0, list.Count() - 1)];
            return (b["center"].ToObject<float>(), b["base"].ToObject<float>(), b["guides"].ToObject<float[]>());
        }

        private static List<float> _chairSeat;

        /// <summary>뒤에서 본 의자 그림(gear/chair-back-단계) 높이 중 앉는 자리 높이 비율 (data/gearlook.json)</summary>
        public static float ChairSeat(int level)
        {
            if (_chairSeat == null)
            {
                var o = Newtonsoft.Json.Linq.JObject.Parse(FileAccess.GetFileAsString("res://data/gearlook.json"));
                _chairSeat = o["chairSeat"].ToObject<List<float>>();
            }
            return _chairSeat[Mathf.Clamp(level - 1, 0, _chairSeat.Count - 1)];
        }
    }
}
