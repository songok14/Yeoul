using System.Collections.Generic;

namespace Hanjeopsi.Core.Data
{
    /// <summary>requests.json (웹 src/data/requests.ts)</summary>
    public sealed class RequestTable
    {
        public List<RequestTemplate> Templates = new List<RequestTemplate>();
    }

    /// <summary>식당 납품 의뢰 틀</summary>
    public sealed class RequestTemplate
    {
        public string Client;
        public string Text;
        public string Species;
        public int Count;
        /// <summary>시세 대비 보상 배율</summary>
        public double Mul;
        /// <summary>납품하면 켜지는 플래그 (노씨 의뢰 → nossiFavor). 없으면 null</summary>
        public string Flag;
    }
}
