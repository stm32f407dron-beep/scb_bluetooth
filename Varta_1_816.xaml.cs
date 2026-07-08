using Microsoft.Maui.Graphics;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;




namespace skb_home;

public partial class Varta_1_816 : ContentPage
{


    // Добавим поле для хранения ссылки на Bluetooth-сервис
    private readonly IBluetooth_service _bluetoothService;
    // Таймер для автоматического опроса
  //  private System.Timers.Timer autoReadTimer;
    // CancellationTokenSource для управления циклом автоматического опроса - флаг для отмены при уходе со страницы
    private CancellationTokenSource autoReadCts;


    double?[] values = { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

    // для массового опроса через TaskCompletionSource
    private TaskCompletionSource<double> channelReadCompletionSource = null;
    // Флаг для блокировки повторного запуска массового опроса
    private bool isReadAllInProgress = false;

    // для одиночного опроса 
    private int? selectedLoopIndex = null;

    // Флаг активности страницы для игнорирования данных при неактивности
    private bool _isActive = false;


    // Конструктор страницы принимает Bluetooth-сервис через DI
    public Varta_1_816(IBluetooth_service bluetooth)
	{
		InitializeComponent();
        _bluetoothService = bluetooth;
        // Запускаем прием при открытии страницы
        _bluetoothService.ReceiverData();

       // _bluetoothService.DataReceived += OnReceiverData;

        // Подписываемся на событие получения данных, чтобы отображать их в терминале
        //_bluetoothService.DataReceived +=  delegate (string rx) 
        //{
        //    // 1. Парсим ток (ток: XXXmA)
        //    var match = Regex.Match(rx, @"ток:\s*([0-9.]+)mA", RegexOptions.IgnoreCase);
        //    if (!match.Success)
        //    {
        //        AddTerminalText(rx); // невалидный пакет — просто отображаем
        //        return;
        //    }

        //    // 2. Переводим значение и делим на 10 по протоколу
        //    if (!double.TryParse(
        //        match.Groups[1].Value,
        //        System.Globalization.NumberStyles.Any,
        //        System.Globalization.CultureInfo.InvariantCulture,
        //        out double valueMa
        //    ))
        //        return;

        //    valueMa = valueMa / 10.0;

        //    // 3. Парсим subindex (номер шлейфа, если есть)
        //    var subindexMatch = Regex.Match(rx, @"субиндекс:\s*(\d+)", RegexOptions.IgnoreCase);
        //    int? subindex = subindexMatch.Success ? int.Parse(subindexMatch.Groups[1].Value) : (int?)null;

        //    // === МАССОВЫЙ ОПРОС  ===
        //    if (channelReadCompletionSource != null)
        //    {
        //        if (subindex.HasValue)
        //        {
        //            values[subindex.Value - 1] = valueMa;
        //            AddTerminalText($"Шлейф {subindex}: ток = {valueMa:F1} mA");
        //        }
        //        else
        //        {
        //            AddTerminalText($"Ответ (номер шлейфа не определен): {valueMa:F1} mA");
        //        }
        //        MainThread.BeginInvokeOnMainThread(() => HistCanvas.Invalidate());
        //        channelReadCompletionSource.TrySetResult(valueMa);
        //        channelReadCompletionSource = null;
        //        return;
        //    }

        //    // === ОДИНОЧНЫЙ ОПРОС (по UI) ===
        //    if (selectedLoopIndex.HasValue && subindex.HasValue && selectedLoopIndex.Value + 1 == subindex.Value)
        //    {
        //        values[selectedLoopIndex.Value] = valueMa;
        //        AddTerminalText($"Шлейф {subindex}: ток = {valueMa:F1} mA");
        //        MainThread.BeginInvokeOnMainThread(() => HistCanvas.Invalidate());
        //        selectedLoopIndex = null;
        //        return;
        //    }

        //    // === Прочий/лишний пакет ===
        //    AddTerminalText($"[{DateTime.Now:HH:mm:ss}] Получен неожиданный ток: {valueMa:F1} mA");
        //};


    }

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


    // При появлении страницы заполняем гистограмму данными
    protected override void OnAppearing()
    {
        base.OnAppearing();
    
        _isActive = true;

        _bluetoothService.DataReceived += OnReceiverData;
        // Пример реальные данные, для теста вставь свои double значения!
        values = new double?[] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

        // Создаем и устанавливаем Drawable для гистограммы
        HistCanvas.Drawable = new HistogramDrawable(values);
        // Cоздаем новый токен для отмены автоматического опроса данных
         autoReadCts = new CancellationTokenSource();

        var token = autoReadCts.Token;
        // передаем токен в метод, который запускает цикл опроса
        StartAutoReadLoop(token);

    }
  //  При исчезновении страницы можно очистить гистограмму или освободить ресурсы
    //protected override void OnDisappearing()
    //{
    //    base.OnDisappearing();
    //    _isActive = false;
    //    autoReadCts?.Cancel();
    //    // Очистка ресурсов
    //    // HistCanvas.Drawable = null;
    //    // Останавливаем автоматический опрос
    //    channelReadCompletionSource?.TrySetCanceled();
    //    channelReadCompletionSource = null;
    //    selectedLoopIndex = null;
    //    isReadAllInProgress = false;
    //    _bluetoothService.DataReceived -= OnReceiverData;
      
    //    autoReadCts?.Dispose();
    //    autoReadCts = null;

    //}


    // Varta_1_816.cs
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // 1) Сразу помечаем страницу неактивной
        _isActive = false;

        // 2) СНАЧАЛА отписываемся от входящих событий Bluetooth.
        // Это самый важный шаг, чтобы "последний пакет из буфера" не попал в эту страницу.
        _bluetoothService.DataReceived -= OnReceiverData;

        // 3) Отменяем автоматический цикл/ожидания
        autoReadCts?.Cancel();

        // 4) Если где-то ждём ответ через TCS — отменяем ожидание
        channelReadCompletionSource?.TrySetCanceled();
        channelReadCompletionSource = null;

        // 5) Сбрасываем флаги/состояние
        selectedLoopIndex = null;
        isReadAllInProgress = false;

        // 6) Освобождаем CTS
        autoReadCts?.Dispose();
        autoReadCts = null;
    }





    //protected override void OnDisappearing()
    //{
    //    base.OnDisappearing();
    //    _isActive = false;

    //    // Остановить любые ожидания ответа по TaskCompletionSource
    //    channelReadCompletionSource?.TrySetCanceled();
    //    channelReadCompletionSource = null;

    //    selectedLoopIndex = null;              // сбросить одиночный опрос
    //    isReadAllInProgress = false;           // флаг массового опроса в "false"

    //    _bluetoothService.DataReceived -= OnReceiverData; // отписаться от приёма данных

    //    autoReadCts?.Cancel();                 // отменить асинхронный цикл опроса
    //    autoReadCts?.Dispose();
    //    autoReadCts = null;
    //}


    //protected override void OnAppearing()
    //{
    //    base.OnAppearing();

    //    _isActive = true;
    //    _bluetoothService.DataReceived += OnReceiverData;

    //    values = new double?[16];
    //    HistCanvas.Drawable = new HistogramDrawable(values);

    //    // Запуск таймера
    //    _autoReadTimer = new System.Timers.Timer(3000);
    //    _autoReadTimer.Elapsed += async (s, e) =>
    //    {
    //        if (!_isActive)
    //            return;

    //        if (!isReadAllInProgress)
    //        {
    //            await MainThread.InvokeOnMainThreadAsync(async () =>
    //            {
    //                await OnReadAllClicked2(this, EventArgs.Empty);
    //            });
    //        }
    //    };
    //    _autoReadTimer.AutoReset = true;
    //    _autoReadTimer.Start();
    //}

    //protected override void OnDisappearing()
    //{
    //    base.OnDisappearing();
    //    _isActive = false;
    //    _bluetoothService.DataReceived -= OnReceiverData;

    //    // Остановка и освобождение таймера
    //    if (_autoReadTimer != null)
    //    {
    //        _autoReadTimer.Stop();
    //        _autoReadTimer.Dispose();
    //        _autoReadTimer = null;
    //    }

    //    channelReadCompletionSource?.TrySetCanceled();
    //    channelReadCompletionSource = null;
    //    selectedLoopIndex = null;
    //    isReadAllInProgress = false;
    //}

    //private  void OnReceiverData(string rx)
    //{

    //    if (!_isActive) return; // Игнорировать если неактивна

    //    if (!_isActive || autoReadCts?.Token.IsCancellationRequested == true) return;

    //    // 1. Парсим ток (ток: XXXmA)
    //    var match = Regex.Match(rx, @"ток:\s*([0-9.]+)mA", RegexOptions.IgnoreCase);
    //    if (!match.Success)
    //    {
    //        AddTerminalText(rx); // невалидный пакет — просто отображаем
    //        return;
    //    }

    //    // 2. Переводим значение и делим на 10 по протоколу
    //    if (!double.TryParse(
    //        match.Groups[1].Value,
    //        System.Globalization.NumberStyles.Any,
    //        System.Globalization.CultureInfo.InvariantCulture,
    //        out double valueMa
    //    ))
    //        return;

    //    valueMa = valueMa / 10.0;

    //    // 3. Парсим subindex (номер шлейфа, если есть)
    //    var subindexMatch = Regex.Match(rx, @"субиндекс:\s*(\d+)", RegexOptions.IgnoreCase);
    //    int? subindex = subindexMatch.Success ? int.Parse(subindexMatch.Groups[1].Value) : (int?)null;

    //    // === МАССОВЫЙ ОПРОС  ===
    //    if (channelReadCompletionSource != null)
    //    {

    //        if (!_isActive || autoReadCts?.Token.IsCancellationRequested == true) return;

    //        if (subindex.HasValue)
    //        {
    //            values[subindex.Value - 1] = valueMa;
    //            AddTerminalText($"Шлейф {subindex}: ток = {valueMa:F1} mA");
    //        }
    //        else
    //        {
    //            AddTerminalText($"Ответ (номер шлейфа не определен): {valueMa:F1} mA");
    //        }
    //        MainThread.BeginInvokeOnMainThread(() => HistCanvas.Invalidate());
    //        channelReadCompletionSource.TrySetResult(valueMa);
    //        channelReadCompletionSource = null;
    //        return;
    //    }

    //    // === ОДИНОЧНЫЙ ОПРОС (по UI) ===
    //    if (selectedLoopIndex.HasValue && subindex.HasValue && selectedLoopIndex.Value + 1 == subindex.Value)
    //    {
    //        values[selectedLoopIndex.Value] = valueMa;
    //        AddTerminalText($"Шлейф {subindex}: ток = {valueMa:F1} mA");
    //        MainThread.BeginInvokeOnMainThread(() => HistCanvas.Invalidate());
    //        selectedLoopIndex = null;
    //        return;
    //    }

    //    // === Прочий/лишний пакет ===
    //    AddTerminalText($"[{DateTime.Now:HH:mm:ss}] Получен неожиданный ток: {valueMa:F1} mA");


    //}

    // Varta_1_816.cs
    private void OnReceiverData(string rx)
    {
        // Одна проверка вместо двух (и сразу учитываем отмену)
        if (!_isActive || autoReadCts?.Token.IsCancellationRequested == true)
            return;

        // 1) Парсим ток (ток: XXXmA)
        var match = Regex.Match(rx, @"ток:\s*([0-9.]+)mA", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            AddTerminalText(rx);
            return;
        }

        if (!double.TryParse(
                match.Groups[1].Value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double valueMa))
            return;

        valueMa /= 10.0;

        // 2) Парсим субиндекс
        var subindexMatch = Regex.Match(rx, @"субиндекс:\s*(\d+)", RegexOptions.IgnoreCase);
        int? subindex = subindexMatch.Success ? int.Parse(subindexMatch.Groups[1].Value) : (int?)null;

        // Берём локальную копию TCS, чтобы уменьшить гонки
        var tcs = channelReadCompletionSource;

        // === МАССОВЫЙ ОПРОС ===
        if (tcs != null)
        {
            if (!_isActive || autoReadCts?.Token.IsCancellationRequested == true)
                return;

            if (subindex.HasValue)
            {
                values[subindex.Value - 1] = valueMa;
                AddTerminalText($"Шлейф {subindex}: ток = {valueMa:F1} mA");
            }
            else
            {
                AddTerminalText($"Ответ (номер шлейфа не определен): {valueMa:F1} mA");
            }

            MainThread.BeginInvokeOnMainThread(() => HistCanvas.Invalidate());

            // Завершаем именно тот tcs, который был на момент входа в метод
            // и только если получилось — чистим поле.
            if (tcs.TrySetResult(valueMa))
                channelReadCompletionSource = null;

            return;
        }

        // === ОДИНОЧНЫЙ ОПРОС ===
        //if (selectedLoopIndex.HasValue && subindex.HasValue && selectedLoopIndex.Value + 1 == subindex.Value)
        //{
        //    values[selectedLoopIndex.Value] = valueMa;
        //    AddTerminalText($"Шлейф {subindex}: ток = {valueMa:F1} mA");
        //    MainThread.BeginInvokeOnMainThread(() => HistCanvas.Invalidate());
        //    selectedLoopIndex = null;
        //    return;
        //}

        // === Прочий пакет ===
        AddTerminalText($"[{DateTime.Now:HH:mm:ss}] Получен неожиданный ток: {valueMa:F1} mA");
    }






    // ============ ОБРАБОТЧИКИ МЕНЮ ============
    // Кнопка ⚙ (Настройки)
    [Obsolete]
    private async void OnSettingsClicked(object sender, EventArgs e)
    {

        string choice = await DisplayActionSheet(
            "Вібір",
            "Назад",
            null,
            "Налаштування",
            "Конфігурація",
            "Струми шлейфів",
            "Коди подій",
            "Коди відновлення",
            "Індифікатор шлейфів",
            "Струми шлейфів",
            "Параметри реле",
            "Стан шлейфа",
            "Про програму"
        );

        switch (choice)
        {


            case "Конфігурація":
                await Navigation.PushAsync(new Configurations(_bluetoothService));
                break;



            case "Налаштування":
                await DisplayAlert("Загальні", "Загальні налаштування", "OK");
                break;

            //case "Струми шлейфів":
            //    await Navigation.PushAsync(new Varta_1_816(_bluetoothService));
            //    break;

            case "Коди подій":
                await Navigation.PushAsync(new AddDevicePage(), animated: false);
                break;

            case "Коди відновлення":
                await Navigation.PushAsync(new Settingpage(_bluetoothService), animated: false);
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




    private async void StartAutoReadLoop(CancellationToken token)
    {
        try
        {

            if (!_isActive) return; // Игнорировать если неактивна
                                    // запрос токена отмену в начале, чтобы не запускать цикл, если уже неактивно

            if (token.IsCancellationRequested) return; // Проверяем токен в начале метода, чтобы не запускать, если уже неактивно
            while (!token.IsCancellationRequested)
            {

                if (!_isActive) break; // <--- проверяем каждый раз, как только стали неактивны — выходим

                if (!isReadAllInProgress)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {

                        if (token.IsCancellationRequested) return; // Проверяем токен в начале метода, чтобы не запускать, если уже неактивно
                        await OnReadAllClicked2(this, EventArgs.Empty, token);
                    });
                }
                await Task.Delay(3000, token);

                if (token.IsCancellationRequested) return; // Проверяем токен в начале метода, чтобы не запускать, если уже неактивно
            }
        }
        catch (TaskCanceledException)
        {
            // Это безопасное завершение — ничего не делаем!
        }
        catch (Exception ex)
        {
            // Логировать или показать ошибку, если нужно
            Debug.WriteLine("AutoReadLoop Exception: " + ex);
            // Можно добавить UI уведомление или другое
        }
    }




    //private async void StartAutoReadLoop(CancellationToken token)
    //{
    //    try
    //    {
    //        while (!token.IsCancellationRequested && _isActive)
    //        {
    //            if (!isReadAllInProgress)
    //                await OnReadAllClicked2(this, EventArgs.Empty, token);

    //            await Task.Delay(3000, token);
    //        }
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        // нормальная отмена
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine("AutoReadLoop Exception: " + ex);
    //    }
    //}






    // Метод для чтения одного канала с ожиданием ответа через TaskCompletionSource



    private async Task<double?> ReadAllChannelValueAsync2(string command, CancellationToken token)
    {

        if (token.IsCancellationRequested) return null; // Проверяем токен в начале метода, чтобы не запускать, если уже неактивно
        var tcs = new TaskCompletionSource<double>();
        channelReadCompletionSource = tcs;

        try
        {
            if (token.IsCancellationRequested) return null; // Проверяем токен перед отправкой, чтобы не отправлять, если уже неактивно
            await _bluetoothService.TransmitterData(command);
           
        }
        catch (Exception ex)
        {
            channelReadCompletionSource = null;
            AddTerminalText($"⚠️ Помилка відпр.: {ex.Message}");
            return null;
        }

        var task = tcs.Task;

        // Ожидаем (timeout можно 5 секунд, можешь увеличить если нужно, но не прерываться заранее)
        if (await Task.WhenAny(task, Task.Delay(3000, token)) == task)
        {
            if (token.IsCancellationRequested) return null; // Проверяем токен после ожидания, чтобы не возвращать результат, если уже неактивно

            channelReadCompletionSource = null;
            return task.Result;
        }
        else
        {
            channelReadCompletionSource = null;
            AddTerminalText("⚠️ Таймаут по відповіді Bluetooth пристрою");
            return null;
        }
    }



//    private async Task<double?> ReadAllChannelValueAsync2(string command, CancellationToken token)
//    {
//        if (token.IsCancellationRequested) return null;
//// 
//        var tcs = new TaskCompletionSource<double>(TaskCreationOptions.RunContinuationsAsynchronously);
//        channelReadCompletionSource = tcs;

//        try
//        {
//            token.ThrowIfCancellationRequested();

//            await _bluetoothService.TransmitterData(command);

//            // ждём либо ответ, либо таймаут/отмену
//            var completed = await Task.WhenAny(tcs.Task, Task.Delay(3000, token));

//            token.ThrowIfCancellationRequested();

//            if (completed == tcs.Task)
//                return await tcs.Task; // вернёт значение
//            else
//            {
//                AddTerminalText("⚠️ Таймаут по відповіді Bluetooth пристрою");
//                return null;
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            // отмена — это нормальный сценарий
//            return null;
//        }
//        catch (Exception ex)
//        {
//            AddTerminalText($"⚠️ Помилка відпр.: {ex.Message}");
//            return null;
//        }
//        finally
//        {
//            // Важно: чистим только если поле всё ещё указывает на НАШ tcs
//            if (ReferenceEquals(channelReadCompletionSource, tcs))
//                channelReadCompletionSource = null;
//        }
//    }







    // Обработчик "Считать все" 




    private async Task OnReadAllClicked2(object sender, EventArgs e, CancellationToken token)
    {

        if (token.IsCancellationRequested) return; // Проверяем токен в начале метода, чтобы не запускать, если уже неактивно


        if (isReadAllInProgress)
            return;
        isReadAllInProgress = true;



        values = new double?[16];
        HistCanvas.Drawable = new HistogramDrawable(values);
        HistCanvas.Invalidate();

        for (int i = 0; i < 16; i++)
        {

            if (token.IsCancellationRequested) return; // Проверяем токен в каждом цикле, чтобы не продолжать, если уже неактивно
            if (!_isActive) return;




            string num = (i + 1).ToString();
            if (!_commands.TryGetValue(num, out string command))
            {
                AddTerminalText($"⚠️ Для шлейфа {num} команда не найдена!");
                continue;
            }

            if (token.IsCancellationRequested) return; // Проверяем токен в начале метода, чтобы не запускать, если уже неактивно

            //  AddTerminalText($"Читаю шлейф {num}...");
            double? channelValue = await ReadAllChannelValueAsync2(command, token); // ждем конкретно первого ответа!


            await Task.Delay(15, token);
            if (token.IsCancellationRequested) return; // Проверяем токен в начале метода, чтобы не запускать, если уже неактивно

            if (channelValue.HasValue)
            {
                values[i] = channelValue.Value; // этот массив всегда актуален после DataReceived
                                                // AddTerminalText($"Шлейф {num}: {channelValue.Value:F1} мА");
            }
            else
            {
                AddTerminalText($"⚠️ Не получен ответ от шлейфа {num}");
            }

            HistCanvas.Invalidate();
            // await Task.Delay(100); // если нужна анимация между шагами
        }

        AddTerminalText("✔️ Считывание всех шлейфов завершено.");
        isReadAllInProgress = false;
    }



    //private async Task OnReadAllClicked2(object sender, EventArgs e, CancellationToken token)
    //{
    //    if (token.IsCancellationRequested) return;
    //    if (isReadAllInProgress) return;

    //    isReadAllInProgress = true;

    //    try
    //    {
    //        values = new double?[16];
    //        HistCanvas.Drawable = new HistogramDrawable(values);
    //        HistCanvas.Invalidate();

    //        for (int i = 0; i < 16; i++)
    //        {
    //            token.ThrowIfCancellationRequested();
    //            if (!_isActive) return;

    //            string num = (i + 1).ToString();
    //            if (!_commands.TryGetValue(num, out string command))
    //            {
    //                AddTerminalText($"⚠️ Для шлейфа {num} команда не найдена!");
    //                continue;
    //            }

    //            var channelValue = await ReadAllChannelValueAsync2(command, token);

    //            token.ThrowIfCancellationRequested();

    //            await Task.Delay(15, token);

    //            if (channelValue.HasValue)
    //                values[i] = channelValue.Value;
    //            else
    //                AddTerminalText($"⚠️ Не получен ответ от шлейфа {num}");

    //            HistCanvas.Invalidate();
    //        }

    //        AddTerminalText("✔️ Считывание всех шлейфов завершено.");
    //    }
    //    catch (OperationCanceledException)
    //    {
    //        // нормально при уходе со страницы
    //    }
    //    finally
    //    {
    //        isReadAllInProgress = false;
    //    }
    //}








    // Обработчик переключателя терминала



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



}


// Класс для рисования гистограммы
// Плавная гистограмма с double и maxY=30
// Плавная гистограмма с double-значениями и maxY=30, topPad=14
//public class HistogramDrawable : IDrawable
//{
//    private readonly double[] data;

//    public HistogramDrawable(double[] values) => data = values;

//    public void Draw(ICanvas canvas, RectF dirtyRect)
//    {
//        double maxY = 30.0;
//        int ticks = 6;
//        int cols = data.Length;
//        float canvasHeight = dirtyRect.Height;
//        float canvasWidth = dirtyRect.Width;

//        float axisPadLeft = 22;
//        float axisPadBottom = 18;
//        float topPad = 14;
//        float minBarHeight = 2f; // Минимальная высота для value==0

//        float barZoneHeight = canvasHeight - axisPadBottom - topPad;
//        float barZoneWidth = canvasWidth - axisPadLeft;
//        float barWidth = cols > 0 ? barZoneWidth / cols * 0.7f : 10;
//        float deltaX = cols > 0 ? barZoneWidth / cols : 20;

//        // Сетка и ось Y
//        for (int y = 0; y <= ticks; y++)
//        {
//            double label = maxY - y * (maxY / ticks);
//            float yPos = topPad + y * barZoneHeight / ticks;
//            canvas.StrokeColor = Colors.Black;
//            canvas.StrokeSize = 1;
//            canvas.DrawLine(axisPadLeft, yPos, canvasWidth, yPos);
//            canvas.FontColor = Colors.Black;
//            canvas.FontSize = 12;
//            canvas.DrawString(label.ToString("0"), axisPadLeft - 8, yPos + 4, HorizontalAlignment.Right);
//        }

//        // Столбики — с цветом и видимостью нулевых
//        for (int i = 0; i < cols; i++)
//        {
//            double val = Math.Clamp(data[i], 0.0, maxY);
//            float h = (float)(val / maxY * (barZoneHeight - 1));
//            if (val == 0.0)
//                h = minBarHeight;

//            float x = axisPadLeft + i * deltaX + (deltaX - barWidth) / 2f;
//            float y = topPad + barZoneHeight - h;

//            canvas.FillColor = GetColorByValue(val);
//            canvas.FillRoundedRectangle(x, y, barWidth, h, 2);
//        }

//        // Подписи X
//        canvas.FontColor = Colors.Black;
//        canvas.FontSize = 12;
//        for (int i = 0; i < cols; i++)
//        {
//            float x = axisPadLeft + i * deltaX + deltaX / 2;
//            float y = topPad + barZoneHeight + 12;
//            canvas.DrawString((i + 1).ToString(), x, y, HorizontalAlignment.Center);
//        }
//    }

//    // Цвет по значению: КЗ=красный, обрыв=синий, норма=зелёный
//    private Color GetColorByValue(double valueMa)
//    {
//        if (valueMa > 28.0 || valueMa == 0.0)
//            return Colors.Red;      // КЗ
//        else if (valueMa > 0.0 && valueMa <= 5.0)
//            return Colors.Blue;     // Обрыв
//        else if (valueMa > 5.0 && valueMa <= 28.0)
//            return Colors.Green;    // Норма
//        return Colors.Silver;       // На всякий случай
//    }
//}



public class HistogramDrawable : IDrawable
{
    private readonly double?[] data;

    public HistogramDrawable(double?[] values) => data = values;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        double maxY = 30.0;
        int ticks = 6;
        int cols = data.Length;
        float canvasHeight = dirtyRect.Height;
        float canvasWidth = dirtyRect.Width;

        float axisPadLeft = 22;
        float axisPadBottom = 18;
        float topPad = 14;
        float minBarHeight = 2f;

        float barZoneHeight = canvasHeight - axisPadBottom - topPad;
        float barZoneWidth = canvasWidth - axisPadLeft;
        float barWidth = cols > 0 ? barZoneWidth / cols * 0.7f : 10;
        float deltaX = cols > 0 ? barZoneWidth / cols : 20;

        // Сетка и ось Y
        for (int y = 0; y <= ticks; y++)
        {
            double label = maxY - y * (maxY / ticks);
            float yPos = topPad + y * barZoneHeight / ticks;
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.DrawLine(axisPadLeft, yPos, canvasWidth, yPos);
            canvas.FontColor = Colors.Black;
            canvas.FontSize = 13;
                
            canvas.DrawString(label.ToString("0"), axisPadLeft - 8, yPos + 4, HorizontalAlignment.Right);
        }

        // Столбики
        for (int i = 0; i < cols; i++)
        {
            double? val = data[i];
            float h;
            Color fillColor;

            if (val.HasValue)
            {
                double clamped = Math.Clamp(val.Value, 0.0, maxY);
                h = (float)(clamped / maxY * (barZoneHeight - 1));
                if (clamped == 0.0)
                    h = minBarHeight;
                fillColor = GetColorByValue(clamped);
            }
            else
            {
                // null — значит нет данных: рисуем серебряный низкий столбик
                h = minBarHeight;
                fillColor = Colors.Black;
            }

            float x = axisPadLeft + i * deltaX + (deltaX - barWidth) / 2f;
            float y = topPad + barZoneHeight - h;

            canvas.FillColor = fillColor;
            canvas.FillRoundedRectangle(x, y, barWidth, h, 2);
        }

        // Подписи X
        canvas.FontColor = Colors.Black;
        canvas.FontSize = 12;
      
        for (int i = 0; i < cols; i++)
        {
            float x = axisPadLeft + i * deltaX + deltaX / 2;
            float y = topPad + barZoneHeight + 12;
            canvas.DrawString((i + 1).ToString(), x, y, HorizontalAlignment.Center);
        }
    }

    private Color GetColorByValue(double valueMa)
    {
        if (valueMa > 28.0 || valueMa == 0.0)
            return Colors.DarkRed;      // КЗ
        else if (valueMa > 0.0 && valueMa <= 5.0)
            return Colors.Blue;     // Обрыв
        else if (valueMa > 5.0 && valueMa <= 28.0)
            return Colors.Green;    // Норма
     //   return Colors.Black;
        return Color.FromArgb("#000000");

    }
}







//_bluetoothService.DataReceived += delegate (string rx) 
//{

//    var match = Regex.Match(rx, @"ток:\s*([0-9.]+)mA", RegexOptions.IgnoreCase);
//    if (match.Success)
//    {
//       double current = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
//       current = current/10; // Добавляем 10% к полученному значению
//        AddTerminalText($"Отримано струм: {current:F1} mA");
//    }

//    //  AddTerminalText(rx); 


//};



//private async void OnReadSingleClicked(object sender, EventArgs e)
//{
//    Button button = sender as Button;
//    if (button == null) return;

//    button.IsEnabled = false;
//    button.BackgroundColor = Colors.DarkRed;


//    values = new double?[16];
//    HistCanvas.Drawable = new HistogramDrawable(values);
//    HistCanvas.Invalidate();




//    await Task.Delay(50); // Имитируем задержку для чтения

//    string loopNumber = LoopNumberEntry.Text;
//    if (string.IsNullOrWhiteSpace(loopNumber) || !int.TryParse(loopNumber, out int loopIndex) || loopIndex < 1 || loopIndex > 16)
//    {
//        await DisplayAlertAsync("Помилка", "Введіть номер!", "OK");
//        button.BackgroundColor = Colors.Black;
//        button.IsEnabled = true;
//        return;
//    }

//    if (_commands.TryGetValue(loopNumber, out string command))
//    {
//        selectedLoopIndex = loopIndex - 1; // фиксируем ожидаемый канал

//        await _bluetoothService.TransmitterData(command);

//        // Здесь можно реализовать таймаут и ожидание, если нужно; если не надо — не делай TaskCompletionSource на одиночный!
//    }

//    button.BackgroundColor = Colors.Black;
//    button.IsEnabled = true;
//}



//private async void OnReadAllClicked(object sender, EventArgs e)
//{
//    if (isReadAllInProgress)
//        return;
//    isReadAllInProgress = true;

//    values = new double?[16];
//    HistCanvas.Drawable = new HistogramDrawable(values);
//    HistCanvas.Invalidate();

//    for (int i = 0; i < 16; i++)
//    {
//        string num = (i + 1).ToString();
//        if (!_commands.TryGetValue(num, out string command))
//        {
//            AddTerminalText($"⚠️ Для шлейфа {num} команда не найдена!");
//            continue;
//        }

//        //  AddTerminalText($"Читаю шлейф {num}...");
//        double? channelValue = await ReadAllChannelValueAsync(command); // ждем конкретно первого ответа!
//        await Task.Delay(15);
//        if (channelValue.HasValue)
//        {
//            values[i] = channelValue.Value; // этот массив всегда актуален после DataReceived
//                                            // AddTerminalText($"Шлейф {num}: {channelValue.Value:F1} мА");
//        }
//        else
//        {
//            AddTerminalText($"⚠️ Не получен ответ от шлейфа {num}");
//        }

//        HistCanvas.Invalidate();
//        // await Task.Delay(100); // если нужна анимация между шагами
//    }

//    AddTerminalText("✔️ Считывание всех шлейфов завершено.");
//    isReadAllInProgress = false;
//}

//private async Task<double?> ReadAllChannelValueAsync(string command)
//{
//    var tcs = new TaskCompletionSource<double>();
//    channelReadCompletionSource = tcs;

//    try
//    {
//        await _bluetoothService.TransmitterData(command);
//    }
//    catch (Exception ex)
//    {
//        channelReadCompletionSource = null;
//        AddTerminalText($"⚠️ Помилка відпр.: {ex.Message}");
//        return null;
//    }

//    var task = tcs.Task;

//    // Ожидаем (timeout можно 5 секунд, можешь увеличить если нужно, но не прерываться заранее)
//    if (await Task.WhenAny(task, Task.Delay(3000)) == task)
//    {
//        channelReadCompletionSource = null;
//        return task.Result;
//    }
//    else
//    {
//        channelReadCompletionSource = null;
//        AddTerminalText("⚠️ Таймаут по відповіді Bluetooth пристрою");
//        return null;
//    }
//}



// --- АВТОМАТИЧЕСКИЙ ОПРОС ---
//if (autoReadTimer == null)
//{
//    autoReadTimer = new System.Timers.Timer(3000); // интервал в мс (3 сек)
//    autoReadTimer.Elapsed += async (s, e) =>
//    {
//        if (!isReadAllInProgress)
//        {
//            // Нужен вызов в UI-потоке! (иначе проблемы с MAUI)
//            await MainThread.InvokeOnMainThreadAsync(() =>
//            {
//                OnReadAllClicked2(this, EventArgs.Empty);
//            });
//        }
//    };
//    autoReadTimer.AutoReset = true;
//    autoReadTimer.Start();
//}




//if (autoReadTimer != null)
//{
//    autoReadTimer.Stop();
//    autoReadTimer.Dispose();
//    autoReadTimer = null;
//}
