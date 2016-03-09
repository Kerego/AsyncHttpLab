using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncHttpLab.Models
{
	public class SongContainer
	{
		public string Name { get; set; }
		public string Artist { get; set; }
		public string FullName => Artist + "-" + Name;

		public string Link { get; set; }
	}
}
