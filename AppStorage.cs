using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Windows.Storage;

namespace Aer
{
	public static class AppStorage
	{
		private const string AppDataFileName = "appdata.json";

		private static readonly string _filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, AppDataFileName);
		private static readonly Dictionary<string, JsonElement> _store = new();

		static AppStorage()
		{
			_store = File.Exists(_filePath)
				? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(_filePath)) ?? new()
				: new();
		}

		public static void Save<T>(string key, T value)
		{
			_store[key] = JsonSerializer.SerializeToElement(value);
			
			// Flush needs to be called after all data had been saved.
			// Is not automatically called, so that multiple Save calls can be batched together
		}

		public static bool TryLoad<T>(string key, out T? deserializedValue)
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

		public static void Flush()
		{
			File.WriteAllText(_filePath, JsonSerializer.Serialize(_store));
		}

		public static void Delete()
		{
			if (File.Exists(_filePath))
			{
				File.Delete(_filePath);
			}
			_store.Clear();
		}
	}
}