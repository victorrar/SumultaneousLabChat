using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using Grpc.Core;
using Protocol;
using ReactiveUI;

namespace Client.ViewModels;

public class ChatViewModelMessage
{
    public string Login { get; set; }
    public IBrush Color { get; set; }
    public string Text { get; set; }
}

public class ChatViewModelLogin
{
    public string Login { get; set; }
    public IBrush Color { get; set; }
}

public class ChatViewModel : ViewModelBase, IRoutableViewModel, IActivatableViewModel
{
    private Chat.ChatClient _chatClient;
    private readonly string _login;
    private string _messageText;


    public ObservableCollection<ChatViewModelMessage> Messages { get; } = new();


    public string MessageText
    {
        get => _messageText;
        set => this.RaiseAndSetIfChanged(ref _messageText, value);
    }

    private const string broadcastLogin = "Any";
    public ICommand SendCommand { get; }
    public string? UrlPathSegment { get; } = "Chat";
    public IScreen HostScreen { get; }
    public ViewModelActivator Activator { get; }
    public ObservableCollection<ChatViewModelLogin> UserList { get; } = new();
    private ChatViewModelLogin _selectedLogin;
    public ChatViewModelLogin SelectedMessage { 
        get => _selectedLogin;
        set => this.RaiseAndSetIfChanged(ref _selectedLogin, value);
    }
    private CancellationTokenSource _cancellationTokenSource;

    public ChatViewModel(IScreen hostScreen, Chat.ChatClient chatClient, string login)
    {
        HostScreen = hostScreen;
        _chatClient = chatClient;
        _login = login;

        SendCommand = ReactiveCommand.CreateFromTask(OnSend);

        _cancellationTokenSource = new CancellationTokenSource();
        Activator = new ViewModelActivator();

        UserList.Add(new() { Login = broadcastLogin, Color = new SolidColorBrush(Colors.White) });
        SelectedMessage = UserList.First();
        UserList.CollectionChanged += (sender, args) => { this.RaisePropertyChanged(nameof(UserList)); };

        Messages.CollectionChanged += (sender, args) => { this.RaisePropertyChanged(nameof(Messages)); };

        this.WhenActivated(disposables =>
        {
            Dispatcher.UIThread.Post(async () => await ListenForStreamUpdates(_cancellationTokenSource.Token),
                DispatcherPriority.Background);

            Disposable.Create(() => _cancellationTokenSource.Cancel()).DisposeWith(disposables);
        });
    }


    private async Task OnSend()
    {
        if (string.IsNullOrEmpty(MessageText))
            return;
        try
        {
            var chatMessageUpstream = new ChatMessageUpstream()
            {
                Login = _login,
                Text = new MessageText
                {
                    Text = MessageText,
                    Login = "",
                    IsPrivate = false,
                },
            };
            
            if (SelectedMessage.Login != broadcastLogin)
            {
                chatMessageUpstream.Text.Login = SelectedMessage.Login;
                chatMessageUpstream.Text.IsPrivate = true;
                
                
                var msg = new ChatViewModelMessage()
                {
                    Login = $"[me -> {chatMessageUpstream.Text.Login}]",
                    Color = BrushOfLogin(chatMessageUpstream.Text.Login),
                    Text = chatMessageUpstream.Text.Text
                };
                   
                Messages.Add(msg);
                
            }
            
            await _chatClient.SendAsync(chatMessageUpstream);

            MessageText = "";
        }
        catch (Grpc.Core.RpcException e)
        {
            Console.Error.Write(e);
        }
    }

    private async Task ListenForStreamUpdates(CancellationToken token)
    {
        using var call = _chatClient.JoinChat(new LoginRequest() { Login = _login });

        try
        {
            await foreach (var message in call.ResponseStream.ReadAllAsync(token))
            {
                switch (message.MessageCase)
                {
                    case ChatMessageDownstream.MessageOneofCase.Text:
                        AddMessageToUi(message.Text);
                        break;
                    case ChatMessageDownstream.MessageOneofCase.UserList:
                        HandleUserList(message.UserList);
                        break;
                    case ChatMessageDownstream.MessageOneofCase.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        catch (RpcException e)
        {
            Console.Error.WriteLine(e);
        }
        finally
        {
            HostScreen.Router.NavigateBack.Execute();
        }
    }

    private void AddMessageToUi(MessageText message)
    {
        var msg = new ChatViewModelMessage()
        {
            Login = message.Login,
            Color = BrushOfLogin(message.Login),
            Text = message.Text
        };
        if (message.IsPrivate)
        {
            msg.Login = $"[{message.Login} -> me]";
        }
        Messages.Add(msg);
    }

    private static SolidColorBrush BrushOfLogin(string login)
    {
        var hsvColor = HsvColor.FromHsv(HashCode.Combine(login) % 360, 0.8, 0.8);
        return new SolidColorBrush(hsvColor.ToRgb(), 1.0);
    }

    private void HandleUserList(MessageUserList message)
    {
        HashSet<string> loginsPrev = new();
        HashSet<string> loginsNew = new();

        foreach (var login in UserList)
        {
            if (login.Login == broadcastLogin)
                continue;
            loginsPrev.Add(login.Login);
        }

        foreach (var login in message.Login)
        {
            loginsNew.Add(login);
        }

        var added = loginsNew.Except(loginsPrev);
        var removed = loginsPrev.Except(loginsNew);

        foreach (var s in added)
        {
            var msg = new ChatViewModelMessage()
            {
                Login = "[system]",
                Color = Brushes.Gray,
                Text = $"{s} joined the chat"
            };
            Messages.Add(msg);
            UserList.Add(new() { Login = s, Color = BrushOfLogin(s) });
        }

        foreach (var s in removed)
        {
            var msg = new ChatViewModelMessage()
            {
                Login = "[system]",
                Color = Brushes.Gray,
                Text = $"{s} left the chat"
            };
            Messages.Add(msg);
            UserList.Remove(UserList.First(x => x.Login == s));
        }
        
    }
}