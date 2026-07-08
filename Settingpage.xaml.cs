
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace skb_home;

public partial class Settingpage : ContentPage
{
    // Добавим поле для хранения ссылки на Bluetooth-сервис
    private readonly IBluetooth_service _bluetoothService;
    // Событие для передачи данных в терминал
    public event Func<string, Task> Settingpage_DataReceived;

    // Список лейблов для чисел
    private List<Label> numberLabels = new List<Label>();
    private double currentAngle = 0;
    private bool isAnimating = false;
    private bool isPaused = false;
    private double startAngle = 180;// 180 - начальный угол смещения (цифра 1 слева)
    private int totalNumbers = 16; // Количество цифр (меняете только здесь)

    // Поля для динамических радиусов
    private double radiusX;
    private double radiusY;
    private double screenWidth;

    // Словарь команд для каждого канала (ключ - номер канала, значение - команда)
    private readonly Dictionary<string, string> _commands = new Dictionary<string, string>
    {

        // Ключи 
        ["1"] = "01010000240501012D",
        ["2"] = "01010000240502012E",
        ["3"] = "01010000240503012F",
        ["4"] = "010100002405040130",
        ["5"] = "010100002405050131",
        ["6"] = "010100002405060132",
        ["7"] = "010100002405070133",
        ["8"] = "010100002405080134",
        ["9"] = "010100002405090135",
        ["10"] = "0101000024050A0136",
        ["11"] = "0101000024050B0137",
        ["12"] = "0101000024050C0138",
        ["13"] = "0101000024050D0139",
        ["14"] = "0101000024050E013A",
        ["15"] = "0101000024050F013B",
        ["16"] = "01010000240510013C",
    };
    // Текущий индекс канала для массового опроса (по умолчанию null, когда не в процессе опроса)
    private int? currentChannelIndex = null;
    // Флаг для определения, что сейчас отображается отфильтрованный список (для управления анимацией)
    private bool isFiltered = false;

    //  private int filteredDisplayDuration = 500;  // ← Для фильтров 500ms вместо 1000ms


    //Флаг для предотвращения одновременного изменения фильтра (защита от двойного клика) 
    private bool isFilterChanging = false;  

    // Массив для хранения текущих значений шлейфов 
    private object[] currentValues = new object[]
    {
    null, null, null, null, null, null, null, null,
    null, null, null, null, null, null, null, null
    };



    // для одиночного опроса по клику пользователя
    private int? selectedLoopIndex = null;
    // для массового опроса через TaskCompletionSource
    private TaskCompletionSource<double> channelReadCompletionSource = null;

    private bool isReadAllInProgress = false;


    // Настройки анимации выезда
    private int displayDuration = 600;
    private double centerX = 0.5;
    private double centerY = 0.27;// 0.32 - центр цифр поднять или опустить

    public Settingpage(IBluetooth_service bluetoothService)
    {
        // инициализация компонентов страницы   
        InitializeComponent();

        PedestalCarousel.ItemsSource = new ObservableCollection<string> { "legend", "image" };

        // ✅ ДОБАВЛЕНО: Вычисляем радиусы перед созданием элементов
        CalculateRadii();

        // Добавляем числа с изометрическим эффектом 
        AddNumbersWithIsometricEffect();
        // Сохраняем ссылку на Bluetooth-сервис
        _bluetoothService = bluetoothService;
        // Запускаем прием данных от Bluetooth-сервиса
        _bluetoothService.ReceiverData();


        // Подписываемся на событие получения данных из Bluetooth-сервиса - для обработки данных и обновления терминала
        _bluetoothService.DataReceived += async (string rx) =>
        {
            // 1. Парсим ток из строки по шаблону "ток: XXXmA"
            var match = Regex.Match(rx, @"ток:\s*([0-9.]+)mA", RegexOptions.IgnoreCase);
            if (!match.Success)
                return;

            string decimalString = match.Groups[1].Value;
            if (!double.TryParse(
                decimalString,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double valueMa
            ))
                return;

            // 2. делим на 10 по протоколу
            valueMa = valueMa / 10.0;

            // ====== 1. МАССОВОЕ СЧИТЫВАНИЕ (цикл "все каналы") ======
            if (channelReadCompletionSource != null)
            {
                int channelNum = currentChannelIndex.HasValue ? (currentChannelIndex.Value + 1) : -1;
                string message = channelNum > 0
                    ? $"Струм шлейфа №{channelNum}: {valueMa.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} mA"
                    : $"Струм шлейфа (невід.): {valueMa.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} mA";
                AddTerminalText(message);

                channelReadCompletionSource.TrySetResult(valueMa);
                channelReadCompletionSource = null;
                return;
            }

            // ====== 2. ОДИНОЧНОЕ СЧИТЫВАНИЕ (по клику пользователя) ======
            if (selectedLoopIndex.HasValue)
            {
                int channelNum = selectedLoopIndex.Value + 1;
                string message = $"Струм шлейфа №{channelNum}: {valueMa.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} mA";
                AddTerminalText(message);

                UpdateSingleValue(selectedLoopIndex.Value, valueMa);
                await ShowNumberInCenterAndFreeze(selectedLoopIndex.Value);
                selectedLoopIndex = null;
                return;
            }

            // ====== 3. Неожиданный пакет ======
            AddTerminalText($"Струм отримано: {valueMa.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} mA");
        };
      

        // Подписываемся на событие получения данных из Bluetooth-сервиса - для записи в терминал
        //_bluetoothService.DataReceived  +=  delegate(string rx)
        //{      
        //        AddTerminalText(rx);          
        //};
        // 
        Settingpage_DataReceived += async delegate (string rx)
        {
            AddTerminalText(rx);
        };


    }


    // появление страницы
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // ✅ ДОБАВИТЬ: Динамическая высота для ScrollView
        double screenHeight = DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;
        LayoutContainer.HeightRequest = screenHeight * 0.9; // В 1.5 раза больше экрана

        if (!isAnimating)
        {

            StartRotationWithPopup();
        }

    }
    //  уход со страницы
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopRotationAnimation();

    }




    //private void OnPedestalSwiped(object sender, PositionChangedEventArgs e)
    //{
    //    // Пример — действия при свайпе между страницами в CarouselView
    //    // Можно реагировать на e.CurrentPosition (0 или 1)
    //    if (e.CurrentPosition == 0)
    //    {
    //        // Первая картинка/страница
    //    }
    //    else if (e.CurrentPosition == 1)
    //    {
    //        // Вторая картинка/страница
    //    }
    //}






    //  Новый метод для расчёта адаптивных радиусов - для разных размеров экранов
    private void CalculateRadii()
    {
        // Получаем ширину экрана в device-independent units
        screenWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;

        // Вычисляем радиусы пропорционально ширине экрана
        // radiusX = 40% от ширины (на телефоне ~143, на планшете ~318)
        // radiusY = 7% от ширины (на телефоне ~25, на планшете ~55)
        radiusX = screenWidth * 0.44;//0.45
        radiusY = screenWidth * 0.087;//0.08
    }




    // Обработчик меню
    // ============ ОБРАБОТЧИКИ МЕНЮ ============

    // Кнопка ⚙ (Настройки)
    [Obsolete]
    private async void OnSettingsClicked(object sender, EventArgs e)
    {

        string choice = await DisplayActionSheet(
            "Налаштування",
            "Назад",
            null,
            "Струми шлейфів",
            "Конфігурація",
            "Налаштування Bluetooth",
            "Налаштування дисплею",
            "Про програму"
        );

        switch (choice)
        {
            case "Струми шлейфів":
                await DisplayAlert("Загальні", "Загальні налаштування", "OK");
                break;

            case "Налаштування Bluetooth":
                await Navigation.PushAsync(new AddDevicePage());
                break;

            case "Налаштування дисплею":
                await Navigation.PushAsync(new AddDevicePage(), animated: false);
                break;

            case "Про програму":
                await DisplayAlert("Про програму", "Версія 1.0\nРозробник: Ваше ім'я", "OK");
                break;
        }
    }

    // Кнопка 🔄 (Обновить)
    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Оновлення", "Дані оновлюються...", "OK");

    }


    // Метод для чтения значений по одному с использованием обработчика OnReadAllClicked - вычитать все шлейфа по очереди,
    // с ожиданием ответа от Bluetooth-сервиса для каждого шлейфа, а не просто с задержкой
    private async Task<double?> ReadSingleChannelValueAsync(string command)
    {
        // Используем локальную переменную для TaskCompletionSource
        var tcs = new TaskCompletionSource<double>();
        channelReadCompletionSource = tcs; // для обратного вызова из Bluetooth обработчика

        try
        {
            await _bluetoothService.TransmitterData(command);
        }
        catch (Exception ex)
        {
            channelReadCompletionSource = null; // обязательно сбрасываем!
            AddTerminalText($"⚠️ Помилка відпр.: {ex.Message}");
            return null;
        }

        // Используем только локальную переменную tcs для .
        var task = tcs.Task;

        if (await Task.WhenAny(task, Task.Delay(3000)) == task)
        {
            channelReadCompletionSource = null; // обязательно сбрасываем!
            return task.Result;
        }
        else
        {
            channelReadCompletionSource = null; // обязательно сбрасываем!
            AddTerminalText("⚠️ Таймаут по відповіді Bluetooth пристрою");
            return null;
        }
    }


    // Метод для обновления значения по индексу, вычитка одного значения за другим - для массового опроса всех каналов по кнопке "Зчитати всі"
    private async void OnReadAllClicked(object sender, EventArgs e)
    {

        if (isReadAllInProgress)
            return; // Уже идет массовый опрос — игнорируем клик
        isReadAllInProgress = true;

        try
        {
            AddTerminalText("📡 Зчитування всіх даних...");

            int kzCount = 0, normaCount = 0, obrivCount = 0;

            for (int i = 0; i < totalNumbers; i++)
            {
                string key = (i + 1).ToString();
                if (_commands.TryGetValue(key, out string command))
                {

                    currentChannelIndex = i;
                    var value = await ReadSingleChannelValueAsync(command); // ждем реальный результат!
                    if (value.HasValue)
                    {
                        double result = value.Value; // если нужно делить на 10, напиши здесь value.Value / 10.0
                        currentValues[i] = result;
                        UpdateLabelText(i);

                        // --- ЛОГИКА КАК В GetColorByValue ---
                        if (result == 0.0 || result > 28.0)
                            kzCount++;
                        else if (result > 0.0 && result <= 5.0)
                            obrivCount++;
                        else if (result > 5.0 && result <= 28.0)
                            normaCount++;
                        // Остальное не учитываем
                    }
                    else
                    {
                        //  currentValues[i] = double.NaN; // или метку "не пришло"
                        currentValues[i] = double.NegativeInfinity;
                        UpdateLabelText(i);
                        AddTerminalText($"⚠️ Немає відповіді від {key}");
                    }
                    await Task.Delay(100);  // плавности ради, можно убрать или увеличить
                }
            }

            AddTerminalText($"✅ Дані оновлено:");
            AddTerminalText($"   🔴 К.З.: {kzCount}");
            AddTerminalText($"   ⚪ Норма: {normaCount}");
            AddTerminalText($"   🟡 Обрив: {obrivCount}");

            await DisplayAlert("Готово",
                $"🔴 К.З.: {kzCount}\n⚪ Норма: {normaCount}\n🟡 Обрив: {obrivCount}",
                "OK");

        }

        finally
        {
            isReadAllInProgress = false; // Снимаем блокировку обязательно!
        }

    }


    // Метод для обновления текста одного лейбла по индексу - показ только одного шлейфа
    private async Task ShowNumberInCenterAndFreeze(int index)
    {
        StopRotationAnimation();

        if (index < 0 || index >= numberLabels.Count) return;
        var label = numberLabels[index];

        isPaused = true;

        var originalBounds = AbsoluteLayout.GetLayoutBounds(label);
        var originalFontSize = label.FontSize;
        var originalRotationX = label.RotationX;
        var originalRotationY = label.RotationY;
        var originalZIndex = label.ZIndex;

        label.ZIndex = 1000;

        var moveAnimation = new Animation();
        moveAnimation.Add(0, 1, new Animation(v =>
        {
            var currentBounds = AbsoluteLayout.GetLayoutBounds(label);
            var newBounds = new Rect(
                originalBounds.X + (centerX - originalBounds.X) * v,
                originalBounds.Y + (centerY - originalBounds.Y) * v,
                currentBounds.Width,
                currentBounds.Height
            );
            AbsoluteLayout.SetLayoutBounds(label, newBounds);
        }));
        moveAnimation.Add(0, 1, new Animation(v =>
        {
            label.FontSize = originalFontSize + (25 - originalFontSize) * v;
        }));
        moveAnimation.Add(0, 1, new Animation(v =>
        {
            label.RotationX = originalRotationX * (1 - v);
            label.RotationY = originalRotationY * (1 - v);
        }));

        moveAnimation.Commit(this, "PopupMove", length: 500);
        await Task.Delay(500);
    }






    // Метод для чтения одного канала по номеру из Entry
    private async void OnReadSingleClicked(object sender, EventArgs e)
    {
        string loopNumber = LoopNumberEntry.Text;

        if (string.IsNullOrWhiteSpace(loopNumber))
        {
            await DisplayAlertAsync("Помилка", "Введіть номер!", "OK");
            return;
        }

        if (_commands.TryGetValue(loopNumber, out string command))
        {
            await DisplayAlertAsync("Успіх", $"Канал {loopNumber}\nКоманда: {command}", "OK");

            if (int.TryParse(loopNumber, out int num) && num >= 1 && num <= totalNumbers)
            {
                // Запоминаем для дальнейшего использования в DataReceived
                selectedLoopIndex = num - 1;
            }
            else
            {
                selectedLoopIndex = null;
            }

            try
            {
                await _bluetoothService.TransmitterData(command);

                // НЕ вызываем ShowNumberInCenterAndFreeze здесь — вызовем после прихода данных!
             //   await (Settingpage_DataReceived?.Invoke($"Відправлено через Settingpage {loopNumber}: {command}") ?? Task.CompletedTask);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Помилка", $"Помилка: {ex.Message}", "OK");
                return;
            }
        }
        else
        {
            await DisplayAlertAsync("Помилка", $"Невірний номер каналу: {loopNumber}\nВведіть число від 1 до 16.", "OK");
        }
    }



    // ============ ОБРАБОТЧИКИ ФИЛЬТРОВ  ============
    /// Фильтр: показать только КЗ (красные)
    /// 

    private async void OnKzFilterTapped(object sender, EventArgs e)
    {
        isPaused = false;
        if (isFilterChanging) return;
        isFilterChanging = true;
        try
        {
            AddTerminalText("Фільтр: показати тільки К.З. (червоні)");
            StopRotationAnimation();
            await Task.Yield();
            await Task.Delay(350);
            // Показываем только КЗ: value == 0.0 или >28.0
            FilterByStatus(v => v == 0.0 || v > 28.0, "🔴 К.З.");
        }
        finally { isFilterChanging = false; }
    }




    //private async void OnKzFilterTapped(object sender, EventArgs e)
    //{
    //    isPaused = false;

    //    // Защита от двойного клика
    //    if (isFilterChanging) return;
    //    isFilterChanging = true;


    //    try      
    //    {

    //        AddTerminalText("Фільтр: показати тільки КЗ (червоні)");

    //        // Останавливаем анимацию
    //        StopRotationAnimation();
    //        // Все незавершённые визуальные задачи (layout, перерисовка, анимация, изменения свойств, измерения) —
    //        // получат шанс завершиться, прежде чем ты продолжишь свою логику.
    //        // гарантирует, что “первая волна” UI-перемещений и layout-ов дойдет до экрана до того, как запустится следующий код
    //        await Task.Yield();  
    //        await Task.Delay(350);  // дополнительная задержка для плавности и гарантии завершения всех UI-операций
    //       // Фильтруем: показываем только красные (>= 26 mA)
    //        FilterByColor(26.0, double.MaxValue, "🔴 КЗ");


    //    }


    //      finally
    //      {
    //     //   await Task.Delay(100);
    //        isFilterChanging = false;  // Разблокируем
    //      }


    //}

    // Фильтр: показать только обрывы (желтые)


    private async void OnObrivFilterTapped(object sender, EventArgs e)
    {
        isPaused = false;
        if (isFilterChanging) return;
        isFilterChanging = true;
        try
        {
            AddTerminalText("Фільтр: показати тільки обриви (жовті)");
            StopRotationAnimation();
            await Task.Yield();
            await Task.Delay(350);
            FilterByStatus(v => v > 0.0 && v <= 5.0, "🟡 Обрив");
        }
        finally { isFilterChanging = false; }
    }



    //private async void OnObrivFilterTapped(object sender, EventArgs e)
    //{
    //    isPaused = false;

    //    // Защита от двойного клика
    //    if (isFilterChanging) return;
    //    isFilterChanging = true;

    //    try 
    //    {
    //        AddTerminalText("Фільтр: показати тільки обриви (жовті)");

    //        // Останавливаем анимацию
    //        StopRotationAnimation();
    //        await Task.Yield();  // 
    //        await Task.Delay(350);  // 
    //        // Фильтруем: показываем только желтые (< 5 mA)
    //        FilterByColor(0.0, 5.0, "🟡 Обрив");



    //    }

    //    finally
    //    {
    //      //  await Task.Delay(100);  
    //       isFilterChanging = false;  // ✅ Разблокируем
    //    }

    //}


    // Фильтр: показать только норму (белые)
    // 

    private async void OnNormaFilterTapped(object sender, EventArgs e)
    {
        isPaused = false;
        if (isFilterChanging) return;
        isFilterChanging = true;
        try
        {
            AddTerminalText("Фільтр: показати тільки норму (білі)");
            StopRotationAnimation();
            await Task.Yield();
            await Task.Delay(350);
            FilterByStatus(v => v > 5.0 && v <= 28.0, "⚪ Норма");
        }
        finally { isFilterChanging = false; }
    }
    //private async  void OnNormaFilterTapped(object sender, EventArgs e)
    //{
    //    isPaused = false;

    //    //  Защита от двойного клика
    //    if (isFilterChanging) return;
    //    isFilterChanging = true;

    //    try 
    //    {

    //        AddTerminalText("Фільтр: показати тільки норму (білі)");

    //        // Останавливаем анимацию
    //        StopRotationAnimation();
    //        await Task.Yield();  // 
    //        await Task.Delay(350);  // 
    //        // Фильтруем: показываем только белые (5-26 mA)
    //        FilterByColor(5.0, 26.0, "⚪ Норма");

    //    }


    //    finally
    //    {
    //       // await Task.Delay(100);
    //        isFilterChanging = false;  // ✅ Разблокируем
    //    }


    //}


 
    // Показать все каналы  
    private async void OnVsiFilterTapped(object sender, EventArgs e)
    {

        isPaused = false; 

        //  Защита от двойного клика
        if (isFilterChanging) return;
        isFilterChanging = true;

        try 
        {

            AddTerminalText("Фільтр: показати всі канали");


            // Останавливаем анимацию
            StopRotationAnimation();

            await Task.Yield();  // 
            await Task.Delay(300);  //

            // Показываем все элементы
            ShowAllChannels();


            // Дополнительная проверка
            await Task.Delay(50);  // Даем время завершиться старым async методам


            // Запускаем анимацию
            if (!isAnimating)
            {
                StartRotationWithPopup();
            }

        }


        finally
        {
            isFilterChanging = false;  //Разблокируем
        }

    }



    // ============ МЕТОДЫ ФИЛЬТРАЦИИ ============
    //private void FilterByColor(double minValue, double maxValue, string statusName)
    //{
    //    int visibleCount = 0;

    //    for (int i = 0; i < numberLabels.Count; i++)
    //    {
    //        // Проверяем, есть ли данные
    //        if (currentValues[i] == null)
    //        {
    //            // Нет данных - скрываем
    //            numberLabels[i].IsVisible = false;
    //            continue;
    //        }

    //        double value = Convert.ToDouble(currentValues[i]);

    //        // Проверяем, входит ли значение в диапазон
    //        bool isInRange = value >= minValue && value < maxValue;

    //        // Показываем или скрываем элемент
    //        numberLabels[i].IsVisible = isInRange;

    //        if (isInRange)
    //        {
    //            visibleCount++;
    //        }
    //    }

    //    // Логируем результат
    //    AddTerminalText($"Знайдено {visibleCount} шлейфів у статусі: {statusName}");
    //}


    // Улучшенный метод фильтрации с поддержкой отсутствия данных и анимации для отфильтрованных элементов
    private void FilterByColor(double minValue, double maxValue, string statusName)
    {
        int visibleCount = 0;

        for (int i = 0; i < numberLabels.Count; i++)
        {
            if (currentValues[i] == null)
            {
                numberLabels[i].IsVisible = false;
                continue;
            }

            double value = Convert.ToDouble(currentValues[i]);
            bool isInRange = value >= minValue && value < maxValue;

            // Показываем или скрываем элемент
            numberLabels[i].IsVisible = isInRange;

            if (isInRange)
            {
                visibleCount++;
            }
        }
        // Устанавливаем флаг фильтрации
        isFiltered = true;  

        AddTerminalText($"Знайдено {visibleCount} шлейфів у статусі: {statusName}");

        //Запускаем анимацию для отфильтрованных
        if (visibleCount > 0 && !isAnimating)
        {
            StartFilteredRotation();
        }

        //Разблокировка
     //   isFilterChanging = false;


    }




    // Универсальный метод фильтрации с предикатом для статуса и поддержкой анимации отфильтрованных элементов
    private void FilterByStatus(Func<double, bool> predicate, string statusName)
    {
        int visibleCount = 0;

        for (int i = 0; i < numberLabels.Count; i++)
        {
            if (currentValues[i] == null)
            {
                numberLabels[i].IsVisible = false;
                continue;
            }

            double value = Convert.ToDouble(currentValues[i]);
            bool isInStatus = predicate(value);

            numberLabels[i].IsVisible = isInStatus;
            if (isInStatus)
                visibleCount++;
        }

        isFiltered = true;
        AddTerminalText($"Знайдено {visibleCount} шлейфів у статусі: {statusName}");

        if (visibleCount > 0 && !isAnimating)
        {
            StartFilteredRotation();
        }
    }


    // Новый метод для анимации только отфильтрованных элементов (показывает только видимые элементы)
    private async void StartFilteredRotation()
    {
        // Защита от одновременного запуска
        if (isAnimating)
        {
            await Task.Delay(100);  // Ждем завершения предыдущей анимации
            if (isAnimating) return;  // Если все еще работает - выходим
        }

        isAnimating = true;

        List<int> visibleIndices = new List<int>();
        for (int i = 0; i < numberLabels.Count; i++)
        {
            if (numberLabels[i].IsVisible)
            {
                visibleIndices.Add(i);
            }
        }

        if (visibleIndices.Count == 0)
        {
            AddTerminalText("⚠️ Немає елементів для відображення");
            isAnimating = false;
            return;
        }

        int currentVisibleIndex = 0;

        while (isAnimating && isFiltered)
        {
            int actualIndex = visibleIndices[currentVisibleIndex];
            await ShowPopupNumber(actualIndex);
            currentVisibleIndex = (currentVisibleIndex + 1) % visibleIndices.Count;
        }

        isAnimating = false;
    }


    //Показать все каналы
    
    private void ShowAllChannels()
    {
        for (int i = 0; i < numberLabels.Count; i++)
        {
            numberLabels[i].IsVisible = true;
        }

        isFiltered = false;  //  Сбрасываем фильтр!

        AddTerminalText($"Показано всіх {numberLabels.Count} шлейфів");
        // Разблокировка
      //  isFilterChanging = false;

    }




    // ============ ОБРАБОТЧИКИ ТЕРМИНАЛА ============  
    //Обработчик переключателя терминала

    private async void OnTerminalToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            // Включаем терминал
            TerminalBorder.IsVisible = true;
            TerminalBorder.Opacity = 0;
            await TerminalBorder.FadeTo(1, 300);

            // Приветственное сообщение
            label4.Text = $"[{DateTime.Now:HH:mm:ss}] Термінал активовано\n";
        }
        else
        {
            // Выключаем терминал
            label4.Text += $"[{DateTime.Now:HH:mm:ss}] Термінал вимкнено\n";

            await Task.Delay(300);
            await TerminalBorder.FadeTo(0, 300);
            TerminalBorder.IsVisible = false;
        }
    }

  
    // Очистить терминал
   
    private void OnClearTerminalClicked(object sender, EventArgs e)
    {
        label4.Text = $"[{DateTime.Now:HH:mm:ss}] Термінал очищено\n";
    }



    // Метод для добавления текста в терминал с проверкой на включение
    public void AddTerminalText(string text)
    {
        // ✅ Проверка на null
        if (TerminalSwitch?.IsToggled != true) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // ✅ Дополнительная проверка внутри UI-потока
            if (label4 == null || scrollView == null) return;

          //  label4.Text += $"[{DateTime.Now:HH:mm:ss}]{Environment.NewLine}{text}";
            label4.Text += $"[{DateTime.Now:HH:mm:ss}] {text}\n";
            scrollView.ScrollToAsync(label4, ScrollToPosition.End, animated: true);
        });
    }


    // Метод для получения цвета по значению 
    private Color GetColorByValue(double valueMa)
    {
        // 🔴 К.З. — больше 26 или ровно 0
        if (valueMa > 28.0 || valueMa == 0.0)
            return Colors.Red;      // КЗ

        // 🟡 Обрыв — больше 0 и <= 5
        else if (valueMa > 0.0 && valueMa <= 5.0)
            return Colors.Yellow;   // Обрыв

        // ⚪ Норма — больше 5 и <= 26
        else if (valueMa > 5.0 && valueMa <= 28.0)
            return Colors.White;    // Норма

        // Для других случаев — например, ошибки или минус
        return Colors.Silver;
    }



    // Метод для добавления чисел с изометрическим эффектом
    private void AddNumbersWithIsometricEffect()
    {
        double angleStep = 360.0 / totalNumbers;

        for (int i = 0; i < totalNumbers; i++)
        {
            double angleInRadians = (startAngle + i * angleStep) * Math.PI / 180.0;

            double x = centerX + Math.Cos(angleInRadians) * radiusX / screenWidth;
            double y = centerY + Math.Sin(angleInRadians) * radiusY / screenWidth;

            Label numberLabel = new Label
            {
                FontSize = 16,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                FontAttributes = FontAttributes.Bold,
                RotationX = -10,
                RotationY = 61,
                ZIndex = 100 + i
            };

            var formattedString = new FormattedString();

            // ✅ Номер шлейфа (синий - нет данных)
            formattedString.Spans.Add(new Span
            {
                Text = $"{i + 1}ш",
                TextColor = Colors.DeepSkyBlue,  // ✅ Синий (нет данных)
                FontAttributes = FontAttributes.Bold
            });

            // ✅ Значение: ? (данные не получены)
            formattedString.Spans.Add(new Span
            {
                Text = " = ?mA",  // ✅ Неизвестное значение!
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold
            });

            numberLabel.FormattedText = formattedString;

            AbsoluteLayout.SetLayoutBounds(numberLabel, new Rect(x, y, -1, -1));
            AbsoluteLayout.SetLayoutFlags(numberLabel, AbsoluteLayoutFlags.PositionProportional);
            LayoutContainer.Children.Add(numberLabel);
            numberLabels.Add(numberLabel);
        }
    }




    // Метод для обновления позиций чисел при вращении
    private void UpdateNumberPositions()
    {
        if (isPaused) return;

        double angleStep = 360.0 / totalNumbers;

        for (int i = 0; i < numberLabels.Count; i++)
        {
            // ✅ Пропускаем скрытые элементы
            if (!numberLabels[i].IsVisible)
                continue;

            double angleInRadians = (startAngle + currentAngle + i * angleStep) * Math.PI / 180.0;

            double x = centerX + Math.Cos(angleInRadians) * radiusX / screenWidth;
            double y = centerY + Math.Sin(angleInRadians) * radiusY / screenWidth;

            AbsoluteLayout.SetLayoutBounds(numberLabels[i], new Rect(x, y, -1, -1));
        }
    }


    // Метод для плавного вращения с показом  для каждого элемента
    private async void StartRotationWithPopup()
    {
        // ✅ ДОБАВИТЬ: Защита от одновременного запуска
        if (isAnimating)
        {
            await Task.Delay(100);  // Ждем завершения предыдущей анимации
            if (isAnimating) return;  // Если все еще работает - выходим
        }

        isAnimating = true;

        int currentIndex = 0;

        while (isAnimating && !isFiltered)
        {
            await RotateToPosition(currentIndex);
            await ShowPopupNumber(currentIndex);
            currentIndex = (currentIndex + 1) % numberLabels.Count;
        }

        isAnimating = false;
    }





    // Метод для плавного вращения к следующей позиции
    private Task RotateToPosition(int targetIndex)
    {
        var tcs = new TaskCompletionSource();

        double angleStep = 360.0 / numberLabels.Count;
        double targetAngle = currentAngle + angleStep;

        var rotationAnimation = new Animation(v =>
        {
            currentAngle = v;
            UpdateNumberPositions();
        }, currentAngle, targetAngle);

        rotationAnimation.Commit(
            this,
            "RotateToNext",
            length: 500,
            finished: (v, c) => tcs.SetResult()
        );

        return tcs.Task;
    }


    // Метод для показа всплывающего окна с числом и его анимацией
    private async Task ShowPopupNumber(int index)
    {
        // ✅ Проверка границ
        if (index < 0 || index >= numberLabels.Count)
            return;

        var label = numberLabels[index];

        // ✅ Проверка видимости
        if (!label.IsVisible)
            return;

        // ✅ Проверка анимации
        if (!isAnimating)
            return;

        isPaused = true;

        var originalBounds = AbsoluteLayout.GetLayoutBounds(label);
        var originalFontSize = label.FontSize;
        var originalRotationX = label.RotationX;
        var originalRotationY = label.RotationY;
        var originalZIndex = label.ZIndex;

        label.ZIndex = 1000;

        var moveAnimation = new Animation();

        moveAnimation.Add(0, 1, new Animation(v =>
        {
            var currentBounds = AbsoluteLayout.GetLayoutBounds(label);
            var newBounds = new Rect(
                originalBounds.X + (centerX - originalBounds.X) * v,
                originalBounds.Y + (centerY - originalBounds.Y) * v,
                currentBounds.Width,
                currentBounds.Height
            );
            AbsoluteLayout.SetLayoutBounds(label, newBounds);
        }));

        moveAnimation.Add(0, 1, new Animation(v =>
        {
            label.FontSize = originalFontSize + (25 - originalFontSize) * v;
        }));

        moveAnimation.Add(0, 1, new Animation(v =>
        {
            label.RotationX = originalRotationX * (1 - v);
            label.RotationY = originalRotationY * (1 - v);
        }));

        moveAnimation.Commit(this, "PopupMove", length: 500);
        await Task.Delay(500);

        if (!isAnimating)
        {
            AbsoluteLayout.SetLayoutBounds(label, originalBounds);
            label.FontSize = originalFontSize;
            label.RotationX = originalRotationX;
            label.RotationY = originalRotationY;
            label.ZIndex = originalZIndex;
            isPaused = false;
            return;
        }

        // ✅ ИЗМЕНЕНО: Проверяем isAnimating каждые 100ms
        int elapsed = 0;
        int checkInterval = 50;

        while (elapsed < displayDuration && isAnimating)
        {
            await Task.Delay(checkInterval);
            elapsed += checkInterval;
        }

        if (!isAnimating)
        {
            AbsoluteLayout.SetLayoutBounds(label, originalBounds);
            label.FontSize = originalFontSize;
            label.RotationX = originalRotationX;
            label.RotationY = originalRotationY;
            label.ZIndex = originalZIndex;
            isPaused = false;
            return;
        }

        var returnAnimation = new Animation();

        returnAnimation.Add(0, 1, new Animation(v =>
        {
            var newBounds = new Rect(
                centerX + (originalBounds.X - centerX) * v,
                centerY + (originalBounds.Y - centerY) * v,
                -1,
                -1
            );
            AbsoluteLayout.SetLayoutBounds(label, newBounds);
        }));

        returnAnimation.Add(0, 1, new Animation(v =>
        {
            label.FontSize = 25 + (originalFontSize - 25) * v;
        }));

        returnAnimation.Add(0, 1, new Animation(v =>
        {
            label.RotationX = originalRotationX * v;
            label.RotationY = originalRotationY * v;
        }));

        returnAnimation.Commit(this, "PopupReturn", length: 500);
        await Task.Delay(500);

        if (!isAnimating)
        {
            AbsoluteLayout.SetLayoutBounds(label, originalBounds);
            label.FontSize = originalFontSize;
            label.RotationX = originalRotationX;
            label.RotationY = originalRotationY;
            label.ZIndex = originalZIndex;
            isPaused = false;
            return;
        }

        label.ZIndex = originalZIndex;
        isPaused = false;
    }




    /// Метод для остановки анимации вращения

    public  void StopRotationAnimation()
    {
        isAnimating = false;
        isFiltered = false;
        isPaused = false;  // ✅ ДОБАВИТЬ: Сбрасываем паузу

        // Останавливаем все анимации
        this.AbortAnimation("RotateToNext");
        this.AbortAnimation("PopupMove");
        this.AbortAnimation("PopupReturn");

        // Возвращаем ВСЕ элементы на эллипс!
        // await   ResetAllElementsToEllipse();
        ResetAllElementsToEllipse();

    }

   

    // Возвращает все элементы на их позиции на эллипсе

    private void ResetAllElementsToEllipse()
    {
        double angleStep = 360.0 / totalNumbers;

        for (int i = 0; i < numberLabels.Count; i++)
        {
            var label = numberLabels[i];

            // Вычисляем правильную позицию на эллипсе
            double angleInRadians = (startAngle + currentAngle + i * angleStep) * Math.PI / 180.0;
            double x = centerX + Math.Cos(angleInRadians) * radiusX / screenWidth;
            double y = centerY + Math.Sin(angleInRadians) * radiusY / screenWidth;

            // Возвращаем элемент на эллипс
            AbsoluteLayout.SetLayoutBounds(label, new Rect(x, y, -1, -1));

            // Восстанавливаем оригинальные параметры
            label.FontSize = 16;       // ✅ Оригинальный размер
            label.RotationX = -10;     // ✅ Оригинальный наклон
            label.RotationY = 61;      // ✅ Оригинальный поворот
            label.ZIndex = 100 + i;    // ✅ Оригинальный Z-index
        }
    }




    // Метод для установки длительности отображения выезжающего числа
    public void SetDisplayDuration(int milliseconds)
    {
        displayDuration = milliseconds;
    }



    // Метод для обновления текста конкретного числа (новый метод с поддержкой отсутствия данных)
    private void UpdateLabelText(int index)
    {
        var formattedString = new FormattedString();

        // ✅ Проверяем, есть ли данные
        if (currentValues[index] == null)
        {
            // Данных нет - показываем "?"
            formattedString.Spans.Add(new Span
            {
                Text = $"{index + 1}ш",
                TextColor = Colors.DeepSkyBlue,  // Синий (нет данных)
                FontAttributes = FontAttributes.Bold
            });

            formattedString.Spans.Add(new Span
            {
                Text = " = ?mA",
                TextColor = Colors.Gray,
                FontAttributes = FontAttributes.Bold
            });
        }
        else
        {
            // Данные есть - определяем цвет
            double value = Convert.ToDouble(currentValues[index]);
            Color loopColor = GetColorByValue(value);

            formattedString.Spans.Add(new Span
            {
                Text = $"{index + 1}ш",
                TextColor = loopColor,  // ✅ Цвет по значению!
                FontAttributes = FontAttributes.Bold
            });

            formattedString.Spans.Add(new Span
            {
                Text = $"={value:F1}mA",
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold
            });
        }

        numberLabels[index].FormattedText = formattedString;
    }





    // Метод для обновления всех чисел новыми значениями
    public void UpdateValues(object[] newValues)
    {
        if (newValues?.Length == totalNumbers)
        {
            currentValues = newValues;
            for (int i = 0; i < numberLabels.Count; i++)
            {
                UpdateLabelText(i);
            }
        }
    }
    // Метод для обновления одного числа новым значением
    public void UpdateSingleValue(int index, double newValue)
    {
        if (index >= 0 && index < totalNumbers)
        {
            currentValues[index] = newValue;
            UpdateLabelText(index);
        }
    }
}



















///////////////////

//// Подписываемся на событие получения данных из Bluetooth-сервиса - для обработки данных и обновления терминала
//_bluetoothService.DataReceived += async (string rx) =>
//{
//    AddTerminalText(rx);

//    var match = Regex.Match(rx, @"ток:\s*([0-9.]+)mA", RegexOptions.IgnoreCase);
//    if (!match.Success)
//        return;

//    string decimalString = match.Groups[1].Value;
//    if (!double.TryParse(
//        decimalString,
//        System.Globalization.NumberStyles.Any,
//        System.Globalization.CultureInfo.InvariantCulture,
//        out double valueMa
//    ))
//        return;

//    // --- Всегда делим на 10 по требованиям протокола ---
//    valueMa = valueMa / 10.0;


//    // Формируем красивый вывод (с точкой)
//    //string message = $"Ток каналу: {valueMa.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} mA";

//    //AddTerminalText(message);



//    // ======== 1. МАССОВОЕ СЧИТЫВАНИЕ (через TaskCompletionSource) =========
//    if (channelReadCompletionSource != null)
//    {
//        channelReadCompletionSource.TrySetResult(valueMa);
//        channelReadCompletionSource = null;
//        return;
//    }

//    // ======== 2. ОДИНОЧНОЕ СЧИТЫВАНИЕ =========
//    if (selectedLoopIndex.HasValue)
//    {
//        UpdateSingleValue(selectedLoopIndex.Value, valueMa);
//        await ShowNumberInCenterAndFreeze(selectedLoopIndex.Value);
//        selectedLoopIndex = null;
//        return;
//    }





//};







////////////////////////////
///////////////////////

// инициализация — один раз в коде
//_bluetoothService.DataReceived += async (string rx) =>
//{
//    AddTerminalText(rx);

//    var match = Regex.Match(rx, @"ток:\s*([0-9.]+)mA", RegexOptions.IgnoreCase);
//    if (match.Success && selectedLoopIndex.HasValue)
//    {
//        string decimalString = match.Groups[1].Value;
//        if (double.TryParse(decimalString, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double valueMa))
//        {


//            valueMa = valueMa / 10.0;  // ← Преобразуем значение!



//            // Обновляем нужный шлейф данными
//            UpdateSingleValue(selectedLoopIndex.Value, valueMa);

//            // Показываем и анимируем выезд в центр
//            await ShowNumberInCenterAndFreeze(selectedLoopIndex.Value);

//            // потом сбрасываем, чтобы не реагировать на каждый следующий канал
//            selectedLoopIndex = null;
//        }
//    }
//};



//private async void OnReadAllClicked(object sender, EventArgs e)
//{
//    AddTerminalText("📡 Зчитування всіх даних...");

//    int kzCount = 0, normaCount = 0, obrivCount = 0;

//    for (int i = 0; i < totalNumbers; i++)
//    {
//        string key = (i + 1).ToString();
//        if (_commands.TryGetValue(key, out string command))
//        {
//            var value = await ReadSingleChannelValueAsync(command); // ждем реальный результат!
//            if (value.HasValue)
//            {
//                currentValues[i] = value.Value;
//                UpdateLabelText(i);

//                if (value >= 26.0)
//                    kzCount++;
//                else if (value >= 5.0)
//                    normaCount++;
//                else
//                    obrivCount++;
//            }
//            else
//            {
//                currentValues[i] = double.NaN; // или метку "не пришло"
//                UpdateLabelText(i);
//                AddTerminalText($"⚠️ Немає відповіді від {key}");
//            }
//            await Task.Delay(100);  // плавности ради, можно убрать или увеличить
//        }
//    }

//    AddTerminalText($"✅ Дані оновлено:");
//    AddTerminalText($"   🔴 КЗ: {kzCount}");
//    AddTerminalText($"   ⚪ Норма: {normaCount}");
//    AddTerminalText($"   🟡 Обрив: {obrivCount}");

//    await DisplayAlert("Готово",
//        $"🔴 КЗ: {kzCount}\n⚪ Норма: {normaCount}\n🟡 Обрив: {obrivCount}",
//        "OK");
//}



// Сгенерировать случайные данные для всех каналов (для демонстрации)
//private async void OnReadAllClicked(object sender, EventArgs e)
//{
//    AddTerminalText("📡 Зчитування всіх даних...");

//    Random random = new Random();
//    int kzCount = 0, normaCount = 0, obrivCount = 0;

//    for (int i = 0; i < totalNumbers; i++)
//    {
//        int type = random.Next(0, 3);
//        double value;

//        if (type == 0)
//        {
//            value = 26.0 + random.NextDouble() * 9.0;  // 🔴 КЗ
//            kzCount++;
//        }
//        else if (type == 1)
//        {
//            value = 5.0 + random.NextDouble() * 20.0;  // ⚪ Норма
//            normaCount++;
//        }
//        else
//        {
//            value = random.NextDouble() * 5.0;  // 🟡 Обрыв
//            obrivCount++;
//        }

//        currentValues[i] = value;
//        UpdateLabelText(i);
//        await Task.Delay(100);  // Плавное обновление
//    }

//    AddTerminalText($"✅ Дані оновлено:");
//    AddTerminalText($"   🔴 КЗ: {kzCount}");
//    AddTerminalText($"   ⚪ Норма: {normaCount}");
//    AddTerminalText($"   🟡 Обрив: {obrivCount}");

//    await DisplayAlert("Готово",
//        $"🔴 КЗ: {kzCount}\n⚪ Норма: {normaCount}\n🟡 Обрив: {obrivCount}",
//        "OK");
//}






//private async void OnReadSingleClicked(object sender, EventArgs e)
//{
//    string loopNumber = LoopNumberEntry.Text;

//    if (string.IsNullOrWhiteSpace(loopNumber))
//    {
//        await DisplayAlertAsync("Помилка", "Введіть номер!", "OK");
//        return;
//    }

//    if (_commands.TryGetValue(loopNumber, out string command))
//    {
//        await DisplayAlertAsync("Успіх", $"Канал {loopNumber}\nКоманда: {command}", "OK");

//        try
//        {
//            await _bluetoothService.TransmitterData(command);

//            // ↓↓↓ Показываем выбранный шлейф в центре и останавливаем анимацию ↓↓↓
//            if (int.TryParse(loopNumber, out int num) && num >= 1 && num <= totalNumbers)
//            {
//                await ShowNumberInCenterAndFreeze(num - 1);
//            }

//            await (Settingpage_DataReceived?.Invoke($"Відправлено через Settingpage {loopNumber}: {command}") ?? Task.CompletedTask);
//        }
//        catch (Exception ex)
//        {
//            await DisplayAlertAsync("Помилка", $"Помилка: {ex.Message}", "OK");
//            return;
//        }
//    }
//    else
//    {
//        await DisplayAlertAsync("Помилка", $"Невірний номер каналу: {loopNumber}\nВведіть число від 1 до 16.", "OK");
//    }
//}


///////////////


// Считать данные для одного канала (по номеру из Entry)
//private async void OnReadSingleClicked(object sender, EventArgs e)
//{
//    //   await DisplayPromptAsync("Зчитування", "Введіть номер каналу (1-16):", "OK", "Скасувати", "1");

//    // Читаем значение из Entry
//    string loopNumber = LoopNumberEntry.Text;

//    // 2. Проверяем на пустоту
//    if (string.IsNullOrWhiteSpace(loopNumber))
//    {
//        await DisplayAlertAsync("Помилка", "Введіть номер!", "OK");
//        return;
//    }
//    //  Проверяем, что введено число от 1 до 16 и получаем соответствующую команду
//    if (_commands.TryGetValue(loopNumber, out string command))
//    {
//        // Команда найдена!
//        await DisplayAlertAsync("Успіх", $"Канал {loopNumber}\nКоманда: {command}", "OK");

//        try
//        {
//            // передаем команду на Bluetooth-сервис
//            await _bluetoothService.TransmitterData(command);

//            await (Settingpage_DataReceived?.Invoke($"Відправлено через Settingpage {loopNumber}: {command}") ?? Task.CompletedTask);
//        }
//        catch (Exception ex)
//        {
//            await DisplayAlertAsync("Помилка", $"Помилка: {ex.Message}", "OK");
//            return;
//        }

//        // await (Settingpage_DataReceived?.Invoke($"Відправлено через Settingpage {loopNumber}: {command}") ?? Task.CompletedTask);

//    }

//    else
//    {
//        // Команда не найдена
//        await DisplayAlertAsync("Помилка", $"Невірний номер каналу: {loopNumber}\nВведіть число від 1 до 16.", "OK");
//    }



//}







/////////////////////////////////////




//private void AddNumbersWithIsometricEffect()
//{
//    double angleStep = 360.0 / totalNumbers;

//    for (int i = 0; i < totalNumbers; i++)
//    {
//        double angleInRadians = (startAngle + i * angleStep) * Math.PI / 180.0;

//        double x = centerX + Math.Cos(angleInRadians) * radiusX / screenWidth;
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / screenWidth;

//        Label numberLabel = new Label
//        {
//            FontSize = 15,
//            HorizontalTextAlignment = TextAlignment.Center,
//            VerticalTextAlignment = TextAlignment.Center,
//            FontAttributes = FontAttributes.Bold,
//            RotationX = -10,
//            RotationY = 63,
//            ZIndex = 100 + i
//        };

//        var formattedString = new FormattedString();

//        // ✅ НОВОЕ: Определяем цвет по значению тока
//        double value = Convert.ToDouble(currentValues[i]);
//        Color loopColor = GetColorByValue(value);

//        // Номер шлейфа (цвет зависит от тока)
//        formattedString.Spans.Add(new Span
//        {
//            Text = $"{i + 1}ш",
//            TextColor = loopColor,  // ✅ Динамический цвет!
//            FontAttributes = FontAttributes.Bold
//        });

//        // Значение тока (всегда белый)
//        formattedString.Spans.Add(new Span
//        {
//            Text = $" = {currentValues[i]:F2}mA",
//            TextColor = Colors.White,
//            FontAttributes = FontAttributes.Bold
//        });

//        numberLabel.FormattedText = formattedString;

//        AbsoluteLayout.SetLayoutBounds(numberLabel, new Rect(x, y, -1, -1));
//        AbsoluteLayout.SetLayoutFlags(numberLabel, AbsoluteLayoutFlags.PositionProportional);
//        LayoutContainer.Children.Add(numberLabel);
//        numberLabels.Add(numberLabel);
//    }
//}






// ✅ ИЗМЕНЕНО: Использует динамические radiusX и radiusY вместо фиксированных - старый метод оставлен для сравнения
//private void AddNumbersWithIsometricEffect()
//{
//    double angleStep = 360.0 / totalNumbers;

//    for (int i = 0; i < totalNumbers; i++)
//    {
//        double angleInRadians = (startAngle + i * angleStep) * Math.PI / 180.0;

//        // ✅ ИЗМЕНЕНО: Деление на screenWidth вместо фиксированного 300
//        double x = centerX + Math.Cos(angleInRadians) * radiusX / screenWidth;
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / screenWidth;

//        Label numberLabel = new Label
//        {
//            FontSize = 16,
//            HorizontalTextAlignment = TextAlignment.Center,
//            VerticalTextAlignment = TextAlignment.Center,
//            FontAttributes = FontAttributes.Bold,
//            RotationX = -10,
//            RotationY = 63,
//            ZIndex = 100 + i
//        };

//        var formattedString = new FormattedString();

//        formattedString.Spans.Add(new Span
//        {
//            Text = $"{i + 1}ш",
//            TextColor = Colors.AliceBlue,
//            FontAttributes = FontAttributes.Bold
//        });

//        formattedString.Spans.Add(new Span
//        {
//            Text = $" = {currentValues[i]:F2}mA",
//            TextColor = Colors.White,
//            FontAttributes = FontAttributes.Bold
//        });

//        numberLabel.FormattedText = formattedString;

//        AbsoluteLayout.SetLayoutBounds(numberLabel, new Rect(x, y, -1, -1));
//        AbsoluteLayout.SetLayoutFlags(numberLabel, AbsoluteLayoutFlags.PositionProportional);
//        LayoutContainer.Children.Add(numberLabel);
//        numberLabels.Add(numberLabel);
//    }
//}




// ✅ ИЗМЕНЕНО: Использует динамические radiusX и radiusY вместо фиксированных
//private void UpdateNumberPositions()
//{
//    if (isPaused) return;

//    double angleStep = 360.0 / totalNumbers;

//    for (int i = 0; i < numberLabels.Count; i++)
//    {
//        double angleInRadians = (startAngle + currentAngle + i * angleStep) * Math.PI / 180.0;

//        // ✅ ИЗМЕНЕНО: Деление на screenWidth вместо фиксированного 300
//        double x = centerX + Math.Cos(angleInRadians) * radiusX / screenWidth;
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / screenWidth;

//        AbsoluteLayout.SetLayoutBounds(numberLabels[i], new Rect(x, y, -1, -1));
//    }
//}




//private void UpdateLabelText(int index)
//{
//    var formattedString = new FormattedString();

//    // ✅ НОВОЕ: Определяем цвет по значению тока
//    double value = Convert.ToDouble(currentValues[index]);
//    Color loopColor = GetColorByValue(value);

//    // Номер шлейфа (цвет зависит от тока)
//    formattedString.Spans.Add(new Span
//    {
//        Text = $"{index + 1}Ш",
//        TextColor = loopColor,  // ✅ Динамический цвет!
//        FontAttributes = FontAttributes.Bold
//    });

//    // Значение тока (всегда белый)
//    formattedString.Spans.Add(new Span
//    {
//        Text = $" = {currentValues[index]:F2}mA",
//        TextColor = Colors.White,  // ✅ Изменено с Lime на White
//        FontAttributes = FontAttributes.Bold
//    });

//    numberLabels[index].FormattedText = formattedString;
//}












// Метод для обновления текста конкретного числа (старый метод оставлен для сравнения)
//private void UpdateLabelText(int index)
//{
//    var formattedString = new FormattedString();

//    formattedString.Spans.Add(new Span
//    {
//        Text = $"{index + 1}Ш",
//        TextColor = Colors.Tomato,
//        FontAttributes = FontAttributes.Bold
//    });

//    formattedString.Spans.Add(new Span
//    {
//        Text = $" = {currentValues[index]:F2}mA",
//        TextColor = Colors.Lime,
//        FontAttributes = FontAttributes.Bold
//    });

//    numberLabels[index].FormattedText = formattedString;
//}








//////////////////////////////////////
















////////////////////////////////////
///

//✅ ПРАВИЛЬНОЕ ПОНИМАНИЕ:
//C#
//Settingpage_DataReceived?.Invoke(...)
////└──────────┬──────────┘ └──┬──┘
////      событие           триггер
////  (список делегатов)   (вызов всех)
//Это:

//✅ Триггер(вызов) события
//✅ Событие внутри содержит список делегатов(подписчиков)
//✅ Invoke запускает ВСЕ делегаты из списка




/////////////////////////////


// Кнопка 🏠 (Домой)
//private async void OnHomeClicked(object sender, EventArgs e)
//{
//    bool confirm = await DisplayAlert(
//        "Підтвердження",
//        "Повернутися на головну сторінку?",
//        "Так",
//        "Ні"
//    );

//    if (confirm)
//    {
//        await Navigation.PopAsync(); // Вернуться на предыдущую страницу
//    }
//}

// ============ АНИМАЦИЯ И ОБНОВЛЕНИЕ ЧИСЕЛ ============
//private void AddNumbersWithIsometricEffect()
//{
//    double radiusX = 143;//144
//    double radiusY = 25;//24
//    double angleStep = 360.0 / totalNumbers;

//    for (int i = 0; i < totalNumbers; i++)
//    {
//        double angleInRadians = (startAngle + i * angleStep) * Math.PI / 180.0;
//        double x = centerX + Math.Cos(angleInRadians) * radiusX / 300.0;
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / 300.0;

//        Label numberLabel = new Label
//        {
//            FontSize = 17,
//            HorizontalTextAlignment = TextAlignment.Center,
//            VerticalTextAlignment = TextAlignment.Center,
//            FontAttributes = FontAttributes.Bold,
//            RotationX = -10,
//            RotationY = 63,
//            ZIndex = 100 + i
//        };

//        // Создаем FormattedString для разноцветного текста
//        var formattedString = new FormattedString();

//        // Номер (красный)
//        formattedString.Spans.Add(new Span
//        {
//            Text = $"{i + 1}ш",
//            TextColor = Colors.Blue,
//            FontAttributes = FontAttributes.Bold
//        });

//        // Знак равно и значение (зеленый)
//        formattedString.Spans.Add(new Span
//        {
//            Text = $" = {currentValues[i]:F3}mA",
//            TextColor = Colors.White,
//            FontAttributes = FontAttributes.Bold
//        });

//        numberLabel.FormattedText = formattedString;

//        AbsoluteLayout.SetLayoutBounds(numberLabel, new Rect(x, y, -1, -1));
//        AbsoluteLayout.SetLayoutFlags(numberLabel, AbsoluteLayoutFlags.PositionProportional);
//        LayoutContainer.Children.Add(numberLabel);
//        numberLabels.Add(numberLabel);
//    }
//}




// Метод для обновления позиций чисел при вращении
//private void UpdateNumberPositions()
//{
//    if (isPaused) return;

//    double radiusX = 143;
//    double radiusY = 25;
//    double angleStep = 360.0 / totalNumbers;

//    for (int i = 0; i < numberLabels.Count; i++)
//    {
//        double angleInRadians = (startAngle + currentAngle + i * angleStep) * Math.PI / 180.0;
//        double x = centerX + Math.Cos(angleInRadians) * radiusX / 300.0;
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / 300.0;

//        AbsoluteLayout.SetLayoutBounds(numberLabels[i], new Rect(x, y, -1, -1));
//    }
//}










/////////////////////////////////

//////////////////////////////////
///
//рабочий метод
//private void AddNumbersWithIsometricEffect()
//{
//    // Радиус эллипса для размещения цифр
//    double radiusX = 125; // Горизонтальный радиус эллипса
//    double radiusY = 25;  // Вертикальный радиус эллипса
//    double centerX = 0.5; // Центр эллипса по оси X
//    double centerY = 0.32; // Центр эллипса по оси Y
//    int totalNumbers = 20; // Количество значений
//    double angleStep = 360.0 / totalNumbers; // Угол между числами в градусах
//    double startAngle = 180; // Начальный угол смещения

//    // Добавляем 20 значений тока
//    for (int i = 0; i < totalNumbers; i++)
//    {
//        // Угол текущего числа в радианах (с учетом начального смещения)
//        double angleInRadians = (startAngle + i * angleStep) * Math.PI / 180.0;

//        // Рассчёт позиции числа на верхнем крае эллипса
//        double x = centerX + Math.Cos(angleInRadians) * radiusX / 300.0; // X-координата
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / 300.0; // Y-координата

//        // Создаём числовую метку
//        Label numberLabel = new Label
//        {
//            Text = $"{currentValues[i]:F2} mA", // Текст: "X.XX mA"
//            FontSize = 15,                       // Размер текста (уменьшил для длинного текста)
//            TextColor = Colors.Lime,             // Цвет текста
//            HorizontalTextAlignment = TextAlignment.Center,
//            VerticalTextAlignment = TextAlignment.End, // Основание - вниз
//            FontAttributes = FontAttributes.Bold,

//            // Наклон внутрь (изометрический)
//            RotationX = -10,
//            RotationY = 75
//        };

//        // Устанавливаем расположение чисел
//        AbsoluteLayout.SetLayoutBounds(numberLabel, new Rect(x, y, -1, -1)); // Позиция
//        AbsoluteLayout.SetLayoutFlags(numberLabel, AbsoluteLayoutFlags.PositionProportional); // Пропорциональное позиционирование

//        // Добавляем число в контейнер
//        LayoutContainer.Children.Add(numberLabel);
//    }
//}




//изометрия по часовой стрелке
//private void AddNumbersWithIsometricEffect()
//{
//    // Радиус эллипса для размещения цифр
//    double radiusX = 125; // Горизонтальный радиус эллипса
//    double radiusY = 25;  // Вертикальный радиус эллипса
//    double centerX = 0.5; // Центр эллипса по оси X
//    double centerY = 0.32; // Центр эллипса по оси Y
//    int totalNumbers = 20; // Количество цифр
//    int startNumber = 1; // Первая цифра
//    double angleStep = 360.0 / totalNumbers; // Угол между числами в градусах
//    double startAngle = 180; // ДОБАВИЛ: начальный угол смещения (цифра 1 слева)

//    // Добавляем 20 чисел
//    for (int i = 0; i < totalNumbers; i++)
//    {
//        // Угол текущего числа в радианах (с учетом начального смещения)
//        double angleInRadians = (startAngle + i * angleStep) * Math.PI / 180.0;

//        // Рассчёт позиции числа на верхнем крае ��ллипса
//        double x = centerX + Math.Cos(angleInRadians) * radiusX / 300.0; // X-координата чисел
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / 300.0; // Y-координата чисел

//        // Создаём числовую метку
//        Label numberLabel = new Label
//        {
//            Text = (startNumber + i).ToString(), // Текст текущего числа
//            FontSize = 16,                        // Размер текста
//            TextColor = Colors.Lime,              // Цвет текста
//            HorizontalTextAlignment = TextAlignment.Center,
//            VerticalTextAlignment = TextAlignment.End, // Основание - вниз
//            FontAttributes = FontAttributes.Bold,

//            // Наклон внутрь (изометрический)
//            RotationX = -10,
//            RotationY = 70
//        };

//        // Устанавливаем расположение чисел
//        AbsoluteLayout.SetLayoutBounds(numberLabel, new Rect(x, y, -1, -1)); // Позиция
//        AbsoluteLayout.SetLayoutFlags(numberLabel, AbsoluteLayoutFlags.PositionProportional); // Пропорциональное позиционирование

//        // Добавляем число в контейнер
//        LayoutContainer.Children.Add(numberLabel);
//    }
//}



//cтарая версия
//private void AddNumbersWithIsometricEffect()
//{
//    // Радиус эллипса для размещения цифр
//    double radiusX = 125; // Горизонтальный радиус эллипса
//    double radiusY = 25;  // Вертикальный радиус эллипса
//    double centerX = 0.5; // Центр эллипса по оси X
//    double centerY = 0.32; // Центр эллипса по оси Y
//    int totalNumbers = 20; // Количество цифр
//    int startNumber = 1; // Первая цифра
//    double angleStep = 360.0 / totalNumbers; // Угол между числами в градусах

//    // Добавляем 20 чисел
//    for (int i = 0; i < totalNumbers; i++)
//    {
//        // Угол текущего числа в радианах
//        double angleInRadians = i * angleStep * Math.PI / 180.0;

//        // Рассчёт позиции числа на верхнем крае эллипса
//        double x = centerX + Math.Cos(angleInRadians) * radiusX / 300.0; // X-координата чисел
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / 300.0; // Y-координата чисел

//        // Создаём числовую метку
//        Label numberLabel = new Label
//        {
//            Text = (startNumber + i).ToString(), // Текст текущего числа
//            FontSize = 16,                        // Размер текста
//            TextColor = Colors.Lime,              // Цвет текста
//            HorizontalTextAlignment = TextAlignment.Center,
//            VerticalTextAlignment = TextAlignment.End, // Основание - вниз
//            FontAttributes = FontAttributes.Bold,

//            // Наклон внутрь (изометрический)
//            RotationX = -10,// Изометрический наклонRotationX = -40
//            RotationY =   70
//        };

//        // Устанавливаем расположение чисел
//        AbsoluteLayout.SetLayoutBounds(numberLabel, new Rect(x, y, -1, -1)); // Позиция
//        AbsoluteLayout.SetLayoutFlags(numberLabel, AbsoluteLayoutFlags.PositionProportional); // Пропорциональное позиционирование

//        // Добавляем число в контейнер
//        LayoutContainer.Children.Add(numberLabel);
//    }
//}





///////////////////////////

//эффект изометрии
//private void AddNumbersWithIsometricEffect()
//{
//    // Радиус эллипса для размещения цифр
//    double radiusX = 125; // Горизонтальный радиус эллипса 140
//    double radiusY = 25;  // Вертикальный радиус эллипса 50
//    double centerX = 0.5; // Центр эллипса по оси X
//    double centerY = 0.32; // Центр эллипса по оси Y
//    int totalNumbers = 20; // Количество чисел
//    int startNumber = 1;   // Начальное число
//    double angleStep = 360.0 / totalNumbers; // Угол между числами в градусах

//    for (int i = 0; i < totalNumbers; i++)
//    {
//        // Угол текущего числа в радианах
//        double angleInRadians = i * angleStep * Math.PI / 180.0;

//        // Координаты числа на эллипсе
//        double x = centerX + Math.Cos(angleInRadians) * radiusX / 300.0; // X координата
//        double y = centerY + Math.Sin(angleInRadians) * radiusY / 300.0; // Y координата

//        // Угол поворота числа к центру (в градусах)
//        double angleToCenter = i * angleStep;

//        // Создаём метку для числа
//        Label numberLabel = new Label
//        {
//            Text = (startNumber + i).ToString(), // Текст текущего числа
//            FontSize = 16,                        // Размер текста
//            TextColor = Colors.Lime,              // Цвет текста
//            HorizontalTextAlignment = TextAlignment.Center,
//            VerticalTextAlignment = TextAlignment.End, // Основание числа вниз
//            FontAttributes = FontAttributes.Bold,

//            // Изометрический эффект
//            RotationX = -30,                      // Изометрический наклон вниз
//            RotationY = angleToCenter - 90        // Поворот вокруг вертикальной оси к центру
//        };

//        // Установить положение числа на эллипсе
//        AbsoluteLayout.SetLayoutBounds(numberLabel, new Rect(x, y, -1, -1));
//        AbsoluteLayout.SetLayoutFlags(numberLabel, AbsoluteLayoutFlags.PositionProportional);

//        // Добавление числа в LayoutContainer
//        LayoutContainer.Children.Add(numberLabel);
//    }
//}