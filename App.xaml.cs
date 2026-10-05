//using Android.Bluetooth;
using Microsoft.Extensions.DependencyInjection;

namespace skb_home
{
    public partial class App : Application
    {

       
        public App(IBluetooth_service bluetoothService)
        {
            InitializeComponent();

            //  Инициализация главной страницы с использованием NavigationPage
            //  MainPage = new NavigationPage(new MainPage());

            // Определяет, что приложение начнётся с NavigationPage, содержащей MainPage как начальную страницу.
            //Вы можете использовать PushAsync для навигации между страницами.
            // MainPage создаётся в App.xaml.cs, и там нужно получить сервис из DI
            #pragma warning disable CS0618 // Тип или член устарел
            //Именно здесь приложение «запускается» с первой страницы
            MainPage = new NavigationPage(new MainPage(bluetoothService)); // Инициализация через NavigationPage       
            #pragma warning restore CS0618 // Тип или член устарел
        }

      
    }
}



//Android запускает приложение → точка входа MainApplication.

//MainApplication.CreateMauiApp() → вызывает MauiProgram.CreateMauiApp().

//MauiProgram собирает DI‑сервисы, шрифты, кодировки → возвращает готовый MauiApp.

//App.xaml.cs → получает сервисы из DI, задаёт стартовую страницу (MainPage).

//MainActivity → управляет жизненным циклом Android (ориентация, разрешения, уведомления).

//App.xaml → подключает стили и ресурсы, чтобы всё выглядело единообразно.

//🔹 Итог
//App.xaml → ресурсы и стили.

//App.xaml.cs → стартовая страница и DI.

//MauiProgram.cs → конфигурация приложения и сервисов.

//MainActivity.cs → Android‑специфическая логика (разрешения, ориентация).

//MainApplication.cs → точка входа Android, запускает MAUI.

//⚡ То есть: Android стартует MainApplication → тот вызывает MauiProgram → создаётся App → открывается MainPage → MainActivity управляет окружением Android.









//////////////////////////////////
//namespace skb_home
//{
//    public partial class App : Application
//    {
//        public App()
//        {
//            InitializeComponent();



//            //  Инициализация главной страницы с использованием NavigationPage
//            //  MainPage = new NavigationPage(new MainPage());

//            // Определяет, что приложение начнётся с NavigationPage, содержащей MainPage как начальную страницу.
//            //Вы можете использовать PushAsync для навигации между страницами.

//#pragma warning disable CS0618 // Тип или член устарел
//            MainPage = new NavigationPage(new MainPage()); // Инициализация через NavigationPage

//            //MainPage = new NavigationPage(new MainPage())
//            //{
//            //    BarBackgroundColor = Colors.DarkSlateGray, // Цвет верхней панели
//            //    BarTextColor = Colors.White               // Цвет текста заголовка
//            //};




//#pragma warning restore CS0618 // Тип или член устарел
//        }

//        //protected override Window CreateWindow(IActivationState? activationState)
//        //{
//        //    return new Window(new AppShell());
//        //}
//    }
//}


