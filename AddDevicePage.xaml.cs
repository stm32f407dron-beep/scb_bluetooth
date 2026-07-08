using Microsoft.Maui.Controls;
using skb_home.Models;
using System.Collections.ObjectModel;
using System.ComponentModel; // Для INotifyPropertyChanged
using System.Windows.Input;

namespace skb_home
{
    public partial class AddDevicePage : ContentPage
    {
       
        public ObservableCollection<CarouselItem> CarouselItems { get; set; }
        public ICommand ItemSelectedCommand { get; set; }

        public bool IsSelected { get; set; } // Новый флаг, указывающий на выбранный элемент

        public AddDevicePage()
        {
            InitializeComponent();

            // Настройка данных: первая страница с рисунком, остальные с снежинкой
            CarouselItems = new ObservableCollection<CarouselItem>
            {
                new CarouselItem { Title = "ВАРТА 1/816", ImagePath = "varta.jpg", IconFallback = "" }, // Первая страница с рисунком
                new CarouselItem { Title = "ВАРТА 1/4", ImagePath = "varta_1_4.jpg", IconFallback = "" }, // Вторая страница со снежинкой
                new CarouselItem { Title = "TK-2GSM-02", ImagePath = "tk_2gsm_02.jpg", IconFallback = "" } // Третья страница со снежинкой
            };

            // Установка команды для обработки тапов
            ItemSelectedCommand = new Command<CarouselItem>(OnItemSelected);

            BindingContext = this;
        }



        //private async void OnItemSelected(CarouselItem selectedItem)
        //{
        //    // Переход на Settingpage только для "ВАРТА 1/816"
        //    if (selectedItem != null && selectedItem.Title == "ВАРТА 1/816")
        //    {
        //        await Navigation.PushAsync(new Settingpage());
        //    }
        //}

        private async void OnItemSelected(CarouselItem selectedItem)
        {
            if (selectedItem != null)
            {
                // Логика изменения рамки
                foreach (var item in CarouselItems)
                {
                    item.IsSelected = item == selectedItem; // Устанавливаем флаг выбранного элемента
                }

                // Переход на Settingpage только для "ВАРТА 1/816"
                if (selectedItem.Title == "ВАРТА 1/816")
                {
                  //  await Navigation.PushAsync(new Settingpage());
                }
            }
        }




    }








    //public class CarouselItem
    //{
    //    public string Title { get; set; } // Заголовок страницы
    //    public string ImagePath { get; set; } // Путь к изображению (если есть)
    //    public string IconFallback { get; set; } // Файл снежинки

    //    // Выбор отображаемого изображения (рисунок или снежинка)
    //    public string ImageToDisplay => string.IsNullOrEmpty(ImagePath) ? IconFallback : ImagePath;
    //}

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