using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android.Util;
using Android;
using Android.Content.PM;
using System;
using System.Collections.Generic;
using System.Text;

namespace skb_home.Platforms.Android
{
    public static class BluetoothPermissionsHelper
    {

        public static readonly int RequestCode = 1001;
        private static TaskCompletionSource<bool>? _tcs;


        // метод для получения списка необходимых разрешений в зависимости от версии Android
        public static string[] GetRequiredPermissions()
        {

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // Android 12+
            {
                // Новые блютуз-разрешения + локация
#pragma warning disable CA1416 // Проверка совместимости платформы
                return new string[]
                {
                    Manifest.Permission.BluetoothScan,      // Для сканирования BLE устройств
                    Manifest.Permission.BluetoothConnect,   // Для подключения к BLE устройствам                 
                    Manifest.Permission.AccessFineLocation   // Для сканирования BLE устройств на Android 12 и ниже
                };
#pragma warning restore CA1416 // Проверка совместимости платформы

            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.M) // Android 6+ (API 23+)
            {
                // Старые разрешения
                return new string[]
                {
                    Manifest.Permission.Bluetooth,          // Для работы с Bluetooth
                    Manifest.Permission.BluetoothAdmin,     // Для администрирования Bluetooth
                    Manifest.Permission.AccessFineLocation // Для сканирования BLE устройств
                };
            }
            else // Android 5 и ниже
            {
                // До Android 6 (API < 23) разрешения выдавались при установке, не нужно спрашивать в рантайме
                return new string[0];
            }


        }

        // метод для проверки, выданы ли все необходимые разрешения
        public static bool HasAllPermissions(MainActivity activity)
        {
            foreach (var permission in GetRequiredPermissions())
            {
                var status = ContextCompat.CheckSelfPermission(activity, permission);

                // INSERT LOG HERE (HasAllPermissions):
                // Вставь эту строку, чтобы логировать статус проверки каждого permission:
                // Android.Util.Log.Debug("BTPerms", $"CheckSelfPermission: {permission} => {status}");





                Log.Debug("BTPerms", $"CheckSelfPermission: {permission} => {status}");
                if (status != Permission.Granted)
                    return false;
            }
            return true;
        }



        public static Task<bool> RequestBluetoothPermissionsAsync(MainActivity activity)
        {
            // Если все разрешения уже есть, сразу возвращаем true
            if (HasAllPermissions(activity))
                // возвращает уже завершённую задачу (Task), содержащую значение true (разрешения уже получены, ничего ждать не надо).
                return Task.FromResult(true);
            // Готовим инструмент для асинхронного ожидания
            // TaskCompletionSource<bool> позволяет создать задачу (Task), которую можно
            // завершить вручную, установив её результат или ошибку.
            // тоесть создаёт задачу, которую можно завершить позже, когда произойдёт
            // какое-то событие (в нашем случае — когда пользователь ответит на диалог разрешения).

            // По сути создаём “контейнер” для задачи, которую позже завершим вручную, когда получим ответ пользователя.
            _tcs = new TaskCompletionSource<bool>();

            //Получение списка необходимых разрешений
            var permissions = GetRequiredPermissions();

            // INSERT LOG HERE (RequestBluetoothPermissionsAsync):
            // Вставь эту одну строку сразу после получения permissions,
            // чтобы увидеть в логах, какие права запрашиваются:
            // Android.Util.Log.Info("BTPerms", $"RequestBluetoothPermissionsAsync: requesting {permissions.Length} permissions: {string.Join(", ", permissions)}");
            Log.Info("BTPerms", $"RequestBluetoothPermissionsAsync: requesting {permissions.Length} permissions: {string.Join(", ", permissions)}");






            if (permissions.Length > 0)
            {
                // Запрашиваем у пользователя нужные разрешения
                ActivityCompat.RequestPermissions(activity, permissions, RequestCode);
            }
            else
            {
                _tcs.SetResult(true); // Нет разрешений для запроса
            }
            //Ты создаёшь TaskCompletionSource<bool>.
            //Получаешь её задачу(.Task), которая пока "ждёт".
            // как бы говоришь: "Эй, вот моя задача, я верну её тебе сейчас,
            // но ты не завершай её пока, я скажу тебе когда".
            // Возвращаешь эту задачу вызывающему коду.
            // Возвращаем задачу, которую можно “ждать” через await в  MainActivity.
            return _tcs.Task;
        }



        // Этот метод должен быть вызван из MainActivity.OnRequestPermissionsResult
        //когда пользователь ответит на запрос разрешений.
        // Тоесть когда надо ждать ответа от пользователя и когда этот ответ придёт.
        // Когда надо получить инфо от андроида Андроид и он вызывает OnRequestPermissionsResult
        public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestCode && _tcs != null)
            {

                //
                Log.Info("BTPerms", $"OnRequestPermissionsResult: received {permissions.Length} permissions");
                for (int i = 0; i < permissions.Length; i++)
                {
                    var perm = permissions[i];
                    var res = (i < grantResults.Length) ? grantResults[i] : Permission.Denied;
                    Log.Info("BTPerms", $"  {perm} => {res}");
                }



                //
                Func<Permission, bool> func = delegate (Permission r)
                {
                    if (r == Permission.Granted)
                    { return true; }
                    else
                    { return false; }


                };
                bool allGranted = grantResults.All(func);
                _tcs.TrySetResult(allGranted);
                _tcs = null;

                //bool granted = grantResults.All(r => r == Permission.Granted);
                //_tcs.TrySetResult(granted);
                //_tcs = null;
            }
        }






    }
}
