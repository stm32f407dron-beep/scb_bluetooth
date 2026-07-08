using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using skb_home.Platforms.Android;


namespace skb_home
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {


        // Статическое свойство для хранения ссылки на текущий экземпляр MainActivity
        public static MainActivity Instance { get; private set; }


        public void SetRequestedOrientation(ScreenOrientation orientation)
        {
            Android.Util.Log.Info("MainActivity", $"Setting RequestedOrientation to: {orientation}");
            RequestedOrientation = orientation;
        }



#pragma warning disable CS8765 // Допустимость значений NULL для типа параметра не соответствует переопределенному элементу (возможно, из-за атрибутов допустимости значений NULL).
        protected override async void OnCreate(Bundle savedInstanceState)
#pragma warning restore CS8765 // Допустимость значений NULL для типа параметра не соответствует переопределенному элементу (возможно, из-за атрибутов допустимости значений NULL).
        {
            base.OnCreate(savedInstanceState);


            // Сохраняем ссылку на текущий экземпляр
            Instance = this;
            Android.Util.Log.Info("MainActivity", "MainActivity OnCreate called. Instance initialized.");

          
            // Фиксируем ориентацию в портретном режиме при запуске приложения
            SetRequestedOrientation(Android.Content.PM.ScreenOrientation.Portrait);




            // Создаём NotificationChannel
            // CreateNotificationChannel();



            // Ждём, пока пользователь ответит на запрос разрешений
            bool granted = await BluetoothPermissionsHelper.RequestBluetoothPermissionsAsync(this);
            // INSERT LOG HERE (optional)
            Android.Util.Log.Info("BTPerms", $"MainActivity: RequestBluetoothPermissionsAsync returned: {granted}");

            if (granted)
            {
                try
                {
                    // Можно работать с Bluetooth
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
                    Toast.MakeText(this, "Bluetooth разрешения получены!", ToastLength.Short).Show();
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

                }
                catch (Exception ex) { Android.Util.Log.Info("Error", $"MainActivity: {ex.Message}"); }
                // Можно работать с Bluetooth
                //#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
                //                Toast.MakeText(this, "Bluetooth разрешения получены!", ToastLength.Short).Show();
                //#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

            }
            else
            {
                // Пользователь отказал — покажи предупреждение или ограничь функционал
#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
                Toast.MakeText(this, "Bluetooth разрешения НЕ получены!", ToastLength.Short).Show();
#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
            }
        }





        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
#pragma warning disable CA1416 // Проверка совместимости платформы
            //Когда можно не вызывать?
            //Только если ты точно уверен, что ничего кроме твоего собственного кода обработкой разрешений не занимается.
            //Но в большинстве случаев вызывать базовую реализацию — хорошая практика.
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);// уведомляет систему и библиотеки - стандартный вызов 
            Android.Util.Log.Info("MainActivity", "OnRequestPermissionsResult called.");
#pragma warning restore CA1416 // Проверка совместимости платформы
            BluetoothPermissionsHelper.OnRequestPermissionsResult(requestCode, permissions, grantResults);// моя  логика
        }



    }
}







// создание канала уведомлений для Foreground Service
/// NoMainActivity — это первая точка входа вашего приложения.
//Канал создаётся один раз за время жизни приложения(его не нужно пересоздавать при каждом вызове сервиса).
//Создание в MainActivity гарантирует, что канал будет доступен до запуска сервиса.

//        private void CreateNotificationChannel()
//        {
//            // Проверяем версию Android, так как NotificationChannel доступен с Android 8.0 (API 26)
//            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
//            {
//#pragma warning disable CA1416 // Проверка совместимости платформы
//                var channel = new NotificationChannel(
//                    "UiPriorityChannel",               // ID канала
//                    "Приоритет UI",                    // Показываемое имя
//                    NotificationImportance.Max        // Важность канала
//                )
//                {
//                    Description = "Уведомления для повышения приоритета UI"
//                };
//#pragma warning restore CA1416 // Проверка совместимости платформы

//                // Регистрируем канал в системе
//                // NotificationManager — системный сервис Android, который управляет уведомлениями и каналами.
//                //GetSystemService(NotificationService):С помощью этого метода мы получаем экземпляр NotificationManager из системы Android.
//                //Конвертация в NotificationManager Так как GetSystemService возвращает объект типа Java.Lang.Object, мы приводим его к NotificationManager.
//                var notificationManager = GetSystemService(NotificationService) as NotificationManager;

//                // Регистрируем созданный канал в системе
//                //Вызывается метод CreateNotificationChannel, который уведомляет систему о новом канале.
//                //Если такой канал уже существует(с таким же ID), ничего не произойдёт.
//#pragma warning disable CA1416 // Проверка совместимости платформы
//                notificationManager?.CreateNotificationChannel(channel);
//#pragma warning restore CA1416 // Проверка совместимости платформы
//            }
//        }


