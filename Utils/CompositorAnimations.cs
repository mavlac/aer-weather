using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Numerics;

namespace Aer.Utils
{
	internal static class CompositorAnimations
	{
		public static void AnimateFadeIn(FrameworkElement element, double duration)
		{
			var visual = ElementCompositionPreview.GetElementVisual(element);
			var compositor = visual.Compositor;

			// Start fully transparent
			visual.Opacity = 0f;

			var animation = compositor.CreateScalarKeyFrameAnimation();
			animation.InsertKeyFrame(0f, 0f);
			animation.InsertKeyFrame(1f, 1f);
			animation.Duration = TimeSpan.FromSeconds(duration);

			visual.StartAnimation(nameof(visual.Opacity), animation);
		}

		public static void AnimatePop(FrameworkElement element, double duration)
		{
			var visual = ElementCompositionPreview.GetElementVisual(element);
			var compositor = visual.Compositor;

			var animation = compositor.CreateVector3KeyFrameAnimation();
			animation.InsertKeyFrame(0f, new Vector3(1f));
			animation.InsertKeyFrame(0.2f, new Vector3(1.05f));
			animation.InsertKeyFrame(1f, new Vector3(1f));
			animation.Duration = TimeSpan.FromSeconds(duration);

			visual.CenterPoint = new Vector3((float)element.ActualWidth / 2, (float)element.ActualHeight / 2, 0);
			visual.StartAnimation(nameof(visual.Scale), animation);
		}
	}
}
