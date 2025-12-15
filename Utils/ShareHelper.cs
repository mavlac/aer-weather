using Microsoft.UI.Xaml;
using System;

namespace Aer.Utils
{
	/// <summary>
	/// https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/winui3#datatransfermanager
	/// https://learn.microsoft.com/en-us/windows/apps/develop/ui/display-ui-objects
	/// https://learn.microsoft.com/en-us/windows/apps/develop/ui/display-ui-objects#for-classes-that-implement-idatatransfermanagerinterop
	/// </summary>
	public static class ShareHelper
	{
		[System.Runtime.InteropServices.ComImport]
		[System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
		[System.Runtime.InteropServices.InterfaceType(
			System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]

		interface IDataTransferManagerInterop
		{
			IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow,
				[System.Runtime.InteropServices.In] ref Guid riid);
			void ShowShareUIForWindow(IntPtr appWindow);
		}

		static readonly Guid _dtm_iid =
			new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

		public static void ShowShare(
			Window window,
			string title,
			string description,
			Uri link)
		{
			// Retrieve the window handle (HWND) of the calling WinUI 3 window.
			var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

			IDataTransferManagerInterop interop =
			Windows.ApplicationModel.DataTransfer.DataTransferManager.As
				<IDataTransferManagerInterop>();

			IntPtr result = interop.GetForWindow(hWnd, _dtm_iid);
			var dataTransferManager = WinRT.MarshalInterface
				<Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(result);

			void Handler(
				Windows.ApplicationModel.DataTransfer.DataTransferManager s,
				Windows.ApplicationModel.DataTransfer.DataRequestedEventArgs args)
			{
				s.DataRequested -= Handler;

				var request = args.Request;
				request.Data.Properties.Title = title;
				request.Data.Properties.Description = description;
				request.Data.SetWebLink(link);
			}

			dataTransferManager.DataRequested += Handler;

			// Show the Share UI
			interop.ShowShareUIForWindow(hWnd);
		}
	}
}
