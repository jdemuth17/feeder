using UniversalFeeder.Mobile.ViewModels;

namespace UniversalFeeder.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadFeeders();
        _viewModel.ReloadFeedTypes();
        PopulateDashFeedTypePicker();
        PopulateDashCupsPicker();
    }

    // ── Picker population ────────────────────────────────────────────────────

    private void PopulateDashFeedTypePicker()
    {
        dashFeedTypePicker.Items.Clear();
        dashFeedTypePicker.Items.Add("(None — use seconds)");
        foreach (var ft in _viewModel.AvailableFeedTypes)
            dashFeedTypePicker.Items.Add(ft.Name);
        dashFeedTypePicker.SelectedIndex = 0;
    }

    private void PopulateDashCupsPicker()
    {
        dashCupsPicker.Items.Clear();
        foreach (var label in FeedTypeViewModel.CupLabels)
            dashCupsPicker.Items.Add(label);
        dashCupsPicker.SelectedIndex = 3; // default = 1 cup
    }

    // ── Picker events ────────────────────────────────────────────────────────

    private void OnDashFeedTypePickerChanged(object sender, EventArgs e)
    {
        int idx = dashFeedTypePicker.SelectedIndex - 1;
        if (idx >= 0 && idx < _viewModel.AvailableFeedTypes.Count)
        {
            _viewModel.SelectedFeedType = _viewModel.AvailableFeedTypes[idx];
            dashCupsSection.IsVisible = true;
            dashRawSection.IsVisible = false;
        }
        else
        {
            _viewModel.SelectedFeedType = null;
            dashCupsSection.IsVisible = false;
            dashRawSection.IsVisible = true;
        }
        UpdateDashComputedLabel();
    }

    private void OnDashCupsPickerChanged(object sender, EventArgs e)
    {
        int cupsIdx = dashCupsPicker.SelectedIndex;
        if (cupsIdx >= 0)
            _viewModel.SelectedCupsIndex = cupsIdx;
        UpdateDashComputedLabel();
    }

    private void UpdateDashComputedLabel()
    {
        if (_viewModel.SelectedFeedType == null || dashCupsPicker.SelectedIndex < 0)
        {
            dashComputedLabel.Text = string.Empty;
            return;
        }
        double cups = FeedTypeViewModel.CupValues[dashCupsPicker.SelectedIndex];
        double secs = cups * _viewModel.SelectedFeedType.SecondsPerCup;
        dashComputedLabel.Text = $"≈ {secs:F1}s motor run";
    }
}
