using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AsyncHttpLab.Models;
using static System.Environment;

namespace AsyncHttpLab
{

	public static class Downloader
	{
		private static List<byte> _response = new List<byte>();
		private static string _path;
		private static string _host;
		private static ContentType _type;

		public delegate Task DownloadCompleted(byte[] response, ContentType contentType);
		public static event DownloadCompleted OnDownloadCompleted;

		/// <summary>
		/// Download file from specified url
		/// </summary>
		/// <param name="url">url to download from </param>
		/// <param name="callback">action applied to the result</param>
		public static void Download(string host, string path, ContentType type)
		{
			_response.Clear();
			_host = host;
			_path = path;
			_type = type;

			Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			var saea = new SocketAsyncEventArgs();
			saea.Completed += SocketActionCompleted;
			saea.RemoteEndPoint = new DnsEndPoint(host, 80);
			socket.ConnectAsync(saea);
		}


		private static async void SocketActionCompleted(object sender, SocketAsyncEventArgs e)
		{
			if (!(SocketError.Success == e.SocketError))
				return;

			try
			{

				switch (e.LastOperation)
				{
					case SocketAsyncOperation.Connect:
						Send(e);
						break;
					case SocketAsyncOperation.Send:
						e.SetBuffer(new byte[2000], 0, 2000);
						e.ConnectSocket.ReceiveAsync(e);
						break;

					case SocketAsyncOperation.Receive:
						//receive till no data is transfered anymore
						if (e.BytesTransferred == 0 && _response.Any())
						{
							var result = GetBody();
							e.ConnectSocket.Dispose();
							e.Dispose();
							e.Completed -= SocketActionCompleted;
							await OnDownloadCompleted(result, _type);
							break;
						}

						_response.AddRange(e.Buffer.Take(e.BytesTransferred));
						e.ConnectSocket.ReceiveAsync(e);
						break;
				}

			}
			catch
			{
				await OnDownloadCompleted(null, _type);
				e.ConnectSocket.Dispose();
				e.Dispose();
				e.Completed -= SocketActionCompleted;
			}
		}

		private static byte[] GetBody()
		{
			byte[] result = null;
			for (int i = 0; i < _response.Count - 4; i++)
			{
				if (_response[i + 0] == 13 &&  //check for two newlines
					_response[i + 1] == 10 &&
					_response[i + 2] == 13 &&
					_response[i + 3] == 10)
				{
					result = _response.Skip(i + 4).Take(_response.Count - i - 4).ToArray();
					break;
				}
			}
			return result;
		}

		private static void Send(SocketAsyncEventArgs e)
		{
			var request = @"GET " + _path + " HTTP/1.0" + NewLine +
			"Host: " + _host + NewLine +
			NewLine;

			byte[] requestBuffer = Encoding.UTF8.GetBytes(request);

			e.SetBuffer(requestBuffer, 0, requestBuffer.Length);
			e.ConnectSocket.SendAsync(e);
		}

	}
}
