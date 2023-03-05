using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Protocol;
using Server.App;
using Server.Domain;

namespace Server.Services;

public class ChatService : Chat.ChatBase
{
    private readonly ILogger<ChatService> _logger;
    private readonly ChatApp _chatApp;

    private UserSession? _session = null;
    private IServerStreamWriter<ChatMessageDownstream>? _responseStream = null!;

    public ChatService(ILogger<ChatService> logger, ChatApp chatApp)
    {
        _logger = logger;
        _chatApp = chatApp;
    }

    public override Task<LoginResponse> CheckLogin(LoginRequest request, ServerCallContext context)
    {
        return Task.FromResult(new LoginResponse()
        {
            Free = !_chatApp.IsLoginExist(request.Login)
        });
    }

    public override async Task JoinChat(LoginRequest request, IServerStreamWriter<ChatMessageDownstream> responseStream, ServerCallContext context)
    {
        _responseStream = responseStream;
        _session = _chatApp.NewUser(request.Login, Respond);

        try
        {
            await Task.Delay(int.MaxValue, context.CancellationToken);
        }
        catch (TaskCanceledException e)
        {
            _logger.Log(LogLevel.Debug, "Lost connection {login}", _session.Login);
        }
        catch (Exception e)
        {
            _logger.Log(LogLevel.Error, e, "ChatRoomService.JoinChat");
        }
        finally
        {
            _session.Dispose();
        }
    }

    private async void Respond(ChatMessageDownstream response)
    {
        await _responseStream?.WriteAsync(response);
    }

    public override async Task<Empty> Send(ChatMessageUpstream request, ServerCallContext context)
    {
        _chatApp.GotMessage(request);
        return new Empty();
    }
}