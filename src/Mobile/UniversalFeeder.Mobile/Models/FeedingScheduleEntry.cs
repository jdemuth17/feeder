using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UniversalFeeder.Mobile.Models
{
    public class FeedingScheduleEntry : INotifyPropertyChanged
    {
        private TimeSpan _time = TimeSpan.Zero;
        private double _durationSeconds = 5.0;
        private bool _enabled = true;
        private string? _feedTypeId;
        private double _cups;
        private int _chimeLeadSeconds;
        private int _chimeCount = 3;
        private double _chimeDurationSeconds = 3.0;

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

        /// <summary>ID of the FeedType used to compute this entry's duration. Null when raw seconds were entered.</summary>
        public string? FeedTypeId
        {
            get => _feedTypeId;
            set { if (_feedTypeId != value) { _feedTypeId = value; OnPropertyChanged(); } }
        }

        /// <summary>Amount in cups when a FeedType is set; 0 when raw seconds were used.</summary>
        public double Cups
        {
            get => _cups;
            set { if (_cups != value) { _cups = value; OnPropertyChanged(); } }
        }

        /// <summary>Extra delay (seconds) inserted between chime end and motor start.</summary>
        public int ChimeLeadSeconds
        {
            get => _chimeLeadSeconds;
            set { if (_chimeLeadSeconds != value) { _chimeLeadSeconds = value; OnPropertyChanged(); } }
        }

        /// <summary>Number of beeps played before the motor runs (0 disables chimes).</summary>
        public int ChimeCount
        {
            get => _chimeCount;
            set { if (_chimeCount != value) { _chimeCount = value; OnPropertyChanged(); } }
        }

        /// <summary>Duration of each beep, in seconds.</summary>
        public double ChimeDurationSeconds
        {
            get => _chimeDurationSeconds;
            set { if (Math.Abs(_chimeDurationSeconds - value) > 0.001) { _chimeDurationSeconds = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
