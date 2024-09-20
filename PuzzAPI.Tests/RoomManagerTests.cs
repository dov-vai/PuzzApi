using System.Net.WebSockets;
using Moq;
using PuzzAPI.ConnectionHandler.RoomManager;

namespace PuzzAPI.Tests;

public class RoomManagerTests
{
    private readonly RoomManager _roomManager;

    public RoomManagerTests()
    {
        _roomManager = new RoomManager();
    }

    [Fact]
    public void CreateRoom_ShouldReturnSuccessAndGenerateIds()
    {
        // Act
        var success = _roomManager.CreateRoom("Test Room", 10, true,  true, out var roomId);

        // Assert
        Assert.True(success);
        Assert.NotNull(roomId);
        Assert.True(_roomManager.Contains(roomId));
    }

    [Fact]
    public void AddPeer_ShouldAddPeerToRoom()
    {
        // Arrange
        var mockSocketHost = new Mock<WebSocket>();
        var mockSocketPeer = new Mock<WebSocket>();

        _roomManager.CreateRoom("Test Room", 10, true, true, out var roomId);

        // Act
        var success = _roomManager.AddPeer(roomId, mockSocketPeer.Object, true, out var peerId);

        // Assert
        Assert.True(success);
        Assert.NotNull(peerId);
        Assert.True(_roomManager.ContainsPeer(roomId, peerId));
    }

    [Fact]
    public async Task RemoveSocketAsync_ShouldRemovePeerAndCloseSocket()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        mockSocket.Setup(s => s.CloseAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _roomManager.CreateRoom("Test Room", 10, true, true, out var roomId);
        _roomManager.AddPeer(roomId, mockSocket.Object, true, out var peerId);

        // Act
        await _roomManager.RemoveSocketAsync(roomId, peerId);

        // Assert
        Assert.False(_roomManager.ContainsPeer(roomId, peerId));
        mockSocket.Verify(s => s.CloseAsync(It.IsAny<WebSocketCloseStatus>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BroadcastAsync_ShouldSendMessageToAllPeersExceptExcluded()
    {
        // Arrange
        var mockSocket1 = new Mock<WebSocket>();
        var mockSocket2 = new Mock<WebSocket>();

        mockSocket1.Setup(s => s.State).Returns(WebSocketState.Open);
        mockSocket2.Setup(s => s.State).Returns(WebSocketState.Open);

        _roomManager.CreateRoom("Test Room", 10, true, true, out var roomId);
        _roomManager.AddPeer(roomId, mockSocket2.Object, true, out _);

        // Act
        await _roomManager.BroadcastAsync(roomId, "Test Message", mockSocket1.Object);

        // Assert
        mockSocket2.Verify(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Once);
        mockSocket1.Verify(s => s.SendAsync(It.IsAny<ArraySegment<byte>>(), WebSocketMessageType.Text, true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetPublicRooms_ShouldReturnPublicRoomsOnly()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        _roomManager.CreateRoom("Public Room", 10, true, true, out _);
        _roomManager.CreateRoom("Private Room", 5, false, true, out _);

        // Act
        var publicRooms = _roomManager.GetPublicRooms();

        // Assert
        Assert.Single(publicRooms);
        Assert.Equal("Public Room", publicRooms.First().Title);
    }

    [Fact]
    public void GetCount_ShouldReturnCorrectRoomCount()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        _roomManager.CreateRoom("Room 1", 10, true,  true, out _);
        _roomManager.CreateRoom("Room 2", 5, false,  true, out _);

        // Act
        var count = _roomManager.GetCount();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void Contains_ShouldReturnTrueIfRoomExists()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        _roomManager.CreateRoom("Room 1", 10, true, true, out var roomId);

        // Act
        var exists = _roomManager.Contains(roomId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void GetHostId_ShouldReturnCorrectHostId()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        _roomManager.CreateRoom("Room 1", 10, true, true, out var roomId);
        _roomManager.AddPeer(roomId, mockSocket.Object, true, out var hostId);

        // Act
        var returnedHostId = _roomManager.GetHostId(roomId);

        // Assert
        Assert.Equal(hostId, returnedHostId);
    }
}
