using Android.App;
using Android.Runtime;
using Java.Lang.Annotation;
using skb_home;
using System.Reflection.Metadata;




//Кто главный для телефона:
//Для операционной системы Android единой точкой входа является концепция класса android.app.Application. 
//    Android не запускает C#-код напрямую — он запускает свою нативную среду (Android Runtime). 
//    Как они соединяются:В недрах MAUI разработчики Microsoft создали класс MauiApplication.
//    Это специальный мост: с одной стороны он выглядит для Android как родной Java-класс приложения,
//    а с другой — содержит всю логику для запуска платформы .NET MAUI.  
//    Твой класс MainApplication наследуется от MauiApplication (public class MainApplication : MauiApplication).  
//    Что происходит при клике на иконку:Android смотрит в манифест, видит твой класс MainApplication и сам создаёт его экземпляр в памяти.
//    Твой класс через base(handle, ownership) передаёт управление родительскому классу MauiApplication. 
//    Базовый класс MAUI выполняет всю сложную техническую работу по связыванию C# и Android,
//    а затем вызывает метод CreateMauiApp(), чтобы забрать твои настройки из MauiProgram.cs. 
//    Именно поэтому писать new MainApplication() в C# не нужно и бессмысленно — операционная система сама «рождает» этот объект,
//    а твой класс, опираясь на базовый класс ядра MAUI, плавно перенаправляет запуск внутрь кроссплатформенной части.  








namespace skb_home
{
    [Application]
    public class MainApplication : MauiApplication
    {

        //IntPtr handle: IntPtr — это так называемый «сырой указатель» на адрес в оперативной памяти устройства.
        //Когда ядро Android (написанное на Java/C++) выделяет память под объект приложения, оно передаёт C#-среде адрес этого участка памяти.

        // JniHandleOwnership ownership: JNI расшифровывается как Java Native Interface(мост между Java и C#). 
        //     Этот параметр указывает сборщику мусора .NET (Garbage Collector), кто несёт ответственность за удаление этого объекта из памяти: Java-машина или среда .NET.

        //Синтаксис : base(handle, ownership):
        //В C# ключевое слово base(...) означает: «Передай эти входящие аргументы наверх, в конструктор базового класса MauiApplication». 
        //Сам блок { } пуст, потому что всю черновую работу по склейке памяти делает родительский класс.



        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

          //    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
            //protected (модификатор доступа): Означает, что вызывать этот метод извне(например, из случайной страницы или кнопки) нельзя.
            //Доступ к нему имеют только сам этот класс и классы платформы MAUI.override (переопределение): 
            //В базовом классе MauiApplication этот метод был объявлен как abstract (пустая заготовка, у которой нет своего тела).
            //Ключевое слово override говорит: «Я предоставляю конкретную реализацию того, как именно создавать моё MAUI - приложение».MauiApp(тип возвращаемого значения) :
            //Метод обязан вернуть готовый объект типа MauiApp(контейнер со всеми сервисами, страницами и настройками).   return MauiProgram.CreateMauiApp();:
            // Здесь MainApplication обращается к статическому классу MauiProgram и вызывает его метод CreateMauiApp().   
        protected override MauiApp CreateMauiApp()
        {
            return MauiProgram.CreateMauiApp();
        }

    }
}



//В Android есть базовый класс Application.

//В MAUI есть класс‑обёртка MauiApplication, который наследует Application и добавляет интеграцию с MAUI.

//Ты создаёшь свой MainApplication, который наследует MauiApplication.

//👉 Таким образом, твой класс получает все возможности Android Application + расширения MAUI, и ты можешь добавить свою логику (например, вызвать MauiProgram.CreateMauiApp()).






//🔹 Взаимодействие с другими классами
//Android Runtime запускает MainApplication.

//MainApplication.CreateMauiApp() вызывает MauiProgram.CreateMauiApp().

//MauiProgram собирает конфигурацию и возвращает MauiApp.

//App.xaml.cs получает этот MauiApp, задаёт стартовую страницу (MainPage).

//MainActivity управляет Android‑специфическими вещами (разрешения, ориентация).

//🔹 Итог
//MainApplication — точка входа Android.

//Он наследует MauiApplication, чтобы связать Android и MAUI.

//В конструкторе просто передаёт системные параметры.

//В CreateMauiApp() вызывает твой конфигуратор (MauiProgram).

//Без него Android не смог бы запустить MAUI‑часть приложения.

//⚡ По сути: Android стартует → MainApplication → MauiProgram → App.xaml.cs → MainPage.







//Android Runtime
//      │
//      ▼
//MainApplication.cs  [Application]
// └─> вызывает CreateMauiApp()
//      │
//      ▼
//MauiProgram.cs
// └─> регистрирует сервисы(DI), шрифты, логирование
// └─> возвращает объект MauiApp
//      │
//      ▼
//App.xaml.cs  (класс App)
// └─> получает сервисы из DI
// └─> задаёт стартовую страницу (MainPage)
//      │
//      ▼
//MainPage.xaml / MainPage.xaml.cs
// └─> UI и логика главной страницы



//MainActivity.cs[Activity]
// └─> точка входа Android Activity
// └─> управляет жизненным циклом (OnCreate)
// └─> фиксирует ориентацию экрана
// └─> запрашивает разрешения Bluetooth
// └─> обрабатывает результат разрешений

