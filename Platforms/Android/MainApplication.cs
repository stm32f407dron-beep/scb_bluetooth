using Android.App;
using Android.Runtime;
using skb_home;

namespace skb_home
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

    //    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();


        protected override MauiApp CreateMauiApp()
        {
            return MauiProgram.CreateMauiApp();
        }

    }
}



//В Android есть базовый класс Application.

//В MAUI есть класс‑обёртка MauiApplication, который наследует Application и добавляет интеграцию с MAUI.

//Ты создаёшь свой MainApplication, который наследует MauiApplication.

//👉 Таким образом, твой класс получает все возможности Android Application + расширения MAUI, и ты можешь добавить свою логику (например, вызвать MauiProgram.CreateMauiApp()).






//🔹 Взаимодействие с другими классами
//Android Runtime запускает MainApplication.

//MainApplication.CreateMauiApp() вызывает MauiProgram.CreateMauiApp().

//MauiProgram собирает конфигурацию и возвращает MauiApp.

//App.xaml.cs получает этот MauiApp, задаёт стартовую страницу (MainPage).

//MainActivity управляет Android‑специфическими вещами (разрешения, ориентация).

//🔹 Итог
//MainApplication — точка входа Android.

//Он наследует MauiApplication, чтобы связать Android и MAUI.

//В конструкторе просто передаёт системные параметры.

//В CreateMauiApp() вызывает твой конфигуратор (MauiProgram).

//Без него Android не смог бы запустить MAUI‑часть приложения.

//⚡ По сути: Android стартует → MainApplication → MauiProgram → App.xaml.cs → MainPage.







//Android Runtime
//      │
//      ▼
//MainApplication.cs  [Application]
// └─> вызывает CreateMauiApp()
//      │
//      ▼
//MauiProgram.cs
// └─> регистрирует сервисы(DI), шрифты, логирование
// └─> возвращает объект MauiApp
//      │
//      ▼
//App.xaml.cs  (класс App)
// └─> получает сервисы из DI
// └─> задаёт стартовую страницу (MainPage)
//      │
//      ▼
//MainPage.xaml / MainPage.xaml.cs
// └─> UI и логика главной страницы



//MainActivity.cs[Activity]
// └─> точка входа Android Activity
// └─> управляет жизненным циклом (OnCreate)
// └─> фиксирует ориентацию экрана
// └─> запрашивает разрешения Bluetooth
// └─> обрабатывает результат разрешений

