using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace skb_home;

public partial class Configurations : ContentPage
{
    // Сервис для работы с Bluetooth, который будет передан через конструктор (внедрение зависимости)
    private readonly IBluetooth_service _bluetoothService;
    // Словарь для хранения команд, где ключ — это комбинация "страница|идентификатор", а значение — ASCII-HEX команда для отправки
    private readonly Dictionary<string, string> _commands = new()
    {
        ["1|Device_Name"] = "0101000021000080A3",
        ["2|CodeObject1"] = "010100002200010429",
        ["2|AccessPoint1"] = "01010000220002183E",
        ["2|AccessPoint2"] = "01010000220003183F",
        ["2|ServerPort1"] = "010100002200040830",
        ["2|ServerPort2"] = "010100002200050831",
        ["2|ipServer1"] = "01010000220006103A",
        ["2|ipServer2"] = "01010000220007103B",
        ["2|SMSPhone1"] = "010100002200081844",
        ["2|UDP1"] = "0101000022000A012F",
        ["2|UDP2"] = "0101000022000B0130",
        ["2|ACK1"] = "0101000022000C0131",
        ["2|ACK2"] = "0101000022000D0132",
        ["2|Hours1"] = "010100002200100135",
        ["2|Minutes1"] = "010100002200110136",
        ["2|Hours2"] = "010100002200120137",
        ["2|Minutes2"] = "010100002200130138",

    };




    // для отладки — сохраняем последнюю выбранную команду  010100002200110136
    private string? _lastSelectedCommand = null;

    //принимаем ответ только когда сами запросили
    private bool _waitingResponse = false;
    // для управления таймаутом ожидания ответа
    private CancellationTokenSource? _waitCts;


    // для отладки — сохраняем ключ последнего запроса, чтобы в логах было понятно, на какой запрос пришёл ответ или сработал таймаут
    private string? _lastRequestKey;


    // UDP
    private bool _enableOption = false;
    //ACK
    private bool _enableAck = false;
    public Configurations(IBluetooth_service bluetooth)
    {
        InitializeComponent();
        _bluetoothService = bluetooth;
    }

    //protected override void OnAppearing()
    //{
    //    base.OnAppearing();

    //    _bluetoothService.DataReceived += OnDataReceived;

    //    // UI по умолчанию — пусто (и мы НЕ будем заполнять без запроса)
    //    _waitingResponse = false;
    //    _waitCts?.Cancel();
    //    _waitCts?.Dispose();
    //    _waitCts = null;
    //    ServerPortValueLabel.Text = "";

    //    // Если ReceiverData реально нужно запускать тут — оставь.
    //    // Но обычно это делают один раз при подключении, а не на каждой странице.
    //    _bluetoothService.ReceiverData();
    //}


    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Сброс режима ожидания
        _waitingResponse = false;

        _waitCts?.Cancel();
        _waitCts?.Dispose();
        _waitCts = null;

      //  ServerPortValueLabel.Text = "";

        // Защита от двойной подписки
        _bluetoothService.DataReceived -= OnDataReceived;
        _bluetoothService.DataReceived += OnDataReceived;

        //  запуск приёма 
        _bluetoothService.ReceiverData();

        // Сброс ключа последнего запроса
        _lastRequestKey = null;

        RequestRead("2|UDP1");
        //  ApplyEnableOptionUi(_enableOption);
     //   RequestRead("2|ACK1");


    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _bluetoothService.DataReceived -= OnDataReceived;

        // На выходе прекращаем ожидание, если оно было
        _waitCts?.Cancel();
        _waitCts?.Dispose();
        _waitCts = null;

        _waitingResponse = false;


        _lastRequestKey = null;

    }

    //private async void OnServerPortTapped(object sender, EventArgs e)
    //{
    //    try
    //    {
    //        // Определяем, откуда пришёл тап: от sender (например, если тапнули по самому элементу) 
    //        string? param = null;
    //        if (sender is TapGestureRecognizer tap)
    //            // берем свойство CommandParameter 
    //            param = tap.CommandParameter?.ToString();
    //        // это другой способ получить тот же CommandParameter, когда sender приходит не как TapGestureRecognizer,
    //        // а как сам UI-элемент (например Label, Grid, Border), на который повешен tap.
    //        else if (sender is Microsoft.Maui.Controls.View view)
    //            param = view.GestureRecognizers?.OfType<TapGestureRecognizer>()
    //                .FirstOrDefault()?.CommandParameter?.ToString();
    //        // создаем массив из двух частей: page и id, разделенных '|'
    //        var parts = (param ?? "").Split('|');
    //        // извлекаем page , иначе ставим '?'
    //        var page = parts.Length > 0 ? parts[0] : "?";
    //        // извлекаем id , иначе ставим '?'
    //        var id = parts.Length > 1 ? parts[1] : "?";
    //        // формируем ключ для словаря
    //        var key = $"{page}|{id}";
    //        // Ищем команду в словаре по ключу
    //        if (!_commands.TryGetValue(key, out var asciiHex))
    //        {
    //            _lastSelectedCommand = null;
    //            await MainThread.InvokeOnMainThreadAsync(() =>
    //                DisplayAlert("Не найдена команда", $"Ключ {key} не задан в словаре", "OK"));
    //            return;
    //        }
    //        // Сохраняем последнюю выбранную команду
    //        _lastSelectedCommand = asciiHex;

    //        // Ставим режим ожидания ответа
    //        _waitingResponse = true;
    //     //   ServerPortValueLabel.Text = "Очікування відповіді...";

    //        // Таймаут ожидания (чтобы не залипло)
    //        _waitCts?.Cancel();
    //        _waitCts?.Dispose();
    //        _waitCts = new CancellationTokenSource();

    //        var token = _waitCts.Token;



    //        _lastRequestKey = key;
    //        Debug.WriteLine($"[TX] key={_lastRequestKey} cmd={_lastSelectedCommand} at={DateTime.Now:HH:mm:ss.fff}");


    //        // Создаем отдельный поток для отслеживания таймаута, чтобы не блокировать UI
    //        _ = Task.Run(async () =>
    //        {
    //            try
    //            {
    //                await Task.Delay(2500, token);
    //                MainThread.BeginInvokeOnMainThread(() =>
    //                {
    //                    if (_waitingResponse) // всё ещё ждём
    //                    {
    //                        _waitingResponse = false;
    //                        // ServerPortValueLabel.Text = "⚠️";
    //                           Debug.WriteLine($"[TIMEOUT] key={_lastRequestKey} at={DateTime.Now:HH:mm:ss.fff}");
    //                        SetPortLabelText(_lastRequestKey, "⚠️");
                         
                           

    //                    }
    //                });
    //            }
    //            catch (OperationCanceledException) { Debug.WriteLine($"[TIMEOUT-CANCELED] key={_lastRequestKey} at={DateTime.Now:HH:mm:ss.fff}"); }
    //        });

    //        // Отправляем команду
    //        await _bluetoothService.TransmitterData(_lastSelectedCommand);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"OnServerPortTapped exception: {ex}");
    //    }
    //}


    private async void OnServerPortTapped(object sender, EventArgs e)
    {
        try
        {
            string? param = null;
            // Определяем, откуда пришёл тап: от sender (например, если тапнули по самому элементу) 
            if (sender is TapGestureRecognizer tap)
                // берем свойство CommandParameter 
                param = tap.CommandParameter?.ToString();
            // это другой способ получить тот же CommandParameter, когда sender приходит не как TapGestureRecognizer,
            // а как сам UI-элемент (например Label, Grid, Border), на который повешен tap.
            else if (sender is Microsoft.Maui.Controls.View view)
                param = view.GestureRecognizers?.OfType<TapGestureRecognizer>()
                    .FirstOrDefault()?.CommandParameter?.ToString();
            // создаем массив из двух частей: page и id, разделенных '|'
            var parts = (param ?? "").Split('|');
            // извлекаем page , иначе ставим '?'
            var page = parts.Length > 0 ? parts[0] : "?";
            // извлекаем id , иначе ставим '?'
            var id = parts.Length > 1 ? parts[1] : "?";
            // формируем ключ для словаря
            var key = $"{page}|{id}";
            // Ищем команду в словаре по ключу
            if (!_commands.TryGetValue(key, out var asciiHex))
            {
                _lastSelectedCommand = null;
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Не найдена команда", $"Ключ {key} не задан в словаре", "OK"));
                return;
            }
            // Сохраняем последнюю выбранную команду
            _lastSelectedCommand = asciiHex;
            // Ставим режим ожидания ответа - флаг ждём ответ на последний запрос.
            _waitingResponse = true;

            //Сбросс таймаута ожидания (на всякий случай, если вдруг остался от прошлого запроса)
            _waitCts?.Cancel();
            // Освобождаем ресурсы старого CancellationTokenSource
            _waitCts?.Dispose();
            // Создаем новый CancellationTokenSource для текущего запроса
            _waitCts = new CancellationTokenSource();
            // Получаем токен для передачи в задачу таймаута
            var token = _waitCts.Token;
            // Сохраняем ключ последнего запроса для логов и отображения результата
            _lastRequestKey = key;
            // Логируем отправляемую команду с ключом и временем
            Debug.WriteLine($"[TX] key={_lastRequestKey} cmd={_lastSelectedCommand} at={DateTime.Now:HH:mm:ss.fff}");


            // Создаем отдельный поток для отслеживания таймаута, чтобы не блокировать UI
            _ = Task.Run(async () =>
            {
                try
                {
                    // Ждем 2.5 секунды  и если сработал токен отмены в OnDataReceived - то будет выброшено OperationCanceledException,
                    // и мы не зайдем в блок if (_waitingResponse), а если токен не был отменён, значит ответа не было и мы покажем таймаут.
                    await Task.Delay(2500, token);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Если пользователь уже получил ответ и _waitingResponse стал false
                        if (_waitingResponse)
                        {
                            _waitingResponse = false;
                            Debug.WriteLine($"[TIMEOUT] key={_lastRequestKey} at={DateTime.Now:HH:mm:ss.fff}");
                            SetPortLabelText(_lastRequestKey, "⚠️");
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine($"[TIMEOUT-CANCELED] key={_lastRequestKey} at={DateTime.Now:HH:mm:ss.fff}");
                }
            });
            // Tx - отправляем команду
            await _bluetoothService.TransmitterData(_lastSelectedCommand);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnServerPortTapped exception: {ex}");
        }
    }






    //private void OnDataReceived(string message)
    //{
    //    // Если мы ничего не запрашивали на этой странице — игнорируем любые входящие пакеты.
    //    if (!_waitingResponse)
    //        return;

    //    // Отбрасываем "хвосты" от опроса шлейфов (как на ![image1](image1)):
    //    // индекс/субиндекс/ток
    //    if (message.Contains("ток:", StringComparison.OrdinalIgnoreCase) ||
    //        message.Contains("субиндекс:", StringComparison.OrdinalIgnoreCase) ||
    //        message.Contains("индекс:", StringComparison.OrdinalIgnoreCase))
    //        return;

    //    // Принимаем ответ - 
    //    _waitingResponse = false;

    //    // отменяем таймаут ожидания
    //    _waitCts?.Cancel();

    //    MainThread.BeginInvokeOnMainThread(() =>
    //    {
    //        // ServerPortValueLabel.Text = message;

    //        SetPortLabelText(_lastRequestKey, message);


    //    });
    //}



    private void OnDataReceived(string message)
    {

       
        string m = message;

       // AddTerminalText(m);

        // Если мы ничего не запрашивали на этой странице — игнорируем любые входящие пакеты.
        if (!_waitingResponse)
            return;


        Debug.WriteLine($"[RX] waiting={_waitingResponse} lastKey={_lastRequestKey} msg='{message}' at={DateTime.Now:HH:mm:ss.fff}");

        // Отбрасываем "хвосты" от опроса шлейфов (индекс/субиндекс/ток)
        if (message.Contains("ток:", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("субиндекс:", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("индекс:", StringComparison.OrdinalIgnoreCase))
            return;


        AddTerminalText(message);
        // Принимаем ответ 
        _waitingResponse = false;

        // При приеме любого ответа, который мы ждали, отменяем таймаут ожидания, чтобы не сработал после получения ответа
        // (на всякий случай, если вдруг пришло несколько сообщений подряд, мы не хотим, чтобы таймаут отработал после первого принятого сообщения)
        // Безопастная цепочка событий: если _waitCts уже null (например, если таймаут уже сработал и очистил его), то вызов Cancel() будет проигнорирован, и мы не получим исключение.
        var cts = _waitCts;
        _waitCts = null;

        try { cts?.Cancel(); } catch { /* игнор */ }
        cts?.Dispose();
        //Вызываем обновление UI в главном потоке, чтобы показать результат пользователю
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetPortLabelText(_lastRequestKey, message);

          //  AddTerminalText(m);
        });
    }


    // Обработчик для кнопки считывания порта ServerPort1
    private async void ServerPort1(object sender, EventArgs e)
    {
        // Получаем кнопку, которая была нажата, через sender, и если это не Button — просто выходим из метода 
        if (sender is not Button btn)
            return;
        // Блокируем кнопку, чтобы пользователь не нажал её повторно, пока выполняется запрос
        btn.IsEnabled = false;

        try
        {
            // Получаем текст из поля ввода и удаляем лишние пробелы
            var text = ServerPortEntry?.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Помилка", "Введіть значення порту", "OK");
                return;
            }
            // преобразовать строку text в целое число int, если не удалось преобразовать или
            // число выходит за пределы от 1 до 65535, то показать сообщение об ошибке и выйти из метода
            if (!int.TryParse(text, out var port) || port < 1 || port > 65535)
            {
                await DisplayAlert("Помилка", "Порт має бути числом від 1 до 65535", "OK");
                return;
            }
            // Преобразуем число порта обратно в строку, чтобы отправить его в виде параметра
            var param = port.ToString();
            // Отправляем команду на запись порта, передавая параметр, и указывая идентификаторы для ServerPort1 (0x04)
            await _bluetoothService.TransmitterData_write(param, 0x02, 0x01, null, 0x04, true);

            await DisplayAlert("Успіх", $"Порт записано: {port}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnMicClicked exception: {ex}");
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            // В любом случае, после завершения операции  разблокируем кнопку
            btn.IsEnabled = true;
        }
    }


    private void ServerPortEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Проверяем, что sender — это Entry, если нет — выходим из метода
        if (sender is not Entry entry)
            return;
        // Получаем новый текст из события, если он null — заменяем на пустую строку
        var newText = e.NewTextValue ?? string.Empty;
        // Если новый текст состоит только из цифр, то ничего не делаем и выходим из метода

        // Проверка на все цифры с помощью делегата и метода Enumerable.All - альтернатива
        //Func<char, bool> predicat = delegate (char c) { return char.IsDigit(c); };

        //if (Enumerable.All(newText,predicat)) { return; } //- то же самое, но с лямбда-выражением вместо анонимного метода
        //if (newText.All(char.IsDigit))
        //    return;

        // Ищем в новом тексте нет недопустимых символов (не цифр), и если их нет — выходим из метода
        if (Enumerable.All(newText, char.IsDigit)) return;



        // Если в новом тексте есть недопустимые символы, то фильтруем его, оставляя только цифры, и обновляем текст в поле ввода
        entry.Text = new string(newText.Where(char.IsDigit).ToArray());
    }






    // Обработчик для кнопки считывания порта ServerPort2    RowDefinitions="Auto,Auto,12,Auto,Auto,12,Auto,Auto,12,Auto,Auto,12,Auto,16"
    private async void ServerPort2(object sender, EventArgs e)
    {

        if (sender is not Button btn)
            return;

        btn.IsEnabled = false;

        try
        {
            var text = ServerPort2Entry?.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Помилка", "Введіть значення порту", "OK");
                return;
            }

            if (!int.TryParse(text, out var port) || port < 1 || port > 65535)
            {
                await DisplayAlert("Помилка", "Порт має бути числом від 1 до 65535", "OK");
                return;
            }

            var param = port.ToString();

            await _bluetoothService.TransmitterData_write(param, 0x02, 0x01, null, 0x05, true);

            await DisplayAlert("Успіх", $"Порт записано: {port}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnMicClicked exception: {ex}");
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            btn.IsEnabled = true;
        }


    }


    private void ServerPort2Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var newText = e.NewTextValue ?? string.Empty;

        if (newText.All(char.IsDigit))
            return;

        entry.Text = new string(newText.Where(char.IsDigit).ToArray());
    }



    // Обработчик для кнопки считывания порта CodeObject1
    private void CodeObject1_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var newText = e.NewTextValue ?? string.Empty;

        if (newText.All(char.IsDigit))
            return;

        entry.Text = new string(newText.Where(char.IsDigit).ToArray());
    }


    // Обработчик для кнопки считывания порта CodeObject1
    private async void CodeObject1(object sender, EventArgs e)
    {

        if (sender is not Button btn)
            return;

        btn.IsEnabled = false;

        try
        {
            var text = CodeObject1Entry?.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Помилка", "Введіть значення порту", "OK");
                return;
            }

            if (!int.TryParse(text, out var port) || port < 1 || port > 65535)
            {
                await DisplayAlert("Помилка", "Порт має бути числом від 1 до 65535", "OK");
                return;
            }

            var param = port.ToString();

            await _bluetoothService.TransmitterData_write(param, 0x02, 0x01, null, 0x01, true);

            await DisplayAlert("Успіх", $"Порт записано: {port}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"OnMicClicked exception: {ex}");
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            btn.IsEnabled = true;
        }


    }



    // Обработчик для кнопки считывания порта AccessPoint1
    private async void AccessPoint1(object sender, EventArgs e)
    {
        if (sender is not Button btn)
            return;

        btn.IsEnabled = false;

        try
        {
            var text = AccessPoint1Entry?.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Помилка", "Введіть значення", "OK");
                return;
            }

            // при желании ограничь длину/набор символов
            if (text.Length > 64)
            {
                await DisplayAlert("Помилка", "Значення занадто довге", "OK");
                return;
            }

            await _bluetoothService.TransmitterData_write(text, 0x02, 0x01, null, 0x02, true);

            await DisplayAlert("Успіх", $"Значення записано: {text}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AccessPoint1 exception: {ex}");
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }



    //private void AccessPoint1_TextChanged(object sender, TextChangedEventArgs e) 
    //{

    //    if (sender is not Entry entry)
    //        return;

    //    var newText = e.NewTextValue ?? string.Empty;

    //    if (newText.All(char.IsDigit))
    //        return;

    //    entry.Text = new string(newText.Where(char.IsDigit).ToArray());



    //}



    private void AccessPoint1Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var newText = e.NewTextValue ?? string.Empty;
        var filtered = newText.Replace("\r", "").Replace("\n", "");

        if (filtered != newText)
            entry.Text = filtered;
    }


    // Обработчик для кнопки считывания порта AccessPoint2
    private async void AccessPoint2(object sender, EventArgs e)
    {
        if (sender is not Button btn)
            return;

        btn.IsEnabled = false;

        try
        {
            var text = AccessPoint2Entry?.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Помилка", "Введіть значення", "OK");
                return;
            }

            // при желании ограничь длину/набор символов
            if (text.Length > 14)
            {
                await DisplayAlert("Помилка", "Значення занадто довге", "OK");
                return;
            }

            await _bluetoothService.TransmitterData_write(text, 0x02, 0x01, null, 0x03, true);

            await DisplayAlert("Успіх", $"Значення записано: {text}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AccessPoint1 exception: {ex}");
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }



    private void AccessPoint2Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var newText = e.NewTextValue ?? string.Empty;
        var filtered = newText.Replace("\r", "").Replace("\n", "");

        if (filtered != newText)
            entry.Text = filtered;
    }





    // Обработчик для кнопки считывания порта IpServer1
    private async void IpServer1(object sender, EventArgs e)
    {
        if (sender is not Button btn)
            return;

        btn.IsEnabled = false;

        try
        {
            var text = IpServer1Entry?.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Помилка", "Введіть значення", "OK");
                return;
            }

            // при желании ограничь длину/набор символов
            if (text.Length > 20)
            {
                await DisplayAlert("Помилка", "Значення занадто довге", "OK");
                return;
            }

            await _bluetoothService.TransmitterData_write(text, 0x02, 0x01, null, 0x06, true);

            await DisplayAlert("Успіх", $"Значення записано: {text}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AccessPoint1 exception: {ex}");
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }




    private void IpServer1Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Выходим, если sender не является Entry (на всякий случай, хотя по идее так должно быть всегда)

        if (sender is not Entry entry)
            return;
        // Получаем новое значение текста из события (e.NewTextValue).
        var newText = e.NewTextValue ?? string.Empty;
        // Удаляем не нужные символы
        var filtered = newText.Replace("\r", "").Replace("\n", "");
        // Если после фильтрации текст изменился, то обновляем текст в поле ввода, чтобы убрать недопустимые символы
        if (filtered != newText)
            entry.Text = filtered;
    }




    /// ///////////////////////
    private void Hours1Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var newText = e.NewTextValue ?? string.Empty;

        if (newText.All(char.IsDigit))
            return;

        entry.Text = new string(newText.Where(char.IsDigit).ToArray());
    }


    private void Minutes1Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        var newText = e.NewTextValue ?? string.Empty;

        if (newText.All(char.IsDigit))
            return;

        entry.Text = new string(newText.Where(char.IsDigit).ToArray());
    }


    //private async void Hours1(object sender, EventArgs e)
    //{
    //    if (sender is not Button btn)
    //        return;

    //    btn.IsEnabled = false;

    //    try
    //    {
    //        var text = Hours1Entry?.Text?.Trim();

    //        if (string.IsNullOrEmpty(text))
    //        {
    //            await DisplayAlert("Помилка", "Введіть години", "OK");
    //            return;
    //        }

    //        if (!int.TryParse(text, out var hours) || hours < 0 || hours > 23)
    //        {
    //            await DisplayAlert("Помилка", "Години мають бути від 0 до 23", "OK");
    //            return;
    //        }

    //        await _bluetoothService.TransmitterData_write(text, 0x02, 0x01, null, 0x10, true);

    //        await DisplayAlert("Успіх", $"Години записано: {hours}", "OK");
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Hours1 exception: {ex}");
    //        await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
    //    }
    //    finally
    //    {
    //        btn.IsEnabled = true;
    //    }
    //}

    private async void Hours1(object sender, EventArgs e)
    {
        if (sender is not Button btn)
            return;

        btn.IsEnabled = false;

        try
        {
            var text = Hours1Entry?.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Помилка", "Введіть години", "OK");
                return;
            }

            if (!int.TryParse(text, out var hours) || hours < 0 || hours > 23)
            {
                await DisplayAlert("Помилка", "Години мають бути від 0 до 23", "OK");
                return;
            }

            await _bluetoothService.TransmitterData_writeByte((byte)hours, 0x02, 0x01, null, 0x10, true);

            await DisplayAlert("Успіх", $"Години записано: {hours}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hours1 exception: {ex}");
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }



    //private async void Minutes1(object sender, EventArgs e)
    //{
    //    if (sender is not Button btn)
    //        return;

    //    btn.IsEnabled = false;

    //    try
    //    {
    //        var text = Minutes1Entry?.Text?.Trim();

    //        if (string.IsNullOrEmpty(text))
    //        {
    //            await DisplayAlert("Помилка", "Введіть хвилини", "OK");
    //            return;
    //        }

    //        if (!int.TryParse(text, out var minutes) || minutes < 0 || minutes > 59)
    //        {
    //            await DisplayAlert("Помилка", "Хвилини мають бути від 0 до 59", "OK");
    //            return;
    //        }

    //        await _bluetoothService.TransmitterData_writeByte(text, 0x02, 0x01, null, 0x11, true);

    //        await DisplayAlert("Успіх", $"Хвилини записано: {minutes}", "OK");
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Minutes1 exception: {ex}");
    //        await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
    //    }
    //    finally
    //    {
    //        btn.IsEnabled = true;
    //    }
    //}


    private async void Minutes1(object sender, EventArgs e)
    {
        if (sender is not Button btn)
            return;

        btn.IsEnabled = false;

        try
        {
            var text = Minutes1Entry?.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                await DisplayAlert("Помилка", "Введіть хвилини", "OK");
                return;
            }

            if (!int.TryParse(text, out var minutes) || minutes < 0 || minutes > 59)
            {
                await DisplayAlert("Помилка", "Хвилини мають бути від 0 до 59", "OK");
                return;
            }

            await _bluetoothService.TransmitterData_writeByte((byte)minutes, 0x02, 0x01, null, 0x11, true);

            await DisplayAlert("Успіх", $"Хвилини записано: {minutes}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Minutes1 exception: {ex}");
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }





    ///////////////////////////





    private void SetPortLabelText(string? key, string text)
    {
        switch (key)
        {

            case "2|CodeObject1":
                CodeObject1ValueLabel.Text = text;
                break;


            case "2|AccessPoint1":
                AccessPoint1ValueLabel.Text = text;
                break;


            case "2|AccessPoint2":
                AccessPoint2ValueLabel.Text = text;
                break;


            case "2|ServerPort1":
                ServerPortValueLabel.Text = text;
                break;

            case "2|ServerPort2":
                ServerPort2ValueLabel.Text = text;
                break;


            case "2|ipServer1":
                IpServer1ValueLabel.Text = text;
                break;



            case "2|Hours1":
                Hours1ValueLabel.Text = text;
                break;

            case "2|Minutes1":
                Minutes1ValueLabel.Text = text;
                break;




            case "2|UDP1":

                if (text == "1")
                {
                    _enableOption = false;
                    ApplyEnableOptionUi(false);
                }
                if (text == "2")
                {

                    _enableOption = true;
                    ApplyEnableOptionUi(true);
                }   
               

                //IpServer1ValueLabel.Text = text;
                break;



            case "2|ACK1":
                {
                    if (text == "0") { _enableAck = false; ApplyEnableAck(false); }
                    if (text == "1") { _enableAck = true; ApplyEnableAck(true); }
                    break;
                }








            default:
                // запасной вариант — чтобы хоть куда-то вывести
                ServerPortValueLabel.Text = text;
                break;
        }
    }


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



    private void OnEnableOptionTapped(object sender, EventArgs e)
    {
        if (_waitingResponse) return; //Навсяк случай блокируем повторный запрос, пока не придёт ответ на предыдущий
        RequestRead("2|UDP1");

      //  ApplyEnableOptionUi(!_enableOption);

        // если нужно — тут же отправляй команду на запись (включить/выключить)
        // пример (условно): отправить 1 или 2 в зависимости от _enableOption
    }


    private void OnEnableACK1Tapped(object sender, EventArgs e)
    {
        if (_waitingResponse) return; //Навсяк случай блокируем повторный запрос, пока не придёт ответ на предыдущий
        RequestRead("2|ACK1");

        //  ApplyEnableOptionUi(!_enableOption);

        // если нужно — тут же отправляй команду на запись (включить/выключить)
        // пример (условно): отправить 1 или 2 в зависимости от _enableOption
    }




    private async void OnUDP1Tapped(object sender, EventArgs e)
    {
        if (_waitingResponse) return;

        EnableOptionBox.IsEnabled = false;
        try
        {
            if (!_enableOption) { await _bluetoothService.TransmitterData_writeByte(0x02, 0x02, 0x01, null, 0x0A, true); }
            else { await _bluetoothService.TransmitterData_writeByte(0x01, 0x02, 0x01, null, 0x0A, true); }
           
            await Task.Delay(300); // 200-500мс, подбери
            RequestRead("2|UDP1"); // чтобы сразу обновить UI с устройства
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }
        finally
        {
            EnableOptionBox.IsEnabled = true;
        }
    }





    private void ApplyEnableOptionUi(bool enabled)
    {
        _enableOption = enabled;

        // Вариант 1: галочка
        EnableOptionCheck.Text = _enableOption ? "✓" : "";

        // Вариант 2: фон
        EnableOptionBox.BackgroundColor = _enableOption
            ? Color.FromArgb("#2E7D32")
            : Colors.Transparent;

        // Обводка
        EnableOptionBox.Stroke = _enableOption
            ? Color.FromArgb("#2E7D32")
            : Colors.White;
    }



    private async void RequestRead(string key)
    {
        if (!_commands.TryGetValue(key, out var asciiHex))
        {
            Debug.WriteLine($"Command not found: {key}");
            return;
        }

        _lastSelectedCommand = asciiHex;
        _lastRequestKey = key;
        _waitingResponse = true;

        _waitCts?.Cancel();
        _waitCts?.Dispose();
        _waitCts = new CancellationTokenSource();
        var token = _waitCts.Token;

        Debug.WriteLine($"[TX] key={_lastRequestKey} cmd={_lastSelectedCommand} at={DateTime.Now:HH:mm:ss.fff}");
        // Запускаем паралельную задачу в отдельном потоке для отслеживания таймаута, чтобы не блокировать UI
        _ = Task.Run(async () =>
        {
            try
            {
                // делаем задержку и уходим из фона в главный поток
                await Task.Delay(2500, token);
                // Далее в фоне ставим задачу в очередь главного потока
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Выполняем в главном потоке, если не пришел ответ и мы всё ещё ждём, то показываем таймаут
                    if (_waitingResponse)
                    {
                        _waitingResponse = false;
                        Debug.WriteLine($"[TIMEOUT] key={_lastRequestKey} at={DateTime.Now:HH:mm:ss.fff}");
                        SetPortLabelText(_lastRequestKey, "⚠️");
                    }
                });
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[TIMEOUT-CANCELED] key={_lastRequestKey} at={DateTime.Now:HH:mm:ss.fff}");
            }
        });
        // Отправляем команду на чтение после запуска фона для таймаута
        await _bluetoothService.TransmitterData(_lastSelectedCommand);
    }




    // Обработчик для кнопки считывания порта ACK1
    private async void OnACK1Tapped(object sender, EventArgs e)
    {
        if (_waitingResponse) return;

        EnableAckBox.IsEnabled = false;
        try
        {
            // ACK: 0/1 (инверсия)
            //byte valueToWrite = _enableAck ? (byte)0x00 : (byte)0x01;

            //await _bluetoothService.TransmitterData_writeByte(valueToWrite, 0x02, 0x01, null, 0x0C, true);


            if (!_enableAck) { await _bluetoothService.TransmitterData_writeByte(0x01, 0x02, 0x01, null, 0x0C, true); }
            else { await _bluetoothService.TransmitterData_writeByte(0x00, 0x02, 0x01, null, 0x0C, true); }




            await Task.Delay(300);
            RequestRead("2|ACK1");
        }

        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Сталася помилка: {ex.Message}", "OK");
        }


        finally
        {
            EnableAckBox.IsEnabled = true;
        }
    }


    private void ApplyEnableAck(bool enabled)
    {
        _enableAck = enabled;

        // Вариант 1: галочка
        EnableAckCheck.Text = _enableAck ? "✓" : "";

        // Вариант 2: фон
        EnableAckBox.BackgroundColor = _enableAck
            ? Color.FromArgb("#2E7D32")
            : Colors.Transparent;

        // Обводка
        EnableAckBox.Stroke = _enableAck
            ? Color.FromArgb("#2E7D32")
            : Colors.White;
    }




}
// Реакция на ввод (например, обновить значение Label)
//ServerPortValueLabel.Text = ServerPortEntry.Text;
//DisplayAlert("Server Port", $"Вы ввели: {ServerPortEntry.Text}", "OK");