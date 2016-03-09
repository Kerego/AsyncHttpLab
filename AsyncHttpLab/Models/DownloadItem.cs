using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncHttpLab.Models
{
	public class DownloadItem
	{
		public string Name { get; set; }
		public ContentType ContentType { get; set; }
		public string Path { get; set; }
		public Action CompletedAction { get; set; }
	}
}
