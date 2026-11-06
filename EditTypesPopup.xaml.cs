using System.Collections.ObjectModel;


namespace skb_home;

public partial class EditTypesPopup : ContentPage
{
    // Посилання на батьківську сторінку
    private readonly Varta_832 _parentPage;
    // Колекція для редагування типів ШС
    private readonly ObservableCollection<LoopRowModel> _loopsToEdit;
    // Масив для відображення текстових значень типів
    private readonly string[] _typeValues = { "Н/Д", "ЛОГ. 1", "ОХОР.", "ПОЖ. 1", "ПОЖ. 2", "ЛОГ. 2" };

    // Словник для збереження початкових значень (на випадок скасування)
    private readonly Dictionary<LoopRowModel, (string Code, string Text)> _initialValues = new();


    private readonly IBluetooth_service? _bluetoothService;



    public EditTypesPopup(Varta_832 parentPage, ObservableCollection<LoopRowModel> loopsToEdit, IBluetooth_service? bluetoothService)
    {
        //Собираем визуальные элементы и связываем их с кодом
        InitializeComponent();
        // Зберігаємо посилання на батьківську сторінку та колекцію для редагування
        _parentPage = parentPage;
        _bluetoothService = bluetoothService;
        _loopsToEdit = loopsToEdit;

        // Робімо «знімок» поточного стану до редагування
        // Зберігаємо початкові значення типів для кожного ШС у словнику
        foreach (var loop in _loopsToEdit)
        {
            _initialValues[loop] = (loop.TypeCode, loop.TypeText);
        }

        BuildRows();
    }

    private void BuildRows()
    {
        // Очистка при каждом входе в метод, чтобы избежать дублирования элементов при повторном вызове
        Container8Loops.Clear();

        foreach (var loop in _loopsToEdit)
        {
            // делаем сетку для колонок: первая для Label, вторая для Entry - для каждого шлейфа автономная сетка
            var grid = new Grid
            {
                ColumnSpacing = 8,
                VerticalOptions = LayoutOptions.Center
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(70)));

            //первая колонка: Label с текстом типа ШС
            //var label = new Label
            //{
            //    Text = $"ШС {loop.LoopNumber}: {loop.TypeText} / ({loop.BufferTypeText})",
            //    VerticalTextAlignment = TextAlignment.Center,
            //    FontSize = 12,
            //    TextColor = Colors.Black,
            //    FontAttributes = FontAttributes.Bold
            //};

            //
            // 1. Перша частина мітки (Чорним: "ШС 1: ПОЖ. 1 / ")
            var spanMain = new Span
            {
                Text = $"ШС {loop.LoopNumber}: {loop.TypeText} / ",
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12
            };

            // 2. Друга частина мітки (Синім: буферне значення)
            string bufferDisplay = string.IsNullOrEmpty(loop.BufferTypeText) || loop.BufferTypeText == "null" ? "(null)" : $"({loop.BufferTypeText})";

            var spanBuffer = new Span
            {
                Text = bufferDisplay,
                TextColor = Colors.Blue, // ⭐ Синій колір для буфера
                FontAttributes = FontAttributes.Bold,
                FontSize = 12
            };

            // 3. Об'єднуємо обидва спани в один Label за допомогою FormattedText
            var label = new Label
            {
                VerticalTextAlignment = TextAlignment.Center,
                FormattedText = new FormattedString
                {
                    Spans = { spanMain, spanBuffer }
                }
            };






            //

            //вторая колонка: Entry для ввода кода типа ШС
            var entry = new Entry
            {
                Text = loop.BufferTypeCode,
                Keyboard = Keyboard.Numeric,
                HorizontalTextAlignment = TextAlignment.Center,
                HeightRequest = 34,
                FontSize = 12
            };

            // делаем подписку изменение текста в Entry событием, которое будет обновлять Label и свойства типа ШС
            entry.TextChanged +=  delegate  (object? sender, TextChangedEventArgs e) { OnEntryTextChanged(loop, spanBuffer, e); };





            // почему везде ряды равны нулю (0)?
            // Потому что для каждого отдельного шлейфа мы создаем свою собственную отдельную маленькую сетку(grid).
            grid.Add(label, 0, 0);
            grid.Add(entry, 1, 0);

            Container8Loops.Add(grid); // добавляем сетку - один ряд в контейнер StackLayout, который уже отображается на экране
        }
    }



    private void OnEntryTextChanged(LoopRowModel loop, Label label, TextChangedEventArgs e)
    {
        // Проверяем введенное значение
        if (int.TryParse(e.NewTextValue, out int code) && code >= 0 && code < _typeValues.Length)
        {
            loop.BufferTypeCode = code.ToString("X2");
            loop.BufferTypeText = _typeValues[code];
            label.Text = $"ШС {loop.LoopNumber}: {loop.TypeText} / ({loop.BufferTypeText})";
        }
        else
        {
            // Не стираем насильно текст в Entry (чтобы Android не падал с исключением индексов),
            // но в логике и на метке четко фиксируем "null"
            loop.BufferTypeCode = "null";
            loop.BufferTypeText = "null";
            label.Text = $"ШС {loop.LoopNumber}: {loop.TypeText} / (null)";
        }
    }


    // ⭐ Метод приймає `Span spanBuffer`, щоб змінювати текст та колір саме у синій частині
    private void OnEntryTextChanged(LoopRowModel loop, Span spanBuffer, TextChangedEventArgs e)
    {
        // Проверяем введенное значение
        if (int.TryParse(e.NewTextValue, out int code) && code >= 0 && code < _typeValues.Length)
        {
            loop.BufferTypeCode = code.ToString("X2");
            loop.BufferTypeText = _typeValues[code];
            spanBuffer.Text = $"({loop.BufferTypeText})";
        }
        else
        {
            // Не стираем насильно текст в Entry (чтобы Android не падал с исключением индексов),
            // но в логике и на метке четко фиксируем "null"
            loop.BufferTypeCode = "null";
            loop.BufferTypeText = "null";
            spanBuffer.Text = "(null)";
        }
    }




    private async void OnSaveClicked(object sender, EventArgs e)
    {

        // Перевіряємо кожний шлейф перед збереженням
        foreach (var loop in _loopsToEdit)
        {
            // Отримуємо початкове значення з того самого знімка, який ми зробили в конструкторі
            string initialCode = _initialValues.ContainsKey(loop) ? _initialValues[loop].Code : "null";

            int blockIndex = (loop.Number - 1) / 8 + 1; // номер блока (1-4) для шлейфів 1-32

            // Если код вообще не менялся с момента открытия окна — разрешаем сохранить без проблем
            if (loop.TypeCode == _initialValues[loop].Code)
            {
                continue; // Пропускаем проверку, так как значение не изменилось - следуем к следующему шлейфу
            }
            else
            {
                // Здесь проверяем сложные случаи: если пользователь стер значение до "null" или сменил тип
                if (int.TryParse(loop.TypeCode, out int code) && code >= 0 && code < _typeValues.Length)
                {


                    // 2. ТУТ ВАШ КОД ВІДПРАВКИ ЧЕРЕЗ BLUETOOTH (через ваш метод TransmitterData_writeByte)
                    if (_bluetoothService != null)
                    {

                        // 1. ДИНАМІЧНО ФОРМУЄМО ІНДЕКС ПАМ'ЯТІ
                        // Базові байти: 0x00, 0x00, 0x30, а останній байт залежить від блоку (0x10 для 1-го, 0x11 для 2-го і т.д.)
                        byte lastByteOfIndex = (byte)(0x10 + (blockIndex - 1));
                        byte[] index = new byte[] { 0x00, 0x00, 0x30, lastByteOfIndex };


                        byte subIndex = (byte)(loop.Number); 


                        // ТУТ ВАШ КОД ВІДПРАВКИ (наприклад, асинхронний виклик запису байта)
                        //    await _bluetoothService.TransmitterData_writeByte(0x03, 0x02, 0x01, index, 0x04); // пример вызова метода отправки байта через Bluetooth
                        await _bluetoothService.TransmitterData_writeByte((byte)code, 0x02, 0x01, index, subIndex);

                        await Task.Delay(150); // Невелика затримка для стабільності передачі (можна налаштувати за потребою)

                    }
                    else
                    {
                        await DisplayAlert("Помилка", "Bluetooth сервіс не ініціалізовано. Збереження неможливе.", "OK");
                        return; // Обов'язково зупиняємо збереження і НЕ закриваємо вікно!
                    }


                }
                else 
                {
                    await DisplayAlert("Помилка", $"Невірний тип для ШС {loop.LoopNumber}. Введіть значення від 0 до 5.", "OK");
                    return; // Зупиняємо збереження і НЕ закриваємо вікно!
                }


            }

        
        }


        // Якщо все заповнено правильно — закриваємо вікно
        await Navigation.PopModalAsync();
    }







    // Кнопка «Зберегти»: просто закриваємо вікно, зміни вже в пам'яті 
    // / Отримуємо початкове значення з того самого знімка, який ми зробили в конструкторі
    //string initialCode = _initialValues.ContainsKey(loop) ? _initialValues[loop].Code : null;
    private async void OnSaveClicked2(object sender, EventArgs e)
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


        // 2.ТУТ БУДЕТ ВАШ КОД ОТПРАВКИ ЧЕРЕЗ BLUETOOTH
        // Пример использования:
        if (_bluetoothService != null) { }






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