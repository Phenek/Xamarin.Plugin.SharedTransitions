using System;
using CoreGraphics;
using UIKit;

namespace Plugin.SharedTransitions.iOS.Extentions
{
    internal static class ViewExtensions
    {
        internal static CGRect GetImageFrameWithAspectRatio(this UIImageView imageView)
        {
            /*
            nfloat aspect = imageView.Image.Size.Width / imageView.Image.Size.Height;

            //calculate new dimensions based on aspect ratio
            nfloat newWidth   = imageView.Frame.Width * aspect;
            nfloat newHeight  = newWidth / aspect;
            nfloat marginBottom  = 0;
            nfloat marginRight = 0;

            if (newWidth > imageView.Frame.Width || newHeight > imageView.Frame.Height)
            {
                //depending on which of the two exceeds the box dimensions set it as the box dimension
                //and calculate the other one based on the aspect ratio
                if (newWidth > newHeight)
                {
                    newWidth  = imageView.Frame.Width;
                    newHeight = newWidth / aspect;
                    marginBottom = (imageView.Frame.Height - newHeight);
                }
                else
                {
                    newHeight  = imageView.Frame.Height;
                    newWidth   = (int)(newHeight * aspect);
                    marginRight = (imageView.Frame.Width - newWidth);
                }
            }
            return new CGRect(imageView.Frame.X - marginRight, imageView.Frame.Y, newWidth, newHeight + marginBottom);
            */


            return new CGRect(imageView.Frame.X, imageView.Frame.Y, imageView.Frame.Width, imageView.Frame.Height);
        }
    }
}