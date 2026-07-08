using Microsoft.Maui.Controls;

namespace skb_home
{
    public class SimpleTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LegendTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is string str)
                return str == "legend" ? LegendTemplate : ImageTemplate;
            return LegendTemplate;
        }
    }
}
