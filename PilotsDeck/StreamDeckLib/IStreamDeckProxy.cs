using StreamDeckLib.Messages;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace StreamDeckLib
{
	public interface IStreamDeckProxy : IDisposable
	{

		Task ConnectAsync(Uri uri, CancellationToken token);

		WebSocketState State { get; }

		Task Register(string registerEvent, string uuid);

		Task<string> GetMessageAsString(CancellationToken token);

		Task SendStreamDeckEvent(BaseStreamDeckArgs args);

	}

}
