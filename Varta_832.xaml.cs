
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

//using static System.Net.Mime.MediaTypeNames;

namespace skb_home;

public partial class Varta_832 : ContentPage
{
    private readonly IBluetooth_service _bluetoothService;

    // Все 32 шлейфа в памяти - список типа LoopRowModel - память  для перезаписи обьекта ObservableCollection<LoopRowModel> CurrentBlockLoops
    private readonly List<LoopRowModel> _allLoops = new();

    // 8 строк, которые отображаются прямо сейчас на экране
    // Это корзина (источник данных) в памяти C#. В ней лежат 8 объектов LoopRowModel.
    public ObservableCollection<LoopRowModel> CurrentBlockLoops { get; set; } = new();

    // Текущий выбранный блок БША (1..4)
    private int _selectedBlock = 1;

    // Флаг режима выносных блоков
    private bool _isRemoteBshvMode = false;


    // Храним текущий выбранный сетевой адрес прибора (число от 1 до 16)
    private int _currentDeviceAddress = 1;

    // Флаг для блокировки повторного нажатия - для идентификатора
    public static   bool variableidentif = false;

    // для нормальной обрабтки кнопки идинтификатора
    private bool _isProcessingidentif = false;


    public Varta_832(IBluetooth_service bluetoothService)
    {
        _bluetoothService = bluetoothService;
        InitializeComponent();
        BindingContext = this; // взаимодействие этой страницы Varta_832 с хамл разметкой - привязка



        // Заполняем адреса от 1 до 15
        DeviceAddressPicker.Items.Clear(); // Очищает список перед заполнением, чтобы элементы не дублировались, если этот метод вызывается повторно.
        for (int addr = 1; addr <= 15; addr++)
        {
            //Хотя сама рамка и сам компонент Picker объявлены в XAML-разметке (Varta_832.xaml),
            //его содержимое (элементы списка от 1 до 16) динамически формируется и
            //добавляется с помощью кода на C#. В .NET MAUI это стандартный подход,
            //когда элементы выпадающего списка генерируются программно (через цикл for),
            //а не прописываются вручную статично в XAML.
            DeviceAddressPicker.Items.Add($"{addr} - ВАРТА832");
        }


        //Начальный выбор адреса №1
        DeviceAddressPicker.SelectedIndex = 0;

        // Создаем все 32 шлейфа и загружаем первые 8 (БВШ 1)
        InitializeAllLoops();// Здесь _allLoops наполняется 32-мя объектами через new LoopRowModel
        ShowBlock(1); // А вот здесь данные попадают на экран


        //Смысл этой команды:
        // LoopsCollectionView — это элемент таблицы из XAML.Свойство ItemsSource переводится как «источник элементов».
        //Этой строчкой вы говорите таблице на экране:
        //«Твои строки лежат вот в этой коллекции CurrentBlockLoops. Смотри в неё и отображай всё, что там есть». - как бы мост связка 
        // коллекции CurrentBlockLoops с хамл разметкой
        LoopsCollectionView.ItemsSource = CurrentBlockLoops;

        // Прием кадра от Bluetooth
        // Подписка на обработчик 
        _bluetoothService.DataReceivedFrame += delegate (VartaFrame frame)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateDataFromFrame(frame);
            });
        };
        // Запускаем прием
        _bluetoothService.ReceiverData_Varta832();
        // подписка на вывод строки данных в терминал
        _bluetoothService.DataReceivedVarta832 += AddTerminalText;


        //подписка для индетификатора
        
        _bluetoothService.DataReceivedIdentifier += delegate (VartaFrame identif)
        {
            MainThread.BeginInvokeOnMainThread(delegate {

                // Пробуем перевести субиндекс (например, "17" из Hex или дес.) в число
                if (int.TryParse(identif.SubIndex, out int absoluteLoopNumber))
                {
                    // Так как шлейфы идут с 1 (от 1 до 32), а индексы в массиве _allLoops с 0:
                    int arrayIndex = absoluteLoopNumber - 1;

                    if (arrayIndex >= 0 && arrayIndex < _allLoops.Count)
                    {
                        var row = _allLoops[arrayIndex];
                        row.Identifier = identif.Identifier;
                        variableidentif = true;
                    }
                }
            });
        };






        //
    }



    // начальная иницилизация
    // Используем List<LoopRowModel> _allLoops  как кэш для начальной иницилизации
    // Список _allLoops у нас спрятан глубоко в логике (private readonly List<LoopRowModel>).
    // Он напрямую к XAML вообще не привязан.
    // В динамике для обновлении страницы хамл не используеться так как не реализует интерфейс  INotifyCollectionChanged
    //просто потом в ShowBlock(1); идет перезапись  CurrentBlockLoops.Add(_allLoops[startIndex + i]); - обьектов List в ObservableCollection
    private void InitializeAllLoops()
    {
        _allLoops.Clear();
        int total = 1;
        for (int bsh = 1; bsh <= 4; bsh++)
        {
            for (int loop = 1; loop <= 8; loop++)
            {
                _allLoops.Add(new LoopRowModel
                {
                    Number = total,
                    Address = "null",
                    LoopNumber = loop,
                    Identifier = "null",
                    TypeText = "null",
                    TypeCode = "null",
                    StatusText = "null",
                    StatusCode = "null"
                });
                total++;
            }
        }
    }

    // Переключение отображаемого блока (1, 2, 3 или 4)
    // _allLoops (List) — это просто «тихий» склад в оперативной памяти. Сам по себе он ничего в XAML не передаёт и не обновляет.

    //На строке CurrentBlockLoops.Add(_allLoops[startIndex + i]); вы просто берёте готовые данные со склада и перекладываете их в CurrentBlockLoops.

    //И уже CurrentBlockLoops, будучи ObservableCollection, при каждом .Add() выбрасывает событие в движок MAUI, заставляя XAML обновить отображение строк на экране.
    private void ShowBlock(int blockIndex)
    {
        _selectedBlock = blockIndex;
        CurrentBlockLoops.Clear();

        int startIndex = (_selectedBlock - 1) * 8;
        for (int i = 0; i < 8; i++)
        {
            // делаем перезапись из List / ВОТ ЗДЕСЬ ОБЪЕКТ ИЗ СУШЕСТВУЮЩЕГО СПИСКА ДОБАВЛЯЕТСЯ В ЭКРАННУЮ КОЛЛЕКЦИЮ! - блодаря этому происходит изменение в хамл
            CurrentBlockLoops.Add(_allLoops[startIndex + i]); 
        }

        // Обновляем визуальный вид кнопок переключения
        UpdateBlockButtonsUI();
    }

    private void UpdateBlockButtonsUI()
    {
        BtnBsh1.BackgroundColor = _selectedBlock == 1 ? Color.FromArgb("#2C2305") : Color.FromArgb("#E0DFD8");
        BtnBsh1.TextColor = _selectedBlock == 1 ? Colors.White : Color.FromArgb("#333333");

        BtnBsh2.BackgroundColor = _selectedBlock == 2 ? Color.FromArgb("#2C2305") : Color.FromArgb("#E0DFD8");
        BtnBsh2.TextColor = _selectedBlock == 2 ? Colors.White : Color.FromArgb("#333333");

        BtnBsh3.BackgroundColor = _selectedBlock == 3 ? Color.FromArgb("#2C2305") : Color.FromArgb("#E0DFD8");
        BtnBsh3.TextColor = _selectedBlock == 3 ? Colors.White : Color.FromArgb("#333333");

        BtnBsh4.BackgroundColor = _selectedBlock == 4 ? Color.FromArgb("#2C2305") : Color.FromArgb("#E0DFD8");
        BtnBsh4.TextColor = _selectedBlock == 4 ? Colors.White : Color.FromArgb("#333333");

        BtnReadCurrent.Text = $"Зчитати поточний БВШ {_selectedBlock}";
    }

    // Обработчик нажатия на кнопки «БВШ 1» ... «БВШ 4»
    //private void OnSelectBshClicked(object sender, EventArgs e)
    //{
    //    if (sender is Button btn)
    //    {
    //        if (btn == BtnBsh1) ShowBlock(1);
    //        else if (btn == BtnBsh2) ShowBlock(2);
    //        else if (btn == BtnBsh3) ShowBlock(3);
    //        else if (btn == BtnBsh4) ShowBlock(4);
    //    }
    //}


    //
    // Клик по основным блокам (БВШ 1..4)
    private void OnSelectBshClicked(object sender, EventArgs e)
    {
        _isRemoteBshvMode = false; // Выключаем режим выносных

        // Сбрасываем цвет кнопки БВШВ в обычный
        BtnBshvMode.BackgroundColor = Color.FromArgb("#E0DFD8");
        BtnBshvMode.TextColor = Color.FromArgb("#333");

        if (sender is Button btn)
        {
            if (btn == BtnBsh1) ShowBlock(1);
            else if (btn == BtnBsh2) ShowBlock(2);
            else if (btn == BtnBsh3) ShowBlock(3);
            else if (btn == BtnBsh4) ShowBlock(4);
        }

        // Обновляем текст главной кнопки
        BtnReadCurrent.Text = $"Зчитати поточний БВШ {_selectedBlock}";
        UpdateBlockButtonsUI(); // Обновляет подсветку кнопок БВШ 1..4
    }


    // Клик по кнопке «БВШВ» в ленте прокрутки
    private void OnBshvModeClicked(object sender, EventArgs e)
    {
        _isRemoteBshvMode = true;

        // Подсвечиваем кнопку БВШВ активным цветом
        BtnBshvMode.BackgroundColor = Color.FromArgb("#2C2305");
        BtnBshvMode.TextColor = Colors.White;

        // Сбрасываем подсветку основных кнопок БВШ 1..4
        BtnBsh1.BackgroundColor = Color.FromArgb("#E0DFD8"); BtnBsh1.TextColor = Color.FromArgb("#333");
        BtnBsh2.BackgroundColor = Color.FromArgb("#E0DFD8"); BtnBsh2.TextColor = Color.FromArgb("#333");
        BtnBsh3.BackgroundColor = Color.FromArgb("#E0DFD8"); BtnBsh3.TextColor = Color.FromArgb("#333");
        BtnBsh4.BackgroundColor = Color.FromArgb("#E0DFD8"); BtnBsh4.TextColor = Color.FromArgb("#333");

        int remoteNum = 1;
        if (int.TryParse(EntryRemoteBlockNumber.Text, out int val))
            remoteNum = val;

        BtnReadCurrent.Text = $"Зчитати виносний БВШВ {remoteNum}";
    }

    // Событие при изменении цифры в Entry для выносных блоков
    private void OnRemoteEntryChanged(object sender, TextChangedEventArgs e)
    {
        if (_isRemoteBshvMode && int.TryParse(e.NewTextValue, out int remoteNum))
        {
            if (remoteNum >= 1 && remoteNum <= 15)
            {
                BtnReadCurrent.Text = $"Зчитати виносний БВШВ {remoteNum}";
            }
        }
    }





    //

    // Обновление данных при получении кадра - обработчик событий
    private void UpdateDataFromFrame(VartaFrame frame)
    {
        if (frame == null || frame.Values == null) return; // При налл  выходим
        if (!int.TryParse(frame.SubIndex, out int bshNumber)) return; // Проверяем субиндекс на число

        int startIndex = (bshNumber - 1) * 8; // вычисляем стартовый индекс
        // доп. защита: startIndex < 0 (Защита снизу) и tartIndex + 7 >= _allLoops.Count (Защита сверху)
        if (startIndex < 0 || startIndex + 7 >= _allLoops.Count) return;

        for (int i = 0; i < 8; i++)
        {
            //мы не создаём строку с нуля, а достаём уже готовую существующую строку из нашего списка-хранилища _allLoops по точному индексу.
            var row = _allLoops[startIndex + i];

            // Первые 8 значений — тип
            if (i < frame.Values.Count)
            {
                // Вызов этих свойст трегерит события OnPropertyChanged(); и обновляет таблицу хамл
                row.TypeText = frame.Values[i].Value;
                row.TypeCode = frame.Values[i].Code;
                row.Address = frame.Values[i].Adress;
            }

            // Следующие 8 значений — стан
            int statusIdx = i + 8;
            if (statusIdx < frame.Values.Count)
            {
                row.StatusText = frame.Values[statusIdx].Value;
                row.StatusCode = frame.Values[statusIdx].Code;
            }

           

        }

        // Если пришедший кадр относится к текущему открытому блоку — обновляем вид на экране
        if (bshNumber == _selectedBlock)
        {
            ShowBlock(_selectedBlock);
        }
    }

  
    // Считывание базовых  блоков  БВШ

    private async void OnReadCurrentBshClicked(object sender, EventArgs e)
    {
        try
        {
            // Передаем значения внутрь метода через аргументы
            string cmd = BuildCommand(_currentDeviceAddress, _selectedBlock);

            await _bluetoothService?.TransmitterData(cmd);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Не вдалося зчитати БВШ {_selectedBlock}: {ex.Message}", "OK");
        }
    }



    // функция красный крест стирание
    private async void OnAddressConfigClicked(object sender, EventArgs e)
    {
        //string result = await DisplayPromptAsync("Адреса приладу", "Введіть мережеву адресу:", initialValue: "1", keyboard: Keyboard.Numeric);
        //if (!string.IsNullOrWhiteSpace(result))
        //{
        //    await DisplayAlert("Адреса", $"Виставлено: {result}", "OK");
        //}

       
        int block = _selectedBlock; // текущий блок
        if (block >= 1 && block <= 4) // защита
        {
            int startIndex = (block - 1) * 8;//выясняем стартовый индекс блока
            switch (block)
            {
                case 1:
                case 2:
                case 3:
                case 4:

                    CurrentBlockLoops.Clear();
                    for (int i = 0; i < 8; i++)
                    {
             
                        // Обращаемся к существующему объекту в хранилище по правильному индексу (+ i)
                        _allLoops[startIndex + i].Address = "null";
                        _allLoops[startIndex + i].TypeText = "null";
                        _allLoops[startIndex + i].TypeCode = "null";
                        _allLoops[startIndex + i].StatusText = "null";
                        _allLoops[startIndex + i].StatusCode = "null";
                        _allLoops[startIndex + i].Identifier = "null";

                        // Докладываем в коллекцию для экрана для отображения изменений
                        CurrentBlockLoops.Add(_allLoops[startIndex + i]);
                       
                    }
                    await DisplayAlert("Видалення", $"Виделено успішно, блоку{block}", "OK");
                    break;

                    default:
                    // Якщо значення поза межами 1-4
                    break;
            }


        }
        else { await DisplayAlert("Видалення", "НЕ Виделено,блок по замежами", "OK"); }



    }


    // запись адреса
    private void OnDeviceAddressChanged(object sender, EventArgs e)
    {
        if (DeviceAddressPicker.SelectedIndex >= 0)
        {
            // Индекс 0 соответствует адресу 1, индекс 1 — адресу 2 и т.д.
            _currentDeviceAddress = DeviceAddressPicker.SelectedIndex + 1;
        }
    }

    // подготовка к отсылке команды базовых блоков БВШ
    private string BuildCommand(int deviceAddress, int bshNumber)
    {
        // 1. Внутренний фрейм по протоколу CV1500 (строго по таблице)
        byte[] frameBytes = new byte[]
        {
        0x01,                        // 1. Read (тип операции: чтение = 0x01)
        (byte)deviceAddress,         // 2. Address (сетевой адрес прибора 1..16 из Picker-а)
        0x00, 0x00, 0x31, 0x00,      // 3. Index (запрос шлейфов БВШ)
        (byte)bshNumber,             // 4. Subindex (номер блока БВШ: 1, 2, 3 или 4)
        0x20                         // 5. Length (длина данных)
        };

        // 2. Считаем контрольную сумму по этим байтам
        int sum = 0;
        foreach (var b in frameBytes)
        {
            sum += b;
        }
        byte checksum = (byte)(sum & 0xFF);

        // 3. Добавляем чексумму в конец массива
        byte[] fullPacket = new byte[frameBytes.Length + 1];
        Array.Copy(frameBytes, fullPacket, frameBytes.Length);
        fullPacket[fullPacket.Length - 1] = checksum;

        // 4. Переводим в ASCII-HEX строку (которая пойдёт в TransmitterData)
        return BitConverter.ToString(fullPacket).Replace("-", "");
    }


    // подготовка к отсылке команды идентфикаторов базовых блоков БВШ
    private string BuildIdentifierCommand(byte read,int deviceAddress,string indxstring, byte subindx,int len)
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
        Array.Copy(frameBytes,0,end,0,frameBytes.Length);
        //добавляем последний байт чексумму
        end[end.Length - 1] = checksum;

        return BitConverter.ToString(end).Trim().Replace("-","");
    }

    //
    private async void OnSaveClicked(object sender, EventArgs e) => await DisplayAlert("Збереження", "Функція у розробці", "OK");

    private async void OnStubTabClicked(object sender, EventArgs e)
    {
        if (sender is Button b) await DisplayAlert("Вкладка", $"Розділ '{b.Text}' у розробці", "OK");
    }

    // Терминал
    private async void OnTerminalToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            TerminalBorder.IsVisible = true;
            TerminalBorder.Opacity = 0;
            await TerminalBorder.FadeTo(1, 200);
            label4.Text = $"[{DateTime.Now:HH:mm:ss}] Термінал активовано\n";
        }
        else
        {
            await TerminalBorder.FadeTo(0, 200);
            TerminalBorder.IsVisible = false;
        }
    }

    // Очистка терминала
    private void OnClearTerminalClicked(object sender, EventArgs e) => label4.Text = "";
    // Добавление текста в терминал
    public void AddTerminalText(string text)
    {
        if (TerminalSwitch?.IsToggled != true) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (label4 == null || scrollView == null) return;
            label4.Text += $"[{DateTime.Now:HH:mm:ss}] {text}\n";
            scrollView.ScrollToAsync(label4, ScrollToPosition.End, animated: true);
        });
    }

    [Obsolete]
    private async void OnSettingsClicked(object sender, EventArgs e) => await DisplayAlert("Налаштування", "Функція у розробці", "OK");


    // Индетификатор
    //private async void OnHeaderIdentifierClicked(object sender, EventArgs e) 
    //{

    //    if (_isProcessingidentif) return;

    //    try
    //    {

    //        _isProcessingidentif = true;

    //        ////
    //        //byte block = 0;
    //        byte block = (byte)((_selectedBlock - 1) * 8);
    //        int device = 9;
    //        byte read = 0x01;
    //        byte subindx = 0x01;
    //        int len = 0x14;

    //        string cmd2;
    //        variableidentif = true;


    //        for (byte i = 1; i < device; i++)
    //        {
    //            // Создаем делегат задачу с возвратом боол , благодаря  await  - эта задача не будет грузить проц., даже находясь в фоне -пуле потоков 
    //            Func<Task<bool>> func = async delegate ()
    //            {

    //                while (!variableidentif)
    //                    await Task.Delay(50);

    //                return true;
    //            };
    //            //Кидаем задачу в пул потоков(фон) и ждем результат неблокируя основной поток
    //            variableidentif = await Task.Run(func);

    //            //if (subindx > 8) return;
    //            //subindx++;

    //            // работа при variableidentif = true;
    //            if (variableidentif)
    //            {
    //                cmd2 = BuildIdentifierCommand(read, _currentDeviceAddress, "00003014", (byte)(block + i), len);
    //                variableidentif = false;
    //                await _bluetoothService?.TransmitterData(cmd2);


    //            }
    //            else
    //            {
    //                await DisplayAlert("Помилка", "Не вдалося зчитати Indef", "OK");
    //                return;
    //            }


    //        }


    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert("Помилка", "Не вдалося зчитати Indef", "OK");
    //    }

    //    finally { _isProcessingidentif = false; }


    //}


    //
    private async void OnHeaderIdentifierClicked(object sender, EventArgs e)
    {
        if (_isProcessingidentif) return;

        try
        {
            _isProcessingidentif = true;

            // Вычисляем смещение блока (для 1-го блока = 0, для 2-го = 8 и т.д.)
            byte block = (byte)((_selectedBlock - 1) * 8);
            int device = 9; // Цикл от 1 до 8 (ровно 8 шлейфов в блоке)
            byte read = 0x01;
            int len = 0x14;

            string cmd2;

            // ВАЖНО: Перед началом всего цикла делаем переменную true, 
            // чтобы первый запрос полетел сразу без ожидания
            variableidentif = true;

            for (byte i = 1; i < device; i++)
            {
                // Работа при variableidentif = true (ждем подтверждения или отправляем следующий)
                if (variableidentif)
                {
                    // Сначала сбрасываем флаг перед отправкой нового запроса
                    variableidentif = false;

                    // Формируем и отправляем команду для текущего шлейфа (block + i)
                    cmd2 = BuildIdentifierCommand(read, _currentDeviceAddress, "00003014", (byte)(block + i), len);
                    await _bluetoothService?.TransmitterData(cmd2);
                }

                // Ожидание ответа от прибора с защитой от зависания (таймаут)
                int timeoutCounter = 0;
                bool received = false;

                while (timeoutCounter < 30) // 30 раз по 50мс = 1.5 секунды ожидания максимум на один шлейф
                {
                    if (variableidentif) // Если прибор прислал ответ и обработчик поднял флаг
                    {
                        received = true;
                        break;
                    }
                    await Task.Delay(50);
                    timeoutCounter++;
                }

                // Если вышло время, а прибор так и не ответил
                if (!received)
                {
                    await DisplayAlert("Помилка", $"Прилад не відповів на шлейф {block + i}", "OK");
                    return;
                }

                // МАЛЕНЬКАЯ ПАУЗА МЕЖДУ ЗАПРОСАМИ: 
                // Дает Bluetooth-модулю и контроллеру прибора «вдохнуть» и не захлебнуться потоком байтов
                await Task.Delay(50);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", "Не вдалося зчитати Indef", "OK");
        }
        finally
        {
            _isProcessingidentif = false;
        }
    }




    //

}


// Модель строки шлейфа
// Класс, который реализует интерфейс INotifyPropertyChanged
// для уведомления об изменениях свойств, что позволяет обновлять UI при изменении данных.
// Именно для этого и существует интерфейс INotifyPropertyChanged - данамическое взаимодействие с хамл страницей.

//   Браво! Вы поняли всё абсолютно точно, на 100%!Вы только что своими словами описали классический паттерн реактивного связывания
//   данных(Data Binding) в.NET MAUI:Давайте закрепим эту логическую цепочку, чтобы не осталось ни тени сомнения:Фоновый поток отработал:
//   Собрал полезную нагрузку и вызвал DataReceivedFrame?.Invoke(frame);.   Главный поток вызвал обработчик:
//   Через MainThread.BeginInvokeOnMainThread управление переходит в метод UpdateDataFromFrame(frame).
//    Код меняет свойства объектов в цикле for:Когда строка row.TypeText = frame.Values[i].Value; 
//    меняет тип — срабатывает сеттер TypeText - вызывается OnPropertyChanged("TypeText") 
//    - движок MAUI обновляет текст колонки «Тип ШС».   Когда строка row.TypeCode = ... меняет код —
//    вызывается OnPropertyChanged("TypeCode") - обновляется колонка кода.Когда строка row.StatusText =
//    frame.Values[statusIdx].Value; меняет состояние — срабатывает сеттер StatusText, вызывается OnPropertyChanged("StatusText") -
//    обновляется колонка статуса(«НОРМА», «Пожежа» и т.д.).   Тут же вызывается UpdateRowColor() - меняется RowBackgroundColor 
//    - вызывается OnPropertyChanged("RowBackgroundColor") - MAUI перекрашивает всю строку таблицы!
//    Каждое свойство с OnPropertyChanged() — это как сигнальная ракета: как только в цикле меняется его значение, MAUI ловит сигнал
//    и мгновенно перерисовывает именно этот маленький кусочек экрана, не дёргая всю таблицу целиком.

//Давайте заглянем под капот и разберём, как именно устроена эта внутренняя подписка движка MAUI — от момента запуска приложения до клика на ячейку.

//Шаг 1. Как MAUI подписывается «под капотом»
//Когда страница Varta_832 только создаётся, в конструкторе выполняется:

//C#
//LoopsCollectionView.ItemsSource = CurrentBlockLoops;
//В XAML у вас написано:

//XML
//<Label Grid.Column="5" Text="{Binding TypeCode}" ... />
//В этот момент графический движок MAUI для каждой из 8 строк делает следующую скрытую работу:

//Создаёт внутренний объект-наблюдатель(Binding Expression).

//Для ячейки TypeCode создаётся невидимый служебный мостик между свойством Label.Text и свойством LoopRowModel.TypeCode.

//Проверяет интерфейс:

//MAUI проверяет:

//«Реализует ли переданный мне объект строки интерфейс INotifyPropertyChanged?»

//Видит: да, реализует!

//Оформляет невидимую подписку:

//Движок MAUI буквально выполняет скрытый C#-код, аналогичный такому:

//C#
//// Внутренний код MAUI, который скрыт от наших глаз:
//row.PropertyChanged += OnBindingPropertyChanged;
//Всё! С этого момента движок MAUI «держит руку на пульсе» объекта row.

//Шаг 2. Что происходит при вызове OnPropertyChanged("TypeCode")
//Теперь переносимся в момент, когда из Bluetooth прилетели свежие данные:

//В цикле выполняется присвоение:

//C#
//row.TypeCode = frame.Values[i].Code; // например, пришла строка "03"
//Срабатывает сеттер внутри класса LoopRowModel:

//C#
//public string TypeCode
//{
//    get => _typeCode;
//    set
//    {
//        _typeCode = value;
//        OnPropertyChanged(); // Компилятор передал сюда "TypeCode"
//    }
//}
//Метод OnPropertyChanged делает вызов:

//C#
//PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TypeCode"));
//Что делает тот самый скрытый внутренний метод MAUI(OnBindingPropertyChanged), который был подписан?

//Он получает оповещение: «У объекта this изменилось свойство с именем "TypeCode"!»

//Движок MAUI смотрит в свой список привязок:

//«Так, к свойству TypeCode у меня привязан Label в 5 - й колонке этой строки».

//Движок считывает новое значение через геттер: row.TypeCode(где уже лежит "03").

//И напрямую в графическом представлении присваивает:

//C#
//labelColumn5.Text = "03";
//Экран смартфона перерисовывает пару пикселей в этой конкретной ячейке.

//Главный вывод:
//Вам не нужно вручную писать код вроде myLabel.Text = row.TypeCode для каждой ячейки.

//Вся рутина переложена на движок MAUI:

//XAML через { Binding TypeCode}
//указывает, куда смотреть.

//Интерфейс INotifyPropertyChanged даёт MAUI колокольчик (событие), в который объект звонит при любом изменении.

//Как только колокольчик звенит с именем "TypeCode", MAUI сам находит нужный Label и перерисовывает его значение на экране.


public class LoopRowModel : INotifyPropertyChanged
{
    private string _typeText = "Пожежний 1";
    private string _typeCode = "03";
    private string _statusText = "НОРМА";
    private string _statusCode = "07";

    private string? _address = "null";

    private string _identifier = "null";
    private Color _rowBackgroundColor = Color.FromArgb("#C8E6C9");

    // номер
    public int Number { get; set; }
    // адресс
    public string? Address
    {
        get { return _address; }

        set { _address = value; OnPropertyChanged(); }
    }

    // номер шлейфа
    public int LoopNumber { get; set; }
    //Ідентіфікатор
    public string Identifier 
    {
        get { return _identifier; }

        set { _identifier = value; OnPropertyChanged(); }
    }


    

    public string TypeText
    {
        get => _typeText;
        set { _typeText = value; OnPropertyChanged(); }
    }

    public string TypeCode
    {
        get => _typeCode;
        set { _typeCode = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();  // Компилятор САМ превращает это в OnPropertyChanged("StatusText");
            UpdateRowColor();
        }
    }

    public string StatusCode
    {
        get => _statusCode;
        set { _statusCode = value; OnPropertyChanged(); }
    }

    public Color RowBackgroundColor
    {
        get => _rowBackgroundColor;
        set { _rowBackgroundColor = value; OnPropertyChanged(); }
    }

    private void UpdateRowColor()
    {
        if (StatusText.Contains("ПОЖЕЖА", StringComparison.OrdinalIgnoreCase) || StatusText.Contains("УВАГА", StringComparison.OrdinalIgnoreCase))
        {
            // Критична пожежна тривога -> Червоний фон  ВІДКЛЮЧ.
            RowBackgroundColor = Color.FromArgb("#FF8A80");
        }
        else if (StatusText.Contains("К.З.", StringComparison.OrdinalIgnoreCase) ||
                 StatusText.Contains("ОБРИВ", StringComparison.OrdinalIgnoreCase) ||              
                 StatusText.Contains("ВІДКЛЮЧ.", StringComparison.OrdinalIgnoreCase) ||
                 StatusText.Contains("ТРИВОГА", StringComparison.OrdinalIgnoreCase))
        {
            // Попередження / Несправність -> Жовтий фон
            RowBackgroundColor = Color.FromArgb("#FFD54F");
        }
        else
        {
            // Нормальні / робочі стани -> Зелений фон
            RowBackgroundColor = Color.FromArgb("#82E0AA");
        }
    }

    // Подписчиком выступает движок привязки данных XAML (Binding Engine).
    public event PropertyChangedEventHandler PropertyChanged;

    //protected void OnPropertyChanged([CallerMemberName] string propName = null)
    //    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

    //Но здесь перед параметром стоит атрибут [CallerMemberName].
    //Этот атрибут даёт компилятору Visual Studio прямую команду:
    //Если вы вызвали OnPropertyChanged() например,внутри свойства StatusText:
    // то в момент компиляции строка превращается в OnPropertyChanged("StatusText");
    protected void OnPropertyChanged([CallerMemberName] string propName = null)
    {  PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)); }  

}









public class VartaValue
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Code { get; set; }

    public string Adress { get; set; }

    public string Identifier { get; set; }
}

public class VartaFrame
{
    public string Index { get; set; }
    public string SubIndex { get; set; }
    public bool Exists { get; set; }
    public string Adress { get; set; }

    public string Identifier { get; set; }

    // Создаем контейнер  для коллекции, Values - это коллекция (ObservableCollection<...>), список.
    public ObservableCollection<VartaValue> Values { get; set; } = new();
}





//Да, абсолютно верно!Вы точно связали вместе XAML-разметку и C#-код.

//Именно в этой точке они соединяются:

//Как эта связка работает в деталях:
//В коде C# вы задаёте источник данных:

//C#
//LoopsCollectionView.ItemsSource = CurrentBlockLoops;
//В CurrentBlockLoops лежат 8 экземпляров вашего класса LoopRowModel.

//CollectionView генерирует 8 строк по шаблону DataTemplate:
//Для каждого из 8 объектов LoopRowModel MAUI создаёт визуальный контейнер Grid с набором Label.
//При этом для каждого Grid его контекстом данных (BindingContext) становится конкретный экземпляр LoopRowModel.

//Скрытая подписка MAUI (Binding Engine):
//Когда движок привязки видит выражения вроде:

//BackgroundColor= "{Binding RowBackgroundColor}"

//Text= "{Binding StatusText}"

//Text= "{Binding TypeText}"

//он проверяет: реализует ли объект LoopRowModel интерфейс INotifyPropertyChanged?
//Видя, что да, реализует, MAUI «под капотом» автоматически делает подписку:

//C#
//row.PropertyChanged += (sender, args) => 
//{
//    // Служебный код MAUI: обновить нужный элемент на экране
//};
//Когда срабатывает ваш код:
//Когда вы меняете свойство в коде:

//C#
//row.StatusText = "ПОЖЕЖА";
//вызывается:

//C#
//OnPropertyChanged();
//// внутри которого: PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StatusText"));
//MAUI слышит этот вызов события, видит имя свойства "StatusText" и адрес конкретной строки(this), после чего мгновенно обновляет текст ячейки на экране.