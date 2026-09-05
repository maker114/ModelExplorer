using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ModelExplorer
{
    public static class UiAnimation
    {
        public static void FadeInOnOpen(Window window)
        {
            window.Loaded += delegate { FadeInWindow(window); };
        }

        public static void FadeInWindow(Window window)
        {
            FrameworkElement content = window.Content as FrameworkElement;
            if (content == null)
            {
                return;
            }

            ScaleTransform scale = content.RenderTransform as ScaleTransform;
            if (scale == null)
            {
                scale = new ScaleTransform(1, 1);
                content.RenderTransformOrigin = new Point(0.5, 0.5);
                content.RenderTransform = scale;
            }
            else
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }

            DoubleAnimation opacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(170));
            opacity.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            content.BeginAnimation(UIElement.OpacityProperty, opacity);

            DoubleAnimation scaleX = new DoubleAnimation(0.97, 1, TimeSpan.FromMilliseconds(190));
            scaleX.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            DoubleAnimation scaleY = new DoubleAnimation(0.97, 1, TimeSpan.FromMilliseconds(190));
            scaleY.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        public static void Flash(UIElement element)
        {
            if (element == null)
            {
                return;
            }

            element.BeginAnimation(UIElement.OpacityProperty, null);
            DoubleAnimation opacity = new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(220));
            opacity.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            element.BeginAnimation(UIElement.OpacityProperty, opacity);
        }

        public static void Refresh(UIElement element)
        {
            if (element == null)
            {
                return;
            }

            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.BeginAnimation(UIElement.RenderTransformProperty, null);
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            DoubleAnimation opacity = new DoubleAnimation(0.45, 1, TimeSpan.FromMilliseconds(170));
            opacity.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            element.BeginAnimation(UIElement.OpacityProperty, opacity);

            TranslateTransform translate = new TranslateTransform(0, 8);
            element.RenderTransform = translate;
            DoubleAnimation y = new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(210));
            y.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            translate.BeginAnimation(TranslateTransform.YProperty, y);
        }

        public static void Pulse(UIElement element, double magnitude = 0.04)
        {
            if (element == null)
            {
                return;
            }

            ScaleTransform scale = element.RenderTransform as ScaleTransform;
            if (scale == null)
            {
                element.RenderTransformOrigin = new Point(0.5, 0.5);
                scale = new ScaleTransform(1, 1);
                element.RenderTransform = scale;
            }
            else
            {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }

            DoubleAnimationUsingKeyFrames scaleX = ScalePulse(magnitude);
            DoubleAnimationUsingKeyFrames scaleY = ScalePulse(magnitude);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        private static DoubleAnimationUsingKeyFrames ScalePulse(double magnitude)
        {
            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(1, TimeSpan.Zero));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(
                1 + magnitude,
                TimeSpan.FromMilliseconds(90),
                new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(
                1,
                TimeSpan.FromMilliseconds(230),
                new QuadraticEase { EasingMode = EasingMode.EaseOut }));
            return animation;
        }
    }
}
