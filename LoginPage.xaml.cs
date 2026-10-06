namespace skb_home;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}


    private async void OnLoginClicked(object sender, EventArgs e)
    {
        // Проверка логина/пароля...
        // После успешного входа закрываем модальное окно:
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Если передумали — просто закрываем окно:
        await Navigation.PopModalAsync();
    }




}