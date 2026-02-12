using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;


public class TrackingHub : Hub
{
    // client calls: JoinDevice("123")
    public Task JoinDevice(string deviceId)
        => Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));

    public Task LeaveDevice(string deviceId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));

    private static string DeviceGroup(string deviceId) => $"device:{deviceId}";
}
