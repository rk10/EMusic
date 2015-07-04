using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WMPLib;

namespace EMusic.Models
{
    public class Player
    {
        WMPLib.WindowsMediaPlayer _player = new WMPLib.WindowsMediaPlayer();

        public event Action<double, double> ChangeProgress;

        public event Action<Track> EndTrack;

        public Player()
        {
            var timer = new Timer(state =>
            {
                if (_player.controls != null && _player.controls.currentItem != null)
                {
                    Console.WriteLine(_player.controls.currentPosition);
                    if (ChangeProgress != null)
                        ChangeProgress(_player.controls.currentPosition, _player.controls.currentItem.duration);
                    if (EndTrack != null && IsEndTrack())
                        EndTrack(Track);
                    _isChangeTrack = false;
                }
                    
            });
            timer.Change(DateTime.Now.Millisecond, TimeSpan.TicksPerMillisecond);
        }

        private Track _saveTrack;
        private double _savePos;
        private bool _isChangeTrack = false;

        bool IsEndTrack()
        {
            bool res =
                (_player.controls.currentPosition >= _player.controls.currentItem.duration &&
                _player.controls.currentItem.duration > 0) ||
                (_saveTrack == Track && _player.controls.currentPosition <= 0 && _savePos > 0 && _isChangeTrack);

            _saveTrack = _track;
            _savePos = _player.controls.currentPosition;
            return res;
        }

        private Track _track;

        public Track Track
        {
            get { return _track; }
            set
            {
                var url =
                    value.TrackFileName != null && File.Exists(value.TrackFileName)
                        ? value.TrackFileName
                        : value.TrackUrl;
                _player.URL = url;
                _track = value;
                //_player.controls.play();
            }
        }

        public void SetPosition(double pos)
        {
            if(_player.controls != null)
                _player.controls.currentPosition = (pos / 100) * _player.controls.currentItem.duration;
        }

        public void Play()
        {
            _isChangeTrack = true;
            _player.controls.play();
        }

        public void Pause()
        {
            _player.controls.pause();
        }

        public void Rewind()
        {
            
        }

        public bool IsPlay()
        {
            return _player.playState == WMPPlayState.wmppsPlaying;
        }
    }
}
