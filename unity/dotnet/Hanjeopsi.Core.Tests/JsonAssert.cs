using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit.Sdk;

namespace Hanjeopsi.Core.Tests
{
    /// <summary>웹이 남긴 JSON 과 C# 결과를 비교한다. 숫자는 값으로, 객체는 키 순서와 상관없이. 다른 곳의 경로를 알려 준다</summary>
    public static class JsonAssert
    {
        public static void Same(JToken expected, JToken actual, string where)
        {
            var diff = Diff(expected, actual, "$");
            if (diff != null) throw new XunitException(where + " 에서 웹과 다르다: " + diff);
        }

        public static string Diff(JToken e, JToken a, string path)
        {
            if (IsNull(e) || IsNull(a))
                return IsNull(e) && IsNull(a) ? null : path + " 웹=" + Show(e) + " C#=" + Show(a);
            if (IsNumber(e) && IsNumber(a))
                return (double)e == (double)a ? null : path + " 웹=" + e + " C#=" + a;
            if (e.Type != a.Type) return path + " 종류 웹=" + e.Type + " C#=" + a.Type;
            switch (e)
            {
                case JObject eo:
                {
                    var ao = (JObject)a;
                    var keys = new HashSet<string>(eo.Properties().Select(p => p.Name));
                    keys.UnionWith(ao.Properties().Select(p => p.Name));
                    foreach (var k in keys.OrderBy(k => k))
                    {
                        var d = Diff(eo[k], ao[k], path + "." + k);
                        if (d != null) return d;
                    }
                    return null;
                }
                case JArray ea:
                {
                    var aa = (JArray)a;
                    if (ea.Count != aa.Count) return path + " 길이 웹=" + ea.Count + " C#=" + aa.Count;
                    for (int i = 0; i < ea.Count; i++)
                    {
                        var d = Diff(ea[i], aa[i], path + "[" + i + "]");
                        if (d != null) return d;
                    }
                    return null;
                }
                default:
                    return JToken.DeepEquals(e, a) ? null : path + " 웹=" + e + " C#=" + a;
            }
        }

        private static bool IsNull(JToken t)
        {
            return t == null || t.Type == JTokenType.Null || t.Type == JTokenType.Undefined || (t is JValue v && v.Value == null);
        }

        private static bool IsNumber(JToken t)
        {
            return t.Type == JTokenType.Integer || t.Type == JTokenType.Float;
        }

        private static string Show(JToken t)
        {
            return IsNull(t) ? "null" : t.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}
