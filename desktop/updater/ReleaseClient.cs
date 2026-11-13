using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
namespace GentlyPick.Updates {
 public static class ReleaseClient {
  static readonly SemaphoreSlim Gate=new SemaphoreSlim(1,1);
  static string cached; static DateTime cachedAt=DateTime.MinValue, retryAt=DateTime.MinValue;
  public static async Task<ReleaseInfo> Latest(Version current=null){
   ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
   using(var client=new HttpClient{Timeout=TimeSpan.FromSeconds(15)}){
    client.DefaultRequestHeaders.UserAgent.ParseAdd("GentlyPick/1.0");
    return await LatestWith(client,current);
   }
  }
  internal static async Task<ReleaseInfo> LatestWith(HttpClient client,Version current){
   await Gate.WaitAsync();
   try{
    if(cached!=null && DateTime.UtcNow-cachedAt<TimeSpan.FromMinutes(10))return ReleaseFeed.Parse(cached,current);
    Exception failure=null;
    if(DateTime.UtcNow>=retryAt){
     try{using(var response=await client.GetAsync("https://api.github.com/repos/Soul1380/html-gentlypick/releases/latest")){
      if((int)response.StatusCode==403||(int)response.StatusCode==429){retryAt=DateTime.UtcNow.AddMinutes(15);}
      response.EnsureSuccessStatusCode();return await Accept(response,current);
     }}catch(Exception e){failure=e;}
    }
    try{using(var response=await client.GetAsync("https://raw.githubusercontent.com/Soul1380/html-gentlypick/main/update-feed.json")){
     response.EnsureSuccessStatusCode();return await Accept(response,current);
    }}catch(Exception e){throw new InvalidOperationException("暂时无法检查更新（GitHub 限流或网络不可用）。请稍后重试，或从 GitHub Releases 下载完整安装程序。现有软件可继续使用。",new AggregateException(failure??e,e));}
   }finally{Gate.Release();}
  }
  static async Task<ReleaseInfo> Accept(HttpResponseMessage response,Version current){
   if(response.Content.Headers.ContentLength>4*1024*1024)throw new InvalidOperationException("Release 信息过大。");
   var content=await response.Content.ReadAsStringAsync();if(content.Length>4*1024*1024)throw new InvalidOperationException("Release 信息过大。");
   var release=ReleaseFeed.Parse(content);cached=content;cachedAt=DateTime.UtcNow;return ReleaseFeed.Parse(content,current);
  }
 }
}
