using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PuzzAPI.RoomManager;
using PuzzAPI.Types;
using Host = PuzzAPI.Types.Host;

namespace PuzzAPI.ConnectionHandler;

public class ConnectionHandler
{
    private readonly IRoomManager _manager;
    private readonly WebSocket _webSocket;
    private bool _host;
    private string? _peerId;
    private string? _roomId;

    public ConnectionHandler(WebSocket webSocket, IRoomManager manager)
    {
        _roomId = null;
        _peerId = null;
        _host = false;
        _webSocket = webSocket;
        _manager = manager;
    }

    public async Task Run()
    {
        try
        {
            await _manager.ReceiveMessageAsync(_webSocket, async (result, buf) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buf, 0, result.Count);
                    var node = JsonNode.Parse(message);
                    var type = (string?)node?["Type"];
                    if (type == null) return;
                    switch (type)
                    {
                        case MessageTypes.Host:
                        {
                            await HandleHost(message);
                            break;
                        }
                        case MessageTypes.Join:
                        {
                            await HandleJoin(message);
                            break;
                        }
                        case MessageTypes.PublicRooms:
                        {
                            await HandlePublicRooms();
                            break;
                        }
                        case MessageTypes.P2PInit:
                        {
                            await HandleP2PInit();
                            break;
                        }
                        case MessageTypes.RtcSignal:
                        {
                            await HandleSignal(message);
                            break;
                        }
                        case MessageTypes.SendInit:
                        {
                            await HandleSendInit(message);
                            break;
                        }
                        case MessageTypes.Disconnect:
                        {
                            await HandleDisconnect();
                            break;
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close || _webSocket.State == WebSocketState.Aborted)
                {
                    await CloseConnection(result);
                }
            });
        }
        catch (WebSocketException ex)
        {
            Debug.WriteLine($"Error caught, closing connection: {ex}");
            await CloseConnection();
        }
    }

    private async Task HandleHost(string msg)
    {
        if (_roomId != null || _peerId != null)
        {
            Debug.WriteLine($"{_peerId} tried to host more than one room");
            return;
        }

        var data = JsonSerializer.Deserialize<Host>(msg);
        if (data != null)
        {
            var success = _manager.CreateRoom(data.Title, data.Public, _webSocket, out _roomId,
                out _peerId);

            if (success)
            {
                _host = true;
                await _manager.SendMessageAsync(
                    _roomId,
                    _peerId,
                    JsonSerializer.Serialize(new Connected { SocketId = _peerId, RoomId = _roomId })
                );
            }
        }
    }

    private async Task HandleJoin(string msg)
    {
        if (_roomId != null || _peerId != null)
        {
            Debug.WriteLine($"{_peerId} tried to join more than one room");
            return;
        }

        var data = JsonSerializer.Deserialize<Join>(msg);

        if (data != null)
        {
            var success = _manager.AddPeer(data.RoomId, _webSocket, out _peerId);

            if (!success)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData,
                    "Provided room ID does not exist",
                    CancellationToken.None);
                return;
            }

            _roomId = data.RoomId;

            await _manager.SendMessageAsync(
                data.RoomId,
                _peerId,
                JsonSerializer.Serialize(new Connected { SocketId = _peerId, RoomId = _roomId })
            );
        }
    }

    private async Task HandlePublicRooms()
    {
        var data = JsonSerializer.Serialize(new PublicRooms
        {
            Rooms = _manager.GetPublicRooms()
        });

        var buffer = Encoding.UTF8.GetBytes(data);
        var arraySegment = new ArraySegment<byte>(buffer, 0, buffer.Length);
        await _webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true,
            CancellationToken.None);
    }

    private async Task HandleP2PInit()
    {
        if (_peerId == null || _roomId == null)
            return;

        var data = JsonSerializer.Serialize(new P2PInit
        {
            SocketId = _peerId,
            RoomId = _roomId,
            HostId = _manager.GetHostId(_roomId) ?? ""
        });

        await _manager.SendMessageAsync(_roomId, _peerId, data);

        if (!_host)
            await _manager.BroadcastAsync(
                _roomId,
                JsonSerializer.Serialize(new ReceiveInit { SocketId = _peerId }),
                _webSocket
            );
    }

    private async Task HandleSignal(string msg)
    {
        if (_roomId == null || _peerId == null)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData,
                "Not _hosting or joined server",
                CancellationToken.None);
            return;
        }

        var data = JsonSerializer.Deserialize<RtcSignal>(msg);

        if (data != null && _manager.ContainsPeer(_roomId, data.SocketId))
        {
            var signalData = JsonSerializer.Serialize(new RtcSignal
            {
                Signal = data.Signal,
                SocketId = _peerId
            });
            await _manager.SendMessageAsync(_roomId, data.SocketId, signalData);
        }
    }

    private async Task HandleSendInit(string msg)
    {
        if (_roomId == null || _peerId == null)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData,
                "Not hosting or joined server",
                CancellationToken.None);
            return;
        }

        var data = JsonSerializer.Deserialize<SendInit>(msg);
        if (data != null)
        {
            var initData = JsonSerializer.Serialize(new SendInit
            {
                SocketId = _peerId
            });
            await _manager.SendMessageAsync(_roomId, data.SocketId, initData);
        }
    }

    private async Task HandleDisconnect()
    {
        if (_roomId == null || _peerId == null) return;

        await _manager.DisconnectPeerAsync(_roomId, _peerId);

        var removeData = JsonSerializer.Serialize(new RemovePeer
        {
            SocketId = _peerId
        });

        await _manager.BroadcastAsync(_roomId, removeData);

        _peerId = null;
        _roomId = null;
        _host = false;
    }

    private async Task CloseConnection(WebSocketReceiveResult? result = null)
    {
        if (_roomId == null || _peerId == null)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed idk", CancellationToken.None);
            return;
        }

        Debug.WriteLine($"Removing socket {_peerId}");
        await _manager.RemoveSocketAsync(_roomId, _peerId, result.CloseStatus, result.CloseStatusDescription);
        var removeData = JsonSerializer.Serialize(new RemovePeer
        {
            SocketId = _peerId
        });
        await _manager.BroadcastAsync(_roomId, removeData);
    }
}