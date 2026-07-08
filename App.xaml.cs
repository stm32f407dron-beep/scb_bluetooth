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
            MainPage = new NavigationPage(new MainPage(bluetoothService)); // Инициализация через NavigationPage       
#pragma warning restore CS0618 // Тип или член устарел
        }

      
    }
}



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


