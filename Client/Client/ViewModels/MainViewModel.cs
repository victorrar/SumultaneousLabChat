using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Client.Views;
using ReactiveUI;
using Splat;

namespace Client.ViewModels;

public class MainViewModel : ViewModelBase, IScreen
{
    // The Router associated with this Screen.
    // Required by the IScreen interface.
    public RoutingState Router { get; } = new RoutingState();

    // The command that navigates a user to first view model.
    public ReactiveCommand<Unit, IRoutableViewModel> GoNext { get; }

    // The command that navigates a user back.
    public ReactiveCommand<Unit, IRoutableViewModel?> GoBack { get; }
    public string Greeting => "Welcome to Avalonia!";

    public MainViewModel()
    {
        Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());

        GoNext = ReactiveCommand.CreateFromObservable(() => Router.Navigate.Execute(new LoginViewModel(this)));

        GoBack =  Router.NavigateBack;
        GoNext.Execute();
    }
}