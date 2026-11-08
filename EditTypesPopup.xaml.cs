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

    // Внедряем зависимость IBluetooth_service через конструктор
    private readonly IBluetooth_service? _bluetoothService;


    // Словарь для хранения ссылок на Span для каждого LoopRowModel, чтобы обновлять текст и цвет в UI
    private readonly Dictionary<LoopRowModel, Span> _spanBuffers = new();


    //адресс устройства
    private int _currentDeviceAddress = 1;

    

    public EditTypesPopup(Varta_832 parentPage, ObservableCollection<LoopRowModel> loopsToEdit, IBluetooth_service? bluetoothService, int currentDeviceAddress)
    {
        //Собираем визуальные элементы и связываем их с кодом
        InitializeComponent();
        // Зберігаємо посилання на батьківську сторінку та колекцію для редагування
        _parentPage = parentPage;
        _bluetoothService = bluetoothService;
        _loopsToEdit = loopsToEdit;
        _currentDeviceAddress = currentDeviceAddress;

        // Робімо «знімок» поточного стану до редагування
        // Зберігаємо початкові значення типів для кожного ШС у словнику - так как передали ссылку на коллекцию, из родительской странице
        foreach (var loop in _loopsToEdit)
        {
            _initialValues[loop] = (loop.TypeCode, loop.TypeText);
        }




        BuildRows();


        


        _bluetoothService?.DataReceivedBuffer += delegate (VartaFrame frame)
        {
            string address = frame.Adress;
            string indexString = frame.Index;
            string subIndex = frame.SubIndex;
            string bufferTypeCode = frame.BufferTypeCode;

            if (int.TryParse(subIndex, out int subIndexValue))
            {
                int collectionIndex = subIndexValue - 1; // Чтоб склыдывало в колекцию  ObservableCollection<LoopRowModel> _loopsToEdit с 0 до 7 (для шлейфов 1-8)

                if (collectionIndex >= 0 && collectionIndex < _loopsToEdit.Count && !string.IsNullOrEmpty(bufferTypeCode))
                {
                    var loop = _loopsToEdit[collectionIndex];// Получаем конкретный шлейф из коллекции по индексу loop - ссылка на объект LoopRowModel  коллекции _loopsToEdit

                    // Оновлюємо дані у UI-потоці
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Заполняем свойства BufferTypeCode и BufferTypeText для конкретного шлейфа
                        loop.BufferTypeCode = bufferTypeCode;

                        if (int.TryParse(bufferTypeCode, out int codeIndex) && codeIndex >= 0 && codeIndex < _typeValues.Length)
                        {
                            loop.BufferTypeText = _typeValues[codeIndex];//   текстові значення для кодів типів
                        }
                        else
                        {
                            loop.BufferTypeText = "null";
                        }

                        // проверка и прорисовка в UI для конкретного шлейфа, если он есть в словаре _spanBuffers
                        if (_spanBuffers.ContainsKey(loop))
                        {
                            // непосредственная запись через Техт
                            _spanBuffers[loop].Text = loop.BufferTypeText == "null" ? "(null)" : $"({loop.BufferTypeText})";

                        }

                        // ⭐ Те, про що ти казав: просто записуємо новий код у текст цього Entry!
                        if (loop.AssociatedEntry != null)
                        {
                            loop.AssociatedEntry.Text = bufferTypeCode == "null" ? "" : bufferTypeCode;
                        }



                    });
                }
            }
        };




        InitializeAllLoops_Buffer();



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


            // Зберігаємо посилання на цей спан для цього шлейфу
            _spanBuffers[loop] = spanBuffer;

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


            // ⭐ Зберігаємо посилання прямо в об'єкті цього конкретного шлейфу!
            loop.AssociatedEntry = entry;

            // делаем подписку изменение текста в Entry событием, которое будет обновлять Label и свойства типа ШС
            entry.TextChanged +=  delegate  (object? sender, TextChangedEventArgs e) { OnEntryTextChanged(loop, spanBuffer, e); };





            // почему везде ряды равны нулю (0)?
            // Потому что для каждого отдельного шлейфа мы создаем свою собственную отдельную маленькую сетку(grid).
            grid.Add(label, 0, 0);
            grid.Add(entry, 1, 0);

            Container8Loops.Add(grid); // добавляем сетку - один ряд в контейнер StackLayout, который уже отображается на экране
        }
    }



    private async void InitializeAllLoops_Buffer() 
    {

        //for (int i = 0; i < _loopsToEdit.Count; i++)
        //{
        //    // Отримуємо конкретний шлейф за поточним індексом `i`
        //    var loop = _loopsToEdit[i];

        //    // Тепер для кожного шлейфу рахуємо його власний блок і субіндекс
        //    int blockIndex = (loop.Number - 1) / 8 + 1;
        //    byte subIndex = (byte)loop.LoopNumber;

        //    // Логіка перевірки та відправки...
        //}

      
        foreach (var loop in _loopsToEdit)
        {
            //Правильно: беремо номер конкретного шлейфу, який зараз обробляється в циклі
            int blockIndex = (loop.Number - 1) / 8 ;
            string indxstring = $"0000301{blockIndex.ToString("X1")}"; // базовий індекс для першого блоку (1-8 шлейфів)
            byte subIndex = (byte)loop.LoopNumber;

            // ⭐ ВИВОДИМО В ДЕБАГ ДЛЯ ПЕРЕВІРКИ
            System.Diagnostics.Debug.WriteLine($"[INIT BUFFER] ШС LoopNumber: {loop.LoopNumber} | Number: {loop.Number} | calculated blockIndex: {blockIndex} | indxstring: {indxstring} | subIndex: {subIndex}");
            string cmd = Build_BufferFrame(0x01, _currentDeviceAddress, indxstring, subIndex, 0x01);
            if (_bluetoothService != null) { await _bluetoothService.TransmitterData(cmd); }
            await Task.Delay(150); // Невелика затримка для стабільності передачі (можна налаштувати за потребою)

        }

    }


    private string Build_BufferFrame(byte read, int deviceAddress, string indxstring, byte subindx, int len)
    {
        // переводим строку индекс в массив байт
        byte[] indx = new byte[indxstring.Length / 2];
        for (int i = 0; i < indx.Length; i++)
        {
            //конвертируем в 16 - hex(Substring - берет по 2 символа и потом их конвнртирует)
            indx[i] = Convert.ToByte(indxstring.Substring(i * 2, 2), 16);
        }

        // создаем динамический список байтов
        List<byte> list = new();
        // заполняем последовательно список 
        list.Add(read);
        list.Add((byte)deviceAddress);
        list.AddRange(indx);
        list.Add(subindx);
        list.Add((byte)len);

        byte[] frameBytes = list.ToArray(); //превращаем список в массив байтов
        //формируем чексумму
        int sum = 0;
        foreach (var b in frameBytes) { sum += b; }
        //вычесляем чексумму
        byte checksum = (byte)(sum & 0xFF);
        //создаем массив с учетом чексуммы
        byte[] end = new byte[frameBytes.Length + 1];
        // копируем из источника frameBytes  в массив еnd
        Array.Copy(frameBytes, 0, end, 0, frameBytes.Length);
        //добавляем последний байт чексумму
        end[end.Length - 1] = checksum;

        return BitConverter.ToString(end).Trim().Replace("-", "");
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
            if (loop.BufferTypeCode == _initialValues[loop].Code)
            {
                // Якщо раніше була решітка, але користувач повернув значення назад — знімаємо її
                loop.IsTypeModified = false;
                continue; // Пропускаем проверку, так как значение не изменилось - следуем к следующему шлейфу
            }
            else
            {
                // Здесь проверяем сложные случаи: если пользователь стер значение до "null" или сменил тип
                if (int.TryParse(loop.BufferTypeCode, out int code) && code >= 0 && code < _typeValues.Length)
                {


                    // 2. ТУТ ВАШ КОД ВІДПРАВКИ ЧЕРЕЗ BLUETOOTH (через ваш метод TransmitterData_writeByte)
                    if (_bluetoothService != null)
                    {

                        // 1. ДИНАМІЧНО ФОРМУЄМО ІНДЕКС ПАМ'ЯТІ
                        // Базові байти: 0x00, 0x00, 0x30, а останній байт залежить від блоку (0x10 для 1-го, 0x11 для 2-го і т.д.)
                        byte lastByteOfIndex = (byte)(0x10 + (blockIndex - 1));
                        byte[] index = new byte[] { 0x00, 0x00, 0x30, lastByteOfIndex };


                      //  byte subIndex = (byte)(loop.Number);
                        byte subIndex = (byte)(loop.LoopNumber);
                        byte adress = (byte)_currentDeviceAddress; // Используем текущий адрес устройства, переданный в конструкторе
                        // ТУТ ВАШ КОД ВІДПРАВКИ (наприклад, асинхронний виклик запису байта)
                        //    await _bluetoothService.TransmitterData_writeByte(0x03, 0x02, 0x01, index, 0x04); // пример вызова метода отправки байта через Bluetooth
                        await _bluetoothService.TransmitterData_writeByte((byte)code, 0x02, adress, index, subIndex);

                        await Task.Delay(150); // Невелика затримка для стабільності передачі (можна налаштувати за потребою)
                                               // ⭐ Успішно збережено: вмикаємо прапорець модифікації (з'явиться решітка / буфер)
                        loop.IsTypeModified = true;

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
        //foreach (var pair in _initialValues)
        //{
        //    var loop = pair.Key;
        //    loop.TypeCode = pair.Value.Code;
        //    loop.TypeText = pair.Value.Text;
        //}

        await Navigation.PopModalAsync();
    }
}