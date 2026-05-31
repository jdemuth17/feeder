using UniversalFeeder.Mobile.ViewModels;

namespace UniversalFeeder.Mobile.Views;

public partial class LogsPage : ContentPage
{
    private readonly LogsViewModel _vm;

    public LogsPage(LogsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Subscribe();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Unsubscribe();
    }
}
