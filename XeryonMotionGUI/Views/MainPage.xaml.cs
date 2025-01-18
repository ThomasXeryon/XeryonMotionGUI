using Microsoft.UI.Xaml;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;

using XeryonMotionGUI.ViewModels;
using Microsoft.UI.Xaml.Media.Animation;

namespace XeryonMotionGUI.Views;

public sealed partial class MainPage : Page
{
    private double SpeedMultiplier = 0.5; // Default speed multiplier
    private const double MaxSpeedMultiplier = 10.0; // Maximum multiplier
    private const double SpeedIncrement = 0.5; // Increment per click

    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        AnimateRodAndScrews();
    }

    private void ActuatorCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimateRodAndScrews();
    }

    private void AnimateRodAndScrews()
    {
        var maxStroke = 50; // Define the stroke range

        // Start animations sequentially with the current SpeedMultiplier
        StartQuarticEaseAnimation(maxStroke, () =>
            StartSineEaseAnimation(maxStroke, () =>
                StartElasticEaseAnimation(maxStroke, () =>
                    StartFullStrokeScan(maxStroke, () =>
                        StartHalfStrokeMovement(maxStroke, () =>
                            StartTurnAround(maxStroke, () =>
                                StartCircleEaseAnimation(maxStroke, () =>
                                    StartBounceEaseAnimation(maxStroke, () =>
                                        StartBackEaseAnimation(maxStroke, () =>
                                            StartBiggerStepsAnimation(maxStroke, null))))))))));
    }
    private void StartHalfStrokeMovement(double maxStroke, Action onComplete)
    {
        var halfStrokeAnimation = new DoubleAnimation
        {
            From = 0,
            To = maxStroke / 2,
            Duration = new Duration(TimeSpan.FromSeconds(2 / SpeedMultiplier)),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        var storyboard = CreateStoryboard(halfStrokeAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Full-stroke scan
    private void StartFullStrokeScan(double maxStroke, Action onComplete)
    {
        var fullStrokeAnimation = new DoubleAnimation
        {
            From = 0,
            To = maxStroke,
            Duration = new Duration(TimeSpan.FromSeconds(3 / SpeedMultiplier)),
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        var storyboard = CreateStoryboard(fullStrokeAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Turn around
    private void StartTurnAround(double maxStroke, Action onComplete)
    {
        var turnAroundAnimation = new DoubleAnimation
        {
            From = maxStroke / 2,
            To = 0,
            Duration = new Duration(TimeSpan.FromSeconds(2.5 / SpeedMultiplier)),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
        };
        var storyboard = CreateStoryboard(turnAroundAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Quartic ease animation
    private void StartQuarticEaseAnimation(double maxStroke, Action onComplete)
    {
        var quarticEaseAnimation = new DoubleAnimation
        {
            From = 0,
            To = maxStroke / 8,
            Duration = new Duration(TimeSpan.FromMilliseconds(300 / SpeedMultiplier)),
            EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseInOut }
        };
        var storyboard = CreateStoryboard(quarticEaseAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Sine ease animation
    private void StartSineEaseAnimation(double maxStroke, Action onComplete)
    {
        var sineEaseAnimation = new DoubleAnimation
        {
            From = maxStroke / 8,
            To = maxStroke / 2,
            Duration = new Duration(TimeSpan.FromMilliseconds(500 / SpeedMultiplier)),
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        var storyboard = CreateStoryboard(sineEaseAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Elastic ease animation
    private void StartElasticEaseAnimation(double maxStroke, Action onComplete)
    {
        var elasticEaseAnimation = new DoubleAnimation
        {
            From = maxStroke / 2,
            To = maxStroke,
            Duration = new Duration(TimeSpan.FromMilliseconds(700 / SpeedMultiplier)),
            EasingFunction = new ElasticEase { Oscillations = 2, Springiness = 3, EasingMode = EasingMode.EaseOut }
        };
        var storyboard = CreateStoryboard(elasticEaseAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Circle ease animation
    private void StartCircleEaseAnimation(double maxStroke, Action onComplete)
    {
        var circleEaseAnimation = new DoubleAnimation
        {
            From = maxStroke,
            To = maxStroke / 6,
            Duration = new Duration(TimeSpan.FromMilliseconds(600 / SpeedMultiplier)),
            EasingFunction = new CircleEase { EasingMode = EasingMode.EaseIn }
        };
        var storyboard = CreateStoryboard(circleEaseAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Bounce ease animation
    private void StartBounceEaseAnimation(double maxStroke, Action onComplete)
    {
        var bounceEaseAnimation = new DoubleAnimation
        {
            From = maxStroke / 6,
            To = maxStroke / 3,
            Duration = new Duration(TimeSpan.FromMilliseconds(400 / SpeedMultiplier)),
            EasingFunction = new BounceEase { Bounces = 3, Bounciness = 2 }
        };
        var storyboard = CreateStoryboard(bounceEaseAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Back ease animation
    private void StartBackEaseAnimation(double maxStroke, Action onComplete)
    {
        var backEaseAnimation = new DoubleAnimation
        {
            From = maxStroke / 3,
            To = maxStroke,
            Duration = new Duration(TimeSpan.FromMilliseconds(500 / SpeedMultiplier)),
            EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseInOut }
        };
        var storyboard = CreateStoryboard(backEaseAnimation, RodTranslation, "X");
        storyboard.Completed += (s, e) => onComplete?.Invoke();
        storyboard.Begin();
    }

    // Bigger steps animation
    private void StartBiggerStepsAnimation(double maxStroke, Action onComplete)
    {
        var biggerStepsAnimation = new DoubleAnimationUsingKeyFrames
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(2000 / SpeedMultiplier))
        };

        for (int i = 0; i <= 6; i++)
        {
            biggerStepsAnimation.KeyFrames.Add(new DiscreteDoubleKeyFrame
            {
                Value = (maxStroke / 6) * i,
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300 * i / SpeedMultiplier))
            });
        }

        var storyboard = CreateStoryboard(biggerStepsAnimation, RodTranslation, "X");

        // Loop back to the first animation
        storyboard.Completed += (s, e) => AnimateRodAndScrews();

        storyboard.Begin();
    }

    // Helper to create storyboard
    private Storyboard CreateStoryboard(Timeline animation, DependencyObject target, string property)
    {
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        storyboard.Children.Add(animation);
        return storyboard;
    }

    private void Canvas_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SpeedMultiplier = 1.5;
        var growStoryboard = new Storyboard();

        // Scale X Animation
        var scaleXAnimation = new DoubleAnimation
        {
            To = 1.2,
            Duration = new Duration(TimeSpan.FromSeconds(0.2))
        };
        Storyboard.SetTarget(scaleXAnimation, ActuatorCanvas);
        Storyboard.SetTargetProperty(scaleXAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");

        // Scale Y Animation
        var scaleYAnimation = new DoubleAnimation
        {
            To = 1.2,
            Duration = new Duration(TimeSpan.FromSeconds(0.2))
        };
        Storyboard.SetTarget(scaleYAnimation, ActuatorCanvas);
        Storyboard.SetTargetProperty(scaleYAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

        growStoryboard.Children.Add(scaleXAnimation);
        growStoryboard.Children.Add(scaleYAnimation);

        growStoryboard.Begin();
    }

    private void Canvas_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SpeedMultiplier = 0.5;
        var shrinkStoryboard = new Storyboard();

        // Scale X Animation
        var scaleXAnimation = new DoubleAnimation
        {
            To = 1,
            Duration = new Duration(TimeSpan.FromSeconds(0.2))
        };
        Storyboard.SetTarget(scaleXAnimation, ActuatorCanvas);
        Storyboard.SetTargetProperty(scaleXAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");

        // Scale Y Animation
        var scaleYAnimation = new DoubleAnimation
        {
            To = 1,
            Duration = new Duration(TimeSpan.FromSeconds(0.2))
        };
        Storyboard.SetTarget(scaleYAnimation, ActuatorCanvas);
        Storyboard.SetTargetProperty(scaleYAnimation, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

        shrinkStoryboard.Children.Add(scaleXAnimation);
        shrinkStoryboard.Children.Add(scaleYAnimation);

        shrinkStoryboard.Begin();
    }

    private void OpenDeviceManager_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "devmgmt.msc",
                UseShellExecute = true
            });

        }
        catch (Exception ex)
        {

        }
    }
}
