using System.Collections.ObjectModel;


namespace skb_home;

public partial class EditTypesPopup : ContentPage
{
    private readonly Varta_832 _parentPage;
    private readonly ObservableCollection<LoopRowModel> _loopsToEdit;
    private readonly string[] _typeValues = { "Н/Д", "ЛОГ. 1", "ОХОР.", "ПОЖ. 1", "ПОЖ. 2", "ЛОГ. 2" };

    // Словник для збереження початкових значень (на випадок скасування)
    private readonly Dictionary<LoopRowModel, (string Code, string Text)> _initialValues = new();

    public EditTypesPopup(Varta_832 parentPage, ObservableCollection<LoopRowModel> loopsToEdit)
    {
        InitializeComponent();
        _parentPage = parentPage;
        _loopsToEdit = loopsToEdit;

        // Робімо «знімок» поточного стану до редагування
        foreach (var loop in _loopsToEdit)
        {
            _initialValues[loop] = (loop.TypeCode, loop.TypeText);
        }

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

            // Змінюємо значення в пам'яті в реальному часі при вводі
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

    // Кнопка «Зберегти»: просто закриваємо вікно, зміни вже в пам'яті
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        // Перевіряємо, чи всі поточні значення в колекції є валідними (відповідають діапазону 0-5)
        foreach (var loop in _loopsToEdit)
        {
            // Перевіряємо, чи введений Code відповідає дозволеним індексам
            if (!int.TryParse(loop.TypeCode, out int code) || code < 0 || code >= _typeValues.Length)
            {
                await DisplayAlert("Помилка", $"Невірний тип для ШС {loop.LoopNumber}. Введіть значення від 0 до 5.", "OK");
                return; // Зупиняємо збереження і НЕ закриваємо вікно!
            }
        }

        // Якщо все заповнено правильно — закриваємо вікно
        await Navigation.PopModalAsync();
    }

    // Кнопка «Скасувати»: повертаємо старі значення з нашого «знімка» і закриваємо
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        foreach (var pair in _initialValues)
        {
            var loop = pair.Key;
            loop.TypeCode = pair.Value.Code;
            loop.TypeText = pair.Value.Text;
        }

        await Navigation.PopModalAsync();
    }
}