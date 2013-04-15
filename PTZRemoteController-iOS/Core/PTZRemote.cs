using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace PTZRemoteController.Core
{
	public class PTZRemote
	{
		private string _remoteGroup;
		private HubConnection _connection;
		private IHubProxy _proxy;

		public async Task<bool> Connect(string relayServerUrl, string remoteGroup, string hubName)
		{
			try
			{
				_remoteGroup = remoteGroup;
				_connection = new HubConnection(relayServerUrl);
				_proxy = _connection.CreateHubProxy(hubName);

				await _connection.Start();
				
				if (_connection.State == ConnectionState.Connected)
					return await _proxy.Invoke("JoinRelay", _remoteGroup)
									   .ContinueWith(task => !task.IsFaulted);
			}
			catch (Exception)
			{
			}

			return false;
		}

		public async Task<bool> MoveUp()
		{
			return await Invoke("Move", 0, 1, _remoteGroup);
		}

		public async Task<bool> MoveDown()
		{
			return await Invoke("Move", 0, -1, _remoteGroup);
		}

		public async Task<bool> MoveLeft()
		{
			return await Invoke("Move", -1, 0, _remoteGroup);
		}

		public async Task<bool> MoveRight()
		{
			return await Invoke("Move", 1, 0, _remoteGroup);
		}

		public async Task<bool> ZoomIn()
		{
			return await Invoke("Zoom", 1, _remoteGroup);
		}

		public async Task<bool> ZoomOut()
		{
			return await Invoke("Zoom", -1, _remoteGroup);
		}

		private async Task<bool> Invoke(string method, params object[] args)
		{
			try 
			{
				if (_proxy == null || _connection == null || _connection.State != ConnectionState.Connected)
					return false;

				return await _proxy.Invoke(method, args)
								   .ContinueWith(task => !task.IsFaulted);
			}
			catch (Exception) 
			{
				return false;
			}
		}
	}
}