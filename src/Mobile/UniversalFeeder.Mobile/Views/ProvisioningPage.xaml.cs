using UniversalFeeder.Mobile.ViewModels;

namespace UniversalFeeder.Mobile.Views;

public partial class ProvisioningPage : ContentPage
{
	public ProvisioningPage(ProvisioningViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
