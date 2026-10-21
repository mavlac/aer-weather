using System.Text;

namespace Aer.Utils.Extensions
{
	internal static class StringExtensions
	{
		public static string ToASCII(this string source, bool removeSpaces)
		{
			if (string.IsNullOrEmpty(source))
				return string.Empty;
			
			// Decompose accented characters into their base character and diacritic
			var normalizedString = source.Normalize(NormalizationForm.FormD);
			
			var stringBuilder = new StringBuilder();
			foreach (var c in normalizedString)
			{
				// Get the Unicode category of the current character
				var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
				
				// Add, if not a non-spacing mark (modification of the base character), to the result
				if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
				{
					stringBuilder.Append(c);
				}
			}
			
			// Recompose the resulting characters into their standard Unicode representation
			var asciiString = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
			
			// Optionally remove literal space characters from the result
			if (removeSpaces)
			{
				asciiString = asciiString.Replace(" ", "");
			}
			
			return asciiString;
		}
	}
}
