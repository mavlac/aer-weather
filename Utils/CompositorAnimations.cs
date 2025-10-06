using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Numerics;

namespace Aer.Utils
{
	internal static class CompositorAnimations
	{
		public static void AnimatePop(FrameworkElement element)
		{
			var visual = ElementCompositionPreview.GetElementVisual(element);
			var compositor = visual.Compositor;

			var animation = compositor.CreateVector3KeyFrameAnimation();
			animation.InsertKeyFrame(0f, new Vector3(1f));
			animation.InsertKeyFrame(0.2f, new Vector3(1.05f));
			animation.InsertKeyFrame(1f, new Vector3(1f));
			animation.Duration = TimeSpan.FromSeconds(0.4);

			visual.CenterPoint = new Vector3((float)element.ActualWidth / 2, (float)element.ActualHeight / 2, 0);
			visual.StartAnimation("Scale", animation);
		}
	}
}
