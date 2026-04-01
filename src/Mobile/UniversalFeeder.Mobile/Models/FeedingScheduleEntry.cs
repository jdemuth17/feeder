using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UniversalFeeder.Mobile.Models
{
    public class FeedingScheduleEntry : INotifyPropertyChanged
    {
        private TimeSpan _time = TimeSpan.Zero;
        private double _durationSeconds = 5.0;
        private bool _enabled = true;

        public TimeSpan Time
        {
            get => _time;
            set { if (_time != value) { _time = value; OnPropertyChanged(); } }
        }

        public double DurationSeconds
        {
            get => _durationSeconds;
            set { if (_durationSeconds != value) { _durationSeconds = value; OnPropertyChanged(); } }
        }

        public bool Enabled
        {
            get => _enabled;
            set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
