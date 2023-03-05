using Protocol;
using Server.Domain;

namespace Server.App;

public class ChatApp
{
    private readonly ILogger<ChatApp> _logger;
    private readonly List<UserSession> _userSessions = new List<UserSession>();

    public ChatApp(ILogger<ChatApp> logger)
    {
        _logger = logger;
    }

    public bool IsLoginExist(string login)
    {
        return _userSessions.Any(session => session.Login == login);
    }

    private ChatMessageDownstream GenerateUserListMessage()
    {
        var list = new MessageUserList();

        _userSessions.ForEach(session => list.Login.Add(session.Login));

        return new ChatMessageDownstream { UserList = list };
    }

    public UserSession NewUser(string login, UserSession.OnRespondDelegate onRespond)
    {
        _logger.Log(LogLevel.Information, "NewUser: \"{login}\" connected!", login);

        if (IsLoginExist(login))
            throw new ArgumentException("User with given login already exist");

        var session = new UserSession(login, onRespond);
        session.OnDispose += SessionDropped;
        _userSessions.Add(session);

        BroadcastUserList();

        return session;
    }

    public void GotMessage(ChatMessageUpstream message)
    {
        _logger.Log(LogLevel.Information, "Got message from \"{login}\": {message}", message.Login, message.Text.Text);

        if (!message.Text.IsPrivate)
        {
            SendBroadcastMessage(message.Login, message.Text.Text);
        }
        else
        {
            SendPrivateMessage(message.Login, message.Text.Login, message.Text.Text);
        }
    }

    private void SessionDropped(UserSession session)
    {
        _logger.Log(LogLevel.Information, "User \"{login}\" disconnected!", session.Login);

        _userSessions.Remove(session);

        BroadcastUserList();
    }

    private void SendPrivateMessage(string from, string to, string text)
    {
        _logger.Log(LogLevel.Information, "User \"{from}\" whispers to \"{to}\": {text}", from, to, text);

        var message = new ChatMessageDownstream
        {
            Text = new MessageText
            {
                Login = from,
                Text = text,
                IsPrivate = true
            }
        };

        var sessionFrom = _userSessions.FirstOrDefault(s => s.Login == from);
        var sessionTo = _userSessions.FirstOrDefault(s => s.Login == to);

        // sessionFrom?.Respond(message);
        sessionTo?.Respond(message);
    }

    private void SendBroadcastMessage(string from, string text)
    {
        _logger.Log(LogLevel.Information, "User \"{from}\" broadcasts: {text}", from, text);

        var message = new ChatMessageDownstream
        {
            Text = new()
            {
                Login = from,
                Text = text
            }
        };

        _userSessions.ForEach(session => session.Respond(message));
    }

    private void BroadcastUserList()
    {
        _logger.Log(LogLevel.Information, "Sent user list to all users");

        var message = GenerateUserListMessage();
        _userSessions.ForEach(session => session.Respond(message));
    }
}