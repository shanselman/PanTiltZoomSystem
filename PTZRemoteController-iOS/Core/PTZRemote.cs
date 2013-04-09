using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace PTZRemoteController.Core
{
	public class PTZRemote
	{
		private readonly string _relayServerUrl, _remoteGroup, _hubName;
		private HubConnection _connection;
		private IHubProxy _proxy;
		
		public PTZRemote(string relayServerUrl, string remoteGroup, string hubName)
		{
			_relayServerUrl = relayServerUrl;
			_remoteGroup = remoteGroup;
			_hubName = hubName;
		}
		
		public async Task<bool> Connect()
		{
			_connection = new HubConnection(_relayServerUrl);
			_proxy = _connection.CreateHubProxy(_hubName);
			
			await _connection.Start();
			
			if (_connection.State == ConnectionState.Connected)
				await _proxy.Invoke("JoinRelay", _remoteGroup);
			
			return _connection.State == ConnectionState.Connected;
		}

		public async Task MoveUp()
		{
			await _proxy.Invoke("Move", 0, 1, _remoteGroup);
		}

		public async Task MoveDown()
		{
			await _proxy.Invoke("Move", 0, -1, _remoteGroup);
		}

		public async Task MoveLeft()
		{
			await _proxy.Invoke("Move", -1, 0, _remoteGroup);
		}

		public async Task MoveRight()
		{
			await _proxy.Invoke("Move", 1, 0, _remoteGroup);
		}

		public async Task ZoomIn()
		{
			await _proxy.Invoke("Zoom", 1, _remoteGroup);
		}

		public async Task ZoomOut()
		{
			await _proxy.Invoke("Zoom", -1, _remoteGroup);
		}
	}
}