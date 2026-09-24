using System;
using System.Collections.Generic;
using System.Globalization;
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
		public const string RecentLocationArgumentsPrefix = "/recentLocationIndex/";

		internal static async Task UpdateAsync(List<Location> recentLocations)
		{
			var jumpList = await JumpList.LoadCurrentAsync();

			jumpList.Items.Clear();

			for (int i = 0; i < recentLocations.Count; i++)
			{
				var recentLocation = recentLocations[i];

				// Creates the arguments, in format "/recentLocationIndex/{index}"
				string arguments = $"{RecentLocationArgumentsPrefix}{i}";

				var jumpListItem = JumpListItem.CreateWithArguments(arguments, recentLocation.Name);
				jumpListItem.Description = $"{recentLocation.Label} ({recentLocation.ReadableCoordinates})";
				jumpListItem.Logo = new Uri("ms-appx:///Assets/JumpListMapPin.png");
				jumpList.Items.Add(jumpListItem);
			}

			await jumpList.SaveAsync();
		}
	}
}
