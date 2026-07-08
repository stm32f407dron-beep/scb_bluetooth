using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using skb_home.Extensions;
using System;
using System.Collections.Generic;


//- рабочий вариант 

//namespace skb_home.Controls
//{
//    public class PopupMenu : ContentView
//    {
//        public Action<string>? OptionSelected; // Коллбек для выбора пункта меню

//        public PopupMenu(List<string> options)
//        {
//            // Фоновый слой для Popup
//            var container = new Grid
//            {
//                BackgroundColor = Colors.Black.WithAlpha(0.5f), // Полупрозрачный черный фон
//                VerticalOptions = LayoutOptions.Fill,
//                HorizontalOptions = LayoutOptions.Fill
//            };

//            // Контейнер для пунктов меню
//            var menuStack = new StackLayout
//            {
//                BackgroundColor = Colors.SlateBlue,
//                Padding = 10,
//                HorizontalOptions = LayoutOptions.Center,
//                VerticalOptions = LayoutOptions.Center,
//                WidthRequest = 250
//            };

//            foreach (var option in options)
//            {
//                var button = new Button
//                {
//                    Text = option,
//                    BackgroundColor = Colors.Transparent,
//                    TextColor = Colors.White,
//                    HorizontalOptions = LayoutOptions.Fill
//                };
//                button.Clicked += (s, e) =>
//                {
//                    OptionSelected?.Invoke(option); // Передаем выбранный элемент в callback
//                    ClosePopup();
//                };
//                menuStack.Children.Add(button);
//            }

//            // Добавляем меню в контейнер
//            container.Children.Add(menuStack);

//            Content = container;

//            // Клик на фон для закрытия меню
//            var tapGestureRecognizer = new TapGestureRecognizer();
//            tapGestureRecognizer.Tapped += (s, e) => ClosePopup();
//            container.GestureRecognizers.Add(tapGestureRecognizer);
//        }





//        private void ClosePopup()
//        {
//            var parentGrid = this.Parent as Grid;

//            if (parentGrid != null)
//            {
//                parentGrid.Children.Remove(this); // Удаляем всплывающее меню
//            }
//        }
//    }
//}




//namespace skb_home.Controls;

//public class PopupMenu : ContentView
//{
//    public Action<string>? OptionSelected;

//    public PopupMenu(List<string> options)
//    {
//        // Абсолютный слой для меню
//        var absoluteLayout = new AbsoluteLayout
//        {
//            VerticalOptions = LayoutOptions.FillAndExpand,
//            HorizontalOptions = LayoutOptions.FillAndExpand
//        };

//        // Полупрозрачный фон, который закрывает меню при клике
//        var backdrop = new BoxView
//        {
//            Color = Colors.Black.WithAlpha(0.5f),
//        };

//        AbsoluteLayout.SetLayoutBounds(backdrop, new Rect(0, 0, 1, 1)); // Накрываем весь экран
//        AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
//        absoluteLayout.Children.Add(backdrop);

//        // Контейнер для меню
//        var menuStack = new VerticalStackLayout
//        {
//            BackgroundColor = Colors.MediumPurple,
//            Padding = 20,
//            HorizontalOptions = LayoutOptions.Center,
//            WidthRequest = 250
//        };

//        foreach (var option in options)
//        {
//            var button = new Button
//            {
//                Text = option,
//                BackgroundColor = Colors.Transparent,
//                TextColor = Colors.White,
//                HorizontalOptions = LayoutOptions.Fill
//            };
//            button.Clicked += (s, e) =>
//            {
//                OptionSelected?.Invoke(option);
//                CloseMenu(); // Закрытие меню после выбора
//            };
//            menuStack.Children.Add(button);
//        }

//        // Позиционирование меню
//        AbsoluteLayout.SetLayoutBounds(menuStack, new Rect(0.5, 0.2, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize)); // Центр сверху
//        AbsoluteLayout.SetLayoutFlags(menuStack, AbsoluteLayoutFlags.PositionProportional);
//        absoluteLayout.Children.Add(menuStack);

//        Content = absoluteLayout;

//        // Клик на фон закрывает меню
//        backdrop.GestureRecognizers.Add(new TapGestureRecognizer
//        {
//            Command = new Command(CloseMenu)
//        });
//    }

//    private void CloseMenu()
//    {
//        var parent = this.Parent as ContentPage;
//        parent?.RemoveChild(this); // Удаление меню
//    }
//}



//namespace skb_home.Controls;

//public class PopupMenu : ContentView
//{
//    public Action<string>? OptionSelected;

//    public PopupMenu(List<string> options)
//    {
//        // Абсолютный слой для меню и фона
//        var absoluteLayout = new AbsoluteLayout
//        {
//            VerticalOptions = LayoutOptions.Fill,
//            HorizontalOptions = LayoutOptions.Fill
//        };

//        // Полупрозрачный фон, который закрывает меню при клике
//        var backdrop = new BoxView
//        {
//            Color = Colors.Black.WithAlpha(0.5f),
//        };

//        AbsoluteLayout.SetLayoutBounds(backdrop, new Rect(0, 0, 1, 1)); // Накрываем весь экран
//        AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
//        absoluteLayout.Children.Add(backdrop);

//        // Контейнер для меню
//        //var menuStack = new VerticalStackLayout
//        //{
//        //    // Настройка прозрачности меню
//        //    BackgroundColor = Colors.MediumPurple.WithAlpha(0.2f), // 50% прозрачности
//        //    Padding = 20,
//        //    HorizontalOptions = LayoutOptions.Center,
//        //    WidthRequest = 250
//        //};
//        ///
//        var menuStack = new VerticalStackLayout
//        {
//            BackgroundColor = new Color(0.1f, 0.3f, 0.6f, 0.5f),
//            Padding = 10,
//            HorizontalOptions = LayoutOptions.Center,
//            WidthRequest = 250
//        };


//        ///

//        foreach (var option in options)
//        {
//            var button = new Button
//            {
//                Text = option,
//                BackgroundColor = Colors.Transparent,
//                TextColor = Colors.White,
//                HorizontalOptions = LayoutOptions.Fill
//            };
//            button.Clicked += (s, e) =>
//            {
//                OptionSelected?.Invoke(option);
//                CloseMenu(); // Закрытие меню после выбора
//            };
//            menuStack.Children.Add(button);
//        }

//        // Позиционирование меню
//        AbsoluteLayout.SetLayoutBounds(menuStack, new Rect(0.5, 0.1, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize)); // Центр сверху
//        AbsoluteLayout.SetLayoutFlags(menuStack, AbsoluteLayoutFlags.PositionProportional);
//        absoluteLayout.Children.Add(menuStack);

//        Content = absoluteLayout;

//        // Клик на фон закрывает меню
//        backdrop.GestureRecognizers.Add(new TapGestureRecognizer
//        {
//            Command = new Command(CloseMenu)
//        });
//    }

//    private void CloseMenu()
//    {
//        var parent = this.Parent as ContentPage;
//        parent?.RemoveChild(this); // Удаление меню
//    }
//}

namespace skb_home.Controls;

//public class PopupMenu : ContentView
//{
//    public Action<string>? OptionSelected;

//    public PopupMenu(List<string> options)
//    {
//        // Абсолютный слой для меню и фона
//        var absoluteLayout = new AbsoluteLayout
//        {
//            VerticalOptions = LayoutOptions.Fill,
//            HorizontalOptions = LayoutOptions.Fill
//        };

//        // Полупрозрачный фон, который закрывает меню при клике  Color = Colors.Black.WithAlpha(0.18f),  Color = Colors.Black.WithAlpha(0.5f),
//        var backdrop = new BoxView
//        {

//            //Color = Colors.Transparent,
//            Color = Colors.Black.WithAlpha(0.85f),
//        };

//        AbsoluteLayout.SetLayoutBounds(backdrop, new Rect(0, 0, 1, 1)); // Накрываем весь экран
//        AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
//        absoluteLayout.Children.Add(backdrop);

//        // Контейнер для меню
//        // Настройка прозрачности меню и размеров BackgroundColor = new Color(0.55f, 0.80f, 1.00f, 0.08f),   BackgroundColor = new Color(0.1f, 0.3f, 0.6f, 0.5f)
//        var menuStack = new VerticalStackLayout
//        {
//            BackgroundColor = new Color(0.1f, 0.3f, 0.6f, 0.5f), // Темно-голубой цвет
//            Padding = new Thickness(10, 5, 10, 5), // Уменьшенные внутренние отступы (изменено)
//            HorizontalOptions = LayoutOptions.Center,
//            WidthRequest = 200, // Уменьшенная ширина меню (изменено)
//            HeightRequest = 150 // Уменьшенная высота меню (добавлено)
//        };

//        foreach (var option in options)
//        {
//            var button = new Button
//            {
//                Text = option,
//                BackgroundColor = Colors.Transparent,
//                TextColor = Colors.White,
//                HorizontalOptions = LayoutOptions.Fill,
//                HeightRequest = 20, // Уменьшенная высота кнопок (изменено)
//                Margin = new Thickness(0, 3, 0, 3) // Маленькие отступы между кнопками (изменено)
//            };
//            button.Clicked += (s, e) =>
//            {
//                OptionSelected?.Invoke(option);
//                CloseMenu(); // Закрытие меню после выбора
//            };
//            menuStack.Children.Add(button);
//        }

//        // Позиционирование меню
//        AbsoluteLayout.SetLayoutBounds(menuStack, new Rect(0.5, 0.1, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize)); // Центр сверху 0.5 0.1
//        AbsoluteLayout.SetLayoutFlags(menuStack, AbsoluteLayoutFlags.PositionProportional);
//        absoluteLayout.Children.Add(menuStack);


//        //menuStack.TranslationX = 2; // ~2 мм вправо
//        //menuStack.TranslationY = 2; // ~2 мм вниз


//        Content = absoluteLayout;

//        // Клик на фон закрывает меню
//        backdrop.GestureRecognizers.Add(new TapGestureRecognizer
//        {
//            Command = new Command(CloseMenu)
//        });
//    }

//    private void CloseMenu()
//    {
//        var parent = this.Parent as ContentPage;
//        parent?.RemoveChild(this); // Удаление меню
//    }
//}

////
//public class PopupMenu : ContentView
//{
//    public Action<string>? OptionSelected;

//    public PopupMenu(List<string> options)
//    {
//        // чтобы у самого PopupMenu не было своего фона
//        BackgroundColor = Colors.Transparent;

//        var absoluteLayout = new AbsoluteLayout
//        {
//            BackgroundColor = Colors.Transparent,
//            VerticalOptions = LayoutOptions.Fill,
//            HorizontalOptions = LayoutOptions.Fill
//        };

//        // Затемнение экрана (можешь уменьшить alpha если слишком темно)
//        var backdrop = new BoxView
//        {
//            Color = Colors.Black.WithAlpha(0.85f)
//        };
//        AbsoluteLayout.SetLayoutBounds(backdrop, new Rect(0, 0, 1, 1));
//        AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
//        absoluteLayout.Children.Add(backdrop);

//        // Панель меню (фон меню)
//        var menuStack = new VerticalStackLayout
//        {
//            BackgroundColor = new Color(0.05f, 0.10f, 0.20f, 0.85f), // тёмно-синий, без серой подложки от Button
//            Padding = new Thickness(10, 8),
//            Spacing = 6,
//            WidthRequest = 200
//        };

//        foreach (var option in options)
//        {
//            // ВАЖНО: не Button, а Grid+Label с TapGestureRecognizer (убирает серую подложку Android у кнопок)
//            var row = new Grid
//            {
//                BackgroundColor = Colors.Transparent,
//                Padding = new Thickness(8, 6),
//                HorizontalOptions = LayoutOptions.Fill
//            };

//            var text = new Label
//            {
//                Text = option,
//                TextColor = Colors.White,
//                FontSize = 16,
//                VerticalOptions = LayoutOptions.Center,
//                HorizontalOptions = LayoutOptions.Start
//            };

//            row.Children.Add(text);

//            var tap = new TapGestureRecognizer();
//            tap.Tapped += (s, e) =>
//            {
//                OptionSelected?.Invoke(option);
//                CloseMenu();
//            };
//            row.GestureRecognizers.Add(tap);

//            menuStack.Children.Add(row);
//        }

//        // Позиция меню + смещение (как тебе понравилось)
//        AbsoluteLayout.SetLayoutBounds(menuStack, new Rect(0.5, 0.1, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
//        AbsoluteLayout.SetLayoutFlags(menuStack, AbsoluteLayoutFlags.PositionProportional);
//        //menuStack.TranslationX = 27;
//        //menuStack.TranslationY = 27;

//        absoluteLayout.Children.Add(menuStack);

//        Content = absoluteLayout;

//        // Тап по фону закрывает мен��
//        backdrop.GestureRecognizers.Add(new TapGestureRecognizer
//        {
//            Command = new Command(CloseMenu)
//        });
//    }

//    private void CloseMenu()
//    {
//        // корректнее удалять из Layout (а не только из ContentPage)
//        if (Parent is Layout layout)
//            layout.Children.Remove(this);
//        else if (Parent is ContentPage page)
//            page.RemoveChild(this);
//    }
//}


public class PopupMenu : ContentView
{
    public Action<string>? OptionSelected;

    public PopupMenu(List<string> options)
    {
        BackgroundColor = Colors.Transparent;

        var absoluteLayout = new AbsoluteLayout
        {
            BackgroundColor = Colors.Transparent,
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill
        };

        // Фон-ловушка (можно оставить лёгкое затемнение или сделать полностью прозрачным)
        var backdrop = new BoxView
        {
            BackgroundColor = Colors.Transparent // или Colors.Black.WithAlpha(0.08f)
        };
        AbsoluteLayout.SetLayoutBounds(backdrop, new Rect(0, 0, 1, 1));
        AbsoluteLayout.SetLayoutFlags(backdrop, AbsoluteLayoutFlags.All);
        absoluteLayout.Children.Add(backdrop);

        // Прозрачная панель меню (ВАЖНО: если сделать 100% transparent,
        // то на Android может просвечивать системный серый фон окна)
        var menuStack = new VerticalStackLayout
        {
            BackgroundColor = new Color(0, 0, 0, 0.02f), // почти прозрачное "стекло"
            Padding = new Thickness(10, 8),
            Spacing = 6,
            WidthRequest = 200
        };

        foreach (var option in options)
        {
            var row = new Grid
            {
                BackgroundColor = Colors.Transparent,
                Padding = new Thickness(8, 6),
                HorizontalOptions = LayoutOptions.Fill
            };

            var text = new Label
            {
                Text = option,
                TextColor = Colors.White,
                FontSize = 17,
                FontAttributes = FontAttributes.Bold, // жирный
                VerticalOptions = LayoutOptions.Center
            };

            row.Children.Add(text);

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                OptionSelected?.Invoke(option);
                CloseMenu();
            };
            row.GestureRecognizers.Add(tap);

            menuStack.Children.Add(row);
        }

        AbsoluteLayout.SetLayoutBounds(menuStack, new Rect(0.5, 0.1, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        AbsoluteLayout.SetLayoutFlags(menuStack, AbsoluteLayoutFlags.PositionProportional);
        menuStack.TranslationX = 0;
        menuStack.TranslationY = 8;

        absoluteLayout.Children.Add(menuStack);

        Content = absoluteLayout;

        backdrop.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(CloseMenu)
        });
    }

    private void CloseMenu()
    {
        if (Parent is Layout layout)
            layout.Children.Remove(this);
        else if (Parent is ContentPage page)
            page.RemoveChild(this);
    }
}



