using System.Collections.ObjectModel;
using System.ComponentModel;

namespace skb_home;

public partial class BKNPage : ContentPage
{

    private readonly IBluetooth_service _bluetoothService;

   
    private readonly List<LoopRowModel_BKN> _allLoops = new();

    // 8 строк, которые отображаются прямо сейчас на экране
    public ObservableCollection<LoopRowModel_BKN> CurrentBlockLoops { get; set; } = new();

  




    public BKNPage(IBluetooth_service bluetoothService)
	{

        _bluetoothService = bluetoothService;
		InitializeComponent();

        // главным источниом данных являеться эта страница
        // для <DataTemplate> - данных {Binding Number} и так далее 
        BindingContext = this; // где искать данные
        //берем контейнер LoopsCollectionView(хамл) и источником данных для него являеться коллекция - CurrentBlockLoops
        // тоесть подключение коллекции до визуального компонента CollectionView на екране
        LoopsCollectionView.ItemsSource = CurrentBlockLoops; // привязать таблицу хамл к коллекции с#

        // Иницилизация глобальной памяти
        InitializeAllLoops();


        for (int i = 0; i < 8; i++)
        {
            CurrentBlockLoops.Add(_allLoops[i]);
        }



    }


    private void InitializeAllLoops()
    {
        _allLoops.Clear();
        int number = 1;

        for (int bkn = 0; bkn <= 15; bkn++)
        {

            for (int loop = 0; loop < 8; loop++)
            {

                _allLoops.Add(new LoopRowModel_BKN()
                {

                    Number = number,
                    Address = "null",
                    LoopNumber = loop,
                    Identifier = "null",
                    TypeText = "null",
                    TypeCode = "null",
                    StatusText = "null",
                    StatusCode = "null"

                });

                number++;

            }


        }

    
    }




    private async void OnClearClicked(object sender, EventArgs e) { }

    private async void OnSaveClicked(object sender, EventArgs e) => await DisplayAlert("Збереження", "Функція у розробці", "OK");


    private async void OnStubTabClicked(object sender, EventArgs e) { }

    private async void OnSelectBshClicked(object sender,EventArgs e) { }
    private async void OnReadCurrentBshClicked(object sender,EventArgs e) { }

    // Терминал
    private async void OnTerminalToggled(object sender, ToggledEventArgs e) { }

    private async void OnClearTerminalClicked(object sender,EventArgs args) { }



}




public class LoopRowModel_BKN : INotifyPropertyChanged
{


    // номер порядковый
    public int Number { get; set; }


    // номер шлейфа
    public int LoopNumber { get; set; }

    //Адрес
    private string? _address = "null";

    public string? Address 
    {
        get { return _address; }

        set
        {
            _address = value;
            //согласно классу PropertyChangedEventArgs передаем строку, которая будет названием свойства, которую движок .NET MAUI будет перерисовывать
            OnPropertyChanged("Address");
        }
    
    }

    private string? _identifier = "null";

    public string? Identifier 
    {
        get { return _identifier; }

        set 
        { 
            _identifier = value;
            OnPropertyChanged("Identifier");
        }

       
    }

    private string? _typeText = "null";


    public string? TypeText 
    {
        get { return _typeText; }

        set { _typeText = value; }
    
    
    }



    private string? _typeCode = "null";

    public string? TypeCode 
    {

        get { return _typeCode; }

        set { _typeCode = value; }
    
    }


    private string? _statusText = "null";

    public string? StatusText 
    {

        get { return _statusText; }

        set { _statusText = value; }
    }

    private string? _statusCode = "null";

    public string? StatusCode
    {
        get { return _statusCode; }

        set { _statusCode = value; }
    }

    //событие которое должно быть реализовано в интерфейсе - INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;

    //внутренний метод который трегерит событие PropertyChangedEventHandler PropertyChanged и вызывает делегат
    protected void OnPropertyChanged(string data) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(data) ); }

}


//Що робить { Binding Address }
//    у XAML?
//Коли у твоєму інтерфейсі з'являється рядок таблиці, розкішний движок MAUI створює об'єкт прив'язки для цього Label. У цей момент він каже:

//«Я бачу, що цей Label прив'язаний до властивості Address моделі LoopRowModel_BKN.

//Я автоматично підписуюся на подію PropertyChanged цього об'єкта і запам'ятовую ключове слово "Address"».

//Тобто { Binding Address } — це не просто пасивне відображення тексту, це активна підписка на подію зміни цієї конкретної властивості.