using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;


public class TrackingHub : Hub
{
    // client calls: JoinDevice("123")
    public Task JoinDevice(string deviceId)
        => Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));

    public Task LeaveDevice(string deviceId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));

    public Task JoinVehicle(string vehicleId)
        => Groups.AddToGroupAsync(Context.ConnectionId, VehicleGroup(vehicleId));

    public Task LeaveVehicle(string vehicleId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, VehicleGroup(vehicleId));

    public Task JoinOrg(int orgId)
        => Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.Org(orgId));

    public Task LeaveOrg(int orgId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroupNames.Org(orgId));

    public Task JoinTopic(string topic)
        => Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.Topic(topic));

    public Task LeaveTopic(string topic)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroupNames.Topic(topic));

    public Task JoinOrgTopic(int orgId, string topic)
        => Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.OrgTopic(orgId, topic));

    public Task LeaveOrgTopic(int orgId, string topic)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroupNames.OrgTopic(orgId, topic));

    private static string DeviceGroup(string deviceId) => $"device:{deviceId}";
    private static string VehicleGroup(string vehicleId) => $"vehicle:{vehicleId}";
}
