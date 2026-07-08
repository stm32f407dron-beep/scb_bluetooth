using Android.Bluetooth;
using Android.Content;
using skb_home.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace skb_home.Platforms.Android
{
    internal class Device_Receiver: BroadcastReceiver
    {
        private readonly Action<Device_info>? _onDeviceFound;
        private readonly Action? _onDiscoveryFinished;


         public Device_Receiver(Action<Device_info> onDeviceFound, Action onDiscoveryFinished)
        {

            _onDeviceFound = onDeviceFound;
            _onDiscoveryFinished = onDiscoveryFinished;

        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action == BluetoothDevice.ActionFound)
            {

                //GetParcelableExtra — стандартный способ получить данные из интента.
                //BluetoothDevice.ExtraDevice — ключ для получения конкретного устройства.
#pragma warning disable CA1422 // Проверка совместимости платформы
                var device = intent.GetParcelableExtra(BluetoothDevice.ExtraDevice) as BluetoothDevice;
#pragma warning restore CA1422 // Проверка совместимости платформы
                if (device != null)
                {
                    var name = string.IsNullOrEmpty(device.Name) ? "Неизвестное устройство" : device.Name;
                    var address = device.Address;

                    var device_info = new Device_info
                    {
                        Name = name,
                        Address = address
                    };
                    // Делегат(ы) _onDeviceFound / _onDiscoveryFinished — это ссылки на методы, зарегистрированные где‑то ещё.
                    _onDeviceFound?.Invoke(device_info);
                }
            }

            else if (intent?.Action == BluetoothAdapter.ActionDiscoveryFinished)
            {
                _onDiscoveryFinished?.Invoke();
            }



        }




    }
}
