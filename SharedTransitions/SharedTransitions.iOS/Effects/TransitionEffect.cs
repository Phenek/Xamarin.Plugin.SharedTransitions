using System;
using System.ComponentModel;
using Plugin.SharedTransitions;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ResolutionGroupName(Transition.ResolutionGroupName)]
[assembly: ExportEffect(typeof(Plugin.SharedTransitions.iOS.Effects.TransitionEffect), Transition.EffectName)]

namespace Plugin.SharedTransitions.iOS.Effects
{
    public class TransitionEffect : PlatformEffect
    {

        protected override void OnAttached()
        {
            if (Control == null)
                return;

            UpdateTag();
        }

        protected override void OnDetached()
        {
        }

        protected override void OnElementPropertyChanged(PropertyChangedEventArgs args)
        {
            Console.WriteLine(args.PropertyName + "\n");

            if (args.PropertyName == Transition.TagProperty.PropertyName ||
                args.PropertyName == Transition.TagGroupProperty.PropertyName)
                UpdateTag();
                
            base.OnElementPropertyChanged(args);
        }

        void UpdateTag()
        {
            if (Element is View element && Control != null)
                Control.Tag = Transition.RegisterTagInStack(element);
        }
    }
}
