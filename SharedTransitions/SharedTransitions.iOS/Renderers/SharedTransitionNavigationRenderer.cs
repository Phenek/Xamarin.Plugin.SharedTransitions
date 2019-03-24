using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Plugin.SharedTransitions;
using Plugin.SharedTransitions.iOS.Extentions;
using Plugin.SharedTransitions.iOS.Renderers;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly:ExportRenderer(typeof(SharedTransitionNavigationPage), typeof(SharedTransitionNavigationRenderer))]

namespace Plugin.SharedTransitions.iOS.Renderers
{
    /// <summary>
    /// Platform Renderer for the NavigationPage responsible to manage the Shared Transitions
    /// </summary>
    [Preserve(AllMembers = true)]
    public class SharedTransitionNavigationRenderer : NavigationRenderer, IUINavigationControllerDelegate, IUIGestureRecognizerDelegate
    {
        int _selectedGroup => SharedTransitionNavigationPage.GetSelectedTagGroup(PropertiesContainer);
        public double SharedTransitionDuration => (double)SharedTransitionNavigationPage.GetSharedTransitionDuration(PropertiesContainer) / 1000;
        public BackgroundAnimation BackgroundAnimation => SharedTransitionNavigationPage.GetBackgroundAnimation(PropertiesContainer);
        public UIScreenEdgePanGestureRecognizer InteractiveTransitionRecognizer = new UIScreenEdgePanGestureRecognizer();

        Page _propertiesContainer;
        public Page PropertiesContainer
        {
            get => _propertiesContainer;
            set
            {
                if (_propertiesContainer == value)
                    return;
                _propertiesContainer = value;
            }
        }

        UIPercentDrivenInteractiveTransition _percentDrivenInteractiveTransition;
        SharedTransitionNavigationPage NavPage => Element as SharedTransitionNavigationPage;
        bool _popToRoot;

        public SharedTransitionNavigationRenderer() : base()
        {
            Delegate = this;
        }

        [Export("navigationController:animationControllerForOperation:fromViewController:toViewController:")]
        public IUIViewControllerAnimatedTransitioning GetAnimationControllerForOperation(UINavigationController navigationController, UINavigationControllerOperation operation, UIViewController fromViewController, UIViewController toViewController)
        {
            if (!_popToRoot)
            {
                //At this point the property TargetPage refers to the view we are pushing or popping
                //This view is not yet visible in our app but the variable is already set
                var viewsToAnimate = new List<(UIView ToView, UIView FromView)>();

                //this is due the overridden management of pop and push
                var destinationPage = operation == UINavigationControllerOperation.Push
                    ? NavPage.CurrentPage
                    : PropertiesContainer;


                if (viewsToAnimate.Any() && !View.GestureRecognizers.Contains(InteractiveTransitionRecognizer))
                    View.AddGestureRecognizer(InteractiveTransitionRecognizer);
                else if (!viewsToAnimate.Any() && View.GestureRecognizers.Contains(InteractiveTransitionRecognizer))
                    View.RemoveGestureRecognizer(InteractiveTransitionRecognizer);

                //Get all the views with tags in the destination page
                //With this, we are sure to dont start transitions with no mathing tags in destination
                //When popping, take only the tags with the selected group (if any).
                //This is to avoid to search al the views in a listview (if any)
                var mapStack = NavPage.TagMap.GetMap(destinationPage, operation == UINavigationControllerOperation.Pop ? _selectedGroup : 0);
                
                foreach (var tagMap in mapStack)
                {
                    var toView = toViewController.View.ViewWithTag(tagMap.Tag);
                    if (toView != null)
                    {
                        //get the matching tag, taking in consideration the GroupTags for dynamic transitions (listview <--> details)
                        //we store the destination view and the corrispondent tag for the source view, so we can match them during transition
                        var correspondingTag = Plugin.SharedTransitions.Transition.GetUniqueTag((int) toView.Tag, _selectedGroup, operation == UINavigationControllerOperation.Pop);
                        var fromView = fromViewController.View.ViewWithTag(correspondingTag);

                        if (fromView != null)
                            viewsToAnimate.Add((toView, fromView));
                    }
                }

                //No view to animate = standard push & pop
                if (viewsToAnimate.Any())
                    return new NavigationTransition(viewsToAnimate, operation, this);
            }

            return null;
        }

        [Export("navigationController:interactionControllerForAnimationController:")]
        public IUIViewControllerInteractiveTransitioning GetInteractionControllerForAnimationController(UINavigationController navigationController, IUIViewControllerAnimatedTransitioning animationController)
        {
            return _percentDrivenInteractiveTransition;
        }

        //During PopToRoot we skip everything and make the default animation
        protected override async Task<bool> OnPopToRoot(Page page, bool animated)
        {
            _popToRoot = true;
            var result = await base.OnPopToRoot(page, true);
            _popToRoot = false;

            return result;
        }

        public override UIViewController PopViewController(bool animated)
        {
            //We need to take the transition configuration from the destination page
            //At this point the pop is not started so we need to go back in the stack
            var pageCount = Element.Navigation.NavigationStack.Count;
            if (pageCount > 1)
                PropertiesContainer = Element.Navigation.NavigationStack[pageCount - 2];

            return base.PopViewController(animated); ;
        }
        
        public override void PushViewController(UIViewController viewController, bool animated)
        {
            //We need to take the transition configuration from the page we are leaving page
            //At this point the current page in the navigation stack is already set with the page we are pusing
            //So we need to go back in the stack to retrieve what we want
            var pageCount = Element.Navigation.NavigationStack.Count;
            PropertiesContainer = pageCount > 1 
                ? Element.Navigation.NavigationStack[pageCount - 2] 
                : NavPage.CurrentPage;

            base.PushViewController(viewController, animated);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            //Add PanGesture on left edge to POP page
            InteractiveTransitionRecognizer.AddTarget(() => InteractiveTransitionRecognizerAction(InteractiveTransitionRecognizer));
            InteractiveTransitionRecognizer.Edges = UIRectEdge.Left;
            View.AddGestureRecognizer(InteractiveTransitionRecognizer);
        }

        public void InteractiveTransitionRecognizerAction(UIScreenEdgePanGestureRecognizer sender)
        {
            var percent = sender.TranslationInView(sender.View).X / sender.View.Frame.Width;

            switch (sender.State)
            {
                case UIGestureRecognizerState.Began:
                    _percentDrivenInteractiveTransition = new UIPercentDrivenInteractiveTransition();
                    PopViewController(true);
                    break;

                case UIGestureRecognizerState.Changed:
                    _percentDrivenInteractiveTransition.UpdateInteractiveTransition(percent);
                    break;

                case UIGestureRecognizerState.Cancelled:
                case UIGestureRecognizerState.Failed:
                    _percentDrivenInteractiveTransition.CancelInteractiveTransition();
                    _percentDrivenInteractiveTransition = null;
                    break;

                case UIGestureRecognizerState.Ended:
                    if (percent > 0.5 || sender.VelocityInView(sender.View).X > 300)
                        _percentDrivenInteractiveTransition.FinishInteractiveTransition();
                    else
                        _percentDrivenInteractiveTransition.CancelInteractiveTransition();

                    _percentDrivenInteractiveTransition = null;
                    break;
            }
        }
    }
}
