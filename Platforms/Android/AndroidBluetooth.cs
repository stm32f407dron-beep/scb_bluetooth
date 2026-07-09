using Android.Bluetooth;
using Android.Content;
using Android.Util;
using AndroidX.ConstraintLayout.Core.Motion.Utils;
using skb_home.Models;
using System;
using System.Collections.Generic;
using System.Text;
using static Android.Views.TextClassifiers.TextLinks;
using static Microsoft.Maui.ApplicationModel.Permissions;
using static System.Net.Mime.MediaTypeNames;

namespace skb_home.Platforms.Android
{
    public class AndroidBluetooth : IBluetooth_service
    {
        // Событие для уведомления об окончании поиска устройств
        public event Action DiscoveryFinished;
        // блюютуз адаптер и контекст приложения — нужны для управления Bluetooth и регистрации ресивера
        private BluetoothAdapter? _adapter;
        // Контекст приложения, используется для регистрации BroadcastReceiver и доступа к системным сервисам и отображения Toast'ов.       
        private Context? _context;
        // Ресивер для получения уведомлений о найденных устройствах и завершении поиска.
        private BroadcastReceiver _receiver;
        // Сокет для связи с устройством после подключения, доступен  для других частей приложения, которые могут захотеть читать/писать данные.
        public BluetoothSocket? bluetoothSocket;

        // Событие для передачи найденных устройств. Когда ресивер обнаруживает устройство, он вызывает это событие, передавая информацию об устройстве.
        public event Action<Device_info> DeviceDiscovered;

        // Флаг для отслеживания, запущен ли процесс приема данных. Это может быть полезно для управления жизненным циклом потока приема данных.
        private volatile bool _rxRunning = false;

        // Флаг для отслеживания, запущен ли процесс приема данных для Varta832.
        private volatile bool _rxRunningVarta832 = false;


        private volatile bool _anyRxRunning = false;
       




        //public string[] TypeValue = {
        //            "Not Data",   // 0
        //            "Short",      // 1
        //            "Breakage",   // 2
        //            "Indefinite", // 3
        //            "Logical 1",  // 4
        //            "Logical 2",  // 5
        //            "Power-off",  // 6
        //            "NORM",       // 7
        //            "ON",         // 8
        //            "OFF",        // 9
        //            "Alarm",      // A (10)
        //            "ATTENTION",  // B (11)
        //            "Fire",       // C (12)
        //            "BLOCKING",   // D (13)
        //            "Deblocking", // E (14)
        //            "LOST"        // F (15)
        //        };


        // Реализация свойства
        public string[] TypeValue { get; } = {
            "Not Data", "Short", "Breakage", "Indefinite",
            "Logical 1", "Logical 2", "Power-off", "NORM",
            "ON", "OFF", "Alarm", "ATTENTION",
            "Fire", "BLOCKING", "Deblocking", "LOST"
        };







        public AndroidBluetooth()
        {
            _adapter = BluetoothAdapter.DefaultAdapter;// Получаем BluetoothAdapter по умолчанию. Это точка входа для всех операций Bluetooth.
            _context = Platform.AppContext;// Получаем контекст приложения. Он нужен для регистрации BroadcastReceiver и доступа к системным сервисам.
        }

        public async Task<bool> StartScanningAsync()
        {
            if (_adapter == null || !_adapter.IsEnabled)
                return false;

            // Platform.CurrentActivity возвращает тип Activity,
            //а  код ожидает именно мой класс активности — MainActivity.
            // Оператор as пытается привести объект к типу MainActivity.
            //Если привести не удалось(например, если активити — это не мой класс, а другой) — результат будет null, а не Exception.
            var activity = Platform.CurrentActivity as MainActivity;
            if (activity == null)
                return false;
            // Запрашиваем разрешения у пользователя - если их ещё нет
            bool granted = await BluetoothPermissionsHelper.RequestBluetoothPermissionsAsync(activity);
            if (!granted)
                return false;
            //Очистка старого Receiver (если был)
            if (_receiver != null)
            {
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
#pragma warning disable CA1416 // Проверка совместимости платформы
                _context.UnregisterReceiver(_receiver);
#pragma warning restore CA1416 // Проверка совместимости платформы
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
                _receiver = null;
            }

            // Создаём новый ресивер и передаём ему два делегата:
            //  - при обнаружении устройства вызывается DeviceDiscovered (если кто-то на него подписан)
            //  - при завершении обнаружения вызывается DiscoveryFinished
            // Итого: да — эти анонимные делегаты — и есть те «методы», которые будут вызываться внутри _receiver
            // и которые в свою очередь триггерят события.
            _receiver = new Device_Receiver(

                delegate (Device_info onDeviceFound) { DeviceDiscovered?.Invoke(onDeviceFound); },
                delegate () { DiscoveryFinished?.Invoke(); }

            );
            // Формируем фильтр интентов: интересуют события найденного устройства и завершения поиска.
            IntentFilter filter = new IntentFilter(BluetoothDevice.ActionFound);//// уведомления о найденных устройствах
            filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished); // Добавляем фильтр для события уведомление об окончании discovery

            // Регистрируем ресивер в контексте приложения. После регистрации он будет получать указанные Broadcast'ы.
            _context.RegisterReceiver(_receiver, filter);
            //// Запускаем процесс классического Bluetooth-сканирования (inquiry). Это асинхронный процесс, результаты придут через BroadcastReceiver.
            _adapter.StartDiscovery();


            // Toast.MakeText(_context, "Сканирование началось", ToastLength.Short).Show();
            // Возвращаем true — сканирование успешно инициировано (фактические устройства будут приходить в событии DeviceDiscovered).
            return true;
        }



        public async Task<bool> ConnectToDeviceAsync(Device_info deviceInfo)
        {

            // Получаем удалённое устройство по его MAC-адресу.
            // используется условный оператор ?. — если _adapter == null, device будет null. 
            // Что делает: у адаптера Bluetooth (поле _adapter) вызывается GetRemoteDevice с MAC-адресом.
            // Возвращает объект BluetoothDevice, представляющий удалённое устройство.
            // нужен объект BluetoothDevice для создания сокета и подключения
            var device = _adapter?.GetRemoteDevice(deviceInfo.Address);
            // Устройство не найдено
            if (device == null)
            {
                Log.Error("BTPerms", $"GetRemoteDevice returned null for {deviceInfo?.Address}");
                return false;
            }

            // //try/catch: если отмена discovery вызовет исключение, оно будет поймано и залогировано как предупреждение.
            try
            {
                // отменяет текущее Bluetooth discovery (сканирование).
                _adapter?.CancelDiscovery();
                // Логирование: Info о том, что отмена вызвана.           
                Log.Info("BTPerms", "CancelDiscovery called before connect");
            }
            catch (Exception ex)
            {
                Log.Warn("BTPerms", $"CancelDiscovery failed: {ex}");
            }
            // Стандартный UUID для Serial Port Profile (SPP)   
            Java.Util.UUID sppUuid = Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
            // Локальная переменная для сокета
            // создание сокета локально, а не сразу присваивание глобальному полю — удобнее при ошибках и закрытии.
            BluetoothSocket localSocket = null;

            try
            {
                // Создаём небезопасный RFCOMM сокет для подключения к устройству по SPP UUID
                // Небезопасный означает, что не используется шифрование или аутентификация
                // CreateInsecureRfcommSocketToServiceRecord пропускает часть безопасной шифровки/аутентификации,
                // часто проще для старых модулей (HC-06). CreateRfcommSocketToServiceRecord (secure) требует pairing/secure channel.
                localSocket = device.CreateInsecureRfcommSocketToServiceRecord(sppUuid);


                // Проверка на null (хотя CreateInsecure... обычно не возвращает null)
                if (localSocket == null)
                {
                    Log.Error("BTPerms", "localSocket is null");
                    return false;
                }

                // создание задачи для подключения сокета (ложим ее в пул потоков) и получаем маркер task
                // создаёт задачу, выполняющую вызов Connect() в пуле потоков, и возвращает объект Task, который служит «маркером» (handle) для этой фоновой работы
                Task task = Task.Run(delegate () { localSocket.Connect(); });

                //  var connectTask = Task.Run(() => localSocket.Connect());
                // Ждём либо завершения подключения, либо таймаута в 14 секунд  
                var completed = await Task.WhenAny(task, Task.Delay(14000)); // 14s timeout
                                                                             //Если первой завершилась Task.Delay (т.е. connect не успел за 14s), логируем timeout, закрываем socket и возвращаем false.
                                                                             //закрывать локальный сокет после таймаута важно, иначе ресурс остаётся открытым и мешает следующим попыткам.
                if (completed != task)
                {
                    Log.Error("BTPerms", "Connect timeout");
                    try { localSocket.Close(); } catch { }
                    return false;
                }

                ///////////////
                // Здесь completed == task — проверим его состояние
                if (task.IsCompletedSuccessfully)
                {
                    // Успешно — можно продолжать
                    bluetoothSocket = localSocket;
                }
                else if (task.IsCanceled)
                {
                    // Отменено
                    Log.Warn("BTPerms", "Connect was canceled");
                    try { localSocket.Close(); } catch { }
                    return false;
                }
                else if (task.IsFaulted)
                {
                    // Упало с исключением — Exception хранится в connectTask.Exception (AggregateException)
                    var agg = task.Exception; // AggregateException
                    var ex = agg?.GetBaseException(); // реальная причина
                    Log.Error("BTPerms", $"Connect failed: {ex}");
                    try { localSocket.Close(); } catch { }
                    return false;
                }



                Log.Info("BTPerms", $"Socket connected to {device.Address} name={device.Name}");
                return true;
            }
            catch (Java.IO.IOException ioEx)
            {
                Log.Error("BTPerms", $"IOException during connect/read: {ioEx}");
                try { localSocket?.Close(); } catch { }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("BTPerms", $"Exception during connect: {ex}");
                try { localSocket?.Close(); } catch { }
                return false;
            }
        }


        // Событие для передачи строки данных, полученной из Bluetooth. Когда данные приходят (например, в другом потоке, который читает из сокета),
        // вызывается это событие, передавая строку данных подписчикам. Action<string> метод, принимающий string и ничего не возвращающий (void)
        
        public event Action<string> DataReceived;
        // задача по приему данных. Реализация будет включать чтение из bluetoothSocket в цикле и вызов DataReceived при получении данных.
        public async Task ReceiverData()
        {
            // Если приём данных уже запущен, не запускаем второй раз. Это предотвращает создание
            // нескольких параллельных задач приёма, что может привести к конфликтам и ошибкам.
            if (_anyRxRunning || _rxRunning) return;

            _anyRxRunning = true;
            _rxRunning = true;

            //Буфер для чтения данных
            byte[] buffer = new byte[4096];
            // Константы для определения начала и конца фрейма данных. маркер начала кадра(RESP_START = 0x02) и маркер конца кадра(FRAME_STOP = 0x05)
            const byte RESP_START = 0x02;
            const byte FRAME_STOP = 0x05;
            // Список для накопления байт, если данные приходят фрагментами. Это может быть полезно, если сообщения не приходят целиком за один раз.
            var recvBuf = new List<byte>();

            try
            {                   
              // получает поток ввода (InputStream) из объекта bluetoothSocket - из сокета
              var input = bluetoothSocket?.InputStream;          
             // Проверяем, что поток не null, eсли bluetoothSocket не был успешно подключён
             if (input == null)
             {
                //вызов события DataReceived с сообщением об ошибке.
                DataReceived?.Invoke("Error: InputStream is null");
                // Сбрасываем флаг приёма, так как мы не можем продолжать без потока.
                _rxRunning = false;
                // Выходим из метода.
                return;
             }
                 
                // Основной цикл приёма данных. Внутри цикла будет чтение из потока и обработка данных.
                // непрерывно слушаем входящий поток без повторного вызова метода извне.
                //Поведение с async / await: так как метод асинхронный и внутри цикла есть await (await Task.Delay / await Task.Run(...)),
                //цикл не будет блокировать поток вызова(UI). await уступает управление, позволяя другому коду исполняться,
                //а затем цикл продолжит работу при возобновлении.
                while (_rxRunning) 
                {
                    // Проверяем, что Bluetooth всё ещё включён
                    if (! _adapter.IsEnabled)
                    {
                        // Если Bluetooth отключён, вызываем событие DataReceived с сообщением об ошибке, а затем прерываем цикл.
                        DataReceived.Invoke("Error: Bluetooth is disabled");
                        // Закрываем сокет, так как связь с устройством потеряна из-за отключения Bluetooth.
                        try { bluetoothSocket?.Close(); }  catch { }
                        // Отписать/удалить ранее зарегистрированный BroadcastReceiver из контекста приложения.
                        try { _context.UnregisterReceiver(_receiver); } catch { }
                        // Выход из while (_rxRunning) и поппадание в блок finally, где _rxRunning будет сброшен.
                        break;

                    }
                    // небольшой delay в 30ms для предотвращения чрезмерного использования CPU при чтении из потока.
                    await Task.Delay(30);

                    //Объявление  переменной  для хранения фактического число байт,
                    // прочитанных одним вызовом Read. Используется далее, чтобы знать, сколько байт взять из буфера.
                    int bytesRead;


                    //═══════════════════════════════════════════════════════════════════════════
                    //              ЭТАП 1: ПРИЕМ БАЙТОВ ИЗ BLUETOOTH
                    //═══════════════════════════════════════════════════════════════════════════

                    try
                    {                    
                        //  Создаем делегат для чтения (синхронная операция)
                        Func<int> func = delegate () { return input.Read(buffer, 0, buffer.Length); };
                        //  Запускаем в фоновом потоке и асинхронно ожидаем(Выполняем чтение в пуле потоков, чтобы не блокировать UI-поток.
                        //  Результат (кол-во байт) сохраняем в bytesRead.)                 
                        bytesRead = await Task.Run(func); // или так -  bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));

                    }

                    catch (Exception readEx) 
                    {
                        // Если при чтении из потока возникло исключение,вызываем событие DataReceived с сообщением об ошибке.                     
                        DataReceived?.Invoke($"Error reading from stream: {readEx.Message}");
                        break; // Выходим из цикла при ошибке чтения
                    }

                    // Если bytesRead == 0, это может означать, что данных нет в данный момент, но соединение всё ещё открыто.
                    // В этом случае просто продолжаем цикл и ждём следующего чтения.
                    // Если байт прочитано 0 или меньше (обычно -1 означает конец потока), обрабатываем это как закрытие соединения или отсутствие данных.
                    if (bytesRead <= 0)
                    {
                       
                        if (bytesRead == -1) {
                            // Соединение закрыто удаленным устройством
                            DataReceived?.Invoke("Connection closed by remote device");
                            try { bluetoothSocket?.Close(); } catch { }
                          //  try { _context.UnregisterReceiver(_receiver); } catch { }
                            break; // Выход из цикла
                        }

                      // Если данных нет, продолжаем цикл (ждём следующего чтения) continue возвращает управление в начало внешнего while (_rxRunning)
                      continue; 
                    }


                    //═══════════════════════════════════════════════════════════════════════════
                    //              ЭТАП 2: НАКОПЛЕНИЕ В БУФЕРЕ
                    //═══════════════════════════════════════════════════════════════════════════

                    // Если байт прочитано, добавляем их в recvBuf для дальнейшей обработки. bytesRead указывает, сколько байт в buffer являются валидными данными.
                    for (int i = 0; i < bytesRead; i++) recvBuf.Add(buffer[i]);

                    // Проверяем, есть ли в recvBuf полный фрейм данных (начинается с RESP_START и заканчивается FRAME_STOP)
                    while (true) 
                    {

                        //═══════════════════════════════════════════════════════════════════════════
                        //              ЭТАП 3: ПОИСК ФРЕЙМА
                        //═══════════════════════════════════════════════════════════════════════════
                        
                        // Ищем индекс начала фрейма (RESP_START) в recvBuf. Если его нет, то данных для обработки нет, и мы можем выйти из внутреннего цикла.
                        int start = recvBuf.IndexOf(RESP_START);

                        // Если маркера начала нет, это означает, что в буфере нет валидных данных для обработки.
                        if (start == -1) 
                        {
                            //  if (recvBuf.Count > 8192) recvBuf.Clear(); // условная очистка буфера, если он слишком большой (можно настроить порог по необходимости)
                            // Если маркера начала нет, это означает, что в буфере нет валидных данных для обработки.
                            // В этом случае мы можем очистить буфер, чтобы избавиться от мусора, и выйти из цикла обработки.
                            recvBuf.Clear();
                            // Если нет маркера начала, удаляем всё до текущего момента, так как это мусор, и выходим из цикла обработки, в цикл приема данных   while (_rxRunning) 
                            break; 
                        }
                        //  Создаём предикат для поиска индекса конца фрейма (FRAME_STOP) после найденного начала.
                        //  Этот предикат будет использоваться в FindIndex для поиска первого байта, который равен FRAME_STOP.
                        Predicate<byte> stopPredicate = delegate (byte b) { return b == FRAME_STOP; };

                        // Ищем индекс конца фрейма (FRAME_STOP) в recvBuf, начиная с позиции после найденного начала.
                        int stop = recvBuf.FindIndex(start + 1, stopPredicate); // или так - int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);

                        if (stop == -1) 
                        {
                            // удаляем всё до найденного начала, так как это мусор, и выходим из цикла обработки. Оставляем в буфере только данные от маркера начала и дальше
                            if (start > 0) { recvBuf.RemoveRange(0,start); }

                            // Если маркера конца нет, это означает, что фрейм данных ещё не полностью получен. В этом случае мы должны подождать,
                            // пока не придут остальные данные, и выйти из цикла обработки.
                            // Выходим из цикла обработки, чтобы дождаться следующего чтения данных, возвращаемся   в цикл приема данных   while (_rxRunning)
                            break;
                        }

                        //═══════════════════════════════════════════════════════════════════════════
                        //              ЭТАП 4: ИЗВЛЕЧЕНИЕ ASCII ДАННЫХ
                        //═══════════════════════════════════════════════════════════════════════════

                        // Длина данных между RESP_START и FRAME_STOP (исключая сами маркеры)
                        int asciiLen = stop - (start + 1);

                        // пришел ли пустой фрейм (например, 0x02 0x05) — если да, удаляем его из буфера и продолжаем поиск следующего фрейма.
                        if (asciiLen <= 0) 
                        {
                            recvBuf.RemoveRange(0, stop + 1);// Удалить [0x02, 0x05]
                            continue; // продолжить поиск следующего фрейма
                        }

                        // Получаем байты между RESP_START и FRAME_STOP и ложим эти байты в массив.
                        byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();


                        //═══════════════════════════════════════════════════════════════════════════
                        //              ЭТАП 5: ASCII ДЕКОДИРОВАНИЕ (БАЙТЫ → ТЕКСТ)                            
                        //═══════════════════════════════════════════════════════════════════════════
                                                     // и
                        //═══════════════════════════════════════════════════════════════════════════
                        //              ЭТАП 6: ОЧИСТКА СТРОКИ
                        //═══════════════════════════════════════════════════════════════════════════

                        // перевод в строку: преобразуем байты в строку, удаляем символы переноса и пробелы, и приводим к верхнему регистру для удобства обработки.
                        string asciiHex = Encoding.ASCII.GetString(asciiBytes) .Replace("\r", "").Replace("\n", "").Trim().ToUpperInvariant();

                        // удаляем из recvBuf всё до конца обработанного фрейма (включая FRAME_STOP), чтобы в буфере остались только необработанные данные.
                        recvBuf.RemoveRange(0, stop + 1);


                        //══════════════════════════════��════════════════════════════════════════════
                        //              ЭТАП 7: ПРОВЕРКА ЧЕТНОСТИ
                        //═══════════════════════════════════════════════════════════════════════════
                        // Проверяем, что длина строки с шестнадцатеричным представлением данных чётная. 
                        if (asciiHex.Length % 2 != 0)
                        {
                            // Если длина нечётная, это означает, что данные не являются корректным
                            // шестнадцатеричным представлением (каждые 2 символа должны представлять 1 байт).
                            DataReceived?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
                            // Если данные некорректные, мы можем проигнорировать этот фрейм и продолжить обработку следующих данных.
                            continue;

                        }

                        //═══════════════════════════════════════════════════════════════════════════
                        //              ЭТАП 8: HEX-ТЕКСТ → БАЙТЫ (обратное преобразование)
                        //═══════════════════════════════════════════════════════════════════════════

                        byte[] frameBytes;// Массив для хранения байт, полученных из шестнадцатеричной строки - результат преобразования asciiHex в байты.
                        try 
                        {
                            //создаёт массив байт длиной asciiHex.Length / 2 (каждые два HEX‑символа — один байт)
                            frameBytes = new byte[asciiHex.Length / 2];
                            // Цикл для преобразования каждой пары символов в байт. Convert.ToByte с основанием 16 преобразует строку в байт.
                            for (int i = 0; i < frameBytes.Length; i++)
                             frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
                          
                        }
                        catch (Exception e)
                        {
                            DataReceived?.Invoke($"RX ERROR: invalid hex -> {asciiHex}");
                            continue;
                        }
                        //═══════════════════════════════════════════════════════════════════════════
                        //              ЭТАП 9: ПРОВЕРКА CHECKSUM
                        //═══════════════════════════════════════════════════════════════════════════


                        // Проверка контрольной суммы: по умолчанию считаем, что контрольная сумма не прошла (false).
                        bool chkOk = false;
                        if (frameBytes.Length >= 1)//выполняем проверку только если в массиве есть хотя бы один байт (иначе нечего проверять).
                        {
                            int sum = 0;
                            //цикл суммирует байты от 0 до frameBytes.Length - 1 (все байты, кроме последнего - чексуммы)                         
                            for (int i = 0; i < frameBytes.Length - 1; i++) sum += frameBytes[i];
                            //Затем вычисляется сумма по модулю(тоесть максимум) 256 (sum & 0xFF) и сравнивается с последним байтом.
                            chkOk = ((byte)(sum & 0xFF)) == frameBytes[frameBytes.Length - 1];
                        }


                        //превращает массив байт в строку с шестнадцатеричным представлением без дефисов.
                        string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");
                        if (chkOk)
                        {
                            // Конвертирует байты (без последнего - checksum) в HEX-строку
                            string payloadHex = frameBytes.Length > 1 ? BitConverter.ToString(frameBytes, 0, frameBytes.Length - 1).Replace("-", "")  : string.Empty;
                            DataReceived?.Invoke($"RX OK payload={payloadHex} full={fullHex}");
                        }
                        else
                        {
                          //  DataReceived?.Invoke($"RX CHK ERROR payload={fullHex}");

                            // формируем формат

                            //1.Берём значение длины данных из frameBytes[7]
                            int length = frameBytes[7];

                            // создаём и заполняем массив под индекс
                            byte[] index = new byte[4];

                            // создаём и заполняем массив под  субиндекс
                            byte subindex = frameBytes[6];

                            string subindexString = subindex.ToString("D");

                            // Копируем 4 байта из frameBytes (с позиции 2) в index (с позиции 0)
                            Array.Copy(frameBytes, 2, index, 0, 4);

                            //  string indexlString = string.Join("", index.Select(b => b.ToString("D")));
                            string indexlString = BitConverter.ToString(index).Replace("-", ""); // пример: "00030139"


                            if (indexlString == "00002200") //Страница 2 TK_Setting
                            {

                                ////////////////////////////////////////
                                //CodeObject1
                                if (subindex == 0x01)
                                {
                                    byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];

                                    // 3. Копируем данные начиная с индекса 8 — но безопасно, не выходя за границы
                                    int maxFromFrame = Math.Max(0, frameBytes.Length - 9);              // сколько байт есть от payloadStart(8) до предпоследнего (не включая чек‑байт)
                                    int copyCount = Math.Min(data.Length, maxFromFrame);               // сколько реально можно скопировать в data
                                    if (copyCount > 0) Array.Copy(frameBytes, 8, data, 0, copyCount);

                                    // Декодируем только первые байты до NUL (0x00)
                                    int nulIndex = Array.FindIndex(data, 0, copyCount, b => b == 0x00);
                                    int textLen = nulIndex >= 0 ? nulIndex : copyCount;
                                    string text = textLen > 0
                                        ? System.Text.Encoding.ASCII.GetString(data, 0, textLen).Trim()
                                        : string.Empty;

                                    //   DataReceived?.Invoke($"кол-во байт: {frameBytes.Length}");
                                    //   if (frameBytes.Length != 9) { DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nCodeObject1: {text}"); }

                                    if (frameBytes.Length != 9) { DataReceived?.Invoke($"{text}");}

                                    //  DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nCodeObject1_: {text}");
                                }

                                //AccessPoint1
                                if (subindex == 0x02)
                                {
                                    byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];

                                    // 3. Копируем данные начиная с индекса 8 — но безопасно, не выходя за границы
                                    int maxFromFrame = Math.Max(0, frameBytes.Length - 9);              // сколько байт есть от payloadStart(8) до предпоследнего (не включая чек‑байт)
                                    int copyCount = Math.Min(data.Length, maxFromFrame);               // сколько реально можно скопировать в data
                                    if (copyCount > 0) Array.Copy(frameBytes, 8, data, 0, copyCount);

                                    // Декодируем только первые байты до NUL (0x00)
                                    int nulIndex = Array.FindIndex(data, 0, copyCount, b => b == 0x00);
                                    int textLen = nulIndex >= 0 ? nulIndex : copyCount;
                                    string text = textLen > 0
                                        ? System.Text.Encoding.ASCII.GetString(data, 0, textLen).Trim()
                                        : string.Empty;

                                    //    DataReceived?.Invoke($"RX-> {fullHex}\nиндекс: {indexlString}\nCodeObject1: {text}");
                                    //DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nAccessPoint1: {text}");
                                    //if (frameBytes.Length != 9) { DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nAccessPoint1: {text}"); }

                                    if (frameBytes.Length != 9) { DataReceived?.Invoke($"{text}");}


                                }

                                //AccessPoint2
                                if (subindex == 0x03)
                                {
                                    byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];

                                    // 3. Копируем данные начиная с индекса 8 — но безопасно, не выходя за границы
                                    int maxFromFrame = Math.Max(0, frameBytes.Length - 9);              // сколько байт есть от payloadStart(8) до предпоследнего (не включая чек‑байт)
                                    int copyCount = Math.Min(data.Length, maxFromFrame);               // сколько реально можно скопировать в data
                                    if (copyCount > 0) Array.Copy(frameBytes, 8, data, 0, copyCount);

                                    // Декодируем только первые байты до NUL (0x00)
                                    int nulIndex = Array.FindIndex(data, 0, copyCount, b => b == 0x00);
                                    int textLen = nulIndex >= 0 ? nulIndex : copyCount;
                                    string text = textLen > 0
                                        ? System.Text.Encoding.ASCII.GetString(data, 0, textLen).Trim()
                                        : string.Empty;



                                    if (frameBytes.Length != 9) { DataReceived?.Invoke($"{text}"); }
                                    //if (frameBytes.Length != 9) { DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nAccessPoint2: {text}"); }
                                    

                                }


                                //ServerPort1
                                if (subindex == 0x04)
                                {
                                    byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];

                                    // 3. Копируем данные начиная с индекса 8 — но безопасно, не выходя за границы
                                    int maxFromFrame = Math.Max(0, frameBytes.Length - 9);              // сколько байт есть от payloadStart(8) до предпоследнего (не включая чек‑байт)
                                    int copyCount = Math.Min(data.Length, maxFromFrame);               // сколько реально можно скопировать в data
                                    if (copyCount > 0) Array.Copy(frameBytes, 8, data, 0, copyCount);

                                    // Декодируем только первые байты до NUL (0x00)
                                    int nulIndex = Array.FindIndex(data, 0, copyCount, b => b == 0x00);
                                    int textLen = nulIndex >= 0 ? nulIndex : copyCount;
                                    string text = textLen > 0
                                        ? System.Text.Encoding.ASCII.GetString(data, 0, textLen).Trim()
                                        : string.Empty;

                                    //if (frameBytes.Length != 9) { DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nServerPort1: {text}"); }
                                    if (frameBytes.Length != 9) { DataReceived?.Invoke($"{text}"); }

                                    //   DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nServerPort1: {text}");

                                }

                                //ServerPort2
                                if (subindex == 0x05)
                                {
                                    byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];

                                    // 3. Копируем данные начиная с индекса 8 — но безопасно, не выходя за границы
                                    int maxFromFrame = Math.Max(0, frameBytes.Length - 9);              // сколько байт есть от payloadStart(8) до предпоследнего (не включая чек‑байт)
                                    int copyCount = Math.Min(data.Length, maxFromFrame);               // сколько реально можно скопировать в data
                                    if (copyCount > 0) Array.Copy(frameBytes, 8, data, 0, copyCount);

                                    // Декодируем только первые байты до NUL (0x00)
                                    int nulIndex = Array.FindIndex(data, 0, copyCount, b => b == 0x00);
                                    int textLen = nulIndex >= 0 ? nulIndex : copyCount;
                                    string text = textLen > 0
                                        ? System.Text.Encoding.ASCII.GetString(data, 0, textLen).Trim()
                                        : string.Empty;

                                 //   if (frameBytes.Length != 9) { DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nServerPort2: {text}"); }

                                    if (frameBytes.Length != 9) { DataReceived?.Invoke($"{text}"); }
                                    //   DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nServerPort1: {text}");

                                }
                                // ipServer1
                                if (subindex == 0x06)
                                {
                                    byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];

                                    // 3. Копируем данные начиная с индекса 8 — но безопасно, не выходя за границы
                                    int maxFromFrame = Math.Max(0, frameBytes.Length - 9);              // сколько байт есть от payloadStart(8) до предпоследнего (не включая чек‑байт)
                                    int copyCount = Math.Min(data.Length, maxFromFrame);               // сколько реально можно скопировать в data
                                    if (copyCount > 0) Array.Copy(frameBytes, 8, data, 0, copyCount);

                                    // Декодируем только первые байты до NUL (0x00)
                                    int nulIndex = Array.FindIndex(data, 0, copyCount, b => b == 0x00);
                                    int textLen = nulIndex >= 0 ? nulIndex : copyCount;
                                    string text = textLen > 0
                                        ? System.Text.Encoding.ASCII.GetString(data, 0, textLen).Trim()
                                        : string.Empty;


                                    if (frameBytes.Length != 9) { DataReceived?.Invoke($"{text}");}

                                    //if (frameBytes.Length != 9) { DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nipServer1: {text}"); }
                                    //   DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nServerPort1: {text}");

                                }

                                // ipServer2
                                if (subindex == 0x07)
                                {
                                    byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];

                                    // 3. Копируем данные начиная с индекса 8 — но безопасно, не выходя за границы
                                    int maxFromFrame = Math.Max(0, frameBytes.Length - 9);              // сколько байт есть от payloadStart(8) до предпоследнего (не включая чек‑байт)
                                    int copyCount = Math.Min(data.Length, maxFromFrame);               // сколько реально можно скопировать в data
                                    if (copyCount > 0) Array.Copy(frameBytes, 8, data, 0, copyCount);

                                    // Декодируем только первые байты до NUL (0x00)
                                    int nulIndex = Array.FindIndex(data, 0, copyCount, b => b == 0x00);
                                    int textLen = nulIndex >= 0 ? nulIndex : copyCount;
                                    string text = textLen > 0
                                        ? System.Text.Encoding.ASCII.GetString(data, 0, textLen).Trim()
                                        : string.Empty;

                                    if (frameBytes.Length != 9) { DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nipServer2: {text}"); }
                                    //   DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nServerPort1: {text}");

                                }



                                // UDP1 - прием ASCII текста
                                //if (subindex == 0x0A)
                                //{
                                //    byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];

                                //    // 3. Копируем данные начиная с индекса 8 — но безопасно, не выходя за границы
                                //    int maxFromFrame = Math.Max(0, frameBytes.Length - 9);              // сколько байт есть от payloadStart(8) до предпоследнего (не включая чек‑байт)
                                //    int copyCount = Math.Min(data.Length, maxFromFrame);               // сколько реально можно скопировать в data
                                //    if (copyCount > 0) Array.Copy(frameBytes, 8, data, 0, copyCount);

                                //    // Декодируем только первые байты до NUL (0x00)
                                //    int nulIndex = Array.FindIndex(data, 0, copyCount, b => b == 0x00);
                                //    int textLen = nulIndex >= 0 ? nulIndex : copyCount;
                                //    string text = textLen > 0
                                //        ? System.Text.Encoding.ASCII.GetString(data, 0, textLen).Trim()
                                //        : string.Empty;

                                //    if (frameBytes.Length != 9) { DataReceived?.Invoke($"{text}");}

                                //  //  if (frameBytes.Length != 9) { DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nipUDP1: {text}"); }
                                //    //   DataReceived?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nServerPort1: {text}");

                                //}


                                // UDP1 - прием бинарника
                                if (subindex == 0x0A)
                                {

                                    int payloadStart = 8;
                                    int payloadAvailable = Math.Max(0, frameBytes.Length - 1 - payloadStart); // -1 чтобы не захватить checksum (последний байт)

                                        if (length >= 1 && payloadAvailable >= 1)
                                        {
                                            byte value = frameBytes[payloadStart]; // 0x01 или 0x02
                                            DataReceived?.Invoke(value.ToString()); // "1" или "2"
                                        }
                                        else
                                        {
                                            DataReceived?.Invoke("RX ERROR: UDP1 no data");
                                        }
                                    

                                }

                                // ACK1 - прием бинарника (0/1)
                                // ACK1: subindex 0x0C, payload 1 byte: 0x00/0x01
                                if (subindex == 0x0C)
                                {
                                    const int payloadStart = 8;

                                    // ожидаем ровно 1 байт payload + 1 байт checksum => минимум 10 байт всего
                                    if (length == 1 && frameBytes.Length >= payloadStart + 1)
                                    {
                                        byte v = frameBytes[payloadStart]; // 0x00 или 0x01
                                        DataReceived?.Invoke(v == 0x00 ? "0" : "1");
                                    }
                                    else
                                    {
                                        // лучше игнорировать неполные кадры
                                        return;
                                    }
                                }



                                ///////////////////////////////
                                // Minutes1
                                if (subindex == 0x11)
                                {
                                    const int payloadStart = 8;
                                    int payloadAvailable = Math.Max(0, frameBytes.Length - 1 - payloadStart);

                                    if (length >= 1 && payloadAvailable >= 1)
                                    {
                                        byte value = frameBytes[payloadStart];
                                        DataReceived?.Invoke(value.ToString());
                                    }
                                }
                                ///////////////////////////////////
                                // Hours1
                                if (subindex == 0x10)
                                {
                                    const int payloadStart = 8;
                                    int payloadAvailable = Math.Max(0, frameBytes.Length - 1 - payloadStart);

                                    if (length >= 1 && payloadAvailable >= 1)
                                    {
                                        byte value = frameBytes[payloadStart];
                                        DataReceived?.Invoke(value.ToString());
                                    }
                                }

                                ///////////////////////////////////////

                            }




                            //индекс — Ток шлейфов
                            if (indexlString == "00002405") 
                            {

                                //Создаем массив data размером length байтов
                                byte[] data = length == 0 ? Array.Empty<byte>() : new byte[length];

                                //Копируем из frameBytes начиная с индекса 8, количество байтов = length, помещаем в data с индекса 0
                                Array.Copy(frameBytes, 8, data, 0, length);

                                // преобразует байт в десятичную строку                              
                                Func<byte, string> toDecimal = delegate (byte b) { return b.ToString("D"); }; // или так - Func<byte, string> toDecimal = b => b.ToString("D");
                                //
                                string decimalString = string.Join("", data.Select(toDecimal));
                                //// Вызов события с десятичным выводом
                                ////    DataReceived?.Invoke($"RX -> {fullHex} ток = {decimalString}mA");

                                DataReceived?.Invoke($" индекс: {indexlString}, субиндекс: {subindexString}\n ток: {decimalString}mA");
                                
                            }


                        }
                           

                    }//  while (true) - внутренний цикл обработки фреймов в recvBuf


                }//  while (_rxRunning) 



            }

            catch (Exception ex)
            {
                // Если при чтении из потока возникло исключение, логируем его и вызываем событие DataReceived с сообщением об ошибке.
                Log.Error("BTPerms", $"Exception in ReceiverData: {ex}");
                DataReceived.Invoke($"Error: {ex.Message}");
            }
            finally
            {
                _rxRunning = false;
                _anyRxRunning = false;
            }

        }
       


        //

        // new method receive data 
        public event Action<string> DataReceivedVarta832;

      

        public async Task ReceiverData_Varta832_Old()
        {
            if (_anyRxRunning || _rxRunningVarta832) return;

            _anyRxRunning = true;
            _rxRunningVarta832 = true;

            byte[] buffer = new byte[4096];
            const byte RESP_START = 0x02;
            const byte FRAME_STOP = 0x05;
            var recvBuf = new List<byte>();

            try
            {
                var input = bluetoothSocket?.InputStream;
                if (input == null)
                {
                    DataReceivedVarta832?.Invoke("Error: InputStream is null");
                    _rxRunningVarta832 = false;
                    return;
                }

                while (_rxRunningVarta832)
                {
                    await Task.Delay(30);

                    int bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));

                    // 🔎 Отладка: сколько байт прочитано
                    DataReceivedVarta832?.Invoke($"DEBUG bytesRead={bytesRead}");

                    if (bytesRead <= 0) continue;

                    for (int i = 0; i < bytesRead; i++)
                        recvBuf.Add(buffer[i]);

                    // 🔎 Отладка: содержимое буфера
                    DataReceivedVarta832?.Invoke($"DEBUG recvBuf={BitConverter.ToString(recvBuf.ToArray())}");

                    while (true)
                    {
                        int start = recvBuf.IndexOf(RESP_START);
                        if (start == -1)
                        {
                            DataReceivedVarta832?.Invoke("DEBUG: no start marker (0x02) found");
                            recvBuf.Clear();
                            break;
                        }

                        int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);
                        if (stop == -1)
                        {
                            DataReceivedVarta832?.Invoke("DEBUG: no stop marker (0x05) yet");
                            if (start > 0) recvBuf.RemoveRange(0, start);
                            break;
                        }

                        int asciiLen = stop - (start + 1);
                        if (asciiLen <= 0)
                        {
                            DataReceivedVarta832?.Invoke("DEBUG: empty frame (0x02 0x05)");
                            recvBuf.RemoveRange(0, stop + 1);
                            continue;
                        }

                        byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();
                        recvBuf.RemoveRange(0, stop + 1);

                        string asciiHex = Encoding.ASCII.GetString(asciiBytes)
                            .Replace("\r", "").Replace("\n", "")
                            .Trim().ToUpperInvariant();

                        DataReceivedVarta832?.Invoke($"DEBUG asciiHex={asciiHex}");

                        if (asciiHex.Length % 2 != 0)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
                            continue;
                        }

                        byte[] frameBytes = new byte[asciiHex.Length / 2];
                        for (int i = 0; i < frameBytes.Length; i++)
                            frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);

                        string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");
                        DataReceivedVarta832?.Invoke($"DEBUG fullHex={fullHex} len={frameBytes.Length}");

                        if (frameBytes.Length < 9)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: frame too short -> {fullHex}");
                            continue;
                        }

                        // Берём длину и индекс
                        int length = frameBytes[7];
                        byte[] index = new byte[4];
                        Array.Copy(frameBytes, 2, index, 0, 4);
                        string indexlString = BitConverter.ToString(index).Replace("-", "");
                        byte subindex = frameBytes[6];

                        DataReceivedVarta832?.Invoke($"DEBUG index={indexlString} subindex={subindex} length={length}");

                        // ⚡ Читаем данные начиная с позиции 8
                        byte[] data = length == 0 ? Array.Empty<byte>() : new byte[128];
                        Array.Copy(frameBytes, 8, data, 0, frameBytes.Length - 9);

                        string text = Encoding.ASCII.GetString(data).Trim('\0').Trim();

                        DataReceivedVarta832?.Invoke($"DEBUG text={text}");

                        // Выводим всегда
                        DataReceivedVarta832?.Invoke($"субиндекс: {subindex}  индекс: {indexlString}\nName: {text}");
                    }
                }
            }
            catch (Exception ex)
            {
                DataReceivedVarta832?.Invoke($"Error: {ex.Message}");
            }
            finally
            {
                _rxRunningVarta832 = false;
                _anyRxRunning = false;
            }
        }







        public async Task ReceiverData_Varta8322()
        {
            // Если прием уже запущен — второй раз не стартуем
            if (_anyRxRunning || _rxRunningVarta832) return;

            _anyRxRunning = true;
            _rxRunningVarta832 = true;

            // Буфер для чтения из Bluetooth-сокета
            byte[] buffer = new byte[4096];

            // Внешние маркеры кадра
            const byte RESP_START = 0x02;
            const byte FRAME_STOP = 0x05;

            // Накопительный буфер для случаев, когда данные приходят кусками
            var recvBuf = new List<byte>();

            try
            {
                // Получаем входной поток сокета
                var input = bluetoothSocket?.InputStream;
                if (input == null)
                {
                    DataReceivedVarta832?.Invoke("Error: InputStream is null");
                    _rxRunningVarta832 = false;
                    return;
                }

                // Основной цикл приема
                while (_rxRunningVarta832)
                {
                    // Если Bluetooth выключен — прекращаем прием
                    if (!_adapter.IsEnabled)
                    {
                        DataReceivedVarta832?.Invoke("Error: Bluetooth is disabled");

                        try { bluetoothSocket?.Close(); } catch { }
                        try { _context.UnregisterReceiver(_receiver); } catch { }

                        break;
                    }

                    // Небольшая задержка, чтобы не крутить CPU слишком активно
                    await Task.Delay(30);

                    int bytesRead;

                    // ЭТАП 1. Чтение байтов из входного потока
                    try
                    {
                        bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));
                    }
                    catch (Exception readEx)
                    {
                        DataReceivedVarta832?.Invoke($"Error reading from stream: {readEx.Message}");
                        break;
                    }

                    // Если поток закрыт удаленным устройством
                    if (bytesRead <= 0)
                    {
                        if (bytesRead == -1)
                        {
                            DataReceivedVarta832?.Invoke("Connection closed by remote device");
                            try { bluetoothSocket?.Close(); } catch { }
                            break;
                        }

                        // Если просто пока нет данных — ждем дальше
                        continue;
                    }

                    // ЭТАП 2. Добавляем прочитанные байты в накопительный буфер
                    for (int i = 0; i < bytesRead; i++)
                        recvBuf.Add(buffer[i]);

                    // ЭТАП 3. Пока в буфере есть потенциальные кадры — разбираем их
                    while (true)
                    {
                        // Ищем начало кадра
                        int start = recvBuf.IndexOf(RESP_START);
                        if (start == -1)
                        {
                            // Начало кадра не найдено — очищаем мусор
                            recvBuf.Clear();
                            break;
                        }

                        // Ищем конец кадра после начала
                        int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);
                        if (stop == -1)
                        {
                            // Конец кадра пока не пришел.
                            // Удаляем мусор до начала кадра, а сам неполный кадр оставляем
                            if (start > 0)
                                recvBuf.RemoveRange(0, start);

                            break;
                        }

                        // Длина ASCII-Hex текста между стартом и стопом
                        int asciiLen = stop - (start + 1);

                        // Пустой кадр вида 0x02 0x05 — пропускаем
                        if (asciiLen <= 0)
                        {
                            recvBuf.RemoveRange(0, stop + 1);
                            continue;
                        }

                        // Извлекаем ASCII-HEX часть кадра
                        byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();

                        // Удаляем обработанный кусок из накопительного буфера
                        recvBuf.RemoveRange(0, stop + 1);

                        // ЭТАП 4. Преобразуем ASCII байты в строку HEX
                        string asciiHex = Encoding.ASCII.GetString(asciiBytes)
                            .Replace("\r", "")
                            .Replace("\n", "")
                            .Trim()
                            .ToUpperInvariant();

                        // HEX-строка должна иметь четную длину
                        if (asciiHex.Length % 2 != 0)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
                            continue;
                        }

                        // ЭТАП 5. Преобразуем HEX-текст в байты
                        byte[] frameBytes;
                        try
                        {
                            frameBytes = new byte[asciiHex.Length / 2];

                            for (int i = 0; i < frameBytes.Length; i++)
                                frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
                        }
                        catch
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: invalid hex -> {asciiHex}");
                            continue;
                        }

                        // Полный кадр в HEX для логов
                        string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");

                        // ЭТАП 6. Минимальная проверка длины кадра
                        // read(1) + address(1) + index(4) + subindex(1) + length(1) + chk(1) = 9 байт
                        if (frameBytes.Length < 9)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: frame too short -> {fullHex}");
                            continue;
                        }

                        // ЭТАП 7. Проверка checksum
                        bool chkOk;
                        {
                            int sum = 0;

                            // Суммируем все байты, кроме последнего (он и есть checksum)
                            for (int i = 0; i < frameBytes.Length - 1; i++)
                                sum += frameBytes[i];

                            byte expectedChk = (byte)(sum & 0xFF);
                            byte actualChk = frameBytes[frameBytes.Length - 1];

                            chkOk = expectedChk == actualChk;
                        }

                        // Если checksum неверная — кадр отбрасываем
                        if (!chkOk)
                        {
                            DataReceivedVarta832?.Invoke($"RX CHK ERROR full={fullHex}");
                            //  continue;
                        }

                        // ЭТАП 8. Разбор структуры кадра
                        byte read = frameBytes[0];
                        byte address = frameBytes[1];

                        byte[] indexBytes = new byte[4];
                        Array.Copy(frameBytes, 2, indexBytes, 0, 4);

                        byte subindex = frameBytes[6];
                        int length = frameBytes[7];

                        string indexString = BitConverter.ToString(indexBytes).Replace("-", "");
                        string subindexString = subindex.ToString("D");

                        // Payload начинается с байта 8, последний байт кадра — checksum
                        const int payloadStart = 8;
                        int payloadAvailable = frameBytes.Length - payloadStart - 1;

                        if (payloadAvailable < 0)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: invalid payload size -> {fullHex}");
                            continue;
                        }

                        // Проверяем, что length не больше реально доступного payload
                        if (length > payloadAvailable)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: length mismatch len={length} available={payloadAvailable} full={fullHex}");
                            continue;
                        }

                        // ЭТАП 9. Копируем payload в отдельный массив
                        byte[] payload = length == 0 ? Array.Empty<byte>() : new byte[length];
                        if (length > 0)
                            Array.Copy(frameBytes, payloadStart, payload, 0, length);

                        // ЭТАП 10. Подготовка удобных представлений payload
                        string payloadHex = payload.Length > 0
                            ? BitConverter.ToString(payload).Replace("-", "")
                            : string.Empty;

                        // Если payload содержит ASCII-строку с NUL в конце — можно попробовать извлечь текст
                        int nulIndex = Array.FindIndex(payload, b => b == 0x00);
                        int textLen = nulIndex >= 0 ? nulIndex : payload.Length;

                        string payloadAscii = textLen > 0
                            ? Encoding.ASCII.GetString(payload, 0, textLen).Trim()
                            : string.Empty;

                        // ЭТАП 11. Отдаем наружу универсальную информацию о кадре
                        // Здесь пока НЕТ привязки к конкретным индексам/субиндексам.
                        DataReceivedVarta832?.Invoke(
                            $"RX OK read={read:X2} addr={address:X2} index={indexString} sub={subindex:X2} len={length} payloadHex={payloadHex} ascii={payloadAscii} full={fullHex}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("BTPerms", $"Exception in ReceiverData_Varta832: {ex}");
                DataReceivedVarta832?.Invoke($"Error: {ex.Message}");
            }
            finally
            {
                _rxRunningVarta832 = false;
                _anyRxRunning = false;
            }
        }




        //public async Task ReceiverData_Varta832()
        //{
        //    if (_anyRxRunning || _rxRunningVarta832) return;

        //    _anyRxRunning = true;
        //    _rxRunningVarta832 = true;

        //    byte[] buffer = new byte[4096];
        //    const byte RESP_START = 0x02;
        //    const byte FRAME_STOP = 0x05;
        //    var recvBuf = new List<byte>();

        //    try
        //    {
        //        var input = bluetoothSocket?.InputStream;
        //        if (input == null)
        //        {
        //            DataReceivedVarta832?.Invoke("Error: InputStream is null");
        //            _rxRunningVarta832 = false;
        //            return;
        //        }

        //        while (_rxRunningVarta832)
        //        {
        //            if (!_adapter.IsEnabled)
        //            {
        //                DataReceivedVarta832?.Invoke("Error: Bluetooth is disabled");
        //                try { bluetoothSocket?.Close(); } catch { }
        //                try { _context.UnregisterReceiver(_receiver); } catch { }
        //                break;
        //            }

        //            await Task.Delay(30);

        //            int bytesRead;
        //            try
        //            {
        //                bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));
        //            }
        //            catch (Exception readEx)
        //            {
        //                DataReceivedVarta832?.Invoke($"Error reading from stream: {readEx.Message}");
        //                break;
        //            }

        //            if (bytesRead <= 0)
        //            {
        //                if (bytesRead == -1)
        //                {
        //                    DataReceivedVarta832?.Invoke("Connection closed by remote device");
        //                    try { bluetoothSocket?.Close(); } catch { }
        //                    break;
        //                }
        //                continue;
        //            }

        //            for (int i = 0; i < bytesRead; i++)
        //                recvBuf.Add(buffer[i]);

        //            while (true)
        //            {
        //                int start = recvBuf.IndexOf(RESP_START);
        //                if (start == -1) { recvBuf.Clear(); break; }

        //                int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);
        //                if (stop == -1)
        //                {
        //                    if (start > 0) recvBuf.RemoveRange(0, start);
        //                    break;
        //                }

        //                int asciiLen = stop - (start + 1);
        //                if (asciiLen <= 0) { recvBuf.RemoveRange(0, stop + 1); continue; }

        //                byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();
        //                recvBuf.RemoveRange(0, stop + 1);

        //                string asciiHex = Encoding.ASCII.GetString(asciiBytes)
        //                    .Replace("\r", "").Replace("\n", "")
        //                    .Trim().ToUpperInvariant();

        //                if (asciiHex.Length % 2 != 0)
        //                {
        //                    DataReceivedVarta832?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
        //                    continue;
        //                }

        //                byte[] frameBytes;
        //                try
        //                {
        //                    frameBytes = new byte[asciiHex.Length / 2];
        //                    for (int i = 0; i < frameBytes.Length; i++)
        //                        frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
        //                }
        //                catch
        //                {
        //                    DataReceivedVarta832?.Invoke($"RX ERROR: invalid hex -> {asciiHex}");
        //                    continue;
        //                }

        //                string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");
        //                if (frameBytes.Length < 9)
        //                {
        //                    DataReceivedVarta832?.Invoke($"RX ERROR: frame too short -> {fullHex}");
        //                    continue;
        //                }

        //                int sum = 0;
        //                for (int i = 0; i < frameBytes.Length - 1; i++) sum += frameBytes[i];
        //                byte expectedChk = (byte)(sum & 0xFF);
        //                byte actualChk = frameBytes[^1];
        //                bool chkOk = expectedChk == actualChk;
        //                if (!chkOk)
        //                {
        //                    DataReceivedVarta832?.Invoke($"RX CHK ERROR full={fullHex}");
        //                }

        //                byte read = frameBytes[0];
        //                byte address = frameBytes[1];
        //                byte[] indexBytes = frameBytes.Skip(2).Take(4).ToArray();
        //                byte subindex = frameBytes[6];
        //                int length = frameBytes[7];

        //                string indexString = BitConverter.ToString(indexBytes).Replace("-", "");
        //                const int payloadStart = 8;
        //                int payloadAvailable = frameBytes.Length - payloadStart - 1;
        //                if (length > payloadAvailable)
        //                {
        //                    DataReceivedVarta832?.Invoke($"RX ERROR: length mismatch len={length} available={payloadAvailable} full={fullHex}");
        //                    continue;
        //                }

        //                // ⚡ Берём payload до контрольной суммы
        //                byte[] payload = frameBytes.Skip(payloadStart).Take(payloadAvailable).ToArray();
        //                string payloadHex = payload.Length > 0 ? BitConverter.ToString(payload).Replace("-", "") : string.Empty;

        //                // 🔑 Декодирование текста в CP1251 с обрезкой по NUL




        //                string payloadText = string.Empty;
        //                try
        //                {
        //                    int nulIndex = Array.IndexOf(payload, (byte)0x00);
        //                    int textLen = nulIndex >= 0 ? nulIndex : payload.Length;


        //                    // 👉 Добавь вот этот DEBUG вывод
        //                    DataReceivedVarta832?.Invoke(
        //                        $"DEBUG payload length={payload.Length} textLen={textLen} bytes={BitConverter.ToString(payload.Take(textLen).ToArray())}"
        //                    );
        //                    var enc = Encoding.GetEncoding(1251); // CP1251
        //                    payloadText = enc.GetString(payload, 0, textLen);


        //                }
        //                catch { }

        //                DataReceivedVarta832?.Invoke(
        //                    $"RX OK read={read:X2} addr={address:X2} index={indexString} sub={subindex:X2} len={length} payloadHex={payloadHex} text=\"{payloadText}\" full={fullHex}"
        //                );

        //                //DataReceivedVarta832?.Invoke($"RX OK  index={indexString} sub={subindex:X2} len={length} payloadHex={payloadHex} text=\"{payloadText}\"" );


        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        DataReceivedVarta832?.Invoke($"Error: {ex.Message}");
        //    }
        //    finally
        //    {
        //        _rxRunningVarta832 = false;
        //        _anyRxRunning = false;
        //    }
        //}



        //


        public async Task ReceiverData_Varta832_text()
        {
            if (_anyRxRunning || _rxRunningVarta832) return;

            _anyRxRunning = true;
            _rxRunningVarta832 = true;

            byte[] buffer = new byte[4096];
            const byte RESP_START = 0x02;
            const byte FRAME_STOP = 0x05;
            var recvBuf = new List<byte>();

            try
            {
                var input = bluetoothSocket?.InputStream;
                if (input == null)
                {
                    DataReceivedVarta832?.Invoke("Error: InputStream is null");
                    _rxRunningVarta832 = false;
                    return;
                }

                while (_rxRunningVarta832)
                {
                    await Task.Delay(30);

                    int bytesRead;
                    try
                    {
                        bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));
                    }
                    catch (Exception readEx)
                    {
                        DataReceivedVarta832?.Invoke($"Error reading from stream: {readEx.Message}");
                        break;
                    }

                    if (bytesRead <= 0) continue;

                    for (int i = 0; i < bytesRead; i++)
                        recvBuf.Add(buffer[i]);

                    while (true)
                    {
                        int start = recvBuf.IndexOf(RESP_START);
                        if (start == -1) { recvBuf.Clear(); break; }

                        int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);
                        if (stop == -1)
                        {
                            if (start > 0) recvBuf.RemoveRange(0, start);
                            break;
                        }

                        int asciiLen = stop - (start + 1);
                        if (asciiLen <= 0) { recvBuf.RemoveRange(0, stop + 1); continue; }

                        byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();
                        recvBuf.RemoveRange(0, stop + 1);

                        string asciiHex = Encoding.ASCII.GetString(asciiBytes)
                            .Replace("\r", "").Replace("\n", "")
                            .Trim().ToUpperInvariant();

                        if (asciiHex.Length % 2 != 0)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
                            continue;
                        }

                        byte[] frameBytes;
                        try
                        {
                            frameBytes = new byte[asciiHex.Length / 2];
                            for (int i = 0; i < frameBytes.Length; i++)
                                frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
                        }
                        catch
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: invalid hex -> {asciiHex}");
                            continue;
                        }

                        string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");
                        if (frameBytes.Length < 9)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: frame too short -> {fullHex}");
                            continue;
                        }

                        // Проверка checksum
                        int sum = 0;
                        for (int i = 0; i < frameBytes.Length - 1; i++) sum += frameBytes[i];
                        byte expectedChk = (byte)(sum & 0xFF);
                        byte actualChk = frameBytes[^1];
                        bool chkOk = expectedChk == actualChk;

                        // Основные поля
                        byte read = frameBytes[0];
                        byte address = frameBytes[1];
                        byte[] indexBytes = frameBytes.Skip(2).Take(4).ToArray();
                        byte subindex = frameBytes[6];
                        int length = frameBytes[7];

                        string indexString = BitConverter.ToString(indexBytes).Replace("-", "");
                        const int payloadStart = 8;
                        int payloadAvailable = frameBytes.Length - payloadStart - 1;

                        // ⚡ Если length некорректный — используем фактическую длину
                        int safeLength = Math.Min(length, payloadAvailable);

                        byte[] payload = frameBytes.Skip(payloadStart).Take(safeLength).ToArray();
                        string payloadHex = payload.Length > 0 ? BitConverter.ToString(payload).Replace("-", "") : string.Empty;

                        string payloadText = string.Empty;
                        string numericValue = string.Empty;

                        try
                        {
                            int nulIndex = Array.IndexOf(payload, (byte)0x00);
                            int textLen = nulIndex >= 0 ? nulIndex : payload.Length;

                            var enc = Encoding.GetEncoding(1251); // CP1251
                            payloadText = enc.GetString(payload, 0, textLen);

                            // ⚡ Если payload всего 1 байт — выводим его как число
                            if (payload.Length == 1)
                            {
                                numericValue = payload[0].ToString(); // десятичное значение
                                payloadText = numericValue;           // перезаписываем текст числом
                            }
                        }
                        catch { }


                        // ⚡ Выводим всегда, но помечаем статус
                        string status = chkOk ? "OK" : "WARN";
                        if (length > payloadAvailable) status = "ERROR";

                        DataReceivedVarta832?.Invoke(
                            $"RX {status} read={read:X2} addr={address:X2} index={indexString} sub={subindex:X2} len={length} " +
                            $"payloadHex={payloadHex} text=\"{payloadText}\" full={fullHex} " +
                            $"chkExp={expectedChk:X2} chkAct={actualChk:X2}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                DataReceivedVarta832?.Invoke($"Error: {ex.Message}");
            }
            finally
            {
                _rxRunningVarta832 = false;
                _anyRxRunning = false;
            }
        }

        // событие для задачи Task ReceiverData_Varta832()
        public event Action<VartaFrame> DataReceivedFrame;
        //  
        public async Task ReceiverData_Varta832()
        {
            // Если прием уже запущен — второй раз не стартуем 
            // Выбор необходимого метода приема данных 
            if (_anyRxRunning || _rxRunningVarta832) return;

            // Помечаем, что прием данных запущен
            _anyRxRunning = true;
            // Этот конкретный флаг  — он показывает, что именно метод ReceiverData_Varta832 сейчас работает
            _rxRunningVarta832 = true;

            // Буфер для чтения из Bluetooth-сокета
            byte[] buffer = new byte[4096];
            // Внешние маркеры кадра старт стоп
            const byte RESP_START = 0x02;
            const byte FRAME_STOP = 0x05;
            // Накопительный буфер для случаев, когда данные приходят кусками
            var recvBuf = new List<byte>();

            try
            {
                // Получаем входной поток сокета
                var input = bluetoothSocket?.InputStream;
                if (input == null)
                {
                    // Если входной поток недоступен — сообщаем об ошибке и выходим
                    DataReceivedVarta832?.Invoke("Error: InputStream is null");
                    _rxRunningVarta832 = false;
                    return;
                }
                // Основной цикл приема данных
                //
                while (_rxRunningVarta832)
                {
                    await Task.Delay(30);
                    // переменная для хранения количества прочитанных данных из входного потока
                    int bytesRead;
                    try
                    {
                        // Чтение данных из входного потока в буфер - await  позволяет не блокировать поток UI,
                        // а Task.Run выполняет чтение в отдельном потоке, но дождется его завершения.
                        bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));
                    }
                    catch (Exception readEx)
                    {
                        // Если произошла ошибка при чтении данных — сообщаем об этом и выходим из цикла
                        DataReceivedVarta832?.Invoke($"Error reading from stream: {readEx.Message}");
                        break;
                    }

                    // если ничего, то возвращаемся в начало цикла while (_rxRunningVarta832)
                    if (bytesRead <= 0) continue;
                    // Добавляем прочитанные байты в накопительный буфер
                    for (int i = 0; i < bytesRead; i++)
                        recvBuf.Add(buffer[i]);

                    // Разбор накопленного буфера на кадры
                    while (true)
                    {
                        // Ищем начало кадра
                        int start = recvBuf.IndexOf(RESP_START);
                        // Если начало кадра не найдено — очищаем мусор и выходим из внутреннего цикла в основной цикл while (_rxRunningVarta832)
                        if (start == -1) { recvBuf.Clear(); break; }
                        // Ищем конец кадра после начала
                        int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);
                        //
                        if (stop == -1)
                        {
                            if (start > 0) recvBuf.RemoveRange(0, start);
                            break;
                        }
                        // полезная часть кадра в ASCII‑HEX,asciiLen = длина этой части.
                        int asciiLen = stop - (start + 1);
                        // Если полезная часть пустая — удаляем кадр и продолжаем разбор
                        if (asciiLen <= 0) { recvBuf.RemoveRange(0, stop + 1); continue; }
                        // Извлекаем ASCII-HEX часть кадра
                        //  recvBuf.Skip(start + 1) → пропускаем все байты до позиции start + 1,nо есть игнорируем сам байт RESP_START и всё, что было до него.                       
                        //.Take(asciiLen) → берём ровно asciiLen байт(это полезная часть кадра между RESP_START и FRAME_STOP).
                        //.ToArray() → превращаем выбранные байты в массив byte[].
                        byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();
                        // Удаляем обработанный кусок из накопительного буфера, включая RESP_START и FRAME_STOP
                        recvBuf.RemoveRange(0, stop + 1);
                        // превращаем массив ASCII‑байтов в строку, которая содержит символы HEX‑представления.
                        string asciiHex = Encoding.ASCII.GetString(asciiBytes)
                            .Replace("\r", "").Replace("\n", "")
                            .Trim().ToUpperInvariant();
                        // Проверяем, что длина HEX-строки четная, так как пару символов стринга - это 1 байт
                        if (asciiHex.Length % 2 != 0)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
                            continue;
                        }

                        byte[] frameBytes;
                        try
                        {
                            frameBytes = new byte[asciiHex.Length / 2];
                            for (int i = 0; i < frameBytes.Length; i++)
                                frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
                        }
                        catch
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: invalid hex -> {asciiHex}");
                            continue;
                        }

                        string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");
                        if (frameBytes.Length < 9)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: frame too short -> {fullHex}");
                            continue;
                        }

                        // Проверка checksum
                        int sum = 0;
                        // Суммируем все байты, кроме последнего (контрольной суммы)
                        for (int i = 0; i < frameBytes.Length - 1; i++) sum += frameBytes[i];
                        // Вычисляем ожидаемую контрольную сумму 
                        byte expectedChk = (byte)(sum & 0xFF);
                        // Получаем фактическую контрольную сумму из последнего байта кадра
                        byte actualChk = frameBytes[^1];
                        // Сравниваем ожидаемую и фактическую контрольные суммы
                        bool chkOk = expectedChk == actualChk;

                        // Основные поля
                        byte read = frameBytes[0];
                        byte address = frameBytes[1];
                        byte[] indexBytes = frameBytes.Skip(2).Take(4).ToArray();
                        byte subindex = frameBytes[6];
                        int length = frameBytes[7];

                        string indexString = BitConverter.ToString(indexBytes).Replace("-", "");
                        const int payloadStart = 8;
                        int payloadAvailable = frameBytes.Length - payloadStart - 1;

                        // ⚡ Если length некорректный — используем фактическую длину
                        int safeLength = Math.Min(length, payloadAvailable);

                        byte[] payload = frameBytes.Skip(payloadStart).Take(safeLength).ToArray();
                        string payloadHex = payload.Length > 0 ? BitConverter.ToString(payload).Replace("-", "") : string.Empty;



                        string payloadText = string.Empty; //строковое представление данных (если это текст).
                        string numericValue = string.Empty;// числовое представление данных (если это числа).
                        string hexValues = string.Empty; // список байтов в HEX‑формате.
                        string decodedValues = string.Empty; //расшифровка по таблице TypeValue
                        try
                        {
                            //  
                            if (indexString == "00003001")
                            {
                                // ⚡ Индекс 00003001 → трактуем как строку
                                // Ищем в массиве payload первый байт 0x00 - как терминатор строки (конец текста). 
                                int nulIndex = Array.IndexOf(payload, (byte)0x00);
                                // Если терминатор найден, то длина текста до него, иначе длина всего payload
                                int textLen = nulIndex >= 0 ? nulIndex : payload.Length;
                                //Создаём объект кодировки Windows‑1251 (CP1251).  
                                var enc = Encoding.GetEncoding(1251); // CP1251
                                // Преобразуем байты payload в строку с использованием кодировки CP1251, начиная с нулевого индекса и длиной textLen.
                                payloadText = enc.GetString(payload, 0, textLen);
                            }
                            else if (indexString == "00003019")
                            {
                                // ⚡ Индекс 00003019 → трактуем как числа
                                if (payload.Length == 1)
                                {
                                    numericValue = payload[0].ToString();
                                    // Расшифровка по таблице TypeValue
                                    if (payload[0] < TypeValue.Length)//  проверяем, что индекс в пределах массива TypeValue
                                        numericValue += $" ({TypeValue[payload[0]]})";
                                }
                                else
                                {
                                    // если несколько байтов → выводим список чисел
                                    numericValue = string.Join(",", payload.Select(b => b.ToString()));
                                }
                            }
                            else if (indexString == "00003100")
                            {
                                numericValue = string.Join(",", payload.Select(b => b.ToString()));
                                hexValues = string.Join(",", payload.Select(b => b.ToString("X2")));

                                var frame = new VartaFrame
                                {
                                    Index = indexString,
                                    SubIndex = subindex.ToString("X2"),
                                    Exists = payload.Length > 0 && payload[0] != 0x00
                                };

                                int count = Math.Min(16, payload.Length - 1);

                                //for (int i = 1; i <= count; i++)
                                //{
                                //    byte b = payload[i];
                                //    string decoded = b < TypeValue.Length
                                //        ? TypeValue[b]
                                //        : $"Unknown({b})";

                                //    frame.Values.Add(new VartaValue { Name = $"ШС{i}", Value = decoded });
                                //}

                                for (int i = 1; i <= count; i++)
                                {
                                    byte b = payload[i];
                                    string decoded = b < TypeValue.Length
                                        ? TypeValue[b]
                                        : $"Unknown({b})";

                                    string name = i <= 8
                                        ? $"Тип ШС{i}"
                                        : $"Стан ШС{i - 8}";

                                    frame.Values.Add(new VartaValue { Name = name, Value = decoded });
                                }






                                DataReceivedFrame?.Invoke(frame);
                            }


                            else
                            {
                                // ⚡ По умолчанию пробуем как текст
                                var enc = Encoding.GetEncoding(1251);
                                payloadText = enc.GetString(payload, 0, payload.Length);
                            }
                        }
                        catch { }


                        // ⚡ Формируем расшифровку для любого payload
                        if (payload.Length > 0)
                        {
                            decodedValues = string.Join(",", payload.Select(b =>
                                b < TypeValue.Length ? TypeValue[b] : $"Unknown({b})"
                            ));
                        }





                        // ⚡ Выводим всегда, но помечаем статус
                        string status = chkOk ? "OK" : "WARN";
                        if (length > payloadAvailable) status = "ERROR";


                        //var frame = new VartaFrame
                        //{
                        //    Index = indexString,
                        //    Value = numericValue,
                        //    Decoded = decodedValues
                        //};
                        //DataReceivedFrame?.Invoke(frame);
                        //DataReceivedFrame?.Invoke(frame);


                        DataReceivedVarta832?.Invoke(
                            $"index={indexString} sub={subindex:X2} len={length} " +
                            $"payloadHex={payloadHex} text=\"{payloadText}\"  value={numericValue} valueHex={hexValues} decoded={decodedValues} " +
                            $"chkExp={expectedChk:X2} chkAct={actualChk:X2}"
                        );






                    }
                }
            }
            catch (Exception ex)
            {
                DataReceivedVarta832?.Invoke($"Error: {ex.Message}");
            }
            finally
            {
                _rxRunningVarta832 = false;
                _anyRxRunning = false;
            }
        }









        public async Task ReceiverData_Varta832_comment()
        {
            // Проверяем: если уже идёт приём (любой из двух receiver'ов), не запускаем второй раз
            if (_anyRxRunning || _rxRunningVarta832) return;

            // Устанавливаем флаги: общий флаг и флаг именно для Varta832
            _anyRxRunning = true;
            _rxRunningVarta832 = true;

            // Буфер для чтения порций данных из Bluetooth-потока (максимум 4096 байт за раз)
            byte[] buffer = new byte[4096];

            // Маркер начала кадра (в ASCII-кодировании это символ STX — 0x02)
            const byte RESP_START = 0x02;

            // Маркер конца кадра (в ASCII-кодировании это символ ENQ — 0x05)
            const byte FRAME_STOP = 0x05;

            // Накопитель: если данные приходят фрагментами, складываем их сюда
            var recvBuf = new List<byte>();

            try
            {
                // Получаем входной поток из Bluetooth-сокета
                var input = bluetoothSocket?.InputStream;

                // Проверяем, что поток не null (иначе сокет не готов)
                if (input == null)
                {
                    // Сообщаем об ошибке в event
                    DataReceivedVarta832?.Invoke("Error: InputStream is null");
                    // Сбрасываем флаг и выходим
                    _rxRunningVarta832 = false;
                    return;
                }

                // Основной цикл приёма: крутимся, пока _rxRunningVarta832 = true
                while (_rxRunningVarta832)
                {
                    // Проверяем, включен ли Bluetooth (может быть отключён извне)
                    if (!_adapter.IsEnabled)
                    {
                        DataReceivedVarta832?.Invoke("Error: Bluetooth is disabled");
                        try { bluetoothSocket?.Close(); } catch { }
                        try { _context.UnregisterReceiver(_receiver); } catch { }
                        break; // Выходим из цикла
                    }

                    // Пауза 30ms, чтобы не крутить CPU на 100%
                    await Task.Delay(30);

                    // Переменная для хранения кол-ва прочитанных байт
                    int bytesRead;

                    try
                    {
                        // Запускаем синхронное чтение из потока в пуле потоков (не блокируем UI)
                        bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));
                    }
                    catch (Exception readEx)
                    {
                        // Если при чтении была ошибка (разрыв соединения и т.д.)
                        DataReceivedVarta832?.Invoke($"Error reading from stream: {readEx.Message}");
                        break; // Выходим из цикла приёма
                    }

                    // Проверяем результат чтения
                    if (bytesRead <= 0)
                    {
                        // Если прочитано 0 байт — просто нет данных, ждём дальше
                        if (bytesRead == -1)
                        {
                            // -1 означает, что поток закрыт удаленным устройством
                            DataReceivedVarta832?.Invoke("Connection closed by remote device");
                            try { bluetoothSocket?.Close(); } catch { }
                            break; // Выходим
                        }
                        // Если просто 0 — пропускаем итерацию и ждём дальше
                        continue;
                    }

                    // Добавляем все прочитанные байты в накопитель
                    for (int i = 0; i < bytesRead; i++)
                        recvBuf.Add(buffer[i]);

                    // Внутренний цикл: пока в recvBuf есть полные кадры — разбираем их
                    while (true)
                    {
                        // Ищем позицию маркера начала кадра (RESP_START = 0x02)
                        int start = recvBuf.IndexOf(RESP_START);

                        // Если маркера начала нет — весь буфер это мусор, очищаем и выходим из внутреннего цикла
                        if (start == -1)
                        {
                            recvBuf.Clear();
                            break;
                        }

                        // Ищем маркер конца кадра (FRAME_STOP = 0x05) начиная со следующего байта после start
                        int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);

                        // Если маркера конца нет — кадр ещё не полный, ждём дальше
                        if (stop == -1)
                        {
                            // Удаляем мусор до начала неполного кадра
                            if (start > 0) recvBuf.RemoveRange(0, start);
                            break; // Выходим из внутреннего цикла, ждём следующей порции данных
                        }

                        // Вычисляем длину ASCII-HEX текста между маркерами (не включая сами маркеры)
                        int asciiLen = stop - (start + 1);

                        // Если пришёл пустой кадр (0x02 0x05) — пропускаем его
                        if (asciiLen <= 0)
                        {
                            recvBuf.RemoveRange(0, stop + 1); // Удаляем кадр целиком
                            continue; // Продолжаем поиск следующего кадра
                        }

                        // Извлекаем ASCII-байты из буфера (между маркерами)
                        byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();

                        // Удаляем обработанный кадр из буфера (включая оба маркера)
                        recvBuf.RemoveRange(0, stop + 1);

                        // Преобразуем ASCII-байты в строку, очищаем от переводов строк, пробелов, приводим к верхнему регистру
                        string asciiHex = Encoding.ASCII.GetString(asciiBytes)
                            .Replace("\r", "").Replace("\n", "")
                            .Trim().ToUpperInvariant();

                        // HEX-строка должна иметь чётную длину (каждые 2 символа = 1 байт)
                        if (asciiHex.Length % 2 != 0)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
                            continue; // Пропускаем этот кадр
                        }

                        // Преобразуем HEX-строку обратно в массив байт
                        byte[] frameBytes;
                        try
                        {
                            frameBytes = new byte[asciiHex.Length / 2]; // Создаём массив размером половина от длины HEX-строки

                            // Каждые 2 символа HEX преобразуем в один байт
                            for (int i = 0; i < frameBytes.Length; i++)
                                frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
                        }
                        catch
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: invalid hex -> {asciiHex}");
                            continue; // Пропускаем кадр с невалидным HEX
                        }

                        // Конвертируем весь кадр обратно в HEX-строку для логирования
                        string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");

                        // Минимальная проверка: кадр должен быть хотя бы 9 байт (заголовок + checksum)
                        if (frameBytes.Length < 9)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: frame too short -> {fullHex}");
                            continue; // Пропускаем короткий кадр
                        }

                        // ═════════════════════════════════════════════════════════
                        // ПРОВЕРКА CHECKSUM
                        // ═════════════════════════════════════════════════════════

                        // Суммируем все байты кроме последнего (последний = checksum)
                        int sum = 0;
                        for (int i = 0; i < frameBytes.Length - 1; i++)
                            sum += frameBytes[i];

                        // Вычисляем ожидаемый checksum как младший байт суммы (маска 0xFF)
                        byte expectedChk = (byte)(sum & 0xFF);

                        // Последний байт кадра — это фактический checksum
                        byte actualChk = frameBytes[^1]; // ^1 означает "последний элемент"

                        // Сравниваем ожидаемый и фактический checksum
                        bool chkOk = expectedChk == actualChk;

                        // Если checksum не совпадает — логируем, но продолжаем разбирать кадр
                        if (!chkOk)
                        {
                            DataReceivedVarta832?.Invoke($"RX CHK ERROR full={fullHex}");
                            // ⚠️ Важно: НЕ continue; — идём дальше разбирать данные
                        }

                        // ═════════════════════════════════════════════════════════
                        // РАЗБОР СТРУКТУРЫ КАДРА
                        // ═════════════════════════════════════════════════════════

                        // Первый байт кадра — операция (чтение/запись)
                        byte read = frameBytes[0];

                        // Второй байт — адрес устройства
                        byte address = frameBytes[1];

                        // Байты 2-5 — индекс (4 байта)
                        byte[] indexBytes = frameBytes.Skip(2).Take(4).ToArray();

                        // Байт 6 — subindex
                        byte subindex = frameBytes[6];

                        // Байт 7 — длина payload'а
                        int length = frameBytes[7];

                        // Конвертируем индекс в HEX-строку для логирования
                        string indexString = BitConverter.ToString(indexBytes).Replace("-", "");

                        // Payload начинается с байта 8
                        const int payloadStart = 8;

                        // Сколько байт доступно для payload'а (всё кроме заголовка и checksum'а)
                        int payloadAvailable = frameBytes.Length - payloadStart - 1;

                        // Проверяем, что length не превышает доступное место
                        if (length > payloadAvailable)
                        {
                            DataReceivedVarta832?.Invoke($"RX ERROR: length mismatch len={length} available={payloadAvailable} full={fullHex}");
                            continue; // Пропускаем кадр
                        }

                        // Извлекаем payload (массив байт полезной нагрузки)
                        byte[] payload = frameBytes.Skip(payloadStart).Take(payloadAvailable).ToArray();

                        // Конвертируем payload в HEX-строку для логирования
                        string payloadHex = payload.Length > 0
                            ? BitConverter.ToString(payload).Replace("-", "")
                            : string.Empty;

                        // ═════════════════════════════════════════════════════════
                        // ДЕКОДИРОВАНИЕ ТЕКСТА В CP1251
                        // ═════════════════════════════════════════════════════════

                        string payloadText = string.Empty;
                        try
                        {
                            // Ищем первый нулевой байт (конец строки в C-стиле)
                            int nulIndex = Array.IndexOf(payload, (byte)0x00);

                            // Если NUL найден — берём только до него, иначе весь payload
                            int textLen = nulIndex >= 0 ? nulIndex : payload.Length;

                            // DEBUG лог: показываем длину и байты для анализа
                            DataReceivedVarta832?.Invoke(
                                $"DEBUG payload length={payload.Length} textLen={textLen} bytes={BitConverter.ToString(payload.Take(textLen).ToArray())}"
                            );

                            // Получаем кодировщик Windows-1251 (кириллица)
                            var enc = Encoding.GetEncoding(1251);

                            // Декодируем байты в строку
                            payloadText = enc.GetString(payload, 0, textLen);
                        }
                        catch
                        {
                            // Если декодирование не удалось — оставляем пустую строку
                        }

                        // ═════════════════════════════════════════════════════════
                        // ВЫВОД В ТЕРМИНАЛ
                        // ═════════════════════════════════════════════════════════

                        // Отправляем полную информацию о кадре в event
                        DataReceivedVarta832?.Invoke(
                            $"RX OK read={read:X2} addr={address:X2} index={indexString} sub={subindex:X2} len={length} payloadHex={payloadHex} text=\"{payloadText}\" full={fullHex}"
                        );

                    } // end while (true) — конец цикла разбора кадров в recvBuf
                } // end while (_rxRunningVarta832) — конец основного цикла приёма
            }
            catch (Exception ex)
            {
                // Если произошла неожиданная ошибка — логируем её
                DataReceivedVarta832?.Invoke($"Error: {ex.Message}");
            }
            finally
            {
                // В конце ВСЕГДА сбрасываем оба флага (даже если была ошибка)
                _rxRunningVarta832 = false;
                _anyRxRunning = false;
            }
        }












        public async Task TransmitterData(string command)
        {

            //// Проверяем, что сокет и его поток готовы к записи
            //if (bluetoothSocket == null || bluetoothSocket.OutputStream == null)
            //    throw new InvalidOperationException("Bluetooth socket not ready");

            try
            {
                // записуем строку команды
                string asciiHex = command;

                byte rawStart = 0x01; // стартовый байт
                byte rawStop = 0x05;  // стоповый байт

                // Преобразуеm строку в массив байт ASCII
                var asciiBytes = Encoding.ASCII.GetBytes(asciiHex);


                // Создаём конечный буфер: +2 для стартового и стопового байта
                byte[] frame = new byte[asciiBytes.Length + 2];
                // добавляем стартовый байт в начало
                frame[0] = rawStart;
                // копируем ASCII байты после стартового байта
                Array.Copy(asciiBytes, 0, frame, 1, asciiBytes.Length);
                // Записываем стоповый байт в конец 
                frame[frame.Length - 1] = rawStop;
                //Получаем поток записи из сокета.
                var outputStream = bluetoothSocket.OutputStream;
                // Асинхронная запись
                await outputStream.WriteAsync(frame, 0, frame.Length);
                // Убедиться, что данные отправлены - вызвать Flush
                await outputStream.FlushAsync();


            }
            catch (Exception ex)
            {
              //  DataReceived?.Invoke($"Error in TransmitterData: {ex.Message}");
                DataReceivedVarta832?.Invoke($"Error in TransmitterData: {ex.Message}");
                return;
            }


            DataReceivedVarta832?.Invoke($" Tx-> {command}"); //просмотр в терминале отправки команды

        }

        // Для предотвращения одновременных вызовов TransmitterData_write, которые могут привести к конфликтам при записи в сокет, используем SemaphoreSlim для синхронизации доступа.
        private static readonly SemaphoreSlim _txLock = new SemaphoreSlim(1, 1);

        public async Task TransmitterData_write(string dataAscii,
                                               byte read = 0x02,
                                               byte address = 0x01,
                                               byte[] index = null,
                                               byte? subindex = null,
                                               bool wrapWithRawStartStop = true)
        {
            await _txLock.WaitAsync();
            try
            {
                if (bluetoothSocket == null || bluetoothSocket.OutputStream == null)
                    throw new InvalidOperationException("Bluetooth socket not ready");

                // Внешние raw как в TransmitterData()
                const byte rawStart = 0x01;
                const byte rawStop = 0x05;
                // Если index не передан, используем дефолтный 4-байтный массив. Проверяем, что index имеет длину 4 байта.
                index ??= new byte[] { 0x00, 0x00, 0x22, 0x00 };
                if (index.Length != 4) throw new ArgumentException("index must be exactly 4 bytes", nameof(index));
                // Если subindex не передан, используем дефолтное значение 0x01. 
                byte usedSubindex = subindex ?? 0x01;

                // данные ASCII + NUL
                byte[] dataBytes = Encoding.ASCII.GetBytes(dataAscii ?? string.Empty);
                if (dataBytes.Length + 1 > 255) throw new ArgumentOutOfRangeException(nameof(dataAscii));
                byte[] dataWithNull = new byte[dataBytes.Length + 1];
                Array.Copy(dataBytes, 0, dataWithNull, 0, dataBytes.Length);
                dataWithNull[dataBytes.Length] = 0x00;

                // формируем внутренний (бинарный) фрейм: read, address, index(4), subindex, length, data...
                var frame = new List<byte>(7 + index.Length + dataWithNull.Length);
                frame.Add(read);
                frame.Add(address);
                frame.AddRange(index);
                frame.Add(usedSubindex);
                frame.Add((byte)dataWithNull.Length);
                frame.AddRange(dataWithNull);

                // checksum по внутреннему фрейму
                int sum = 0;
                foreach (var b in frame) sum += b;
                byte chk = (byte)(sum & 0xFF);
                frame.Add(chk);

                // Бинарный внутренний фрейм (включая chk)
                byte[] frameBytes = frame.ToArray();
                var innerBinaryHex = BitConverter.ToString(frameBytes).Replace("-", "");
                System.Diagnostics.Debug.WriteLine($"INNER FRAME (binary with chk): {innerBinaryHex}");
                System.Diagnostics.Debug.WriteLine($"rawStart=0x{rawStart:X2}, rawStop=0x{rawStop:X2}");

                // Конвертируем внутренний фрейм в ASCII-HEX (каждый байт -> 2 ASCII символа '0'..'F')
                string asciiHex = innerBinaryHex; // уже в виде "0201..." верхнего регистра
                var asciiBytes = Encoding.ASCII.GetBytes(asciiHex);
                System.Diagnostics.Debug.WriteLine($"INNER FRAME (ASCII-HEX text): {asciiHex}");

                // Формируем toSend: rawStart + asciiBytes + rawStop  (или без обёртки если wrapWithRawStartStop=false)
                byte[] toSend;
                if (wrapWithRawStartStop)
                {
                    toSend = new byte[1 + asciiBytes.Length + 1];
                    toSend[0] = rawStart;
                    Array.Copy(asciiBytes, 0, toSend, 1, asciiBytes.Length);
                    toSend[toSend.Length - 1] = rawStop;
                }
                else
                {
                    toSend = asciiBytes;
                }

                // Лог итогового буфера (HEX представление байтов toSend)
                var hex = BitConverter.ToString(toSend).Replace("-", "");
                System.Diagnostics.Debug.WriteLine($"TX (HEX): {hex}");

                // Отправляем
                try
                {
                    var outStream = bluetoothSocket.OutputStream;
                    await outStream.WriteAsync(toSend, 0, toSend.Length).ConfigureAwait(false);
                    await outStream.FlushAsync().ConfigureAwait(false);

                    System.Diagnostics.Debug.WriteLine("TX OK");
                }
                catch (Exception writeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"TX ERROR: {writeEx}");
                    throw;
                }
            }
            finally
            {
                _txLock.Release();
            }
        }




        public async Task TransmitterData_write2(string dataAscii,
                                       byte read = 0x02,
                                       byte address = 0x01,
                                       byte[] index = null,
                                       byte? subindex = null,
                                       bool wrapWithRawStartStop = true)
        {
            await _txLock.WaitAsync();
            try
            {
                if (bluetoothSocket == null || bluetoothSocket.OutputStream == null)
                    throw new InvalidOperationException("Bluetooth socket not ready");

                const byte rawStart = 0x01;
                const byte rawStop = 0x05;

                index ??= new byte[] { 0x00, 0x00, 0x22, 0x00 };
                if (index.Length != 4) throw new ArgumentException("index must be exactly 4 bytes", nameof(index));

                byte usedSubindex = subindex ?? 0x01;

                // ✅ Тут ключевая правка: выбираем формат payload
                byte[] payload;

                bool isBinaryEnum =
                    usedSubindex == 0x0A || // UDP1
                    usedSubindex == 0x0B || // UDP2
                    usedSubindex == 0x0C || // ACK1
                    usedSubindex == 0x0D;   // ACK2

                if (isBinaryEnum)
                {
                    // ожидаем "1"/"2" и превращаем в 0x01/0x02
                    if (!byte.TryParse((dataAscii ?? "").Trim(), out var v))
                        throw new ArgumentException("Binary value must be a number (e.g. 1 or 2)", nameof(dataAscii));

                    payload = new[] { v }; // length = 1, без NUL
                }
                else
                {
                    // как было раньше: ASCII + NUL
                    byte[] dataBytes = Encoding.ASCII.GetBytes(dataAscii ?? string.Empty);
                    if (dataBytes.Length + 1 > 255) throw new ArgumentOutOfRangeException(nameof(dataAscii));
                    payload = new byte[dataBytes.Length + 1];
                    Array.Copy(dataBytes, 0, payload, 0, dataBytes.Length);
                    payload[dataBytes.Length] = 0x00; // NUL
                }

                // внутренний фрейм
                var frame = new List<byte>(2 + index.Length + 2 + payload.Length + 1);
                frame.Add(read);
                frame.Add(address);
                frame.AddRange(index);
                frame.Add(usedSubindex);
                frame.Add((byte)payload.Length);
                frame.AddRange(payload);

                int sum = 0;
                foreach (var b in frame) sum += b;
                frame.Add((byte)(sum & 0xFF));

                byte[] frameBytes = frame.ToArray();

                string innerBinaryHex = BitConverter.ToString(frameBytes).Replace("-", "");
                var asciiBytes = Encoding.ASCII.GetBytes(innerBinaryHex);

                byte[] toSend;
                if (wrapWithRawStartStop)
                {
                    toSend = new byte[1 + asciiBytes.Length + 1];
                    toSend[0] = rawStart;
                    Array.Copy(asciiBytes, 0, toSend, 1, asciiBytes.Length);
                    toSend[^1] = rawStop;
                }
                else
                {
                    toSend = asciiBytes;
                }

                var outStream = bluetoothSocket.OutputStream;
                await outStream.WriteAsync(toSend, 0, toSend.Length).ConfigureAwait(false);
                await outStream.FlushAsync().ConfigureAwait(false);
            }
            finally
            {
                _txLock.Release();
            }
        }




        public async Task TransmitterData_writeByte(
    byte value,
    byte read = 0x02,
    byte address = 0x01,
    byte[]? index = null,
    byte? subindex = null,
    bool wrapWithRawStartStop = true)
        {
            await _txLock.WaitAsync();
            try
            {
                if (bluetoothSocket == null || bluetoothSocket.OutputStream == null)
                    throw new InvalidOperationException("Bluetooth socket not ready");

                const byte rawStart = 0x01;
                const byte rawStop = 0x05;

                index ??= new byte[] { 0x00, 0x00, 0x22, 0x00 };
                if (index.Length != 4) throw new ArgumentException("index must be exactly 4 bytes", nameof(index));

                byte usedSubindex = subindex ?? 0x01;

                // ✅ payload = 1 byte (без ASCII и без NUL)
                byte[] payload = new[] { value };

                // формируем внутренний (бинарный) фрейм: read, address, index(4), subindex, length, payload...
                var frame = new List<byte>(7 + index.Length + payload.Length);
                frame.Add(read);
                frame.Add(address);
                frame.AddRange(index);
                frame.Add(usedSubindex);
                frame.Add((byte)payload.Length);   // length = 1
                frame.AddRange(payload);           // data = 01 или 02

                // checksum по внутреннему фрейму
                int sum = 0;
                foreach (var b in frame) sum += b;
                byte chk = (byte)(sum & 0xFF);
                frame.Add(chk);

                // Бинарный внутренний фрейм (включая chk)
                byte[] frameBytes = frame.ToArray();
                var innerBinaryHex = BitConverter.ToString(frameBytes).Replace("-", "");
                System.Diagnostics.Debug.WriteLine($"INNER FRAME (binary with chk): {innerBinaryHex}");
                System.Diagnostics.Debug.WriteLine($"rawStart=0x{rawStart:X2}, rawStop=0x{rawStop:X2}");

                // Конвертируем внутренний фрейм в ASCII-HEX
                string asciiHex = innerBinaryHex;
                var asciiBytes = Encoding.ASCII.GetBytes(asciiHex);
                System.Diagnostics.Debug.WriteLine($"INNER FRAME (ASCII-HEX text): {asciiHex}");

                // Формируем toSend: rawStart + asciiBytes + rawStop
                byte[] toSend;
                if (wrapWithRawStartStop)
                {
                    toSend = new byte[1 + asciiBytes.Length + 1];
                    toSend[0] = rawStart;
                    Array.Copy(asciiBytes, 0, toSend, 1, asciiBytes.Length);
                    toSend[toSend.Length - 1] = rawStop;
                }
                else
                {
                    toSend = asciiBytes;
                }

                var hex = BitConverter.ToString(toSend).Replace("-", "");
                System.Diagnostics.Debug.WriteLine($"TX (HEX): {hex}");

                // Отправляем
                var outStream = bluetoothSocket.OutputStream;
                await outStream.WriteAsync(toSend, 0, toSend.Length).ConfigureAwait(false);
                await outStream.FlushAsync().ConfigureAwait(false);

                System.Diagnostics.Debug.WriteLine("TX OK");
            }
            finally
            {
                _txLock.Release();
            }
        }









    }
}







//< !--Батарея / Питание-- >
//< Button Text = "🔋 Живлення" />
//< Button Text = "🪫 Розряджена батарея" />
//< Button Text = "⚡ Електрика" />
//< Button Text = "🔌 Підключення" />
//< Button Text = "🔆 Яскравість" />
//< Button Text = "💡 Лампочка" />
//< Button Text = "🕯 Світло" />

//< !--Сигналы и индикация -->
//<Button Text="📡 Сигнал"/>
//<Button Text="📶 Зв'язок"/>
//<Button Text="📊 Діаграма"/>
//<Button Text="📈 Графік вгору"/>
//<Button Text="📉 Графік вниз"/>
//<Button Text="📋 Дані"/>
//<Button Text="🔔 Сповіщення"/>
//<Button Text="🚨 Тривога"/>
//<Button Text="⚠️ Увага"/>
//<Button Text="❗ Помилка"/>
//<Button Text="✅ ОК"/>

//<!-- Измерения -->
//<Button Text="📏 Вимірювання"/>
//<Button Text="⚖️ Баланс"/>
//<Button Text="🌡️ Температура"/>
//<Button Text="💨 Вентиляція"/>
//<Button Text="🔥 Нагрів"/>
//<Button Text="❄️ Охолодження"/>

//<!-- Действия -->
//<Button Text="🔍 Пошук"/>
//<Button Text="🔄 Оновити"/>
//<Button Text="⚙️ Налаштування"/>
//<Button Text="🔧 Ремонт"/>
//<Button Text="🔨 Сервіс"/>
//<Button Text="🛠️ Інструменти"/>
//<Button Text="⚗️ Тест"/>
//<Button Text="🧪 Діагностика"/>

//<!-- Управление -->
//<Button Text="▶️ Старт"/>
//<Button Text="⏸️ Пауза"/>
//<Button Text="⏹️ Стоп"/>
//<Button Text="⏺️ Запис"/>
//<Button Text="⏏️ Витягнути"/>
//<Button Text="🔀 Перемикач"/>
//<Button Text="🔁 Повтор"/>
//<Button Text="🔂 Цикл"/>

//<!-- Навигация -->
//<Button Text="⬆️ Вгору"/>
//<Button Text="⬇️ Вниз"/>
//<Button Text="⬅️ Вліво"/>
//<Button Text="➡️ Вправо"/>
//<Button Text="↗️ Вгору-вправо"/>
//<Button Text="↘️ Вниз-вправо"/>
//<Button Text="🔝 На гору"/>
//<Button Text="🔚 В кінець"/>

//<!-- Статусы -->
//<Button Text="✔️ Успішно"/>
//<Button Text="❌ Скасувати"/>
//<Button Text="🔴 Вимкнено"/>
//<Button Text="🟢 Увімкнено"/>
//<Button Text="🟡 Очікування"/>
//<Button Text="🟠 Попередження"/>
//<Button Text="⚫ Офлайн"/>
//<Button Text="🔵 Онлайн"/>

//<!-- Дополнительные -->
//<Button Text="🎯 Ціль"/>
//<Button Text="📍 Позиція"/>
//<Button Text="🧲 Магніт"/>
//<Button Text="⚛️ Атом"/>
//<Button Text="🔬 Дослідження"/>
//<Button Text="🖥️ Монітор"/>
//<Button Text="💻 Комп'ютер"/>
//<Button Text="⌨️ Клавіатура"/>
//<Button Text="🖱️ Миша"/>






//═══════════════════════════════════════════════════════════════════════════
//                    BLUETOOTH УСТРОЙСТВО(отправитель)
//═══════════════════════════════════════════════════════════════════════════

//Исходные данные устройства:
//┌──────┬──────┬──────┐
//│ 0x12 │ 0x34 │ 0x46 │  ← Бинарные данные (температура, команда и т.д.)
//└──────┴──────┴──────┘
//   ↓      ↓      ↓

//Устройство вычисляет checksum:
//0x12 + 0x34 = 0x46 ✅

//Устройство преобразует в ASCII HEX-текст:
//0x12 → '1','2' (0x31, 0x32)
//0x34 → '3','4' (0x33, 0x34)
//0x46 → '4','6' (0x34, 0x36)

//Устройство формирует фрейм:
//┌──────┬──────┬──────┬──────┬──────┬──────┬──────┬──────┐
//│ 0x02 │ 0x31 │ 0x32 │ 0x33 │ 0x34 │ 0x34 │ 0x36 │ 0x05 │
//└──────┴──────┴──────┴──────┴──────┴──────┴──────┴──────┘
//   ↑    └───────────────┬────────────────┘              ↑
// START          ASCII текст "123446"                  STOP

//ОТПРАВКА через Bluetooth →


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 1: ПРИЕМ БАЙТОВ ИЗ BLUETOOTH
//═══════════════════════════════════════════════════════════════════════════

//input.Read(buffer, 0, buffer.Length)
//   ↓
//byte[] buffer = [0x02, 0x31, 0x32, 0x33, 0x34, 0x34, 0x36, 0x05]
//                 └──────────────────┬───────────────────────┘
//                         бинарные байты из Bluetooth
//   ↓

//ТИП ДАННЫХ: byte[] (массив байтов)
//ФОРМАТ: Бинарные данные


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 2: НАКОПЛЕНИЕ В БУФЕРЕ
//═══════════════════════════════════════════════════════════════════════════

//for (int i = 0; i < bytesRead; i++) 
//    recvBuf.Add(buffer[i]);
//   ↓
//List<byte> recvBuf = [0x02, 0x31, 0x32, 0x33, 0x34, 0x34, 0x36, 0x05]
//                     └────────────────┬─────────────────────────┘
//                              накопленные данные
//   ↓

//ТИП ДАННЫХ: List<byte> (динамический список байтов)
//ЗАЧЕМ: Данные могут приходить фрагментами!


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 3: ПОИСК ФРЕЙМА
//═══════════════════════════════════════════════════════════════════════════

//int start = recvBuf.IndexOf(RESP_START);  // start = 0
//int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);  // stop = 7
//   ↓

//recvBuf:
//┌──────┬──────┬──────┬──────┬──────┬──────┬──────┬──────┐
//│ 0x02 │ 0x31 │ 0x32 │ 0x33 │ 0x34 │ 0x34 │ 0x36 │ 0x05 │
//└──────┴──────┴──────┴──────┴──────┴──────┴──────┴──────┘
//   ↑    └───────────────┬────────────────┘              ↑
// start           данные (6 байтов)                    stop
//  (0)                                                  (7)


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 4: ИЗВЛЕЧЕНИЕ ASCII ДАННЫХ
//═══════════════════════════════════════════════════════════════════════════

//int asciiLen = stop - (start + 1);  // 7 - 1 = 6

//byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();
//                           Skip(1)         Take(6)
//   ↓

//byte[] asciiBytes = [0x31, 0x32, 0x33, 0x34, 0x34, 0x36]
//                     └───────────────┬────────────────┘
//                              ASCII байты (без маркеров)
//   ↓

//ТИП ДАННЫХ: byte[] (массив байтов)
//СОДЕРЖАНИЕ: ASCII-коды символов '1','2','3','4','4','6'


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 5: ASCII ДЕКОДИРОВАНИЕ (БАЙТЫ → ТЕКСТ)
//═══════════════════════════════════════════════════════════════════════════

//string asciiHex = Encoding.ASCII.GetString(asciiBytes);
//   ↓

//byte[] → string:
//0x31 → '1'
//0x32 → '2'
//0x33 → '3'
//0x34 → '4'
//0x34 → '4'
//0x36 → '6'

//string asciiHex = "123446"
//                  └──┬──┘
//                  HEX-текст
//   ↓

//ТИП ДАННЫХ: string (текст)
//СОДЕРЖАНИЕ: "123446" (HEX-представление в виде текста)


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 6: ОЧИСТКА СТРОКИ
//═══════════════════════════════════════════════════════════════════════════

//asciiHex = asciiHex.Replace("\r", "")
//                   .Replace("\n", "")
//                   .Trim()
//                   .ToUpperInvariant();
//   ↓

//Если было: "  123446\r\n"  →  "123446"
//Если было: "123446"        →  "123446"
//Если было: "abcd"          →  "ABCD"

//string asciiHex = "123446"  ← Очищенная HEX-строка
//   ↓

//ТИП ДАННЫХ: string
//ФОРМАТ: Чистая HEX-строка в верхнем регистре


//══════════════════════════════��════════════════════════════════════════════
//              ЭТАП 7: ПРОВЕРКА ЧЕТНОСТИ
//═══════════════════════════════════════════════════════════════════════════

//if (asciiHex.Length % 2 != 0)
//   ↓

//"123446".Length = 6
//6 % 2 = 0 → четная ✅

//Продолжаем обработку


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 8: HEX-ТЕКСТ → БАЙТЫ (обратное преобразование)
//═══════════════════════════════════════════════════════════════════════════

//frameBytes = new byte[asciiHex.Length / 2];  // 6 / 2 = 3
//for (int i = 0; i < frameBytes.Length; i++)
//    frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
//   ↓

//Преобразование HEX-текста → числа:
//"12" → 0x12 (18₁₀)
//"34" → 0x34 (52₁₀)
//"46" → 0x46 (70₁₀)

//byte[] frameBytes = [0x12, 0x34, 0x46]
//                    └────┬────┘ └┬┘
//                      данные  checksum
//   ↓

//ТИП ДАННЫХ: byte[] (массив байтов)
//СОДЕРЖАНИЕ: Восстановленные бинарные данные!


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 9: ПРОВЕРКА CHECKSUM
//═══════════════════════════════════════════════════════════════════════════

//int sum = 0;
//for (int i = 0; i < frameBytes.Length - 1; i++) 
//    sum += frameBytes[i];
//   ↓

//sum = 0x12 + 0x34 = 0x46

//chkOk = ((byte)(sum & 0xFF)) == frameBytes[frameBytes.Length - 1]
//      = 0x46 == 0x46
//      = true ✅
//   ↓

//Checksum правильный! Данные целые!


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 10: ФОРМИРОВАНИЕ РЕЗУЛЬТАТА ДЛЯ ОТОБРАЖЕНИЯ
//═══════════════════════════════════════════════════════════════════════════

//string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");
//   ↓
//BitConverter.ToString([0x12, 0x34, 0x46])
//   ↓
//"12-34-46"
//   ↓ .Replace("-", "")
//"123446"

//fullHex = "123446"  ← ВСЕ данные с checksum
//   ↓


//string payloadHex = BitConverter.ToString(frameBytes, 0, 2).Replace("-", "");
//   ↓
//BitConverter.ToString([0x12, 0x34, 0x46], 0, 2)
//                                         ↑  ↑
//                                      начало количество
//   ↓
//"12-34"
//   ↓ .Replace("-", "")
//"1234"

//payloadHex = "1234"  ← Только полезные данные (БЕЗ checksum)
//   ↓


//═══════════════════════════════════════════════════════════════════════════
//              ЭТАП 11: ОТПРАВКА СОБЫТИЯ
//═══════════════════════════════════════════════════════════════════════════

//DataReceived?.Invoke($"RX OK payload={payloadHex} full={fullHex}");
//   ↓

//Событие с сообщением:
//"RX OK payload=1234 full=123446"
//              └��┬─┘      └──┬──┘
//           полезные     все данные
//            данные     (с checksum)
//   ↓

//UI / другой код получает это сообщение и отображает результат!


//═══════════════════════════════════════════════════════════════════════════
//                            КОНЕЦ ЦИКЛА
//═══════════════════════════════════════════════════════════════════════════

//Обработанный фрейм удаляется из recvBuf:
//recvBuf.RemoveRange(0, stop + 1);

//Цикл продолжается для следующего фрейма или ожидает новых данных...




//BLUETOOTH УСТРОЙСТВО
//          ↓
//    [БИНАРНЫЕ БАЙТЫ]
//          ↓
//   ╔═══════════════════╗
//   ║  ЭТАП 1: ПРИЕМ    ║
//   ║  byte[] buffer    ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║  ЭТАП 2: БУФЕР    ║
//   ║  List<byte>       ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║  ЭТАП 3: ПОИСК    ║
//   ║  start/stop       ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║ ЭТАП 4: ИЗВЛЕЧЬ   ║
//   ║ byte[] asciiBytes ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║ ЭТАП 5: ДЕКОДИНГ  ║
//   ║ string "123446"   ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║ ЭТАП 6: ОЧИСТКА   ║
//   ║ Trim/Upper        ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║ ЭТАП 7: ПРОВЕРКА  ║
//   ║ четности          ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║ ЭТАП 8: HEX→БАЙТЫ ║
//   ║ [0x12,0x34,0x46]  ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║ ЭТАП 9: CHECKSUM  ║
//   ║ Проверка          ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║ ЭТАП 10: ФОРМАТ   ║
//   ║ для отображения   ║
//   ╚═══════════════════╝
//          ↓
//   ╔═══════════════════╗
//   ║ ЭТАП 11: СОБЫТИЕ  ║
//   ║ DataReceived      ║
//   ╚═══════════════════╝
//          ↓
//      UI / ЭКРАН




//┌──────┬─────────────────────────┬────────────────────────────┬──────────────────────────┬──────────────────────────┐
//│ Этап │    Операция             │      Входные данные        │    Выходные данные       │    Детали / Зачем        │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  1   │ Прием из Bluetooth      │ Bluetooth-поток            │ byte[] buffer            │ Получить сырые данные    │
//│      │ input.Read()            │ (бинарный поток)           │ [0x02, 0x31, 0x32,       │ из Bluetooth-устройства  │
//│      │                         │                            │  0x33, 0x34, 0x34,       │                          │
//│      │                         │                            │  0x36, 0x05]             │                          │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  2   │ Накопление в буфере     │ byte[] buffer              │ List<byte> recvBuf       │ Данные могут приходить   │
//│      │ recvBuf.Add()           │ [0x02, 0x31, 0x32...]      │ [0x02, 0x31, 0x32,       │ фрагментами! Накапливаем │
//│      │ (цикл for)              │                            │  0x33, 0x34, 0x34,       │ до получения полного     │
//│      │                         │                            │  0x36, 0x05]             │ фрейма                   │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  3   │ Поиск границ фрейма     │ List<byte> recvBuf         │ int start = 0            │ Найти начало (0x02) и    │
//│      │ IndexOf(RESP_START)     │ [0x02, 0x31, 0x32,         │ int stop = 7             │ конец (0x05) фрейма      │
//│      │ FindIndex(FRAME_STOP)   │  ...0x05]                  │ int asciiLen = 6         │ для извлечения данных    │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  4   │ Извлечение данных       │ List<byte> recvBuf         │ byte[] asciiBytes        │ Убрать маркеры START     │
//│      │ Skip(start+1)           │ [0x02, 0x31, 0x32,         │ [0x31, 0x32, 0x33,       │ и STOP, оставить только  │
//│      │ Take(asciiLen)          │  0x33, 0x34, 0x34,         │  0x34, 0x34, 0x36]       │ полезные ASCII-байты     │
//│      │ ToArray()               │  0x36, 0x05]               │                          │                          │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  5   │ ASCII декодирование     │ byte[] asciiBytes          │ string asciiHex          │ Преобразовать ASCII-коды │
//│      │ Encoding.ASCII          │ [0x31, 0x32, 0x33,         │ "123446"                 │ в символы:               │
//│      │ .GetString()            │  0x34, 0x34, 0x36]         │                          │ 0x31→'1', 0x32→'2' и т.д.│
//│      │                         │ (ASCII коды '1','2','3'..) │                          │                          │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  6   │ Очистка строки          │ string "  123446\r\n  "    │ string "123446"          │ Удалить \r, \n, пробелы, │
//│      │ Replace("\r","")        │ (может быть с мусором)     │ (чистая строка)          │ привести к верхнему      │
//│      │ Replace("\n","")        │                            │                          │ регистру (A-F)           │
//│      │ Trim()                  │                            │                          │                          │
//│      │ ToUpperInvariant()      │                            │                          │                          │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  7   │ Проверка четности длины │ string "123446"            │ bool (valid/invalid)     │ Длина ДОЛЖНА быть четной!│
//│      │ asciiHex.Length % 2     │ Length = 6                 │ 6 % 2 = 0 ✅             │ Потому что 2 HEX-символа │
//│      │                         │ 6 % 2 == 0? → true         │ Продолжаем обработку     │ = 1 байт (на этапе 8)    │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  8   │ HEX-строка → Байты      │ string "123446"            │ byte[] frameBytes        │ ПО 2 СИМВОЛА → 1 байт:   │
//│      │ ПО 2 СИМВОЛА!           │                            │ [0x12, 0x34, 0x46]       │                          │
//│      │                         │ Разбиение на ПАРЫ:         │                          │ "12" → 0x12 (18₁₀)       │
//│      │ for (i=0; i<3; i++)     │ ┌────┬────┬────┐           │ ┌──────┬──────┬──────┐   │ "34" → 0x34 (52₁₀)       │
//│      │   pair = Substring      │ │ 12 │ 34 │ 46 │           │ │ 0x12 │ 0x34 │ 0x46 │   │ "46" → 0x46 (70₁₀)       │
//│      │          (i*2, 2)       │ └────┴────┴────┘           │ └──────┴──────┴──────┘   │                          │
//│      │   Convert.ToByte        │  2 сим 2 сим 2 сим         │  байт   байт   байт      │ Convert.ToByte(pair, 16) │
//│      │          (pair, 16)     │                            │                          │ для каждой ПАРЫ          │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  9   │ Проверка контрольной    │ byte[] frameBytes          │ bool chkOk               │ Суммируем ВСЕ байты      │
//│      │ суммы (checksum)        │ [0x12, 0x34, 0x46]         │ true/false               │ КРОМЕ последнего:        │
//│      │                         │                            │                          │                          │
//│      │ sum = 0                 │ └────┬────┘ └┬┘            │ sum = 0x12 + 0x34 = 0x46 │ sum & 0xFF (модуль 256)  │
//│      │ for (i=0; i<Len-1; i++) │    данные  checksum        │ 0x46 == 0x46? → true ✅  │ сравниваем с последним   │
//│      │   sum += frameBytes[i]  │                            │ Данные целые!            │ байтом (контрольная      │
//│      │ chkOk = (sum & 0xFF)    │                            │                          │ сумма)                   │
//│      │    == frameBytes[Len-1] │                            │                          │                          │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  10  │ Байты → HEX-строка      │ byte[] frameBytes          │ string fullHex           │ Для отображения и        │
//│      │ (для отображения)       │ [0x12, 0x34, 0x46]         │ "123446"                 │ логирования результата   │
//│      │                         │                            │                          │                          │
//│      │ BitConverter.ToString() │                            │ string payloadHex        │ fullHex = ВСЕ данные     │
//│      │ .Replace("-", "")       │ ┌──────┬──────┬──────┐     │ "1234"                   │ (с checksum)             │
//│      │                         │ │ 0x12 │ 0x34 │ 0x46 │     │                          │                          │
//│      │ payload: без checksum   │ └──────┴──────┴──────┘     │ (БЕЗ последнего байта)   │ payloadHex = только      │
//│      │ full: с checksum        │  данные      checksum      │                          │ полезные данные          │
//│      │                         │  (0,1)        (2)          │                          │ (БЕЗ checksum)           │
//├──────┼─────────────────────────┼────────────────────────────┼──────────────────────────┼──────────────────────────┤
//│  11  │ Отправка события        │ string payloadHex          │ UI / Обработчик события  │ Передать результат       │
//│      │ DataReceived?.Invoke()  │ "1234"                     │                          │ в UI для отображения     │
//│      │                         │ string fullHex             │ Сообщение:               │ пользователю или для     │
//│      │ if (chkOk)              │ "123446"                   │ "RX OK payload=1234      │ дальнейшей обработки     │
//│      │   "RX OK..."            │                            │  full=123446"            │                          │
//│      │ else                    │                            │ или                      │                          │
//│      │   "RX CHK ERROR..."     │                            │ "RX CHK ERROR            │                          │
//│      │                         │                            │  payload=123446"         │                          │
//└──────┴─────────────────────────┴────────────────────────────┴──────────────────────────┴──────────────────────────┘




//public async Task ReceiverData_Varta832()
//{
//    // Если прием уже запущен — второй раз не стартуем 
//    // Выбор необходимого метода приема данных 
//    if (_anyRxRunning || _rxRunningVarta832) return;

//    // Помечаем, что прием данных запущен
//    _anyRxRunning = true;
//    // Этот конкретный флаг  — он показывает, что именно метод ReceiverData_Varta832 сейчас работает
//    _rxRunningVarta832 = true;

//    // Буфер для чтения из Bluetooth-сокета
//    byte[] buffer = new byte[4096];
//    // Внешние маркеры кадра старт стоп
//    const byte RESP_START = 0x02;
//    const byte FRAME_STOP = 0x05;
//    // Накопительный буфер для случаев, когда данные приходят кусками
//    var recvBuf = new List<byte>();

//    try
//    {
//        // Получаем входной поток сокета
//        var input = bluetoothSocket?.InputStream;
//        if (input == null)
//        {
//            // Если входной поток недоступен — сообщаем об ошибке и выходим
//            DataReceivedVarta832?.Invoke("Error: InputStream is null");
//            _rxRunningVarta832 = false;
//            return;
//        }
//        // Основной цикл приема данных
//        while (_rxRunningVarta832)
//        {
//            await Task.Delay(30);
//            // переменная для хранения количества прочитанных данных из входного потока
//            int bytesRead;
//            try
//            {
//                // Чтение данных из входного потока в буфер - await  позволяет не блокировать поток UI,
//                // а Task.Run выполняет чтение в отдельном потоке, но дождется его завершения.
//                bytesRead = await Task.Run(() => input.Read(buffer, 0, buffer.Length));
//            }
//            catch (Exception readEx)
//            {
//                // Если произошла ошибка при чтении данных — сообщаем об этом и выходим из цикла
//                DataReceivedVarta832?.Invoke($"Error reading from stream: {readEx.Message}");
//                break;
//            }

//            // если ничего, то возвращаемся в начало цикла while (_rxRunningVarta832)
//            if (bytesRead <= 0) continue;
//            // Добавляем прочитанные байты в накопительный буфер
//            for (int i = 0; i < bytesRead; i++)
//                recvBuf.Add(buffer[i]);

//            // Разбор накопленного буфера на кадры
//            while (true)
//            {
//                // Ищем начало кадра
//                int start = recvBuf.IndexOf(RESP_START);
//                // Если начало кадра не найдено — очищаем мусор и выходим из внутреннего цикла в основной цикл while (_rxRunningVarta832)
//                if (start == -1) { recvBuf.Clear(); break; }
//                // Ищем конец кадра после начала
//                int stop = recvBuf.FindIndex(start + 1, b => b == FRAME_STOP);
//                //
//                if (stop == -1)
//                {
//                    if (start > 0) recvBuf.RemoveRange(0, start);
//                    break;
//                }
//                // полезная часть кадра в ASCII‑HEX,asciiLen = длина этой части.
//                int asciiLen = stop - (start + 1);
//                // Если полезная часть пустая — удаляем кадр и продолжаем разбор
//                if (asciiLen <= 0) { recvBuf.RemoveRange(0, stop + 1); continue; }
//                // Извлекаем ASCII-HEX часть кадра
//                //  recvBuf.Skip(start + 1) → пропускаем все байты до позиции start + 1,nо есть игнорируем сам байт RESP_START и всё, что было до него.                       
//                //.Take(asciiLen) → берём ровно asciiLen байт(это полезная часть кадра между RESP_START и FRAME_STOP).
//                //.ToArray() → превращаем выбранные байты в массив byte[].
//                byte[] asciiBytes = recvBuf.Skip(start + 1).Take(asciiLen).ToArray();
//                // Удаляем обработанный кусок из накопительного буфера, включая RESP_START и FRAME_STOP
//                recvBuf.RemoveRange(0, stop + 1);
//                // превращаем массив ASCII‑байтов в строку, которая содержит символы HEX‑представления.
//                string asciiHex = Encoding.ASCII.GetString(asciiBytes)
//                    .Replace("\r", "").Replace("\n", "")
//                    .Trim().ToUpperInvariant();
//                // Проверяем, что длина HEX-строки четная, так как пару символов стринга - это 1 байт
//                if (asciiHex.Length % 2 != 0)
//                {
//                    DataReceivedVarta832?.Invoke($"RX ERROR: odd hex length -> {asciiHex}");
//                    continue;
//                }

//                byte[] frameBytes;
//                try
//                {
//                    frameBytes = new byte[asciiHex.Length / 2];
//                    for (int i = 0; i < frameBytes.Length; i++)
//                        frameBytes[i] = Convert.ToByte(asciiHex.Substring(i * 2, 2), 16);
//                }
//                catch
//                {
//                    DataReceivedVarta832?.Invoke($"RX ERROR: invalid hex -> {asciiHex}");
//                    continue;
//                }

//                string fullHex = BitConverter.ToString(frameBytes).Replace("-", "");
//                if (frameBytes.Length < 9)
//                {
//                    DataReceivedVarta832?.Invoke($"RX ERROR: frame too short -> {fullHex}");
//                    continue;
//                }

//                // Проверка checksum
//                int sum = 0;
//                // Суммируем все байты, кроме последнего (контрольной суммы)
//                for (int i = 0; i < frameBytes.Length - 1; i++) sum += frameBytes[i];
//                // Вычисляем ожидаемую контрольную сумму 
//                byte expectedChk = (byte)(sum & 0xFF);
//                // Получаем фактическую контрольную сумму из последнего байта кадра
//                byte actualChk = frameBytes[^1];
//                // Сравниваем ожидаемую и фактическую контрольные суммы
//                bool chkOk = expectedChk == actualChk;

//                // Основные поля
//                byte read = frameBytes[0];
//                byte address = frameBytes[1];
//                byte[] indexBytes = frameBytes.Skip(2).Take(4).ToArray();
//                byte subindex = frameBytes[6];
//                int length = frameBytes[7];

//                string indexString = BitConverter.ToString(indexBytes).Replace("-", "");
//                const int payloadStart = 8;
//                int payloadAvailable = frameBytes.Length - payloadStart - 1;

//                // ⚡ Если length некорректный — используем фактическую длину
//                int safeLength = Math.Min(length, payloadAvailable);

//                byte[] payload = frameBytes.Skip(payloadStart).Take(safeLength).ToArray();
//                string payloadHex = payload.Length > 0 ? BitConverter.ToString(payload).Replace("-", "") : string.Empty;

//                //string payloadText = string.Empty;
//                //string numericValue = string.Empty;

//                //try
//                //{
//                //    int nulIndex = Array.IndexOf(payload, (byte)0x00);
//                //    int textLen = nulIndex >= 0 ? nulIndex : payload.Length;

//                //    var enc = Encoding.GetEncoding(1251); // CP1251
//                //    payloadText = enc.GetString(payload, 0, textLen);

//                //    // ⚡ Если payload всего 1 байт — выводим его как число
//                //    if (payload.Length == 1)
//                //    {
//                //        numericValue = payload[0].ToString(); // десятичное значение
//                //    }
//                //}
//                //catch { }


//                string payloadText = string.Empty;
//                string numericValue = string.Empty;
//                string hexValues = string.Empty;
//                string decodedValues = string.Empty;
//                try
//                {
//                    if (indexString == "00003001")
//                    {
//                        // ⚡ Индекс 00003001 → трактуем как строку
//                        int nulIndex = Array.IndexOf(payload, (byte)0x00);
//                        int textLen = nulIndex >= 0 ? nulIndex : payload.Length;

//                        var enc = Encoding.GetEncoding(1251); // CP1251
//                        payloadText = enc.GetString(payload, 0, textLen);
//                    }
//                    else if (indexString == "00003019")
//                    {
//                        // ⚡ Индекс 00003019 → трактуем как числа
//                        if (payload.Length == 1)
//                        {
//                            numericValue = payload[0].ToString();
//                            // Расшифровка по таблице TypeValue
//                            if (payload[0] < TypeValue.Length)
//                                numericValue += $" ({TypeValue[payload[0]]})";
//                        }
//                        else
//                        {
//                            // если несколько байтов → выводим список чисел
//                            numericValue = string.Join(",", payload.Select(b => b.ToString()));
//                        }
//                    }
//                    else if (indexString == "00003100")
//                    {
//                        // ⚡ Индекс 00003100 → всегда список чисел
//                        numericValue = string.Join(",", payload.Select(b => b.ToString()));
//                        hexValues = string.Join(",", payload.Select(b => b.ToString("X2")));

//                    }
//                    else
//                    {
//                        // ⚡ По умолчанию пробуем как текст
//                        var enc = Encoding.GetEncoding(1251);
//                        payloadText = enc.GetString(payload, 0, payload.Length);
//                    }
//                }
//                catch { }








//                // ⚡ Выводим всегда, но помечаем статус
//                string status = chkOk ? "OK" : "WARN";
//                if (length > payloadAvailable) status = "ERROR";

//                //DataReceivedVarta832?.Invoke(
//                //    $"RX {status} read={read:X2} addr={address:X2} index={indexString} sub={subindex:X2} len={length} " +
//                //    $"payloadHex={payloadHex} text=\"{payloadText}\" value={numericValue} full={fullHex} " +
//                //    $"chkExp={expectedChk:X2} chkAct={actualChk:X2}"
//                //);


//                //DataReceivedVarta832?.Invoke(
//                //    $" index={indexString} sub={subindex:X2} len={length} " +
//                //    $"payloadHex={payloadHex} text=\"{payloadText}\" valueHex={hexValues} " +
//                //    $"chkExp={expectedChk:X2} chkAct={actualChk:X2}"
//                //  );


//                DataReceivedVarta832?.Invoke(
//                    $"index={indexString} sub={subindex:X2} len={length} " +
//                    $"payloadHex={payloadHex} text=\"{payloadText}\" valueHex={hexValues} decoded={decodedValues} " +
//                    $"chkExp={expectedChk:X2} chkAct={actualChk:X2}"
//                );







//                //DataReceivedVarta832?.Invoke(
//                //  $"read={read:X2} addr={address:X2} index={indexString} sub={subindex:X2} len={length} " +
//                //  $"payloadHex={payloadHex} text=\"{payloadText}\" value={numericValue} valueHex={hexValues} full={fullHex} " +
//                //  $"chkExp={expectedChk:X2} chkAct={actualChk:X2}"
//                //);

//            }
//        }
//    }
//    catch (Exception ex)
//    {
//        DataReceivedVarta832?.Invoke($"Error: {ex.Message}");
//    }
//    finally
//    {
//        _rxRunningVarta832 = false;
//        _anyRxRunning = false;
//    }
//}



