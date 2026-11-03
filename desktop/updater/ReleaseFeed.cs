using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace GentlyPick.Updates {
    public sealed class ReleaseInfo {
        public Version Version;
        public string Notes, AssetUrl, Sha256;
        public long Size;
    }
    public static class ReleaseFeed {
        public const string AssetName = "LightExcerpt-win-x64-update.zip";
        public static Version ParseVersion(string value) {
            Match m = Regex.Match(value ?? "", @"^v?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$", RegexOptions.CultureInvariant);
            int a,b,c;
            if (!m.Success || !int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out a) ||
                !int.TryParse(m.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out b) ||
                !int.TryParse(m.Groups[3].Value, NumberStyles.None, CultureInfo.InvariantCulture, out c))
                throw new FormatException("版本号必须是三段数字，例如 1.2.3。");
            return new Version(a,b,c);
        }
        public static ReleaseInfo Parse(string json,Version current = null) {
            var root = new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 }.DeserializeObject(json) as IDictionary<string,object>;
            if (root == null) throw new FormatException("Release 数据无效。");
            if (Flag(root,"draft") || Flag(root,"prerelease")) throw new FormatException("不能使用草稿或预发布版本。");
            var version = ParseVersion(StringValue(root,"tag_name"));
            if(current!=null && version<=current) return new ReleaseInfo { Version=version, Notes=StringValue(root,"body") };
            object raw;
            if (!root.TryGetValue("assets",out raw) || !(raw is IEnumerable) || raw is string || raw is IDictionary)
                throw new FormatException("Release 附件列表无效。");
            IDictionary<string,object> found = null;
            foreach (object entry in (IEnumerable)raw) {
                var a = entry as IDictionary<string,object>;
                if (a == null) throw new FormatException("Release 附件格式无效。");
                if (StringValue(a,"name") != AssetName) continue;
                if (found != null) throw new FormatException("Release 中有重复更新包。");
                found = a;
            }
            if (found == null) throw new FormatException("Release 没有 Windows 更新包。");
            string digest = StringValue(found,"digest");
            if (!Regex.IsMatch(digest,@"^sha256:[0-9a-fA-F]{64}$")) throw new FormatException("GitHub 未提供有效的 SHA-256。");
            long size;
            if (!long.TryParse(StringValue(found,"size"),out size) || size < 1 || size > 2L*1024*1024*1024) throw new FormatException("更新包大小无效。");
            string url = StringValue(found,"browser_download_url"); Uri u;
            if (!Uri.TryCreate(url,UriKind.Absolute,out u) || u.Scheme != "https" || u.Host != "github.com" || !string.IsNullOrEmpty(u.UserInfo) ||
                !u.AbsolutePath.StartsWith("/Soul1380/html-gentlypick/releases/download/",StringComparison.OrdinalIgnoreCase))
                throw new FormatException("更新包地址不属于指定仓库。");
            return new ReleaseInfo { Version=version, Notes=StringValue(root,"body"), Size=size, Sha256=digest.Substring(7).ToLowerInvariant(), AssetUrl=url };
        }
        static string StringValue(IDictionary<string,object> d,string key) { object v; return d.TryGetValue(key,out v) && v!=null ? Convert.ToString(v,CultureInfo.InvariantCulture) : ""; }
        static bool Flag(IDictionary<string,object> d,string key) { object v; if(!d.TryGetValue(key,out v) || !(v is bool)) throw new FormatException("Release 状态无效。"); return (bool)v; }
    }
}
