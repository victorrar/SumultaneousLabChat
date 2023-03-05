using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Client.ViewModels;
using ReactiveUI;

namespace Client.Views;

public partial class LoginView : ReactiveUserControl<LoginViewModel>
{
    public LoginView()
    {
        this.WhenActivated(disposables => { });
        InitializeComponent();
        
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

}