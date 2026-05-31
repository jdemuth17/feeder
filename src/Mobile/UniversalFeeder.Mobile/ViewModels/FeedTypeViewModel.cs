using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using UniversalFeeder.Mobile.Models;
using UniversalFeeder.Mobile.Services;

namespace UniversalFeeder.Mobile.ViewModels
{
    public class FeedTypeViewModel : BindableObject
    {
        // Cup value table shared with ScheduleViewModel / DashboardViewModel
        public static readonly double[] CupValues = { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0, 3.5, 4.0 };
        public static readonly string[] CupLabels = { "¼ cup", "½ cup", "¾ cup", "1 cup", "1¼ cups", "1½ cups", "1¾ cups", "2 cups", "2½ cups", "3 cups", "3½ cups", "4 cups" };

        private readonly FeedTypeService _feedTypeService;
        private readonly MqttService _mqttService;

        public ObservableCollection<FeedType> FeedTypes { get; } = new();

        // ── Add / calibrate form ─────────────────────────────────────────────

        private string _newName = string.Empty;
        public string NewName
        {
            get => _newName;
            set { _newName = value; OnPropertyChanged(); }
        }

        private double _newSecondsPerCup = 5.0;
        public double NewSecondsPerCup
        {
            get => _newSecondsPerCup;
            set { _newSecondsPerCup = Math.Max(0.1, value); OnPropertyChanged(); }
        }

        // ── Calibration ──────────────────────────────────────────────────────

        private string _feederId = string.Empty;
        public string FeederId
        {
            get => _feederId;
            set { _feederId = value; OnPropertyChanged(); }
        }

        private double _calibrationSeconds = 5.0;
        public double CalibrationSeconds
        {
            get => _calibrationSeconds;
            set { _calibrationSeconds = Math.Clamp(value, 1, 300); OnPropertyChanged(); }
        }

        private int _measuredCupsIndex = 3; // default = 1 cup
        public int MeasuredCupsIndex
        {
            get => _measuredCupsIndex;
            set { _measuredCupsIndex = Math.Clamp(value, 0, CupValues.Length - 1); OnPropertyChanged(); }
        }
        public double MeasuredCups => CupValues[_measuredCupsIndex];

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _calibrationStatus = string.Empty;
        public string CalibrationStatus
        {
            get => _calibrationStatus;
            set { _calibrationStatus = value; OnPropertyChanged(); }
        }

        // ── Commands ─────────────────────────────────────────────────────────

        public ICommand AddManualCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RunCalibrationCommand { get; }
        public ICommand SaveCalibrationCommand { get; }

        public FeedTypeViewModel(FeedTypeService feedTypeService, MqttService mqttService)
        {
            _feedTypeService = feedTypeService;
            _mqttService = mqttService;

            AddManualCommand = new Command(OnAddManual);
            DeleteCommand = new Command<FeedType>(OnDelete);
            RunCalibrationCommand = new Command(async () => await OnRunCalibrationAsync());
            SaveCalibrationCommand = new Command(OnSaveCalibration);

            Reload();
        }

        public void SetFeederId(string id) => FeederId = id ?? string.Empty;

        public void Reload()
        {
            FeedTypes.Clear();
            foreach (var ft in _feedTypeService.GetAll())
                FeedTypes.Add(ft);
        }

        private void OnAddManual()
        {
            if (string.IsNullOrWhiteSpace(NewName)) return;
            var ft = new FeedType { Name = NewName.Trim(), SecondsPerCup = NewSecondsPerCup };
            _feedTypeService.Save(ft);
            FeedTypes.Add(ft);
            NewName = string.Empty;
            NewSecondsPerCup = 5.0;
        }

        private void OnDelete(FeedType ft)
        {
            if (ft == null) return;
            _feedTypeService.Delete(ft.Id);
            FeedTypes.Remove(ft);
        }

        private async Task OnRunCalibrationAsync()
        {
            if (string.IsNullOrWhiteSpace(FeederId) || !_mqttService.IsConnected)
            {
                CalibrationStatus = "Connect to MQTT and select a feeder on the dashboard first.";
                return;
            }

            IsBusy = true;
            CalibrationStatus = $"Running motor for {CalibrationSeconds:F0}s — watch how much food drops...";
            await _mqttService.SendFeedCommandAsync(FeederId, (int)(CalibrationSeconds * 1000));
            IsBusy = false;
            CalibrationStatus = "Done! Set the measured cups below, then tap Save Rate.";
        }

        private void OnSaveCalibration()
        {
            if (string.IsNullOrWhiteSpace(NewName))
            {
                CalibrationStatus = "Enter a feed type name first.";
                return;
            }

            double cups = MeasuredCups;
            if (cups <= 0)
            {
                CalibrationStatus = "Measured cups must be > 0.";
                return;
            }

            double rate = CalibrationSeconds / cups;
            var ft = new FeedType { Name = NewName.Trim(), SecondsPerCup = rate };
            _feedTypeService.Save(ft);
            FeedTypes.Add(ft);
            CalibrationStatus = $"Saved \"{ft.Name}\" — {rate:F2}s per cup.";
            NewName = string.Empty;
        }
    }
}
