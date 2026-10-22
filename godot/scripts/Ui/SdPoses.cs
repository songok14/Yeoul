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
    }
}
