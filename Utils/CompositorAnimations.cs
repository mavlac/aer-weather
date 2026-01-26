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

		public static void AnimatePop(FrameworkElement element, float scale, double duration)
		{
			// Ensure layout exists and element is visible before animating
			if (element.Visibility == Visibility.Visible &&
				element.ActualWidth > 0 &&
				element.ActualHeight > 0)
			{
				StartAnimation();
			}
			else
			{
				void OnLayoutUpdated(object? s, object? e)
				{
					if (element.Visibility == Visibility.Visible &&
						element.ActualWidth > 0 &&
						element.ActualHeight > 0)
					{
						element.LayoutUpdated -= OnLayoutUpdated;
						StartAnimation();
					}
				}
				
				element.LayoutUpdated += OnLayoutUpdated;
			}

			void StartAnimation()
			{
				var visual = ElementCompositionPreview.GetElementVisual(element);
				var compositor = visual.Compositor;
				
				visual.CenterPoint = new Vector3(
					(float)element.ActualWidth / 2f,
					(float)element.ActualHeight / 2f,
					0f);
				
				var animation = compositor.CreateVector3KeyFrameAnimation();
				animation.InsertKeyFrame(0f, new Vector3(scale));
				animation.InsertKeyFrame(1f, new Vector3(1f));
				animation.Duration = TimeSpan.FromSeconds(duration);
				
				visual.StartAnimation(nameof(visual.Scale), animation);
			}
		}
	}
}
