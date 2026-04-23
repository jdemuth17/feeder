using System;
using Microsoft.Maui.Controls;
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
        set
        {
            _viewModel.SetFeederId(value);
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        var ts = timePicker.Time ?? TimeSpan.Zero;
        if (!double.TryParse(amountEntry.Text, out var secs)) secs = 5.0;
        if (!_viewModel.AddEntry(ts, secs, enabledSwitch.IsToggled, out var error) && error != null)
        {
            await DisplayAlertAsync("Invalid entry", error, "OK");
            return;
        }
        amountEntry.Text = string.Empty;
    }
}
