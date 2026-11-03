using System;
using GentlyPick.Updates;
class Tests {
    static void MustFail(Action action) { try { action(); } catch(FormatException) { return; } throw new Exception("expected rejection"); }
    static int Main() {
        string h=new string('a',64);
        string json="{\"tag_name\":\"v1.2.3\",\"draft\":false,\"prerelease\":false,\"body\":\"说明\",\"assets\":[{\"name\":\"LightExcerpt-win-x64-update.zip\",\"size\":12,\"digest\":\"sha256:"+h+"\",\"browser_download_url\":\"https://github.com/Soul1380/html-gentlypick/releases/download/v1.2.3/LightExcerpt-win-x64-update.zip\"}]}";
        var release=ReleaseFeed.Parse(json);
        if(release.Version!=new Version(1,2,3)||release.Size!=12||release.Sha256!=h)throw new Exception("valid release");
        MustFail(()=>ReleaseFeed.Parse(json.Replace("\"draft\":false","\"draft\":true")));
        MustFail(()=>ReleaseFeed.Parse(json.Replace("\"prerelease\":false","\"prerelease\":true")));
        MustFail(()=>ReleaseFeed.Parse(json.Replace("sha256:","sha512:")));
        MustFail(()=>ReleaseFeed.Parse(json.Replace("github.com/","example.com/")));
        MustFail(()=>ReleaseFeed.Parse(json.Replace("v1.2.3","v1.2")));
        MustFail(()=>ReleaseFeed.Parse(json.Replace("\"assets\":[", "\"assets\":\"invalid\",\"ignored\":[")));
        if(ReleaseFeed.ParseVersion("1.10.0")<=ReleaseFeed.ParseVersion("1.2.99"))throw new Exception("numeric order");
        var same=ReleaseFeed.Parse("{\"tag_name\":\"v1.2.3\",\"draft\":false,\"prerelease\":false}",new Version(1,2,3));
        if(same.Version!=new Version(1,2,3))throw new Exception("no update handling");
        Console.WriteLine("PASS release parsing, versions and invalid assets");return 0;
    }
}
