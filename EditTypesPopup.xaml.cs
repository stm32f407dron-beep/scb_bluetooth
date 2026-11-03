using System.Collections.ObjectModel;

namespace skb_home;

public partial class EditTypesPopup : ContentPage
{
    private readonly Varta_832 _parentPage;
    private readonly ObservableCollection<LoopRowModel> _loopsToEdit;
    private readonly string[] _typeValues = { "Н/Д", "ЛОГ. 1", "ОХОР.", "ПОЖ. 1", "ПОЖ. 2", "ЛОГ. 2" };

    public EditTypesPopup(Varta_832 parentPage, ObservableCollection<LoopRowModel> loopsToEdit)
    {
        InitializeComponent();
        _parentPage = parentPage;
        _loopsToEdit = loopsToEdit;

        BuildRows();
    }

    private void BuildRows()
    {
        Container8Loops.Clear();

        foreach (var loop in _loopsToEdit)
        {
            var grid = new Grid
            {
                ColumnSpacing = 8,
                VerticalOptions = LayoutOptions.Center
            };

            // Добавляем колонки напрямую через .Add()
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(70)));

            var label = new Label
            {
                Text = $"ШС {loop.LoopNumber}: {loop.TypeText}",
                VerticalTextAlignment = TextAlignment.Center,
                FontSize = 12,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold
            };

            var entry = new Entry
            {
                Text = loop.TypeCode,
                Keyboard = Keyboard.Numeric,
                HorizontalTextAlignment = TextAlignment.Center,
                HeightRequest = 34,
                FontSize = 12
            };

            entry.TextChanged += (sender, e) =>
            {
                if (int.TryParse(e.NewTextValue, out int code) && code >= 0 && code < _typeValues.Length)
                {
                    loop.TypeCode = code.ToString("X2");
                    loop.TypeText = _typeValues[code];
                    label.Text = $"ШС {loop.LoopNumber}: {loop.TypeText}";
                }
            };

            grid.Add(label, 0, 0);
            grid.Add(entry, 1, 0);

            Container8Loops.Add(grid);
        }
    }

    private async void OnSaveAndCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}