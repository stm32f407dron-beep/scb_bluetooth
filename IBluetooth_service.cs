using skb_home.Models;

using System;
using System.Collections.Generic;
using System.Text;




namespace skb_home
{
    public interface IBluetooth_service
    {


        //  событие для передачи найденных устройств
        public event Action<Device_info> DeviceDiscovered;
        // Новое событие для уведомления об окончании поиска устройств
        event Action DiscoveryFinished;

        //задача для начала сканирования устройств
        Task<bool> StartScanningAsync();


        // задача для соединения с устройством
        Task<bool> ConnectToDeviceAsync(Device_info deviceInfo);


        // Событие: вызывается при получении строки данных из Bluetooth
        // Событие = КОНТЕЙНЕР для делегатов
        // DataReceived - это СОБЫТИЕ, которое может хранить СПИСОК методов
        public event Action<string> DataReceived;

        //задача по приему данных
        Task ReceiverData();

        // задача по передаче данных
        public  Task TransmitterData(string s);


        Task TransmitterData_write(string dataAscii, byte read = 0x02, byte address = 0x01, byte[] index = null, byte? subindex = null, bool wrapWithRawStartStop = true);

        Task TransmitterData_write2(string dataAscii, byte read = 0x02, byte address = 0x01, byte[] index = null, byte? subindex = null, bool wrapWithRawStartStop = true);


        Task TransmitterData_writeByte(byte value, byte read = 0x02, byte address = 0x01, byte[]? index = null, byte? subindex = null, bool wrapWithRawStartStop = true);



        // задача для Varta832 под вывод в терминал для отладки
        public event Action<string> DataReceivedVarta832;
        Task ReceiverData_Varta832_Old();



        // Новая задача для Varta832
        Task ReceiverData_Varta832();
        //В интерфейсе IBluetooth_service объявляем контракт 
        //Смысл делегата Action<VartaFrame>: Это готовый тип делегата из.NET, который говорит:
        //«Любой метод-обработчик, который подпишется на это событие, обязан принимать
        //ровно один параметр типа VartaFrame и возвращать void» (сигнатура вида void Method(VartaFrame frame)). ⚡
        event Action<VartaFrame> DataReceivedFrame;

        // ⚡ Новое свойство для таблицы кодов
        string[] TypeValue { get; }

    }
}




// Событие = КОНТЕЙНЕР для делегатов
// DataReceived - это СОБЫТИЕ, которое может хранить СПИСОК методов
//┌──────────────────────────────────────────────────────────┐
//│  AndroidBluetooth.cs                                     │
//│                                                          │
//│  public event Action<string> DataReceived;               │
//│         ^                                                │
//│         └── Это КОНТЕЙНЕР для делегатов                  │
//└──────────────────────────────────────────────────────────┘
//         │
//         │ Подписка (+=)
//         ▼
//┌──────────────────────────────────────────────────────────┐
//│  MainPage.cs                                             │
//│                                                          │
//│  _bluetoothService.DataReceived += OnDataReceived;       │
//│                                    ^^^^^^^^^^^^^^        │
//│                                    Делегат (метод)       │
//│                                                          │
//│  private void OnDataReceived(string data)                │
//│  {                                                       │
//│      // Обработка данных                                │
//│  }                                                       │
//└──────────────────────────────────────────────────────────┘

//         ▲
//         │ Вызов (Invoke)
//         │
//┌──────────────────────────────────────────────────────────┐
//│  AndroidBluetooth.cs                                     │
//│                                                          │
//│  DataReceived.Invoke("Error: InputStream is null");      │
//│               ^^^^^^                                     │
//│               Вызывает ВСЕ подписанные методы            │
//└──────────────────────────────────────────────────────────┘
