using Newtonsoft.Json;
using StreamDeckLib.Messages;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckLib
{

	internal class StreamDeckProxy : IStreamDeckProxy
	{
		private readonly ClientWebSocket _Socket = new ClientWebSocket();
		//CHANGED
		private static bool isSending = false;

		public WebSocketState State { get { return _Socket.State; } }

		public Task ConnectAsync(Uri uri, CancellationToken token)
		{
			return _Socket.ConnectAsync(uri, token);
		}
		public Task Register(string registerEvent, string uuid)
		{
			return _Socket.SendAsync(GetPluginRegistrationBytes(registerEvent, uuid), WebSocketMessageType.Text, true, CancellationToken.None);
		}

		//CHANGED
		public async Task SendStreamDeckEvent(BaseStreamDeckArgs args)
		{
			var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(args));
            while (isSending)
            {

            }
            isSending = true;
            try
            {
				await _Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
			}
			catch (Exception ex)
            {
				throw ex;
			}
			finally
            {
				isSending = false;
				
            }			
		}

		//CHANGED
		public async Task<string> GetMessageAsString(CancellationToken token)
		{
			byte[] buffer = new byte[65536];
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer, 0, buffer.Length); ;
            WebSocketReceiveResult result = new WebSocketReceiveResult(0, WebSocketMessageType.Text, false);
            int totalCount = 0;
            while (!result.EndOfMessage)
            {
                result = await _Socket.ReceiveAsync(segment, token);
                totalCount += result.Count;
                segment = new ArraySegment<byte>(buffer, totalCount, buffer.Length - totalCount);
            }
            string jsonString = Encoding.UTF8.GetString(buffer);
            
            return jsonString;
        }

		private ArraySegment<byte> GetPluginRegistrationBytes(string registerEvent, string uuid)
		{
			var registration = new Messages.Info.PluginRegistration
			{
				@event = registerEvent,
				uuid = uuid
			};

			var outString = JsonConvert.SerializeObject(registration);
			var outBytes = Encoding.UTF8.GetBytes(outString);

			return new ArraySegment<byte>(outBytes);
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{           // TODO: dispose managed state (managed objects).
				}

				_Socket.Dispose();
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		~StreamDeckProxy()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			GC.SuppressFinalize(this);
		}
		#endregion

	}

}
