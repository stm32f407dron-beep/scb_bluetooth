using System.Collections.ObjectModel;

namespace skb_home;

public partial class IdentifierPage : ContentPage
{

    private readonly Varta_832 _parentPage;

    private ObservableCollection<LoopRowModel> _loopsToEdit;

    // temporary
    private readonly Dictionary<LoopRowModel,string> _initialValues = new();

    private readonly IBluetooth_service? _bluetoothService;

    private readonly int _currentDeviceAddress;

    private List<LoopRowModel> _allLoops;


    private bool _isRemoteBshvMode = false;



    private string _initialIdentifierValue = string.Empty; // Переменная для хранения начального значения Identifier


     private int _selectedBlock = 1;



    public IdentifierPage(Varta_832 parentPage, ObservableCollection<LoopRowModel> loopsToEdit,
        IBluetooth_service? bluetoothService, int currentDeviceAddress, List<LoopRowModel> allLoops, bool isRemoteBshvMode,int selectedBlock)
	{

        _parentPage = parentPage;
        _loopsToEdit = loopsToEdit;
        _bluetoothService = bluetoothService;
        _currentDeviceAddress = currentDeviceAddress;
        _allLoops = allLoops;
        _isRemoteBshvMode = isRemoteBshvMode;
        _selectedBlock = selectedBlock;

        InitializeComponent();



        BuildRows();
    }






    private void BuildRows()
    {
        // Очистка при каждом входе в метод, чтобы избежать дублирования элементов при повторном вызове
        Container8Loops.Clear();
       

        bool hasNullIdentifiers = _loopsToEdit.Any(delegate (LoopRowModel loop) { return string.IsNullOrEmpty(loop.Identifier); });
      

            foreach (var item in _loopsToEdit)
            {

                if (string.IsNullOrEmpty(item.Identifier)) 
                {
                    item.Identifier = "null";

                }

                

               _initialValues[item] = item.Identifier; // сохраняем начальное значение Identifier для каждого шлейфа  

            }
        
        
        
        

        foreach (var loop in _loopsToEdit)
        {
            // делаем сетку для колонок: первая для Label, вторая для Entry - для каждого шлейфа автономная сетка
            var grid = new Grid
            {
                ColumnSpacing = 8,
                VerticalOptions = LayoutOptions.Center
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition( GridLength.Star));

            //первая колонка: Label с текстом индификатора ШС
            var label = new Label
            {
                Text = $"ШС {loop.LoopNumber}:",
                VerticalTextAlignment = TextAlignment.Center,
                FontSize = 13,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold
            };
            //вторая колонка: Entry для ввода текста индификатора ШС
            var entry = new Entry
            {
                Text = loop.Identifier,
                Keyboard = Keyboard.Text,
                HorizontalTextAlignment = TextAlignment.Center,
                HeightRequest = 34,
                FontSize = 13
            };

            // делаем изменение текста в Entry событием, которое будет обновлять Label и свойства типа ШС
            entry.TextChanged += delegate (object? sender, TextChangedEventArgs e)
            {


                // Если новое значение не пустое, обновляем свойства
                //  loop.Identifier = e.NewTextValue;
                entry.Text = e.NewTextValue;
              //  _initialIdentifierValue = e.NewTextValue; // сохраняем новое значение в переменной _initialIdentifierValue
                _initialValues[loop] = e.NewTextValue; // обновляем значение в словаре _initialValues


            };


            entry.Unfocused += delegate(object? o, FocusEventArgs e)
            {
                if (string.IsNullOrEmpty(entry.Text))
                {
                    // Если новое значение пустое, устанавливаем текст в "null"
                    entry.Text = "null";
                  //  loop.Identifier = "null";
                  _initialValues[loop] = "null"; // обновляем значение в словаре _initialValues
                }
            };



            // почему везде ряды равны нулю (0)?
            // Потому что для каждого отдельного шлейфа мы создаем свою собственную отдельную маленькую сетку(grid).
            grid.Add(label, 0, 0);
            grid.Add(entry, 1, 0);

            Container8Loops.Add(grid); // добавляем сетку - один ряд в контейнер StackLayout, который уже отображается на экране
        }
    }




    private async void OnCancelClicked(object sender, EventArgs e)
    {
        //foreach (var pair in _initialValues)
        //{
        //    var loop = pair.Key;
        //    loop.TypeCode = pair.Value.Code;
        //    loop.TypeText = pair.Value.Text;
        //}

        await Navigation.PopModalAsync();
    }



    private async void OnSaveClicked(object sender, EventArgs e)
    {

        string s = _initialIdentifierValue;

        if (!_isRemoteBshvMode) // для базовых индетификаторов БВШ
        {

            try
            {
                foreach (var loop in _loopsToEdit)
                {
                    if (loop.Identifier != _initialValues[loop])
                    {

                        byte subIndexByte = (byte)(loop.Number + 0x00);
                        await _bluetoothService?.TransmitterData_write(_initialValues[loop], 0x02, (byte)_currentDeviceAddress, new byte[] { 0x00, 0x00, 0x30, 0x14 }, subIndexByte, true);
                        await Task.Delay(100); // Ждем 1 секунду перед закрытием страницы

                    }

                }

            }
            catch (Exception ex)
            {

                await DisplayAlert("Помилка", $"Помилка запису: {ex.Message}", "OK");
                return;
            }

        }
        else  // для внешних индефикаторов БВШВ
        {

            string indxstring = "";
            try
            {
                //Сборка индекса
                indxstring = $"01{_selectedBlock:X2}2102";
                byte[] index = new byte[4];
                for (int i = 0; i < index.Length; i++)
                {
                    index[i] = Convert.ToByte(indxstring.Substring(i * 2, 2), 16);
                }

                foreach (var item in _loopsToEdit)
                {

                    if (item.Identifier != _initialValues[item]) 
                    {
                        byte subIndexByte = (byte)(item.LoopNumber + 0x00);
                        await _bluetoothService?.TransmitterData_write(_initialValues[item], 0x02, (byte)_currentDeviceAddress, index, subIndexByte, true);
                        await Task.Delay(100); // Ждем 1 секунду перед закрытием страницы


                    }


                }


            }
            catch (Exception ex)
            {

                await DisplayAlert("Помилка", $"Помилка запису: {ex.Message}", "OK");
                return;
            }
            


        }

      //  await _bluetoothService?.TransmitterData_write(s, 0x02, (byte)_currentDeviceAddress, new byte[] { 0x00, 0x00, 0x30, 0x14 }, 0x02, true);
        //public async Task TransmitterData_write(string dataAscii, byte read = 0x02,byte address = 0x01, byte[] index = null,  byte? subindex = null, bool wrapWithRawStartStop = true)

        await Task.Delay(20); // Ждем 1 секунду перед закрытием страницы
        await Navigation.PopModalAsync();
    }




}