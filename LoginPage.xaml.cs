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

    private async void OnLoginClicked(object sender, EventArgs e)
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

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string login = LoginEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text?.Trim() ?? "";

        var result = await AuthService.RegisterClientAsync(login, password);

        if (result.Success)
        {
            await DisplayAlert("Успіх", $"Користувача {login} успішно зареєстровано!\nТепер натисніть кнопку «Увійти».", "OK");
        }
        else
        {
            await DisplayAlert("Помилка реєстрації", result.ErrorMessage, "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}