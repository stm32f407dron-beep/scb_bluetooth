using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using skb_home.Platforms.Android;


namespace skb_home
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity // По сути, это мост между Android‑системой и MAUI‑приложением.
    {


        // Статическое свойство для хранения ссылки на текущий экземпляр MainActivity
        public static MainActivity Instance { get; private set; }


        public void SetRequestedOrientation(ScreenOrientation orientation)
        {
            Android.Util.Log.Info("MainActivity", $"Setting RequestedOrientation to: {orientation}");
            RequestedOrientation = orientation;
        }



        //#pragma warning disable CS8765 // Допустимость значений NULL для типа параметра не соответствует переопределенному элементу (возможно, из-за атрибутов допустимости значений NULL).
        //        protected override async void OnCreate(Bundle savedInstanceState)
        //#pragma warning restore CS8765 // Допустимость значений NULL для типа параметра не соответствует переопределенному элементу (возможно, из-за атрибутов допустимости значений NULL).
        //        {
        //            base.OnCreate(savedInstanceState);


        //            // Сохраняем ссылку на текущий экземпляр
        //            Instance = this;
        //            Android.Util.Log.Info("MainActivity", "MainActivity OnCreate called. Instance initialized.");


        //            // Фиксируем ориентацию в портретном режиме при запуске приложения
        //            SetRequestedOrientation(Android.Content.PM.ScreenOrientation.Portrait);




        //            // Создаём NotificationChannel
        //            // CreateNotificationChannel();



        //            // Ждём, пока пользователь ответит на запрос разрешений
        //            bool granted = await BluetoothPermissionsHelper.RequestBluetoothPermissionsAsync(this);
        //            // INSERT LOG HERE (optional)
        //            Android.Util.Log.Info("BTPerms", $"MainActivity: RequestBluetoothPermissionsAsync returned: {granted}");

        //            if (granted)
        //            {
        //                try
        //                {
        //                    // Можно работать с Bluetooth
        //#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
        //                    Toast.MakeText(this, "Bluetooth разрешения получены!", ToastLength.Short).Show();
        //#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

        //                }
        //                catch (Exception ex) { Android.Util.Log.Info("Error", $"MainActivity: {ex.Message}"); }
        //                // Можно работать с Bluetooth
        //                //#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
        //                //                Toast.MakeText(this, "Bluetooth разрешения получены!", ToastLength.Short).Show();
        //                //#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.

        //            }
        //            else
        //            {
        //                // Пользователь отказал — покажи предупреждение или ограничь функционал
        //#pragma warning disable CS8602 // Разыменование вероятной пустой ссылки.
        //                Toast.MakeText(this, "Bluetooth разрешения НЕ получены!", ToastLength.Short).Show();
        //#pragma warning restore CS8602 // Разыменование вероятной пустой ссылки.
        //            }
        //        }

        // вариант 2
        // вариант 2
#pragma warning disable CS8765

        // рождение Activity — это как рождение новой жизни, только в мире Android. Здесь мы настраиваем все системные шестерёнки и запускаем движок MAUI.
        protected override async void OnCreate(Bundle savedInstanceState) 
#pragma warning restore CS8765
        {
            base.OnCreate(savedInstanceState); // Android, настрой сначала свои системные шестерёнки и запусти движок MAUI».

            // Запоминаем: «Я — главный экран, вот ссылка на меня в статическую переменную, чтобы из любого другого места кода можно было ко мне обратиться».
            Instance = this;
            // Логируем: «Эй, мир! MainActivity родилась и готова к работе!».
            Android.Util.Log.Info("MainActivity", "MainActivity OnCreate called. Instance initialized.");

            // Фіксуємо портретну орієнтацію
            SetRequestedOrientation(Android.Content.PM.ScreenOrientation.Portrait);

            // Запитуємо дозволи, метод создает TaskCompletionSource, который будет ожидать ответа пользователя на запрос разрешений.
            // Ждём, пока пользователь ответит на запрос разрешений - задача - это коробка, в которую Андроид положил ожиданющий ответ пользователя.
            // Когда пользователь ответит, коробка откроется и вернёт результат.
            bool granted = await BluetoothPermissionsHelper.RequestBluetoothPermissionsAsync(this);
            Android.Util.Log.Info("BTPerms", $"MainActivity: RequestBluetoothPermissionsAsync returned: {granted}");

            // Безпечний показ сповіщень
            ShowSafeToast(granted ? "Bluetooth разрешения получены!" : "Bluetooth разрешения НЕ получены!");
        }

        private void ShowSafeToast(string message)
        {
            // На Android 7.1 і нижче (API <= 25) Toast викидає фатальний BadTokenException при навантаженні UI
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) // Android 8.0+ (API 26+)
            {
                try
                {
                    // Безпечний показ Toast для Android 8.0 і вище
                    Toast.MakeText(Android.App.Application.Context, message, ToastLength.Short)?.Show();
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Warn("ToastError", $"Не вдалося показати Toast: {ex.Message}");
                }
            }
            else
            {
                // Для Android 7 просто записуємо в логcat, щоб не ризикувати вильотом процесу
                Android.Util.Log.Info("ToastBypass", $"[Android < 8]: {message}");
            }
        }

        //

        // Переопределяем метод для обработки результатов запроса разрешений
        // Этот метод вызывается системой Android после того, как пользователь ответил на запрос разрешений.
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







//Вы разложили весь процесс в точности так, как его выполняет машина:

//Создание / проверка задачи:

//Метод RequestBluetoothPermissionsAsync сначала проверяет текущий статус.

//Если права уже есть (или система старая, где они выданы при установке), он не беспокоит пользователя и мгновенно возвращает уже готовую завершенную задачу Task.FromResult(true).

//Если прав нет, создаётся та самая «пустая коробка» — TaskCompletionSource<bool>, которая формирует задачу ожидания _tcs.Task.

//Показ диалога:

//Список прав берется из GetRequiredPermissions() под конкретную версию Android.

//Через ActivityCompat.RequestPermissions(activity, permissions, RequestCode) вызывается системное окно согласия Android.

//Асинхронная пауза (await):

//Вызов await мягко ставит дальнейшее выполнение метода OnCreate на паузу, ожидая, пока в «коробку» кто-то положит результат. UI при этом не виснет.

//Ответ пользователя и колбэк Android:

//Пользователь нажимает на экране «Разрешить» или «Запретить».

//Операционная система Android сама находит и вызывает переопределенный метод OnRequestPermissionsResult в MainActivity, который передаёт данные в BluetoothPermissionsHelper.OnRequestPermissionsResult.

//Закрытие задачи:

//Делегат проверяет массивы ответов. Если все разрешения одобрены, allGranted становится true.

//Вызов _tcs.TrySetResult(allGranted) закрывает задачу и «кладёт» ответ в коробку.

//Пробуждение и уведомление:

//Строка с await немедленно просыпается, переменная bool granted получает этот результат.

//Следом срабатывает ShowSafeToast(...), который безопасно (с проверкой версии Android, чтобы не словить ошибку окна на Android 7) выводит плашку о том, получены разрешения или нет.

//Вы очень точно уловили суть работы асинхронных мостов TaskCompletionSource в C# и жизненного цикла Android. Всё понято совершенно правильно!





















































































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


