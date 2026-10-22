using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace skb_home;

public partial class Varta_832 : ContentPage
{
    private readonly IBluetooth_service _bluetoothService;

    // Все 32 шлейфа в памяти
    private readonly List<LoopRowModel> _allLoops = new();

    // 8 строк, которые отображаются прямо сейчас на экране
    public ObservableCollection<LoopRowModel> CurrentBlockLoops { get; set; } = new();

    // Текущий выбранный блок (1..4)
    private int _selectedBlock = 1;

    public Varta_832(IBluetooth_service bluetoothService)
    {
        _bluetoothService = bluetoothService;
        InitializeComponent();
        BindingContext = this;

        DeviceAddressPicker.SelectedIndex = 0;

        // Создаем все 32 шлейфа и загружаем первые 8 (БВШ 1)
        InitializeAllLoops();
        ShowBlock(1);
        LoopsCollectionView.ItemsSource = CurrentBlockLoops;

        // Прием кадра от Bluetooth
        _bluetoothService.DataReceivedFrame += delegate (VartaFrame frame)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateDataFromFrame(frame);
            });
        };

        _bluetoothService.ReceiverData_Varta832();
        _bluetoothService.DataReceivedVarta832 += AddTerminalText;
    }

    private void InitializeAllLoops()
    {
        _allLoops.Clear();
        int total = 1;
        for (int bsh = 1; bsh <= 4; bsh++)
        {
            for (int loop = 1; loop <= 8; loop++)
            {
                _allLoops.Add(new LoopRowModel
                {
                    Number = total,
                    Address = bsh,
                    LoopNumber = loop,
                    Identifier = $"ШС №{total:D2}",
                    TypeText = "Пожежний 1",
                    TypeCode = "03",
                    StatusText = "НОРМА",
                    StatusCode = "07"
                });
                total++;
            }
        }
    }

    // Переключение отображаемого блока (1, 2, 3 или 4)
    private void ShowBlock(int blockIndex)
    {
        _selectedBlock = blockIndex;
        CurrentBlockLoops.Clear();

        int startIndex = (_selectedBlock - 1) * 8;
        for (int i = 0; i < 8; i++)
        {
            CurrentBlockLoops.Add(_allLoops[startIndex + i]);
        }

        // Обновляем визуальный вид кнопок переключения
        UpdateBlockButtonsUI();
    }

    private void UpdateBlockButtonsUI()
    {
        BtnBsh1.BackgroundColor = _selectedBlock == 1 ? Color.FromArgb("#082044") : Color.FromArgb("#E0DFD8");
        BtnBsh1.TextColor = _selectedBlock == 1 ? Colors.White : Color.FromArgb("#333333");

        BtnBsh2.BackgroundColor = _selectedBlock == 2 ? Color.FromArgb("#082044") : Color.FromArgb("#E0DFD8");
        BtnBsh2.TextColor = _selectedBlock == 2 ? Colors.White : Color.FromArgb("#333333");

        BtnBsh3.BackgroundColor = _selectedBlock == 3 ? Color.FromArgb("#082044") : Color.FromArgb("#E0DFD8");
        BtnBsh3.TextColor = _selectedBlock == 3 ? Colors.White : Color.FromArgb("#333333");

        BtnBsh4.BackgroundColor = _selectedBlock == 4 ? Color.FromArgb("#082044") : Color.FromArgb("#E0DFD8");
        BtnBsh4.TextColor = _selectedBlock == 4 ? Colors.White : Color.FromArgb("#333333");

        BtnReadCurrent.Text = $"Зчитати поточний БВШ {_selectedBlock}";
    }

    // Обработчик нажатия на кнопки «БВШ 1» ... «БВШ 4»
    private void OnSelectBshClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn == BtnBsh1) ShowBlock(1);
            else if (btn == BtnBsh2) ShowBlock(2);
            else if (btn == BtnBsh3) ShowBlock(3);
            else if (btn == BtnBsh4) ShowBlock(4);
        }
    }

    // Обновление данных при получении кадра
    private void UpdateDataFromFrame(VartaFrame frame)
    {
        if (frame == null || frame.Values == null) return;
        if (!int.TryParse(frame.SubIndex, out int bshNumber)) return;

        int startIndex = (bshNumber - 1) * 8;
        if (startIndex < 0 || startIndex + 7 >= _allLoops.Count) return;

        for (int i = 0; i < 8; i++)
        {
            var row = _allLoops[startIndex + i];

            // Первые 8 значений — тип
            if (i < frame.Values.Count)
            {
                row.TypeText = frame.Values[i].Value;
                row.TypeCode = frame.Values[i].Code;
            }

            // Следующие 8 значений — стан
            int statusIdx = i + 8;
            if (statusIdx < frame.Values.Count)
            {
                row.StatusText = frame.Values[statusIdx].Value;
                row.StatusCode = frame.Values[statusIdx].Code;
            }
        }

        // Если пришедший кадр относится к текущему открытому блоку — обновляем вид на экране
        if (bshNumber == _selectedBlock)
        {
            ShowBlock(_selectedBlock);
        }
    }

    // Считывание выбранного блока
    private async void OnReadCurrentBshClicked(object sender, EventArgs e)
    {
        // Команды для субиндексов 01, 02, 03, 04
        string cmd = _selectedBlock switch
        {
            1 => "010100003100012054",
            2 => "010100003100022055",
            3 => "010100003100032056",
            4 => "010100003100042057",
            _ => "010100003100012054"
        };

        try
        {
            await _bluetoothService?.TransmitterData(cmd);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Не вдалося зчитати БВШ {_selectedBlock}: {ex.Message}", "OK");
        }
    }

    // Тулбар
    private async void OnAddressConfigClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync("Адреса приладу", "Введіть мережеву адресу:", initialValue: "1", keyboard: Keyboard.Numeric);
        if (!string.IsNullOrWhiteSpace(result))
        {
            await DisplayAlert("Адреса", $"Виставлено: {result}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e) => await DisplayAlert("Збереження", "Збережено успішно", "OK");

    private async void OnStubTabClicked(object sender, EventArgs e)
    {
        if (sender is Button b) await DisplayAlert("Вкладка", $"Розділ '{b.Text}' у розробці", "OK");
    }

    // Терминал
    private async void OnTerminalToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            TerminalBorder.IsVisible = true;
            TerminalBorder.Opacity = 0;
            await TerminalBorder.FadeTo(1, 200);
            label4.Text = $"[{DateTime.Now:HH:mm:ss}] Термінал активовано\n";
        }
        else
        {
            await TerminalBorder.FadeTo(0, 200);
            TerminalBorder.IsVisible = false;
        }
    }

    private void OnClearTerminalClicked(object sender, EventArgs e) => label4.Text = "";

    public void AddTerminalText(string text)
    {
        if (TerminalSwitch?.IsToggled != true) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (label4 == null || scrollView == null) return;
            label4.Text += $"[{DateTime.Now:HH:mm:ss}] {text}\n";
            scrollView.ScrollToAsync(label4, ScrollToPosition.End, animated: true);
        });
    }

    [Obsolete]
    private async void OnSettingsClicked(object sender, EventArgs e) => await DisplayAlert("Налаштування", "Меню", "OK");
}

public class LoopRowModel : INotifyPropertyChanged
{
    private string _typeText = "Пожежний 1";
    private string _typeCode = "03";
    private string _statusText = "НОРМА";
    private string _statusCode = "07";
    private Color _rowBackgroundColor = Color.FromArgb("#C8E6C9");

    public int Number { get; set; }
    public int Address { get; set; }
    public int LoopNumber { get; set; }
    public string Identifier { get; set; }

    public string TypeText
    {
        get => _typeText;
        set { _typeText = value; OnPropertyChanged(); }
    }

    public string TypeCode
    {
        get => _typeCode;
        set { _typeCode = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
            UpdateRowColor();
        }
    }

    public string StatusCode
    {
        get => _statusCode;
        set { _statusCode = value; OnPropertyChanged(); }
    }

    public Color RowBackgroundColor
    {
        get => _rowBackgroundColor;
        set { _rowBackgroundColor = value; OnPropertyChanged(); }
    }

    private void UpdateRowColor()
    {
        if (StatusText.Contains("Пожежа", StringComparison.OrdinalIgnoreCase) ||
            StatusText.Contains("ПОЖЕЖА", StringComparison.OrdinalIgnoreCase) ||
            StatusText.Contains("ТРИВОГА", StringComparison.OrdinalIgnoreCase) ||
            StatusCode == "0C" || StatusCode == "12")
        {
            RowBackgroundColor = Color.FromArgb("#FF8A80");
        }
        else
        {
            RowBackgroundColor = Color.FromArgb("#C8E6C9");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}

public class VartaValue
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Code { get; set; }
}

public class VartaFrame
{
    public string Index { get; set; }
    public string SubIndex { get; set; }
    public bool Exists { get; set; }
    public string Adress { get; set; }
    public ObservableCollection<VartaValue> Values { get; set; } = new();
}