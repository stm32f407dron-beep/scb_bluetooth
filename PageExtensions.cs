using Microsoft.Maui.Controls;

namespace skb_home.Extensions
{
    public static class PageExtensions
    {
        public static void AddChild(this ContentPage page, View child)
        {
            var layout = page.Content as Layout;
            if (layout != null)
            {
                layout.Children.Add(child);
            }
        }

        public static void RemoveChild(this ContentPage page, View child)
        {
            var layout = page.Content as Layout;
            if (layout != null)
            {
                layout.Children.Remove(child);
            }
        }
    }
}