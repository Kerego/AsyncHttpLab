using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static System.Environment;
using HtmlAgilityPack;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using AsyncHttpLab.Models;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AsyncHttpLab
{
	
	

	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		ObservableCollection<SongContainer> _songs = new ObservableCollection<SongContainer>();
		Queue<DownloadItem> _downloadQueue = new Queue<DownloadItem>();
		ToastNotifier _toastNotifier;
		const string _mp3Host = "cdndl.zaycev.net";
		const string _searchHost = "zaycev.net";

		public MainPage()
		{
			this.InitializeComponent();
			_toastNotifier = ToastNotificationManager.CreateToastNotifier();
			Downloader.OnDownloadCompleted += DownloadCompleted;
		}

		private async Task DownloadCompleted(byte[] response, ContentType type)
		{
			var downloadItem = _downloadQueue.Dequeue();

			if (type == ContentType.mp3)
				await SaveMp3(response, downloadItem.Name);
			else if (type == ContentType.html)
				LoadSongsInfo(response);
			
			if(downloadItem.CompletedAction != null)
				downloadItem.CompletedAction();

			if (!_downloadQueue.Any())
				await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => Progress.Visibility = Visibility.Collapsed);
			else
			{
				var next = _downloadQueue.Peek();
				Downloader.Download(type == ContentType.mp3 ? _mp3Host : _searchHost, next.Path, next.ContentType);
			}
		}


		private void SearchButtonClick(object sender, RoutedEventArgs e)
		{
			var path = "/search.html?query_search=" + MusicInput.Text.Replace(' ', '+');
			var downloadItem = new DownloadItem() { ContentType = ContentType.html, Name = "search.html", Path = path };
			_downloadQueue.Enqueue(downloadItem);
			if (_downloadQueue.Count > 1)
				return;
			Downloader.Download("zaycev.net", path, ContentType.html);
			Progress.Visibility = Visibility.Visible;
		}
		
		private void DownloadSongButtonClick(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var song = (button.DataContext as SongContainer);
			button.IsEnabled = false;

			Action completedAction = async () => await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => button.IsEnabled = true);
			var downloadItem = new DownloadItem() { Name = song.FullName, ContentType = ContentType.mp3, Path = song.Link, CompletedAction = completedAction };

			_downloadQueue.Enqueue(downloadItem);
			if (_downloadQueue.Count > 1)
				return;
			Downloader.Download("cdndl.zaycev.net", song.Link, ContentType.mp3);
			Progress.Visibility = Visibility.Visible;
		}


		private async void LoadSongsInfo(byte[] bytes)
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				if (bytes == null)
					return;

				_songs.Clear();
				var html = Encoding.UTF8.GetString(bytes);

				ResourceLoader loader = new ResourceLoader("Regexs");
				var pattern = loader.GetString("zaycevRegex");
				var matches = Regex.Matches(html, pattern, RegexOptions.Singleline);

				foreach (Match match in matches)
				{
					var link = match.Groups[1].Value;
					var mp3 = link.IndexOf(".mp3");
					var downloadLink = link.Insert(mp3, ("/" + match.Groups[2].Value + "_-_" + match.Groups[3].Value).ToLower().Replace(' ', '_').Replace('\'', '_'));
					var container = new SongContainer() { Name = match.Groups[3].Value, Artist = match.Groups[2].Value, Link = downloadLink };
					_songs.Add(container);
				}
			});
		}

		private async Task SaveMp3(byte[] bytes, string name)
		{
			if(bytes == null)
			{
				await ShowToastNotification("Could not download file " + name, "Fail");
				return;
			}

			var storageFile = await KnownFolders.MusicLibrary.CreateFileAsync(name + ".mp3", CreationCollisionOption.ReplaceExisting);
			using (var stream = await storageFile.OpenStreamForWriteAsync())
			{
				await stream.WriteAsync(bytes, 0, bytes.Length);
				await stream.FlushAsync();
			}
			await ShowToastNotification(name + " saved in Music Folder", "Download Completed");
		}

		private async Task ShowToastNotification(string text, string status)
		{
			await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
						() =>
						{
							var xmlToastTemplate = "<toast launch=\"app-defined-string\">" +
									"<visual>" +
									"<binding template =\"ToastGeneric\">" +
										"<text>" + 
											status +
										"</text>" +
										"<text>" +
											text +
										"</text>" +
									"</binding>" +
									"</visual>" +
								"</toast>";

							// load the template as XML document
							var xmlDocument = new XmlDocument();
							xmlDocument.LoadXml(xmlToastTemplate);

							// create the toast notification and show to user
							var toastNotification = new ToastNotification(xmlDocument);

							_toastNotifier.Show(toastNotification);
						});
		}

	}
}
