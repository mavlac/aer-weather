using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Aer.Utils
{
	internal class FrameworkUtils
	{
		public static T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
		{
			int count = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(parent, i);

				if (child is T fe && fe.Name == name)
					return fe;

				var result = FindChildByName<T>(child, name);
				if (result != null)
					return result;
			}
			return null;
		}
	}
}
