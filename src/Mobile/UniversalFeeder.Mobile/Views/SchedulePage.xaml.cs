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

    private void OnAddClicked(object sender, EventArgs e)
    {
        var ts = timePicker.Time ?? TimeSpan.Zero;
        if (!double.TryParse(amountEntry.Text, out var secs)) secs = 5.0;
        _viewModel.AddEntry(ts, secs, enabledSwitch.IsToggled);
        amountEntry.Text = string.Empty;
    }
}
