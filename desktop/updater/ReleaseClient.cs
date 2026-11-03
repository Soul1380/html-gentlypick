using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GentlyPick.Updates {
    public static class ReleaseClient {
        public static async Task<ReleaseInfo> Latest(Version current = null) {
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            using(var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)}) {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("GentlyPick/1.0");
                client.DefaultRequestHeaders.Add("Accept","application/vnd.github+json");
                using(var response=await client.GetAsync("https://api.github.com/repos/Soul1380/html-gentlypick/releases/latest")) {
                    if(response.StatusCode==HttpStatusCode.NotFound)return null;
                    response.EnsureSuccessStatusCode();
                    string content=await response.Content.ReadAsStringAsync();
                    if(content.Length>4*1024*1024)throw new InvalidOperationException("Release 信息过大。");
                    return ReleaseFeed.Parse(content,current);
                }
            }
        }
    }
}
