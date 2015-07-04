using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace EMusic.Models
{
    public class VKApi
    {
        public static string AccessToken { get; set; }

        public static void ExtractAccessToken(string uri)
        {
            int beginIndex = uri.IndexOf("access_token=") + 13;
            int endIndex = uri.Substring(beginIndex).IndexOf("&");
            AccessToken = uri.Substring(beginIndex, endIndex);
        }

        public static Uri Request(string method, IDictionary<string, object> parameters) 
        {
            var paramsStr = new StringBuilder();
            foreach (var key in parameters.Keys)
                paramsStr.Append(key + "=" + parameters[key] + "&");
            return new Uri(string.Format(@"https://api.vk.com/method/{0}?{1}access_token={2}", method, paramsStr.ToString(), AccessToken));
        }

        public static Uri GetAuthUri()
        {
            var uriStr = ConfigurationManager.AppSettings["AuthUri"] as string;
            return new Uri(uriStr, UriKind.Absolute);
        }

        public static void GetGroupTracks(MusiсDir dir, long offset, Action<IList<Track>> callback)
        {
            var method = "audio.get";

            var param = new Dictionary<string, object>();
            param["offset"] = offset.ToString();
            param["count"] = 100;
            param["owner_id"] = "-" + dir.Gid;
            param["version"] = @"5.34";

            using (var wc = new WebClient())
            {
                //wc.Proxy.Credentials = CredentialCache.DefaultCredentials;
                wc.DownloadStringCompleted += (s, e) =>
                {
                    if (e.Result != null)
                    {
                        var result = new List<Track>();

                        var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(e.Result);

                        var root = json["response"] as ArrayList;

                        foreach (var dirInfo in root)
                            //if (dirInfo is Dictionary<string, object>)
                            {
                                var tmp = dirInfo as Dictionary<string, object>;

                                if (tmp != null && 
                                    tmp.ContainsKey("aid") && 
                                    tmp.ContainsKey("url") &&
                                    tmp.ContainsKey("artist") &&
                                    tmp.ContainsKey("duration") &&
                                    tmp.ContainsKey("title"))
                                {
                                    var track = new Track()
                                    {
                                        Tid = (int)tmp["aid"],
                                        MusicDirID = dir.MusicDirID,
                                        TrackAuthor = tmp["artist"] as string,
                                        TrackDuration = (int)tmp["duration"],
                                        TrackUrl = tmp["url"] as string,
                                        TrackName = tmp["title"] as string
                                    };
                                    result.Add(track);
                                }
                            }

                        callback(result);
                    }
                };
                wc.DownloadStringAsync(Request(method, param));
            }
        }

        public static void GetMusicDirs(Action<IDictionary<long, string>> callback) 
        {
            var method = "groups.search";

            var param = new Dictionary<string, object>();
            param["q"] = @"E:\Music";
            param["version"] = @"5.34";

            using(var wc = new WebClient()) 
            {
                //wc.Proxy.Credentials = CredentialCache.DefaultCredentials;
                wc.DownloadStringCompleted += (s, e) =>
                {
                    if (e.Result != null)
                    {
                        var result = new Dictionary<long, string>();

                        var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(e.Result);

                        var root = json["response"] as ArrayList;

                        foreach (var dirInfo in root)
                            if (dirInfo is Dictionary<string, object>)
                            {
                                var tmp = dirInfo as Dictionary<string, object>;
                                if (tmp.ContainsKey("gid") && tmp.ContainsKey("name"))
                                    result[long.Parse((tmp["gid"] ?? "0").ToString())] = (tmp["name"] ?? 0).ToString();
                            }
          
                        callback(result);
                    }
                };
                wc.DownloadStringAsync(Request(method, param));
            }
        } 
    }
}
