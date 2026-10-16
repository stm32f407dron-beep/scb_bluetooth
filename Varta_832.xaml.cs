using System.Collections.ObjectModel;
using System.Globalization;

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

    public ObservableCollection<VartaFrame> Frames { get; set; } = new(); // Коллекция фреймов Varta

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


        //_bluetoothService.DataReceivedFrame += frame =>
        //{
        //    MainThread.BeginInvokeOnMainThread(() =>
        //    {
        //        if (Frames.Count == 0)
        //        {
        //            Frames.Add(frame); // первый раз создаём
        //        }
        //        else
        //        {
        //            Frames[0] = frame; // потом обновляем
        //        }
        //    });
        //};

        // Подписка на событие DataReceivedFrame
        //_bluetoothService.DataReceivedFrame += delegate (VartaFrame frame)
        //{
        //    MainThread.BeginInvokeOnMainThread(delegate
        //    {
        //        // Обновление UI или выполнение других действий при получении фрейма
        //        // Например, можно обновить список Frames или выполнить другую логику

        //        if (Frames.Count == 0) Frames.Add(frame); // первый раз создаём
        //        else Frames[0] = frame; // потом обновляем или перезаписываем, чтобы не дублировать

        //    });

        //};



        // Подписка на событие DataReceivedFrame
        //_bluetoothService.DataReceivedFrame += delegate (VartaFrame frame)
        //{
        //    MainThread.BeginInvokeOnMainThread(delegate
        //    {
        //        if (Frames.Count < 2)
        //        {
        //            // Первый и второй раз — добавляем элементы в список (будет 2 рамки)
        //            Frames.Add(frame);
        //        }
        //        else
        //        {
        //            // Если уже есть две рамки — обновляем вторую (или нужную по индексу)
        //            Frames[1] = frame;
        //        }
        //    });
        //};


        // Подписка на событие DataReceivedFrame в Varta_832
        _bluetoothService.DataReceivedFrame += delegate (VartaFrame frame)
        {
            MainThread.BeginInvokeOnMainThread(delegate
            {
                // Гарантируем, что в коллекции есть место под оба блока (Frames[0] и Frames[1])
                while (Frames.Count < 2)
                {
                    Frames.Add(new VartaFrame());
                }

                // В зависимости от субиндекса раскладываем по своим рамкам:
                if (frame.SubIndex == "01")
                {
                    Frames[0] = frame; // Левая рамка (БША 1)
                }
                else if (frame.SubIndex == "02")
                {
                    Frames[1] = frame; // Правая рамка (БША 2)
                }
            });
        };




        _bluetoothService.ReceiverData_Varta832();  // Запуск задачи для приёма данных Varta 832

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




    //private async void OnSaveTerminalClicked(object sender, EventArgs e)
    //{
    //    try
    //    {
    //        string s = "010100003100012054";  //01010000301902FF4C   // 010100003100012054



    //        // Текст из терминала
    //        await  _bluetoothService?.TransmitterData(s);

    //        await DisplayAlert("Команда", "Отослана", "OK");
    //    }
    //    catch (Exception ex)
    //    {
    //        await DisplayAlert("Помилка", $"Не вдалося : {ex.Message}", "OK");
    //    }
    //}


    private async void OnReadBsvh1Clicked(object sender, EventArgs e)
    {
        try
        {
            string s = "010100003100012054"; // Твой текущий адрес/команда010100003100012054
            await _bluetoothService?.TransmitterData(s);
            await DisplayAlert("Команда", "БША 1: Отослана", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Не вдалося зчитати БША 1: {ex.Message}", "OK");
        }
    }

    private async void OnReadBsvh2Clicked(object sender, EventArgs e)
    {
        try
        {
            string s = "010100003100022055"; // Тот же самый адрес/команда
            await _bluetoothService?.TransmitterData(s);
            await DisplayAlert("Команда", "БША 2: Отослана", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Помилка", $"Не вдалося зчитати БША 2: {ex.Message}", "OK");
        }
    }


    // Кнопка ⚙ (Настройки)
    [Obsolete("Используйте OnSettingsClickedAsync вместо этого метода.")]
    private async void OnSettingsClicked(object sender, EventArgs e) // Обработчик нажатия кнопки "Настройки" OnSettingsClicked
    {

       
        string choice = await DisplayActionSheet(
            "Вібір",
            "Назад",
            null,
            "Налаштування",
            "Конфігурація",
            "Струми шлейфів",
            "Коди подій",
            "Коди відновлення",
            "Індифікатор шлейфів",
            "Струми шлейфів",
            "Параметри реле",
            "Стан шлейфа",
            "Про програму"
        );

        switch (choice)
        {
            case "Налаштування":
                await DisplayAlert("Загальні", "Загальні налаштування", "OK");
                break;

    
            case "Про програму":
                await DisplayAlert("Про програму", "Версія 1.0\nРозробник: Ваше ім'я", "OK");
                break;
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
    public bool Exists { get; set; } // Флаг существования объекта

    // Коллекция объектов VartaValue
    public ObservableCollection<VartaValue> Values { get; set; } = new();

    //public string Header => $"{Index} (Sub={SubIndex})";

    public string Headers {get { return $"{Index} (Sub={SubIndex})"; } }

}

public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                // Пожар и тревоги
                "Fire" => Color.FromArgb("#D32F2F"), // Ярко-красный
                "Alarm" => Color.FromArgb("#D32F2F"), // Красный

                // Неисправности линии
                "Breakage" => Color.FromArgb("#B78103"), // Оранжевый (обрыв)
                "Short" => Color.FromArgb("#D84315"), // Красно-оранжевый (короткое замыкание)

                // Предупреждения
                "ATTENTION" => Color.FromArgb("#F57F17"), // Янтарный (внимание)

                // Норма и включение
                "NORM" => Color.FromArgb("#2E7D32"), // Зеленый (норма)
                "ON" => Color.FromArgb("#2E7D32"), // Зеленый (включено / активен)
                "OFF" => Color.FromArgb("#757575"), // Серый (выключен)

                // Отсутствие связи и питания
                "Not Data" => Color.FromArgb("#757575"), // Серый (нет данных)
                "Power-off" => Color.FromArgb("#616161"), // Темно-серый (нет питания)
                "LOST" => Color.FromArgb("#9E9E9E"), // Светло-серый (потерян)

                // Блокировки
                "BLOCKING" => Color.FromArgb("#8E24AA"), // Фиолетовый (блокировка)
                "Deblocking" => Color.FromArgb("#3949AB"), // Индиго (разблокировка)

                // Неопределенные и логические состояния
                "Indefinite" => Color.FromArgb("#103381"), // Темно-синий (не определено)
                "Logical 1" => Color.FromArgb("#00838F"), // Бирюзовый
                "Logical 2" => Color.FromArgb("#00695C"), // Морской

                "FIRE_ZONE1" => Color.FromArgb("#103381"), // Красный (пожарная зона 1)
                "FIRE_ZONE2" => Color.FromArgb("#103381"), // Красный (пожарная зона 2)
                "LOG1" => Color.FromArgb("#103381"), // Бирюзовый (логическая зона 1)
                "LOG2" => Color.FromArgb("#103381"), // Морской (логическая зона 2)
                "SECURETY" => Color.FromArgb("#103381"), // Зеленый (охрана)
                "N/D" => Color.FromArgb("#103381"), // Серый (нет данных)

                // Любой другой неизвестный текст
                _ => Color.FromArgb("#000000")  // Черный по умолчанию
            };
        }

        return Color.FromArgb("#103381");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}