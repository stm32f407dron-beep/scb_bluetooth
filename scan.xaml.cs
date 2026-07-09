using skb_home.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
//using static skb_home.MainPage;

namespace skb_home;

public partial class scan : ContentPage
{
    public ObservableCollection<CarouselItemScan> CarouselItemScans { get; set; } // Коллекция элементов карусели
    private bool _isRotating = false; // Флаг состояния для запуска/остановки вращения
    private bool _isRotating_с = false; // Флаг состояния для запуска/остановки вращения
    //код для работы с Bluetooth

    private readonly IBluetooth_service _bluetoothService;// Сервис Bluetooth
    private readonly ObservableCollection<Device_info> _devices = new ObservableCollection<Device_info>();// Коллекция обнаруженных устройств
    private bool _suppressScanToggle = false;
    //
    public ICommand ItemSelectedCommand { get; }// Команда для обработки выбора элемента - прибора


    public scan(IBluetooth_service bluetooth)
    {
        InitializeComponent();


        // Инициализация команды
        ItemSelectedCommand = new Command<CarouselItemScan>(OnItemSelected);

        //  StartRotatingImage();

        _bluetoothService = bluetooth;// Инициализация сервиса Bluetooth
        //
        DevicesCollectionView.ItemsSource = _devices;// Привязка коллекции устройств к CollectionView

        _bluetoothService.DiscoveryFinished += delegate()// Обработчик завершения сканирования
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                //activityIndicator.IsVisible = false;
                //activityIndicator.IsRunning = false;
                ScanSwitch.IsToggled = false;
            });
        };

        _bluetoothService.DeviceDiscovered += OnDeviceDiscovered;// Обработчик обнаружения устройства
        //

        // Инициализация данных карусели
        CarouselItemScans = new ObservableCollection<CarouselItemScan>
        {
         //   new CarouselItemScan { Title = "ВАРТА 1/816", ImagePath = "varta.jpg", IconFallback = "" },
            new CarouselItemScan { Title = "ВАРТА 2/816", ImagePath = "varta.jpg", IconFallback = "" },
            new CarouselItemScan { Title = "ВАРТA 832", ImagePath = "v_832.jpg", IconFallback = "" }
        };

        BindingContext = this;// Установка контекста привязки
    }



    private async void OnItemSelected(CarouselItemScan selectedItem)
    {
        if (selectedItem != null)
        {
            // Логика изменения рамки (или выделения элемента)
            foreach (var item in CarouselItemScans)
            {
                item.IsSelected = item == selectedItem; // Устанавливаем флаг "выбранный элемент"
            }

            // Логика для перехода на Settingpage
            if (selectedItem.Title == "ВАРТА 1/816")
            {
              //  await Navigation.PushAsync(new Settingpage(_bluetoothService));
            }
            // Логика для перехода на Varta_1_816
            if (selectedItem.Title == "ВАРТА 2/816")
            {
                await Navigation.PushAsync(new Varta_1_816(_bluetoothService));
            }

            // Логика для перехода на Varta_832
            if (selectedItem.Title == "ВАРТA 832")
            {
                await Navigation.PushAsync(new Varta_832(_bluetoothService));
            }



        }
    }



    private async void StartRotatingImage()// Метод для запуска вращения картинки
    {
        rotatingImage.AnchorX = 0.5; // Центр вращения
        rotatingImage.AnchorY = 0.5; // Центр вращения
        _isRotating = true; // Устанавливаем флаг

        // Цикл только при включённом вращении
        while (_isRotating)
        {
            await rotatingImage.RotateYTo(360, 1000); // Вращение
            rotatingImage.RotationY = 0; // Сброс угла
        }
    }


    private void StopRotatingImage()// Метод для остановки вращения картинки
    {
        _isRotating = false; // Сбрасываем флаг для завершения цикла
    }


    


    private void OnDeviceDiscovered(Device_info device)// Обработчик обнаружения устройства
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (device == null) return;
            // Предотвращаем дубликаты по Address
            Func<Device_info, bool> func = delegate (Device_info d)
            {
                return string.Equals(d.Address, device.Address, System.StringComparison.OrdinalIgnoreCase);
            };

            if (!_devices.Any(func)) { _devices.Add(device); }

            // Предотвращаем дубликаты по Address
            //if (!_devices.Any(d => string.Equals(d.Address, device.Address, System.StringComparison.OrdinalIgnoreCase)))
            //{
            //    _devices.Add(device);
            //}
        });
    }



    






    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        // Если карусель уже вращается, предотвращаем двойной запуск
        if (_isRotating_с)
        {
            await DisplayAlert("Внимание", "Устройство уже подключается. Пожалуйста, подождите.", "OK");
            return;
        }

        Device_info device = null;

        // 1) Попытка получить устройство из CommandParameter
        if (e?.Parameter is Device_info p)
        {
            device = p;
            await DisplayAlert("Информация об устройстве", $"{device.Name}\n{device.Address}", "OK");
        }

        // 2) Если CommandParameter не указан, пытаемся получить устройство через BindingContext
        if (device == null && sender is VisualElement ve && ve.BindingContext is Device_info ctx)
        {
            device = ctx;
            await DisplayAlert("Информация об устройстве", $"{device.Name}\n{device.Address}", "OK");
        }

        // Если устройству не удалось присвоить значение — выходим из метода
        if (device == null)
        {
            await DisplayAlert("Ошибка", "Устройство не выбрано.", "OK");
            return;
        }

        await DisplayAlert("Устройство выбрано", $"{device.Name}\n{device.Address}", "OK");

        // Начинаем вращать карусель
        var rotationTask = StartCarouselRotationDuringConnection();

        bool connected = false; // Переменная для статуса подключения

        try
        {
            // Подключаемся к устройству
            connected = await _bluetoothService.ConnectToDeviceAsync(device);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка подключения", $"Произошла ошибка: {ex.Message}", "OK");
        }
        finally
        {
            // Всегда останавливаем карусель
            StopCarouselRotationAfterConnection();
        }

        // Показываем сообщение пользователю
        if (connected)
        {
            await DisplayAlert("Подключение", $"Успешно подключено к {device.Name}", "OK");
        }
        else
        {
            await DisplayAlert("Подключение", $"Не удалось подключиться к {device.Name}", "OK");
        }
    }





    private async Task StartCarouselRotationDuringConnection()
    {
        // Устанавливаем флаг вращения
        _isRotating_с = true;

        if (CarouselItemScans == null || CarouselItemScans.Count == 0)
        {
            // Защита от ошибки, если элементов нет в карусели
            Console.WriteLine("Карусель не содержит элементов.");
            return;
        }

        while (_isRotating_с)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (carouselView.CurrentItem is CarouselItemScan currentItem)
                    {
                        // Находим текущий индекс элемента
                        int currentIndex = CarouselItemScans.IndexOf(currentItem);
                        int nextIndex = (currentIndex + 1) % CarouselItemScans.Count; // Определяем следующий элемент
                        carouselView.ScrollTo(nextIndex, position: ScrollToPosition.Center, animate: true); // Переключаем на следующий
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при вращении карусели: {ex.Message}");
            }

            // Устанавливаем интервал между переключениями
            await Task.Delay(250);
        }
    }
    private void StopCarouselRotationAfterConnection()
    {
        // Сбрасываем флаг, чтобы остановить вращение
        _isRotating_с = false;
    }



    ////


    private async void OnBluetoothToggled(object sender, ToggledEventArgs e)//для будущего
    { }


    private async void OnScanToggled(object sender, ToggledEventArgs e)// Обработчик переключения сканирования
    {
        if (_suppressScanToggle)
        {
            return; // Предотвращает повторный вызов
        }

        _suppressScanToggle = true;

        try
        {
            if (e.Value) // Сканирование активировано
            {
                _devices.Clear(); // Очистить результаты
                bool scanStarted = await _bluetoothService.StartScanningAsync();

                if (scanStarted)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StartRotatingImage(); // Вращение запускается
                    });
                    await DisplayAlert("Scan", "Сканирование началось!", "OK");
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StopRotatingImage(); // Остановка вращения
                        rotatingImage.RotationY = 0; // Вернуть в исходное положение
                        ScanSwitch.IsToggled = false; // Сброс переключателя
                    });
                    await DisplayAlert("Scan", "Нет разрешений на сканирование!", "OK");
                }
            }
            else // Остановить сканирование
            {
                MainThread.BeginInvokeOnMainThread(StopRotatingImage); // Останавливаем вращение, но иконка остаётся
            }
        }
        finally
        {
            _suppressScanToggle = false;
        }
    }



   





    // Обработчик изменения размера окна (ориентация экрана)
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > height) // Горизонтальная ориентация
        {
            carouselView.PeekAreaInsets = 280; // Уменьшаем отступы между элементами
            carouselView.HeightRequest = 360; // Уменьшаем высоту для компактного вида
        }
        else // Вертикальная ориентация
        {
            carouselView.PeekAreaInsets = 70; // Увеличиваем отступы между элементами
            carouselView.HeightRequest = 360; // Увеличиваем высоту
        }
    }

    private async void OnCarouselItemChanged(object sender, CurrentItemChangedEventArgs e)
    {
        if (e.CurrentItem is CarouselItemScan selectedItem)
        {
            int selectedIndex = CarouselItemScans.IndexOf(selectedItem);

            foreach (var item in CarouselItemScans)
            {
                double scale = 0.7;
                double width = 180;
                double height = 180;
                double opacity = 0.5;
                double rotationY = 40;

                // Проверка на центральный элемент
                if (item == selectedItem)
                {
                    scale = 1.0;
                    width = 230;
                    height = 230;
                    opacity = 1.0;
                    rotationY = 0;
                }
                else if (CarouselItemScans.IndexOf(item) < selectedIndex) // Левый элемент
                {
                    rotationY = -40;
                }

                // Применение изменений без задержек
                item.Scale = scale;
                item.Width = width;
                item.Height = height;
                item.Opacity = opacity;
                item.RotationY = rotationY;
            }
        }
    }
}

// Модель данных для элемента карусели
public class CarouselItemScan : INotifyPropertyChanged
{
    private double scale = 1.0;
    private double rotationY = 0;
    private double opacity = 1.0;
    private double width = 200;
    private double height = 200;

    private bool isSelected; // Новое свойство

    public string Title { get; set; }
    public string ImagePath { get; set; }
    public string IconFallback { get; set; }

    public double Scale
    {
        get => scale;
        set
        {
            if (Math.Abs(scale - value) > 0.01)
            {
                scale = value;
                OnPropertyChanged(nameof(Scale));
            }
        }
    }

    public double RotationY
    {
        get => rotationY;
        set
        {
            if (Math.Abs(rotationY - value) > 0.01)
            {
                rotationY = value;
                OnPropertyChanged(nameof(RotationY));
            }
        }
    }

    public double Opacity
    {
        get => opacity;
        set
        {
            if (Math.Abs(opacity - value) > 0.01)
            {
                opacity = value;
                OnPropertyChanged(nameof(Opacity));
            }
        }
    }

    public double Width
    {
        get => width;
        set
        {
            if (Math.Abs(width - value) > 0.01)
            {
                width = value;
                OnPropertyChanged(nameof(Width));
            }
        }
    }

    public double Height
    {
        get => height;
        set
        {
            if (Math.Abs(height - value) > 0.01)
            {
                height = value;
                OnPropertyChanged(nameof(Height));
            }
        }
    }


    // Новое свойство для выделения выбранного элемента
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected != value)
            {
                isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }





    public string ImageToDisplay => string.IsNullOrEmpty(ImagePath) ? IconFallback : ImagePath;

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Task AnimateAsync(string propertyName, double value, uint duration)
    {
        // Упростил тайминги обновления и убрал лишние вызовы
        return Task.Run(() => Device.BeginInvokeOnMainThread(() =>
        {
            var propertyInfo = this.GetType().GetProperty(propertyName);
            if (propertyInfo?.CanWrite == true)
            {
                propertyInfo.SetValue(this, value);
            }
        }));
    }
}


////////////////////////вращение карусели при подключении к устройству

//private async void OnItemTapped(object sender, TappedEventArgs e)
//{
//    Device_info device = null;

//    // 1) Попробуем получить из CommandParameter (e.Parameter)
//    if (e?.Parameter is Device_info p)
//    {
//        device = p;
//        await DisplayAlert("Device from e.Parameter", $"{device.Name}\n{device.Address}", "OK");
//    }

//    // 2) Если нет — попробуем через sender.BindingContext (чаще работает)
//    //sender — это обычно Grid, на котором висит жест
//    //VisualElement — базовый класс для всех визуальных элементов в MAUI
//    //BindingContext содержит привязанный объект (в данном случае Device_info)


//    if (device == null && sender is VisualElement ve && ve.BindingContext is Device_info ctx)
//    {
//        device = ctx;
//        await DisplayAlert("Device from sender.BindingContext", $"{device.Name}\n{device.Address}", "OK");
//    }

//    if (device == null)
//        return;

//    // Пока что просто показать, что элемент выбран
//    await DisplayAlert("Устройство выбрано", $"{device.Name}\n{device.Address}", "OK");

//    // Попытка подключения к выбранному устройству - через ваш сервис Bluetooth
//    bool conneckted = await _bluetoothService.ConnectToDeviceAsync(device);

//    if (conneckted)
//    {
//        await DisplayAlert("Подключение", $"Успешно подключено к {device.Name}", "OK");
//    }
//    else
//    {
//        await DisplayAlert("Подключение", $"Не удалось подключиться к {device.Name}", "OK");
//    }

//}


//private async Task StartCarouselRotationDuringConnection()
//{
//    _isRotating_с = true; // Включаем флаг вращения
//    while (_isRotating)
//    {
//        await MainThread.InvokeOnMainThreadAsync(() =>
//        {
//            if (carouselView.CurrentItem is CarouselItemScan currentItem)
//            {
//                int currentIndex = CarouselItemScans.IndexOf(currentItem);
//                int nextIndex = (currentIndex + 1) % CarouselItemScans.Count;
//                carouselView.ScrollTo(nextIndex, position: ScrollToPosition.Center, animate: true);
//            }
//        });
//        await Task.Delay(500); // Вращаем карусель каждые 1 секунду (можно настроить)
//    }
//}

//private async Task StartCarouselRotationDuringConnection()
//{
//    _isRotating_с = true; // Включаем флаг вращения
//    while (_isRotating_с)
//    {
//        await MainThread.InvokeOnMainThreadAsync(() =>
//        {
//            if (carouselView.CurrentItem is CarouselItemScan currentItem)
//            {
//                int currentIndex = CarouselItemScans.IndexOf(currentItem); // Определяем текущий элемент
//                int nextIndex = (currentIndex + 1) % CarouselItemScans.Count; // Переход на следующий элемент
//                carouselView.ScrollTo(nextIndex, position: ScrollToPosition.Center, animate: true); // Перелистываем к следующему элементу
//            }
//        });
//        await Task.Delay(300); // Интервал времени между сменой элементов
//    }
//}



//private void StopCarouselRotationAfterConnection()
//{
//    _isRotating_с = false; // Снимаем флаг вращения
//}



/// ///////////////////////////////////


///////////////////////////





//private async void OnScanToggled(object sender, ToggledEventArgs e)
//{
//    if (_suppressScanToggle)
//    {
//        // Предотвращает повторный вызов перключателя
//        return;
//    }

//    _suppressScanToggle = true; // Устанавливаем флаг перед началом действий

//    try
//    {
//        // Ваш код
//        if (e.Value)
//        {
//            // Очистка данных
//            _devices.Clear();
//            bool b = await _bluetoothService.StartScanningAsync();

//            if (b)
//            {
//                MainThread.BeginInvokeOnMainThread(() =>
//                {
//                    activityIndicator.IsVisible = true;
//                    activityIndicator.IsRunning = true;
//                });
//                await DisplayAlert("Scan", "Сканирование началось!", "OK");
//            }
//            else
//            {
//                MainThread.BeginInvokeOnMainThread(() =>
//                {
//                    activityIndicator.IsVisible = false;
//                    activityIndicator.IsRunning = false;
//                    ScanSwitch.IsToggled = false;
//                });
//                await DisplayAlert("Scan", "Нет разрешений на сканирование!", "OK");
//            }
//        }
//        else
//        {
//            // Прекращение индикации и сброс состояния переключателя
//            MainThread.BeginInvokeOnMainThread(() =>
//            {
//                activityIndicator.IsVisible = false;
//                activityIndicator.IsRunning = false;
//            });
//        }
//    }
//    finally
//    {
//        _suppressScanToggle = false; // Сбрасываем флаг после завершения
//    }
//}





// Метод для постоянного вращения картинки
//private async void StartRotatingImage()
//{
//    while (true) // Бесконечный цикл
//    {
//        await rotatingImage.RotateTo(360, 1000); // Вращаем на 360° за 1000 мс
//        rotatingImage.Rotation = 0; // Сбрасываем вращение для нового цикла
//    }
//}

//private async void StartRotatingImage()
//{
//    rotatingImage.AnchorX = 0.5; // Установка точки вращения по горизонтали (центр по X)
//    rotatingImage.AnchorY = 0.5; // Установка точки вращения по вертикали (центр по Y)

//    while (true) // Бесконечный цикл для постоянного вращения
//    {
//        await rotatingImage.RotateYTo(360, 1000); // Вращение на 360° по вертикальной оси за 1 секунду
//        rotatingImage.RotationY = 0; // Сбрасываем угол для нового цикла
//    }
//}








////////////////////////////////////////
///
//private async void OnCarouselItemChanged(object sender, CurrentItemChangedEventArgs e)
//{
//    if (e.CurrentItem is CarouselItemScan selectedItem)
//    {
//        int selectedIndex = CarouselItemScans.IndexOf(selectedItem);

//        for (int i = 0; i < CarouselItemScans.Count; i++)
//        {
//            var item = CarouselItemScans[i];

//            if (i == selectedIndex)
//            {
//                // Центральный элемент — без наклона
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 1.0, 250),
//                    item.AnimateAsync("Width", 230, 250),
//                    item.AnimateAsync("Height", 230, 250),
//                    item.AnimateAsync("Opacity", 1.0, 250),
//                    item.AnimateAsync("RotationY", 0, 250) // Убираем углы наклона
//                );
//            }
//            else if (i < selectedIndex) // Левый элемент
//            {
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 0.7, 250),
//                    item.AnimateAsync("Width", 180, 250),
//                    item.AnimateAsync("Height", 180, 250),
//                    item.AnimateAsync("Opacity", 0.5, 250),
//                    item.AnimateAsync("RotationY", -60, 250) // Поворот влево
//                );
//            }
//            else if (i > selectedIndex) // Правый элемент
//            {
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 0.7, 250),
//                    item.AnimateAsync("Width", 180, 250),
//                    item.AnimateAsync("Height", 180, 250),
//                    item.AnimateAsync("Opacity", 0.5, 250),
//                    item.AnimateAsync("RotationY", 60, 250) // Поворот вправо
//                );
//            }
//        }
//    }
//}









//private async void OnCarouselItemChanged(object sender, CurrentItemChangedEventArgs e)
//{
//    if (e.CurrentItem is CarouselItemScan selectedItem)
//    {
//        int selectedIndex = CarouselItemScans.IndexOf(selectedItem);

//        for (int i = 0; i < CarouselItemScans.Count; i++)
//        {
//            var item = CarouselItemScans[i];

//            if (i == selectedIndex)
//            {
//                // Центральный элемент
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 1.0, 250),
//                    item.AnimateAsync("Width", 230, 250),
//                    item.AnimateAsync("Height", 230, 250),
//                    item.AnimateAsync("Opacity", 1.0, 250),
//                    item.AnimateAsync("RotationY", 80, 250) // Убираем поворот
//                );
//            }
//            else
//            {
//                // Боковые элементы — ставим одинаковый угол поворота (60)
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 0.7, 250),
//                    item.AnimateAsync("Width", 180, 250),
//                    item.AnimateAsync("Height", 180, 250),
//                    item.AnimateAsync("Opacity", 0.5, 250),
//                    item.AnimateAsync("RotationY", 60, 250) // Одинаково для боковых
//                );
//            }
//        }
//    }
//}






////////////////////////////////////////
//private async void OnCarouselItemChanged(object sender, CurrentItemChangedEventArgs e)
//{
//    if (e.CurrentItem is CarouselItemScan selectedItem)
//    {
//        foreach (var item in CarouselItemScans)
//        {
//            if (item == selectedItem)
//            {
//                // Центральный элемент — увеличиваем, убираем наклон
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 1.0, 250),
//                    item.AnimateAsync("Width", 230, 250),
//                    item.AnimateAsync("Height", 230, 250),
//                    item.AnimateAsync("Opacity", 1.0, 250),
//                    item.AnimateAsync("RotationY", 0, 250) // Выравнивание
//                );
//            }
//            else
//            {
//                // Соседние элементы — уменьшаем и поворачиваем для изометрии
//                var isLeft = item == CarouselItemScans.First(); // Левый или правый элемент
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 0.7, 250),
//                    item.AnimateAsync("Width", 180, 250),
//                    item.AnimateAsync("Height", 180, 250),
//                    item.AnimateAsync("Opacity", 0.5, 250),
//                    item.AnimateAsync("RotationY", isLeft ? -30 : 30, 250) // Поворот влево/вправо
//                );
//            }
//        }
//    }
//}







//private async void OnCarouselItemChanged(object sender, CurrentItemChangedEventArgs e)
//{
//    if (e.CurrentItem is CarouselItemScan selectedItem)
//    {
//        foreach (var item in CarouselItemScans)
//        {
//            if (item == selectedItem)
//            {
//                // Центральный элемент — увеличиваем
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 1.0, 250),
//                    item.AnimateAsync("Width", 230, 250),
//                    item.AnimateAsync("Height", 230, 250),
//                    item.AnimateAsync("Opacity", 1.0, 250),
//                    item.AnimateAsync("RotationY", 0, 250) // Нет наклона
//                );
//            }
//            else
//            {
//                // Соседние элементы — уменьшаем
//                await Task.WhenAll(
//                    item.AnimateAsync("Scale", 0.7, 250),
//                    item.AnimateAsync("Width", 180, 250),
//                    item.AnimateAsync("Height", 180, 250),
//                    item.AnimateAsync("Opacity", 0.5, 250),
//                    item.AnimateAsync("RotationY", item == CarouselItemScans.First() ? -45 : 45, 250) // Поворот
//                );
//            }
//        }
//    }
//}