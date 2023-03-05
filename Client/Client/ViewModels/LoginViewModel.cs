using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Grpc.Net.Client;
using Protocol;
using ReactiveUI;
using Splat;

namespace Client.ViewModels;

public class LoginViewModel : ViewModelBase, IRoutableViewModel
{
    private bool _isEnabled = true;
    private string _login;
    private string _serverUrl = "http://localhost:5000";
    public string UrlPathSegment => "Login";

    public IScreen HostScreen { get; }

    public string ServerUrl
    {
        get => _serverUrl;
        set => this.RaiseAndSetIfChanged(ref _serverUrl, value);
    }

    public string Login
    {
        get => _login;
        set => this.RaiseAndSetIfChanged(ref _login, value);
    }

    public ICommand LoginCommand { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public LoginViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen; // ?? Locator.Current.GetService<IScreen>())
        LoginCommand = ReactiveCommand.CreateFromTask(OnLogin);
        
        var names = new List<string>{ "Alice", "Bob", "Charlie", "Chuck", "Dan", "Grace" };

        var name = names[Random.Shared.Next(names.Count)];
        Login = name;
    }

    private async Task OnLogin()
    {
        if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(ServerUrl))
            return;

        IsEnabled = false;

        try
        {
            var grpcClient = new Chat.ChatClient(GrpcChannel.ForAddress(ServerUrl));
            var response = await grpcClient.CheckLoginAsync(new LoginRequest() { Login = Login });
            if (response.Free)
                HostScreen.Router.Navigate.Execute(new ChatViewModel(HostScreen, grpcClient, Login));
        }
        catch (Grpc.Core.RpcException e)
        {
            Console.WriteLine(e);
        }

        IsEnabled = true;
    }
}