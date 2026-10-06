using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace skb_home;

public enum UserRole
{
    None,       // Не авторизован
    Client,     // Клиент
    ServerAdmin // Администратор сервера
}

public static class AuthService
{
    public static UserRole CurrentRole { get; private set; } = UserRole.None;
    public static string CurrentUserName { get; private set; } = string.Empty;

    // Делегат и геттер проверки авторизации
    public static Func<bool> checkAuthDelegate = delegate ()
    {
        return CurrentRole != UserRole.None;
    };
    public static bool IsAuthorized => checkAuthDelegate();

    // Делегат и геттер проверки прав администратора
    public static Func<bool> checkAuthAdminDelegate = delegate ()
    {
        return CurrentRole == UserRole.ServerAdmin;
    };
    public static bool IsAdmin => checkAuthAdminDelegate();

    private const string AuthRoleKey = "user_role_key";
    private const string AuthUserKey = "user_name_key";
    private const string RegisteredUsersKey = "registered_clients_json";

    private static Dictionary<string, string> _registeredClients = new(StringComparer.OrdinalIgnoreCase);

    public static async Task InitAsync()
    {
        try
        {
            string savedRole = await SecureStorage.Default.GetAsync(AuthRoleKey);
            string savedUser = await SecureStorage.Default.GetAsync(AuthUserKey);

            if (!string.IsNullOrEmpty(savedRole) && Enum.TryParse(savedRole, out UserRole role))
            {
                CurrentRole = role;
                CurrentUserName = savedUser ?? "";
            }

            string clientsJson = await SecureStorage.Default.GetAsync(RegisteredUsersKey);
            if (!string.IsNullOrEmpty(clientsJson))
            {
                _registeredClients = JsonSerializer.Deserialize<Dictionary<string, string>>(clientsJson)
                                     ?? new(StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            _registeredClients = new(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static async Task<(bool Success, string ErrorMessage)> RegisterClientAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login))
            return (false, "Логін не може бути порожнім!");

        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            return (false, "Пароль має містити хоча б 4 символи!");

        if (_registeredClients.ContainsKey(login.Trim()))
            return (false, "Користувач із таким логіном вже існує!");

        _registeredClients[login.Trim()] = password;

        try
        {
            string json = JsonSerializer.Serialize(_registeredClients);
            await SecureStorage.Default.SetAsync(RegisteredUsersKey, json);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, $"Помилка збереження: {ex.Message}");
        }
    }

    public static async Task<bool> TryLoginAsync(string login, string password)
    {
        // 1. Пароль администратора сервера
        if (password == "Admin#Server2026")
        {
            CurrentRole = UserRole.ServerAdmin;
            CurrentUserName = "Адміністратор Сервера";
            await SaveSessionAsync();
            return true;
        }

        // 2. Проверка зарегистрированного клиента
        if (!string.IsNullOrWhiteSpace(login))
        {
            if (_registeredClients.TryGetValue(login.Trim(), out string storedPassword))
            {
                if (storedPassword == password)
                {
                    CurrentRole = UserRole.Client;
                    CurrentUserName = login.Trim();
                    await SaveSessionAsync();
                    return true;
                }
            }
        }

        return false;
    }

    private static async Task SaveSessionAsync()
    {
        try
        {
            await SecureStorage.Default.SetAsync(AuthRoleKey, CurrentRole.ToString());
            await SecureStorage.Default.SetAsync(AuthUserKey, CurrentUserName);
        }
        catch
        {
            // Защита от ошибок в эмуляторах
        }
    }

    public static async Task LogoutAsync()
    {
        CurrentRole = UserRole.None;
        CurrentUserName = string.Empty;
        try
        {
            SecureStorage.Default.Remove(AuthRoleKey);
            SecureStorage.Default.Remove(AuthUserKey);
        }
        catch
        {
        }
    }
}