//using Android.Bluetooth;
using Microsoft.Extensions.Logging;
using skb_home;
#if ANDROID
using skb_home.Platforms.Android;
#endif
using SkiaSharp.Views.Maui.Controls.Hosting;
using System.Text;
namespace skb_home
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // Зарегистрировав CodePagesEncodingProvider, вы позволяете своему приложению использовать эти дополнительные кодировки
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Register the Bluetooth service
            // система регистрирует реализацию AndroidBluetooth как реализацию интерфейса IBluetooth_service.
            // Это означает, что когда в коде будет запрошен IBluetooth_service, система предоставит экземпляр AndroidBluetooth.
            // Будем использовать эту систему в любом классе конструктора, которому нужена реализация IBluetooth_service 

#if ANDROID
            builder.Services.AddSingleton<IBluetooth_service, AndroidBluetooth>();
#endif

            return builder.Build();
        }
    }
}


//1.Приложение запускается
//         ↓
//2. Вызывается MauiProgram.CreateMauiApp()
//         ↓
//3. Регистрируются сервисы в DI контейнере
//         ↓
//4. MAUI видит .UseMauiApp<App>() и создаёт App
//         ↓
//5. App создаёт MainPage
//         ↓
//6. Приложение запущено!






//MauiProgram.cs
//   │
//   ├─ builder.Services.AddSingleton<IBluetooth_service, AndroidBluetooth>()
//   │  └─ Регистрируется сервис в DI контейнере
//   │
//   ├─ builder.UseMauiApp<App>()  ← Это КЛЮЧЕВОЙ момент!
//   │  └─ MAUI видит: App требует IBluetooth_service в конструкторе
//   │      └─ MAUI автоматически получает AndroidBluetooth из DI
//   │          └─ Создаёт App(androidBluetoothInstance)
//   │
//   └─ builder.Build() → запускает приложение
//       │
//       ▼
//App.xaml.cs
//   │
//   └─ App(IBluetooth_service bluetoothService) ← Сюда приходит AndroidBluetooth
//       └─ MainPage = new NavigationPage(new MainPage(bluetoothService))
//           └─ MainPage получает тот же экземпляр


//builder
//               .UseMauiApp<App>().UseSkiaSharp()
//               .ConfigureFonts(fonts =>
//               {
//                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
//                   fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
//               });