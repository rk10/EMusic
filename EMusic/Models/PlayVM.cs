using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EMusic.Models;
using Microsoft.Practices.Prism;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.ViewModel;

namespace EMusicServices
{
    public class PlayVM : NotificationObject
    {
        private MusicService MusicService { get; set; }
        private bool _isMusicDirsSync;
        private Player _player;
        private bool _IsSearchMode = false;

        public PlayVM()
        {
            _player = new Player();

            _player.ChangeProgress += (curr, all) =>
            {
                _currentProgress = (curr / all) * 100;
                RaisePropertyChanged("CurrentProgress");
            };

            _player.EndTrack += track =>
            {
                var vm = ShownTracks.Where(t => t.Track.TrackID == track.TrackID).FirstOrDefault();
                if (vm == null)
                    _currentProgress = 0;
                else
                {
                    ShownTracks[(ShownTracks.IndexOf(vm) + 1) % ShownTracks.Count].Play();
                    //CurrentProgress = 0;
                }
                Raise();
            };

            ShownTracks = new ObservableCollection<TrackVM>();
            ShownMusiсDirs = new ObservableCollection<MusiсDirVM>();
            MusicService = new MusicService();

            var arr = new List<MusiсDirVM>();
            MusicService.GetMusicDirs().ForEach(d => arr.Add(new MusiсDirVM(ShownMusiсDirs, ShownTracks, _player, MusicService) { MusiсDir = d }));
            ShownMusiсDirs.AddRange(arr.OrderBy(d => d.NameDir).ToList());
            
            MusicService.DownloadMusicDirs((dirs) =>
            {
                MusicService.SupplementMusicDirs(dirs).ForEach(d => ShownMusiсDirs.Add(new MusiсDirVM(ShownMusiсDirs, ShownTracks, _player, MusicService) { MusiсDir = d }));
                Raise();
            });

            SearchModeCommand = new DelegateCommand(SearchModeExecute);

            Raise();
        }

        private double _currentProgress;

        public double CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                _player.SetPosition(value);
                _currentProgress = value;
            }
        }

        public bool IsBusy { get; set; }

        public ImageSource SearchImg
        {
            get { return IsBusy ? Images.ClockImg : Images.SearchImg; }
        }

        public DelegateCommand SearchModeCommand { get; set; }

        public void SearchModeExecute()
        {
            IsBusy = true; 
            Raise();
            MusicService.SearchNextTrack(track =>
            {
                IsBusy = false;
                Raise();
                ShownTracks.Insert(0, new TrackVM(MusicService, _player, ShownTracks) { IsNewTrack = true, Track = track});
                MusicService.SaveTrackInDB(track);
            });
        }

        public ObservableCollection<TrackVM> ShownTracks { get; set; }
        public ObservableCollection<MusiсDirVM> ShownMusiсDirs { get; set; }

        public void Raise()
        {
            RaisePropertyChanged("IsBusy");
            RaisePropertyChanged("SelectTrack");
            RaisePropertyChanged("SelectMusiсDir");
            RaisePropertyChanged("ShownMusiсDirs");
            RaisePropertyChanged("ShownTracks");
            RaisePropertyChanged("SearchImg");
            RaisePropertyChanged("CurrentProgress");
        }

        //public event PropertyChangingEventHandler PropertyChanging;
    }

    public class MusiсDirVM : NotificationObject
    {
        public MusiсDirVM(ObservableCollection<MusiсDirVM> shownMusiсDirs, ObservableCollection<TrackVM> shownTracks, Player player, MusicService service)
        {
            ShownMusiсDirs = shownMusiсDirs;
            ShownTracks = shownTracks;
            Player = player;
            Service = service;
            SelectMusicDirCommand = new DelegateCommand(SelectMusicDirExecute);
            Raise();
        }

        public DelegateCommand SelectMusicDirCommand { get; set; }

        public void SelectMusicDirExecute()
        {
            foreach (var dir in ShownMusiсDirs)
            {
                dir.IsSelect = false;
                dir.Raise();
            }
            IsSelect = true;
            Raise();

            ShownTracks.Clear();
            if (Contents == null)
            {
                var arrTracks = NameDir.Equals(@"e:\") ? Service.GetTracks() : MusiсDir.Tracks;

                Contents = new List<TrackVM>();
                foreach (var track in arrTracks.Where(t => t.TrackSave == 1))
                    Contents.Add(new TrackVM(Service, Player, ShownTracks) { IsNewTrack = false, Track = track });
            }
            Player.Pause();
            Contents.ForEach(t => ShownTracks.Add(t));
        }

        public ObservableCollection<MusiсDirVM> ShownMusiсDirs { get; set; }
        public ObservableCollection<TrackVM> ShownTracks { get; set; }
        public Player Player { get; set; }
        public MusicService Service { get; set; }

        public Brush CellBackground
        {
            get { return new SolidColorBrush(IsSelect ? Colors.Gray : Colors.Transparent); }
        }

        public bool IsSelect { get; set; }

        public MusiсDir MusiсDir { get; set; }

        public string NameDir
        {
            get
            {
                return MusiсDir.NameDir.Replace("Music\\", "").Replace("music\\", "").ToLower();
            }
        }

        public List<TrackVM> Contents { get; set; }

        public void Raise()
        {
            RaisePropertyChanged("CellBackground");
            RaisePropertyChanged("NameDir");
        }
    }

    public class TrackVM : NotificationObject
    {
        //private Track _track;

        public TrackVM(MusicService service, Player player, ObservableCollection<TrackVM> shownTracks)
        {
            Service = service;
            ShownTracks = shownTracks;
            Player = player;

            SaveTrackCommand = new DelegateCommand(SaveTrackExecute);
            PlayPauseTrackCommand = new DelegateCommand(PlayPauseTrackExecute);

            IsPlay = false;
            IsBusy = false;
        }

        public Player Player { get; set; }

        public ObservableCollection<TrackVM> ShownTracks { get; set; }

        public DelegateCommand PlayPauseTrackCommand { get; set; }

        public void Play()
        {
            IsPlay = false;
            PlayPauseTrackExecute();
        }

        public void PlayPauseTrackExecute()
        {
            Player.Pause();

            IsPlay = !IsPlay;

            if (IsPlay)
            {
                foreach (var track in ShownTracks)
                    if (track != this)
                    {
                        track.IsPlay = false;
                        track.Raise();
                    }
                Player.Track = Track;
                Player.Play();
            }
            else
            {
                Player.Pause();
            }

            Raise();
        }

        public DelegateCommand SaveTrackCommand { get; set; }

        public void SaveTrackExecute()
        {
            IsBusy = true;
            Raise();
            Service.SaveTrack(Track, track =>
            {
                IsBusy = false;
                Raise();
            });
            IsNewTrack = false;
            
            //PropertyChanging(null, null);
        }

        public bool IsBusy { get; set; }

        public bool IsPlay { get; set; }

        public Track Track { get; set; }

        private MusicService Service { get; set; }

        public bool IsNewTrack { get; set; }

        public string TrackName
        {
            get
            {
                return Track != null ? (Track.TrackName.Length > 30 ? Track.TrackName.Substring(0, 30) : Track.TrackName) : "";
            }
        }

        public string TrackAuthor
        {
            get
            {
                return (Track != null
                    ? (Track.TrackAuthor.Length > 15 ? Track.TrackAuthor.Substring(0, 15) : Track.TrackAuthor) +
                      (" (" + Track.MusiсDir.NameDir + ")")
                    : "");
            }
        }

        public Visibility IsVisibilitySaveButton
        {
            get { return IsNewTrack ? Visibility.Visible : Visibility.Collapsed; }
        }

        public ImageSource PlayPauseImg
        {
            get
            {
                return IsPlay ? Images.PauseImg : Images.PlayImg;
            }
        }

        public ImageSource SaveImg
        {
            get
            {
                return IsBusy ? Images.ClockImg : Images.SaveImg;
            }
        }

        public void Raise()
        {
            RaisePropertyChanged("PlayPauseImg");
            RaisePropertyChanged("IsVisibilitySaveButton");
            RaisePropertyChanged("SaveImg");
        }

        //public event PropertyChangingEventHandler PropertyChanging;
    }

    class Images
    {
        public static BitmapImage PlayImg = new BitmapImage(GetImgUri(@"Image/play.png"));
        public static ImageSource PauseImg = new BitmapImage(GetImgUri(@"Image/pause.png"));
        public static ImageSource SearchImg = new BitmapImage(GetImgUri(@"Image/search.png"));
        //public static ImageSource ProgressImg = new BitmapImage(GetImgUri(@"Image/progress.GIF"));
        public static ImageSource SaveImg = new BitmapImage(GetImgUri(@"Image/save.png"));
        public static ImageSource ClockImg = new BitmapImage(GetImgUri(@"Image/clock.png"));

        public static Uri GetImgUri(string respurcePath)
        {
            var uri = string.Format(
                "pack://application:,,,/{0};component/{1}"
                , Assembly.GetExecutingAssembly().GetName().Name
                , respurcePath
            );

            return new Uri(uri);
        }
    }
}
