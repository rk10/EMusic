using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EMusic.Models
{
    public class SettingsStr
    {
        public static string BatchRelativeCoef = "BatchRelativeCoef";
        public static string DirSaveTracks = "DirSaveTracks";
    }

    public enum TrackBatch
    {
        EXPERIMENT, LIKE
    }

    public class MusicService
    {
        private int _counterBatchTracks;
        private TrackBatch _currentBatch = TrackBatch.LIKE;

        private MusicContext Context { get; set; }
        public List<Track> Tracks { get; set; }

        public TrackBatch CurrentBatch { get; set; }
        public List<MusiсDir> CurrentSearchDirs { get; set; }

        private Random _rand = new Random(DateTime.Now.Millisecond);

        public MusicService()
        {
            Context = new MusicContext();
            //Context.CreateDatabaseIfNotExists();
        }

        public void DownloadMusicDirs(Action<IDictionary<long, string>> callback)
        {
            VKApi.GetMusicDirs(callback);
        }

        public List<MusiсDir> GetMusicDirs()
        {
            return Context.MusiсDirs.Include("Tracks").ToList();
        }

        public List<Track> GetTracks()
        {
            return Context.Tracks.ToList();
        }

        public void SaveTrackInDB(Track track)
        {
            Context.Tracks.Add(track);
            Context.SaveChanges();
        }

        public List<MusiсDir> SupplementMusicDirs(IDictionary<long, string> mdirs)
        {
            var newDirs = new List<MusiсDir>();
            var newOffsets = new List<DownloadOffset>(); 
            foreach (var key in mdirs.Keys.Where(id => Context.MusiсDirs.Count(md => md.Gid == id) == 0))
            {
                var name = mdirs[key];

                var dir = new MusiсDir();
                dir.Gid = key;
                dir.NameDir = mdirs[key];
                dir.NegativeRating = 0;
                dir.PositiveRating = 0;
                newDirs.Add(dir);

                var offset = new DownloadOffset();
                offset.MusiсDir = dir;
                offset.Offset = 0;
                newOffsets.Add(offset);
            }
            Context.MusiсDirs.AddRange(newDirs);
            Context.DownloadOffsets.AddRange(newOffsets);
            Context.SaveChanges();
            return newDirs;
        }

        private void ChangeTrackBatch()
        {
            if (_counterBatchTracks == 0)
            {
                var likes =
                    Context.MusiсDirs.Where(md => md.PositiveRating - md.NegativeRating > 0);

                CurrentSearchDirs = new List<MusiсDir>();

                if (likes.Count() < 2 || _currentBatch == TrackBatch.LIKE)
                {
                    _counterBatchTracks = 1;
                    _currentBatch = TrackBatch.EXPERIMENT;
                    CurrentSearchDirs.AddRange(Context.MusiсDirs);
                }
                else
                {
                    _counterBatchTracks = 2;
                    _currentBatch = TrackBatch.LIKE;
                    CurrentSearchDirs.AddRange(likes.ToList());
                }
            }
            _counterBatchTracks--;
        }

        private int _offset = 0;
        public Dictionary<long, List<Track>> _cashTracks = new Dictionary<long, List<Track>>();
        public Dictionary<long, int> _cachOffsets = new Dictionary<long, int>();

        private void GetFromCash(MusiсDir dir, Action<Track> callback)
        {
            if (!_cashTracks.ContainsKey(dir.MusicDirID))
                _cashTracks[dir.MusicDirID] = new List<Track>();

            int offset = _cachOffsets.ContainsKey(dir.MusicDirID) ? _cachOffsets[dir.MusicDirID] : 0;

            if (_cashTracks == null || _cashTracks[dir.MusicDirID].Count == 0)
            {
                VKApi.GetGroupTracks(dir, offset, dict =>
                {
                    _cashTracks[dir.MusicDirID].AddRange(dict.Where(k => Context.Tracks.Count(t => t.Tid == k.Tid) == 0).ToList());

                    if (_cashTracks[dir.MusicDirID].Count == 0 && dict.Count < 100)
                        callback(null);
                    else
                    {
                        _cachOffsets[dir.MusicDirID] = offset;
                        GetFromCash(dir, callback);
                    }
                });
            }
            else
            {
                int index = _rand.Next(_cashTracks[dir.MusicDirID].Count);

                Track track = null;

                do
                {
                    if (_cashTracks[dir.MusicDirID].Count == 0)
                    {
                        track = null;
                        break;
                    }

                    track = _cashTracks[dir.MusicDirID][index];
                    _cashTracks[dir.MusicDirID].RemoveAt(index);
                    index = (index + 1) % _cashTracks[dir.MusicDirID].Count();
                }
                while (Context.Tracks.Count(t => t.Tid == track.Tid) > 0);

                if (track == null)
                {
                    _cachOffsets[dir.MusicDirID] = offset + 100;
                    GetFromCash(dir, callback);
                }
                else
                {
                    callback(track);
                }
            }
        }

        public void CloseDirForSearch(MusiсDir dir)
        {
            dir.CloseForSearch = 1;
            Context.SaveChanges();
        }

        public void SearchNextTrack( Action<Track> callback )
        {
            ChangeTrackBatch();
            var dir = CurrentSearchDirs[_rand.Next(CurrentSearchDirs.Count)];
            GetFromCash(dir, callback);
        }

        public void SaveTrack(Track track, Action<Track> callback)
        {
            using (var wc = new WebClient())
            {
                wc.DownloadDataCompleted += (sender, args) =>
                {
                    var dirRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Music", "EMusic"); //Context.Settings.Where(s => s.Key.Equals(SettingsStr.DirSaveTracks)).FirstOrDefault();

                    if (!Directory.Exists(dirRoot))
                        Directory.CreateDirectory(dirRoot);

                    track.MusiсDir = Context.MusiсDirs.FirstOrDefault(md => md.MusicDirID == track.MusicDirID);

                    var tmp = track.MusiсDir.NameDir.Split('\\');
                    var dirPart = tmp[tmp.Length - 1].ToLower();

                    var dir = Path.Combine(dirRoot, dirPart);

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var finalPath = Path.Combine(dir, track.TrackName + ".mp3");

                    File.WriteAllBytes(finalPath, args.Result);
                    track.TrackFileName = finalPath;

                    track.TrackSave = 1;
                    track.MusiсDir.PositiveRating++;
                    Context.SaveChanges();

                    if (callback != null)
                        callback(track);
                };
                wc.DownloadDataAsync(new Uri(track.TrackUrl));
            }

            
        }


    }
}
