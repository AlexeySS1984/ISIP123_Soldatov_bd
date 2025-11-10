using System.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISIP123_Soldatov_bd
{
    internal class Program
    {
        private static Users currentUser = null;
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            //SeedDatabaseIfNeeded();

            while (true)
            {
                Console.Clear();
                if (currentUser == null)
                {
                    ShowGuestMenu();
                }
                else
                {
                    ShowUserMenu();
                }
            }
        }

        #region Меню

        private static void ShowGuestMenu()
        {
            Console.WriteLine("== Маркетплейс 'Товары для дома' (Гость) ==");
            Console.WriteLine("1. Просмотр товаров");
            Console.WriteLine("2. Регистрация");
            Console.WriteLine("3. Войти в аккаунт");
            Console.WriteLine("4. Выход");
            Console.Write("Выберите опцию: ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    //ViewProducts();
                    break;
                case "2":
                    //RegisterUser();
                    break;
                case "3":
                    //LoginUser();
                    break;
                case "4":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Неверный ввод.");
                    break;
            }
            Pause();
        }

        private static void ShowUserMenu()
        {
            Console.WriteLine($"== Маркетплейс (Вы вошли как: {currentUser.username}) ==");
            Console.WriteLine("1. Просмотр товаров (и добавление в корзину)");
            Console.WriteLine("2. Моя корзина");
            Console.WriteLine("3. Оформить заказ (купить все из корзины)");
            Console.WriteLine("4. Мои заказы");
            Console.WriteLine("5. Выйти из аккаунта");
            Console.Write("Выберите опцию: ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    //ViewProducts();
                    break;
                case "2":
                    //ViewCart();
                    break;
                case "3":
                    //Checkout();
                    break;
                case "4":
                    //ViewMyOrders();
                    break;
                case "5":
                    currentUser = null;
                    Console.WriteLine("Вы вышли из аккаунта.");
                    break;
                default:
                    Console.WriteLine("Неверный ввод.");
                    break;
            }
            Pause();
        }

        #endregion
        private static void Pause()
        {
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
    }
}