using Microsoft.Maui.Controls;
using UniversalFeeder.Mobile.ViewModels;

namespace UniversalFeeder.Mobile.Views;

[QueryProperty(nameof(FeederId), "feederId")]
public partial class FeedTypePage : ContentPage
{
    private readonly FeedTypeViewModel _viewModel;

    public FeedTypePage(FeedTypeViewModel viewModel)
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
        _viewModel.Reload();
        PopulateMeasuredCupsPicker();
    }

    private void PopulateMeasuredCupsPicker()
    {
        measuredCupsPicker.Items.Clear();
        foreach (var label in FeedTypeViewModel.CupLabels)
            measuredCupsPicker.Items.Add(label);
        measuredCupsPicker.SelectedIndex = _viewModel.MeasuredCupsIndex;
        measuredCupsLabel.Text = FeedTypeViewModel.CupLabels[_viewModel.MeasuredCupsIndex];
    }

    private void OnMeasuredCupsPickerChanged(object sender, EventArgs e)
    {
        int idx = measuredCupsPicker.SelectedIndex;
        if (idx < 0 || idx >= FeedTypeViewModel.CupLabels.Length) return;
        _viewModel.MeasuredCupsIndex = idx;
        measuredCupsLabel.Text = FeedTypeViewModel.CupLabels[idx];
    }
}
