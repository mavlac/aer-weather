using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using static Aer.Data.LocationManager;

namespace Aer.Utils
{
	/// <summary>
	/// App Icon right-click menu (JumpList) manager.
	/// </summary>
	internal class JumpListManager
	{
		private const string ArgumentsFormat = "/locationId/{0},{1}"; // lat, long

		internal static async Task UpdateAsync(List<Location> recentLocations)
		{
			var jumpList = await JumpList.LoadCurrentAsync();

			jumpList.Items.Clear();

			recentLocations.Reverse();
			foreach (var recentLocation in recentLocations)
			{
				var jumpListItem =
					JumpListItem.CreateWithArguments(
						string.Format(ArgumentsFormat, recentLocation.Latitude, recentLocation.Longitude),
						recentLocation.Label);
				jumpListItem.Description = recentLocation.ReadableCoordinates;
				jumpListItem.Logo = new Uri("ms-appx:///Assets/JumpListMapPin-48.png");
				jumpList.Items.Add(jumpListItem);
			}

			await jumpList.SaveAsync();
		}
	}
}
