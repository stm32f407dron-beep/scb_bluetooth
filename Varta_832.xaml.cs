using System.Collections.ObjectModel;

namespace skb_home;

public partial class Varta_832 : ContentPage
{

    private readonly IBluetooth_service _bluetoothService;


    private readonly Dictionary<string, string> _commands = new()
    {
        ["1|Device_Name"] = "010100003001001447",
        ["2|CodeObject1"] = "010100002200010429",
        ["2|AccessPoint1"] = "01010000220002183E",
        ["2|AccessPoint2"] = "01010000220003183F",
        ["2|ServerPort1"] = "010100002200040830",
        ["2|ServerPort2"] = "010100002200050831",
        ["2|ipServer1"] = "01010000220006103A",
        ["2|ipServer2"] = "01010000220007103B",
        ["2|SMSPhone1"] = "010100002200081844",
        ["2|UDP1"] = "0101000022000A012F",
        ["2|UDP2"] = "0101000022000B0130",
        ["2|ACK1"] = "0101000022000C0131",
        ["2|ACK2"] = "0101000022000D0132",
        ["2|Hours1"] = "010100002200100135",
        ["2|Minutes1"] = "010100002200110136",
        ["2|Hours2"] = "010100002200120137",
        ["2|Minutes2"] = "010100002200130138",

    };

    public ObservableCollection<VartaFrame> Frames { get; set; } = new();

    //010100003001001447

    public Varta_832(IBluetooth_service bluetoothService)
    {
        _bluetoothService = bluetoothService;
        InitializeComponent();
        BindingContext = this;

        //_bluetoothService.DataReceivedFrame += frame =>
        //{
        //    MainThread.BeginInvokeOnMainThread(() => Frames.Add(frame));
        //};


        _bluetoothService.DataReceivedFrame += frame =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Frames.Count == 0)
                {
                    Frames.Add(frame); // первый раз создаём
                }
                else
                {
                    Frames[0] = frame; // потом обновляем
                }
            });
        };




        _bluetoothService.ReceiverData_Varta832();     //ReceiverData_Varta832_Old()    ReceiverData_Varta832()

        // Подписка на событие DataReceivedVarta832
        _bluetoothService.DataReceivedVarta832 += AddTerminalText;
    }




    // Обработка полученных данных Varta 832

    private async void OnTerminalToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            // Включаем терминал
            TerminalBorder.IsVisible = true;
            TerminalBorder.Opacity = 0;
            await TerminalBorder.FadeTo(1, 300);

            // Приветственное сообщение
            label4.Text = $"[{DateTime.Now:HH:mm:ss}] Термінал активовано\n";
        }
        else
        {
            // Выключаем терминал
            label4.Text += $"[{DateTime.Now:HH:mm:ss}] Термінал вимкнено\n";

            await Task.Delay(300);
            await TerminalBorder.FadeTo(0, 300);
            TerminalBorder.IsVisible = false;
        }
    }


    // Очистить терминал
    private void OnClearTerminalClicked(object sender, EventArgs e)
    {
        label4.Text = $"[{DateTime.Now:HH:mm:ss}] Термінал очищено\n";
    }


    // Метод для добавления текста в терминал с проверкой на включение
    public void AddTerminalText(string text)
    {
        // ✅ Проверка на null
        if (TerminalSwitch?.IsToggled != true) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // ✅ Дополнительная проверка внутри UI-потока
            if (label4 == null || scrollView == null) return;

            //  label4.Text += $"[{DateTime.Now:HH:mm:ss}]{Environment.NewLine}{text}";
            label4.Text += $"[{DateTime.Now:HH:mm:ss}] {text}\n";
            scrollView.ScrollToAsync(label4, ScrollToPosition.End, animated: true);
        });
    }




    private async void OnSaveTerminalClicked(object sender, EventArgs e)
    {
        try
        {
            string s = "010100003100012054";  //01010000301902FF4C   // 010100003100012054



            // Текст из терминала
            await  _bluetoothService?.TransmitterData(s);

            await DisplayAlert("Збереження", "Файл збережено", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Не вдалося зберегти: {ex.Message}", "OK");
        }
    }



    




}



public class VartaValue
{
    public string Name { get; set; }   // Название колонки ("ШС1", "ШС2" ...)
    public string Value { get; set; }  // Расшифрованное значение
}

public class VartaFrame
{
    public string Index { get; set; }
    public string SubIndex { get; set; }
    public bool Exists { get; set; }

    // Коллекция объектов VartaValue
    public ObservableCollection<VartaValue> Values { get; set; } = new();

    public string Header => $"{Index} (Sub={SubIndex})";
}

