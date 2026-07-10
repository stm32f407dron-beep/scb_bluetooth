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



//==========================
//🔹 БАЗОВЫЕ КОМАНДЫ
//==========================
//git status        # Проверить состояние репозитория
//git branch        # Список веток, * показывает текущую
//git switch <ветка> # Переключиться на другую ветку

//==========================
//🔹 РАБОТА С ИЗМЕНЕНИЯМИ
//==========================
//git add <файл>    # Добавить конкретный файл
//git add .         # Добавить все изменения
//git commit -m "Сообщение"  # Сделать коммит
//git push          # Отправить коммиты на GitHub
//git pull          # Подтянуть изменения с GitHub

//==========================
//🔹 ВЕТКИ
//==========================
//git branch <имя>  # Создать новую ветку
//git switch <имя>  # Переключиться на ветку
//git merge <ветка> # Слить указанную ветку в текущую

//==========================
//🔹 УПРАВЛЕНИЕ ФАЙЛАМИ
//==========================
//git rm <файл>     # Удалить файл из репозитория
//git rm -r --cached obj bin .vs  # Убрать временные папки
//git restore <файл> # Откатить изменения
//git stash         # Спрятать все изменения
//git stash pop     # Вернуть спрятанные изменения

//==========================
//🔹 ПРОВЕРКИ
//==========================
//git log           # История коммитов
//git diff          # Различия в изменённых файлах

//==========================
//🔹 .GITIGNORE (MAUI.NET / VS)
//==========================
//.vs/
//bin/
//obj/
//Debug/
//Release/
//*.user
//*.suo
//*.log
//*.tmp
//*.cache
//*.db
//*.sqlite
//*.apk
//*.aab
//*.ipa
//*.dll
//*.exe
//*.pdb
//packages/
//artifacts/
//**/resizetizer/*
