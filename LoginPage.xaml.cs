using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Controls;
using System;

namespace skb_home;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e) // Метод для обработки нажатия кнопки входа
    {
        string login = LoginEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Помилка", "Введіть пароль!", "OK");
            return;
        }

        bool success = await AuthService.TryLoginAsync(login, password);

        if (success)
        {
            string roleName = AuthService.IsAdmin ? "Адміністратор Сервера 🔑" : "Клієнт 👤";
            await DisplayAlert("Успішно", $"Ви увійшли як: {roleName}", "OK");

            // Возврат на MainPage
            await Navigation.PopModalAsync();
        }
        else
        {
            await DisplayAlert("Відмова", "Невірний логін або пароль!", "OK");
            PasswordEntry.Text = string.Empty;
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e) // Метод для обработки нажатия кнопки регистрации
    {
        string login = LoginEntry.Text?.Trim() ?? ""; // Отримання логіну з поля вводу та видалення пробілів
        string password = PasswordEntry.Text?.Trim() ?? ""; // Отримання пароля з поля вводу та видалення пробілів
       
        if (sender is Button button) 
        {

            string originalText = button.Text;

            button.IsEnabled = false;
            button.Text = "⏳ Реєстрація..."; // Візуальний статус


            try
            {
              var result = await AuthService.RegisterClientAsync(login, password); // Виклик методу реєстрації користувача


                if (result.Success) // после регистрации показываем диалоговое окно 
                {
                    await DisplayAlert("Успіх", $"Користувача {login} успішно зареєстровано!\nТепер натисніть кнопку «Увійти».", "OK");
                }
                else
                {
                    await DisplayAlert("Помилка реєстрації", result.ErrorMessage, "OK");
                }



            }
            finally
            {
                // Обов'язково повертаємо початковий текст
                button.Text = originalText;
                button.IsEnabled = true;





            }






        }

        //var result = await AuthService.RegisterClientAsync(login, password); // Виклик методу реєстрації користувача

        //if (result.Success) // после регистрации показываем диалоговое окно 
        //{
        //    await DisplayAlert("Успіх", $"Користувача {login} успішно зареєстровано!\nТепер натисніть кнопку «Увійти».", "OK");
        //}
        //else
        //{
        //    await DisplayAlert("Помилка реєстрації", result.ErrorMessage, "OK");
        //}
    }
    // напоминалка про обработчик событий
    // object sender - это объект, который вызвал событие (например, кнопка), в него sender передается ссылка на этот объект - екзепляр кнопк Button
    // EventArgs e - это объект, который содержит данные о событии, в данном случае он не используется, но его нужно передавать в метод обработчик события

    private async void OnCancelClicked(object sender, EventArgs e) // Метод для обработки нажатия кнопки отмены
    {
        await Navigation.PopModalAsync();
    }
}