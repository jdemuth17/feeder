using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace UniversalFeeder.Mobile.Models
{
    public class FeederDevice : INotifyPropertyChanged
    {
        private string _nickname = string.Empty;
        private bool _isSelected;

        public string UniqueId { get; set; } = string.Empty;

        public string Nickname
        {
            get => _nickname;
            set { if (_nickname != value) { _nickname = value; OnPropertyChanged(); } }
        }

        public string IpAddress { get; set; } = string.Empty;
        public DateTime ProvisionedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
