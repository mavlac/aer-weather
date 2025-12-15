using Microsoft.UI.Xaml;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace Aer.Utils
{
	public static class ShareHelper
	{
		public static void ShowShare(
			Window window,
			string title,
			string description,
			Uri link)
		{
			// TODO Interop
			// https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/winui3

			var dataTransferManager = DataTransferManager.GetForCurrentView();

			void Handler(DataTransferManager s, DataRequestedEventArgs args)
			{
				s.DataRequested -= Handler;

				var request = args.Request;
				request.Data.Properties.Title = title;
				request.Data.Properties.Description = description;

				request.Data.SetWebLink(link);
			}

			dataTransferManager.DataRequested += Handler;

			DataTransferManager.ShowShareUI();
		}
	}
}
