using System;
using Microsoft.Maui.Controls;
using UniversalFeeder.Mobile.Models;
using UniversalFeeder.Mobile.ViewModels;

namespace UniversalFeeder.Mobile.Views;

[QueryProperty(nameof(FeederId), "feederId")]
public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;

    public SchedulePage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public string FeederId
    {
        set => _viewModel.SetFeederId(Uri.UnescapeDataString(value ?? string.Empty));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ReloadFeedTypes();
        PopulateFeedTypePicker();
        PopulateCupsPicker();
    }

    // ── Picker population ────────────────────────────────────────────────────

    private void PopulateFeedTypePicker()
    {
        feedTypePicker.Items.Clear();
        feedTypePicker.Items.Add("(None — enter seconds)");
        foreach (var ft in _viewModel.AvailableFeedTypes)
            feedTypePicker.Items.Add(ft.Name);
        feedTypePicker.SelectedIndex = 0;
    }

    private void PopulateCupsPicker()
    {
        cupsPicker.Items.Clear();
        foreach (var label in ScheduleViewModel.CupLabels)
            cupsPicker.Items.Add(label);
        cupsPicker.SelectedIndex = 3; // default = 1 cup
    }

    // ── Picker events ────────────────────────────────────────────────────────

    private void OnFeedTypePickerChanged(object sender, EventArgs e)
    {
        bool hasFeedType = feedTypePicker.SelectedIndex > 0;
        cupsSection.IsVisible = hasFeedType;
        rawSecondsSection.IsVisible = !hasFeedType;
        UpdateComputedLabel();
    }

    private void OnCupsPickerChanged(object sender, EventArgs e) => UpdateComputedLabel();

    private void OnChimeLeadStepperChanged(object sender, ValueChangedEventArgs e)
    {
        int secs = (int)e.NewValue;
        chimeLeadLabel.Text = secs == 0 ? "No chime delay" : $"{secs}s before motor";
    }

    private void OnChimeCountStepperChanged(object sender, ValueChangedEventArgs e)
    {
        int count = (int)e.NewValue;
        chimeCountLabel.Text = count == 0 ? "No chimes" : (count == 1 ? "1 chime" : $"{count} chimes");
    }

    private void OnChimeDurationStepperChanged(object sender, ValueChangedEventArgs e)
    {
        chimeDurationLabel.Text = $"{e.NewValue:F1}s per chime";
    }

    private void UpdateComputedLabel()
    {
        int ftIdx = feedTypePicker.SelectedIndex - 1;
        int cupsIdx = cupsPicker.SelectedIndex;
        if (ftIdx < 0 || ftIdx >= _viewModel.AvailableFeedTypes.Count || cupsIdx < 0)
        {
            computedDurationLabel.Text = string.Empty;
            return;
        }
        var ft = _viewModel.AvailableFeedTypes[ftIdx];
        var cups = ScheduleViewModel.CupValues[cupsIdx];
        computedDurationLabel.Text = $"≈ {cups * ft.SecondsPerCup:F1}s motor run";
    }

    // ── Add entry ────────────────────────────────────────────────────────────

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var ts = timePicker.Time ?? TimeSpan.Zero;

        FeedType? feedType = null;
        double cups = ScheduleViewModel.CupValues[3]; // 1 cup default
        double rawSeconds = 5.0;

        int ftIdx = feedTypePicker.SelectedIndex - 1;
        if (ftIdx >= 0 && ftIdx < _viewModel.AvailableFeedTypes.Count)
        {
            feedType = _viewModel.AvailableFeedTypes[ftIdx];
            int cupsIdx = cupsPicker.SelectedIndex;
            if (cupsIdx >= 0 && cupsIdx < ScheduleViewModel.CupValues.Length)
                cups = ScheduleViewModel.CupValues[cupsIdx];
        }
        else
        {
            if (!double.TryParse(amountEntry.Text, out rawSeconds))
                rawSeconds = 5.0;
        }

        int chimeLeadSeconds = (int)chimeLeadStepper.Value;
        int chimeCount = (int)chimeCountStepper.Value;
        double chimeDurationSeconds = chimeDurationStepper.Value;

        if (!_viewModel.AddEntry(ts, feedType, cups, rawSeconds, chimeLeadSeconds,
                                  chimeCount, chimeDurationSeconds,
                                  enabledSwitch.IsToggled, out var error) && error != null)
        {
            await DisplayAlertAsync("Invalid entry", error, "OK");
            return;
        }

        amountEntry.Text = string.Empty;
    }
}
