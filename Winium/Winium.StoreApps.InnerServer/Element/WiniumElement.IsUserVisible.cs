﻿namespace Winium.StoreApps.InnerServer.Element
{
    #region

    using Windows.Foundation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Controls;

    #endregion

    internal partial class WiniumElement
    {
        #region Methods

        internal bool IsUserVisible()
        {
            /* TODO: There is IsOffscreen method on AutomationPeer that can be used.
             * But it returns true even if currentElement is on another pivot (i.e. invisible)
             * var peer = FrameworkElementAutomationPeer.FromElement(this.Element);
             * return peer != null && peer.IsOffscreen();
             * This might improve speed but will make all non user interactable elements, like border, scrollbiew "invisible"
             */
            var visualRoot = WiniumVirtualRoot.Current.VisualRoot.Element;
            var currentElement = this.Element;

            var currentElementSize = new Size(currentElement.ActualWidth, currentElement.ActualHeight);

            if (currentElement is AppBarButton)
                return CheckAppBarButton(currentElement as AppBarButton);

            if (currentElement is CommandBar)
                return CheckCommandBar(currentElement as CommandBar);

            // Check if currentElement is of zero size
            if (currentElementSize.Width <= 0 || currentElementSize.Height <= 0)
            {
                return false;
            }

            var zero = new Point(0, 0);

            var rect = new Rect(zero, currentElementSize);
            var bound = currentElement.TransformToVisual(visualRoot).TransformBounds(rect);
            var rootRect = new Rect(zero, visualRoot.RenderSize);
            rootRect.Intersect(bound);

            // Check if currentElement is offscreen. Save time on traversing tree if currentElement is not in root rect
            if (rootRect.IsEmpty)
            {
                return false;
            }

            while (true)
            {
                if (currentElement.Visibility != Visibility.Visible || !(currentElement.Opacity > 0))
                {
                    return false;
                }

                var container = VisualTreeHelper.GetParent(currentElement) as FrameworkElement;
                if (container == null)
                {
                    return true;
                }

                if (container.RenderSize.IsEmpty)
                {
                    // There are some currentElements in UI tree that always return zero size, e.g. ContentControl, etc.
                    continue;
                }

                // FIXME we only check if currentElement is visible in parent and parent visible in his parent and so on
                // we do not actully check if any part of original currentElement is visible in grandparents
                currentElementSize = new Size(currentElement.ActualWidth, currentElement.ActualHeight);
                rect = new Rect(zero, currentElementSize);
                bound = currentElement.TransformToVisual(container).TransformBounds(rect);
                var containerRect = new Rect(zero, container.RenderSize);

                containerRect.Intersect(bound);

                // Check if currentElement is offscreen
                if (containerRect.IsEmpty)
                {
                    return false;
                }

                currentElement = container;
            }
        }

        private CommandBar FindCommandBar(AppBarButton element)
        {
            DependencyObject parent = element;

            while ( parent != null  )
            {
                parent = VisualTreeHelper.GetParent(parent);

                FrameworkElement parentElement = parent as FrameworkElement;

                if (parentElement != null && parentElement.Visibility == Visibility.Collapsed )
                    return null;

                if (parent.GetType() == typeof(AppBar) || parent.GetType() == typeof(CommandBar))
                    return parent as CommandBar;
            }

            return null;
        }

        private bool CheckAppBarButton(AppBarButton appBarButton )
        {
            if ( appBarButton == null)
                return false;

            if (appBarButton.IsCompact || !appBarButton.IsEnabled || appBarButton.Visibility != Visibility.Visible )
                return false;

            return CheckCommandBar(FindCommandBar(appBarButton));
        }

        private bool CheckCommandBar(CommandBar bar)
        {
            return ( bar != null && bar.IsEnabled && bar.Visibility == Visibility.Visible );
        }

        #endregion
    }
}
