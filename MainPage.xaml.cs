using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using skb_home;
using skb_home.Controls; // Пространство имен PopupMenu
using skb_home.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
#if ANDROID
using Android.Content.PM;
#endif
using SkiaSharp;
using SkiaSharp.Views.Maui;
//using SkiaSharp.Views.Maui.Controls;
//#if ANDROID
//using SkiaSharp.Views.Android;
//#endif

namespace skb_home;

public partial class MainPage : ContentPage
{

    // Добавляем зависимость от Bluetooth-сервиса через конструктор
    private readonly IBluetooth_service _bluetoothService; // Добавим поле для хранения ссылки на Bluetooth-сервис


    // Поле для хранения активного меню
    private PopupMenu? activePopupMenu;

    // Коллекция элементов для карусели
    public ObservableCollection<CarouselItem> CarouselItems { get; set; }

    // Команда для обработки выбора элемента карусели
    public ICommand ItemSelectedCommand { get; set; }

    // Свойство для хранения выбранного элемента (можно использовать для изменения рамки)
    public bool IsSelected { get; set; } // Новый флаг, указывающий на выбранный элемент
    //

    private bool _isRotating = false;


    public MainPage(IBluetooth_service bluetoothService)
    {
        InitializeComponent();
        
        NavigationPage.SetHasNavigationBar(this, false); // Убираем стандартную панель
        _bluetoothService = bluetoothService; // Инициализируем Bluetooth-сервис

      //  StartRotatingImage();

        // **Важное дополнение**
        // Фиксация ориентации экрана на портретную при заходе на страницы
#if ANDROID
        if (MainActivity.Instance != null)
        {
            Android.Util.Log.Info("MainPage", "Setting screen orientation to Portrait (in constructor).");
            MainActivity.Instance.SetRequestedOrientation(Android.Content.PM.ScreenOrientation.Portrait);
        }
#endif





        //
        // Настройка данных: первая страница с рисунком, остальные с снежинкой
        CarouselItems = new ObservableCollection<CarouselItem>
            {
              //  new CarouselItem { Title = "ВАРТА 1/816", ImagePath = "varta.jpg", IconFallback = "" }, // Первая страница с рисунком
                new CarouselItem { Title = "ВАРТА 2/816", ImagePath = "varta.jpg", IconFallback = "" }, // Вторая страница со снежинкой
                new CarouselItem { Title = "ВАРТA 832", ImagePath = "v_832.jpg", IconFallback = "" } // Третья страница со снежинкой
            };

        // Установка команды для обработки тапов
        ItemSelectedCommand = new Command<CarouselItem>(OnItemSelected);

        // Установка контекста данных для привязки
                   
        BindingContext = this;
     
    }


    // Метод для запуска вращения картинки
    private async void StartRotatingImage()// Метод для запуска вращения картинки
    {
        rotatingImage.AnchorX = 0.5; // Центр вращения
        rotatingImage.AnchorY = 0.5; // Центр вращения
        _isRotating = true; // Устанавливаем флаг

        // Цикл только при включённом вращении
        while (_isRotating)
        {
            await rotatingImage.RotateYTo(360, 12000); // Вращение
            rotatingImage.RotationY = 0; // Сброс угла
        }
    }





    // Фиксация ориентации экрана на портретную при заходе на страницы
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Фиксация ориентации экрана на портретную при отображении страницы
#if ANDROID
        if (MainActivity.Instance != null)
        {
            Android.Util.Log.Info("MainPage", "Setting screen orientation to Portrait (OnAppearing).");
            MainActivity.Instance.SetRequestedOrientation(Android.Content.PM.ScreenOrientation.Portrait);
        }
#endif

        if (!_isRotating)
        {
            StartRotatingImage();
        }



    }

    // Снятие ограничения ориентации при уходе со страницы
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

#if ANDROID
        if (MainActivity.Instance != null)
        {
            Android.Util.Log.Info("MainPage", "Releasing screen orientation restriction.");
            MainActivity.Instance.SetRequestedOrientation(Android.Content.PM.ScreenOrientation.Unspecified);
        }
#endif
    }



    // Обработчик нажатия на кнопку домик - авьторизация
    private void OnHashClicked(object sender, EventArgs e)
    {
        // Ваш код для обработки нажатия кнопки #
         DisplayAlert("Меню", "Функція авторизація в розробці.", "OK");
    }





    // Обработчик выбора элемента карусели
    private async void OnItemSelected(CarouselItem selectedItem)
    {
#if ANDROID       
        Android.Util.Log.Info("MainPage", $"Item selected: {selectedItem?.Title ?? "null"}");
#endif
        if (selectedItem != null)
        {
            // Логика изменения рамки
            foreach (var item in CarouselItems)
            {
                item.IsSelected = item == selectedItem; // Устанавливаем флаг выбранного элемента
            }

            // Переход на Settingpage  "ВАРТА 1/816"
            if (selectedItem.Title == "ВАРТА 1/816")
            {
//#if ANDROID
//                Android.Util.Log.Info("MainPage", "Navigating to Settingpage.");
//#endif
//                await Navigation.PushAsync(new Settingpage(_bluetoothService));
            }

            // Переход на  страницу Varta_1_816 "ВАРТА 2/816"
            if (selectedItem.Title == "ВАРТА 2/816")
            {
#if ANDROID
                Android.Util.Log.Info("MainPage", "Navigating to Settingpage.");
#endif
                await Navigation.PushAsync(new Varta_1_816(_bluetoothService));
            }



            // Логика для перехода на Varta_832
            if (selectedItem.Title == "ВАРТA 832")
            {
                await Navigation.PushAsync(new Varta_832(_bluetoothService));
            }



        }
    }
    //
    //  Обработчик нажатия на кнопку меню
    private void OnMenuClicked(object sender, EventArgs e)
    {
        // Проверка, если меню уже активно
        if (activePopupMenu != null)
        {
            this.RemoveChild(activePopupMenu); // Убираем меню
            activePopupMenu = null;
            return;
        }

        // Создание пунктов меню Додати прилад Нова автоматизація  Сканувати
        var options = new List<string>
        {
            "Сканувати",
            "Додати прилад",
            "Нова автоматизація"
        };

        // Создаем новое меню
        var popupMenu = new PopupMenu(options)
        {
            OptionSelected = OptionSelected
        };

        activePopupMenu = popupMenu;

        // Проверяем верхний тип контейнера
        var layout = this.Content as AbsoluteLayout;
        if (layout != null)
        {
            layout.Children.Add(popupMenu); // Добавляем меню поверх контента
        }
    }
    // Обработчик выбора пункта меню
    private async void OptionSelected(string option)
    {
        switch (option)
        {
            case "Додати прилад":
#if ANDROID
                Android.Util.Log.Info("MainPage", "Navigating to AddDevicePage.");
#endif
                await Navigation.PushAsync(new AddDevicePage());
                break;
            case "Нова автоматизація":
                await DisplayAlert("Меню", "Функція автоматизація в розробці.", "OK");
                break;
            case "Сканувати":
#if ANDROID
                Android.Util.Log.Info("MainPage", "Navigating to scan page.");
#endif
                await Navigation.PushAsync(new scan(_bluetoothService),animated: false);
               
                break;
        }

        // Убираем меню после выбора
        if (activePopupMenu != null)
        {
            this.RemoveChild(activePopupMenu);
            activePopupMenu = null;
        }
    }

    // Обработчик нажатия на ссылку
    private void OnLinkTapped(object sender, EventArgs e)
    {
        try
        {
            // Открыть ссылку в браузере
            Launcher.OpenAsync(new Uri("https://www.chelmash.com.ua/"));
        }
        catch (Exception ex)
        {
            // Обработка ошибок
            DisplayAlert("Ошибка", "Не удалось открыть ссылку.", "OK");
        }
    }



    // Обработчик нажатия на кнопку "Добавить устройство"
    private async void OnAddDeviceTapped(object sender, EventArgs e)
    {
      //  await DisplayAlert("Добавление", "Добавить устройство или систему", "OK");
        await Navigation.PushAsync(new AddDevicePage());
        // Здесь можно добавить переход на новую страницу
        // Например: await Navigation.PushAsync(new AddDevicePage());
    }

    // Класс для элементов карусели
    public class CarouselItem : INotifyPropertyChanged
    {
        private bool isSelected; // Закрытое поле для свойства IsSelected

        public string Title { get; set; } // Заголовок страницы
        public string ImagePath { get; set; } // Путь к изображению (если есть)
        public string IconFallback { get; set; } // Файл снежинки

        // Свойство для управления выделением элемента
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected)); // Уведомление об изменении свойства
                }
            }
        }

        // Выбор отображаемого изображения (рисунок или снежинка)
        public string ImageToDisplay => string.IsNullOrEmpty(ImagePath) ? IconFallback : ImagePath;

        // Реализация интерфейса INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }



}








//пока оставлена для SkiaSharp.Views.Maui

//private async void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
//{

//    // Выводим в консоль доступные ресурсы
//    var resourceNames = typeof(MainPage).Assembly.GetManifestResourceNames();
//    Console.WriteLine("== Доступные ресурсы ==");
//    foreach (var resourceName in resourceNames)
//    {
//        Console.WriteLine(resourceName);
//    }
//    Console.WriteLine("=========================");






//    var canvas = e.Surface.Canvas;
//    canvas.Clear(SKColors.Transparent);
//    var info = e.Info;
//    var rect = new SKRect(0, 0, info.Width, info.Height);

//    try
//    {
//        // Используем MAUI API для асинхронного открытия файла
//        using var stream = await FileSystem.OpenAppPackageFileAsync("Images/fon.jpg");

//        if (stream == null)
//        {
//            Console.WriteLine("Ошибка: не удалось найти рисунок fon.jpg в App Package.");
//            return;
//        }

//        // Декодируем изображение и рендерим через SkiaSharp
//        using var bitmap = SKBitmap.Decode(stream);

//        if (bitmap != null)
//        {
//            canvas.DrawBitmap(bitmap, rect);

//            // Градиентное размытие (если нужно)
//            using var paint = new SKPaint
//            {
//                IsAntialias = true,
//                Shader = SKShader.CreateRadialGradient(
//                    new SKPoint(info.Width / 2, info.Height / 2),
//                    info.Width / 2,
//                    new[] { SKColors.Transparent, SKColors.Black.WithAlpha(120) },
//                    null,
//                    SKShaderTileMode.Clamp)
//            };
//            canvas.DrawRect(rect, paint);
//        }
//        else
//        {
//            Console.WriteLine("Ошибка: фон не смог загрузиться!");
//        }
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"Ошибка при рендере фона: {ex.Message}");
//    }
//}






















///////////////////////////////////////
//private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
//{
//    var canvas = e.Surface.Canvas;

//    canvas.Clear(SKColors.Transparent);

//    var info = e.Info;
//    var rect = new SKRect(0, 0, info.Width, info.Height);

//    try
//    {
//        // Пробуем загрузить изображение
//        using var bitmap = SKBitmap.Decode("Resources/Images/fon.jpg");

//        if (bitmap is not null)
//        {
//            // Рисуем изображение
//            canvas.DrawBitmap(bitmap, rect);

//            // Наносим градиент
//            using var paint = new SKPaint
//            {
//                IsAntialias = true,
//                Shader = SKShader.CreateRadialGradient(
//                    new SKPoint(info.Width / 2, info.Height / 2),
//                    info.Width / 2,
//                    new[] { SKColors.Transparent, SKColors.Black.WithAlpha(120) },
//                    null,
//                    SKShaderTileMode.Clamp)
//            };

//            // Рисуем градиент
//            canvas.DrawRect(rect, paint);
//        }
//        else
//        {
//            Console.WriteLine("Ошибка: Изображение не удалось загрузить!");
//        }
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine("Ошибка при рендере фона: " + ex.Message);
//    }
//}








///////////////////////////

//    protected override void OnAppearing()
//    {
//        base.OnAppearing();

//#if ANDROID
//        Android.Util.Log.Info("MainPage", "OnAppearing called."); // Лог вызова OnAppearing

//        // Фиксация ориентации
//        if (MainActivity.Instance != null)
//        {
//            Android.Util.Log.Info("MainPage", "MainActivity.Instance is initialized. Setting orientation to Portrait.");
//            MainActivity.Instance.SetRequestedOrientation(Android.Content.PM.ScreenOrientation.Portrait);
//        }
//        else
//        {
//            Android.Util.Log.Warn("MainPage", "MainActivity.Instance is null. Unable to set orientation.");
//        }
//#endif
//    }


//    protected override void OnDisappearing()
//    {
//        base.OnDisappearing();
//#if ANDROID
//        // if (MainActivity.Instance != null)
//        // // Снимаем ограничение ориентации при выходе со страницы
//        //MainActivity.Instance.RequestedOrientation = ScreenOrientation.Unspecified;

//        // Возвращаем возможность поворачиваться на других страницах
//        if (MainActivity.Instance != null)
//        {
//            MainActivity.Instance.SetRequestedOrientation(ScreenOrientation.Unspecified);
//        }



//#endif

//    }



////////////////////////////
///







// - рабочий вариант с центровкой
//namespace skb_home
//{
//    public partial class MainPage : ContentPage
//    {
//        public MainPage()
//        {
//            InitializeComponent();
//            NavigationPage.SetHasNavigationBar(this, false); // Скрываем верхнюю панель
//        }

//        private void OnMenuClicked(object sender, EventArgs e)
//        {
//            // Список пунктов меню
//            var options = new List<string>
//            {
//                "Добавить устройство",
//                "Новая автоматизация",
//                "Сканировать"
//            };

//            // Создание нового всплывающего меню
//            var popupMenu = new PopupMenu(options)
//            {
//                OptionSelected = OptionSelected // Обработчик выбора пункта
//            };

//            // Добавляем меню на страницу
//            this.AddChild(popupMenu);
//        }





//        private async void OptionSelected(string option)
//        {
//            // Реакция на выбранный пункт
//            switch (option)
//            {
//                case "Добавить устройство":
//                    await Navigation.PushAsync(new AddDevicePage());
//                    break;
//                case "Новая автоматизация":
//                    await DisplayAlert("Меню", "Создание автоматизации.", "OK");
//                    break;
//                case "Сканировать":
//                    await DisplayAlert("Меню", "Сканирование.", "OK");
//                    break;
//            }



//        }


//    }
//}






///////////////////////////////////////////////////////////////////////////////////////////////////////////////

//private async void OnSpaceClicked(object sender, EventArgs e)
//{
//    // Переход на страницу "Пространство"
//    //  await Navigation.PushAsync(new SpacePage()); // Предполагаемая страница

//    await DisplayAlert("Меню", "Создание автоматизации.", "OK");
//}

//private async void OnAutomationClicked(object sender, EventArgs e)
//{
//    // Переход на страницу "Автоматизация"
//    //   await Navigation.PushAsync(new AutomationPage()); // Предполагаемая страница

//    await DisplayAlert("Меню", "Создание автоматизации.", "OK");
//}

//private async void OnProfileClicked(object sender, EventArgs e)
//{
//    // Переход на страницу "Я"
//    //   await Navigation.PushAsync(new ProfilePage()); // Предполагаемая страница
//    await DisplayAlert("Меню", "Создание автоматизации.", "OK");

//}



///////////////////////////////////////////
//    protected override void OnAppearing()
//    {
//        base.OnAppearing();


//#if ANDROID


//        //if (MainActivity.Instance != null)
//        //    // Ограничиваем ориентацию экрана только на портретную при входе на страницу
//        //    MainActivity.Instance.RequestedOrientation = ScreenOrientation.Portrait;


//        // Ограничиваем ориентацию экрана строго на портретную
//        if (MainActivity.Instance != null)
//        {
//            MainActivity.Instance.SetRequestedOrientation(ScreenOrientation.Portrait);
//        }




//#endif


//    }


//    protected override void OnAppearing()
//    {
//        base.OnAppearing();

//#if ANDROID
//        Android.Util.Log.Info("MainPage", "OnAppearing called."); // Лог вызова OnAppearing

//        // Фиксация ориентации
//        if (MainActivity.Instance != null)
//        {
//            Android.Util.Log.Info("MainPage", "MainActivity.Instance is initialized. Setting orientation to Portrait.");
//            MainActivity.Instance.SetRequestedOrientation(Android.Content.PM.ScreenOrientation.Portrait);
//        }
//        else
//        {
//            Android.Util.Log.Warn("MainPage", "MainActivity.Instance is null. Unable to set orientation.");
//        }
//#endif
//    }