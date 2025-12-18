using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Aer.Data
{
	public static class AppStorage
	{
		private const string AppDataFileName = "appdata.json";
		
		private static readonly string _filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, AppDataFileName);
		
		private static readonly Dictionary<string, JsonElement> _store = new();

		static AppStorage()
		{
			_store =
				File.Exists(_filePath)
					? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(_filePath)) ?? new()
					: new();
		}

		/// <summary>
		/// Saves the value under the given key in the persistent store.
		/// </summary>
		/// <remarks>
		/// This method does not automatically write changes to storage.
		/// To ensure that all saved data is written, call the Flush after all SaveValue operations.
		/// This way SaveValue calls can be batched together before flushing, to improve performance.
		/// </remarks>
		public static void SaveValue<T>(string key, T value)
		{
			_store[key] = JsonSerializer.SerializeToElement(value);
		}

		public static void Flush()
		{
			File.WriteAllText(_filePath, JsonSerializer.Serialize(_store));
		}

		public static bool TryGetValue<T>(string key, out T? deserializedValue)
		{
			if (_store.TryGetValue(key, out var value))
			{
				try
				{
					deserializedValue = value.Deserialize<T>();
					return true;
				}
				catch
				{
					Debug.WriteLine($"Error trying to deserialize the saved value of key '{key}'");
					deserializedValue = default;
					return false;
				}
			}

			deserializedValue = default;
			return false;
		}

		public static void Delete()
		{
			if (File.Exists(_filePath))
			{
				File.Delete(_filePath);
			}
			_store.Clear();
		}

		/// <summary>
		/// Opens the local folder where the app data is stored in File Explorer.
		/// </summary>
		public static void OpenLocalFolder()
		{
			var folderPath = ApplicationData.Current.LocalFolder.Path;
			Debug.WriteLine($"Opening LocalFolder: {folderPath}");
			Process.Start(new ProcessStartInfo
			{
				FileName = folderPath,
				UseShellExecute = true
			});
		}
	}
}