using Protocol;
using Server.Services;

namespace Server.Domain;

public class UserSession : IDisposable
{
    private readonly OnRespondDelegate? _onRespond;
    
    public event Action<UserSession>? OnDispose;

    public delegate void OnRespondDelegate(ChatMessageDownstream chatMessage);

    public string Login { get; }

    public UserSession(string login, OnRespondDelegate? onRespond = null)
    {
        _onRespond = onRespond;
        Login = login;
    }

    public void Respond(ChatMessageDownstream chatMessage)
    {
        _onRespond?.Invoke(chatMessage);
    }

    public void Dispose()
    {
        OnDispose?.Invoke(this);
    }
}