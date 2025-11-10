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
                    ViewProducts();
                    break;
                case "2":
                    RegisterUser();
                    break;
                case "3":
                    LoginUser();
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
                    ViewProducts();
                    break;
                case "2":
                    ViewCart();
                    break;
                case "3":
                    Checkout();
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

        #region Функции
        // 1. Регистрация
        private static void RegisterUser()
        {
            Console.WriteLine("== Регистрация нового пользователя ==");
            Console.Write("Введите имя пользователя (login): ");
            string username = Console.ReadLine();
            Console.Write("Введите Email: ");
            string email = Console.ReadLine();
            Console.Write("Введите пароль: ");
            string pass1 = Console.ReadLine();
            Console.Write("Подтвердите пароль: ");
            string pass2 = Console.ReadLine();

            if (pass1 != pass2)
            {
                Console.WriteLine("Ошибка: Пароли не совпадают.");
                return;
            }

            if (Core.Context.Users.Any(u => u.username == username || u.email == email))
            {
                Console.WriteLine("Ошибка: Пользователь с таким логином или Email уже существует.");
                return;
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(pass1);

            try
            {
                Users newUser = new Users
                {
                    username = username,
                    email = email,
                    password_hash = hashedPassword,
                    first_name = "Новый",
                    last_name = "Пользователь",
                    created_at = DateTime.Now
                };

                Core.Context.Users.Add(newUser);
                Core.Context.SaveChanges();
                Console.WriteLine("Регистрация прошла успешно!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при регистрации: {ex.Message}");
            }
        }
        // 2. Вход
        private static void LoginUser()
        {
            Console.WriteLine("== Вход в аккаунт ==");
            Console.Write("Введите имя пользователя (login): ");
            string username = Console.ReadLine();
            Console.Write("Введите пароль: ");
            string pass = Console.ReadLine();


            var user = Core.Context.Users.FirstOrDefault(u => u.username == username && u.password_hash == pass);

            if (user != null)
            {
                currentUser = user;
                Console.WriteLine($"Добро пожаловать, {currentUser.first_name ?? currentUser.username}!");
            }
            else
            {
                Console.WriteLine("Неверный логин или пароль.");
            }
        }
        // 3. Просмотр товаров
        private static void ViewProducts()
        {
            Console.WriteLine("== Список доступных товаров ==");
            var products = Core.Context.Products.Include(p => p.Categories).ToList();

            foreach (var p in products)
            {
                string categoryName = p.Categories != null ? p.Categories.name : "Без категории";
                Console.WriteLine($"ID: {p.product_id} | {p.name} | Цена: {p.price:C} | Остаток: {p.stock_quantity} | Категория: {categoryName}");
            }

            if (currentUser != null)
            {
                Console.WriteLine("------------------------------------------");
                Console.Write("Введите ID товара, чтобы добавить в корзину (или 0 для выхода): ");
                if (int.TryParse(Console.ReadLine(), out int productId) && productId != 0)
                {
                    AddProductToCart(productId);
                }
            }
        }
        // 4. Добавление в корзину
        private static void AddProductToCart(int productId)
        {
            var product = Core.Context.Products.Find(productId);
            if (product == null)
            {
                Console.WriteLine("Товар не найден.");
                return;
            }

            Console.Write($"Введите количество (доступно: {product.stock_quantity}): ");
            if (!int.TryParse(Console.ReadLine(), out int quantity) || quantity <= 0)
            {
                Console.WriteLine("Неверное количество.");
                return;
            }

            if (quantity > product.stock_quantity)
            {
                Console.WriteLine("Недостаточно товара на складе.");
                return;
            }

            try
            {
                var cartItem = Core.Context.CartItems.FirstOrDefault(ci =>
                    ci.user_id == currentUser.user_id && ci.product_id == productId);

                if (cartItem != null)
                {
                    cartItem.quantity += quantity;
                }
                else
                {
                    cartItem = new CartItems
                    {
                        user_id = currentUser.user_id,
                        product_id = productId,
                        quantity = quantity,
                        added_at = DateTime.Now
                    };
                    Core.Context.CartItems.Add(cartItem);
                }

                Core.Context.SaveChanges();
                Console.WriteLine($"Товар '{product.name}' (x{quantity}) добавлен в корзину.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления в корзину: {ex.Message}");
            }
        }

        // 5. Просмотр корзины
        private static void ViewCart()
        {
            Console.WriteLine("== Моя корзина ==");
            var cartItems = Core.Context.CartItems
                .Include(ci => ci.Products)
                .Where(ci => ci.user_id == currentUser.user_id)
                .ToList();

            if (!cartItems.Any())
            {
                Console.WriteLine("Ваша корзина пуста.");
                return;
            }

            decimal total = 0;
            foreach (var item in cartItems)
            {
                decimal subtotal = item.quantity * item.Products.price;
                Console.WriteLine($"- Товар: {item.Products.name} | Кол-во: {item.quantity} | Цена: {item.Products.price:C} | Сумма: {subtotal:C}");
                total += subtotal;
            }

            Console.WriteLine($"--------------------\nИтого: {total:C}");
        }
        // 6. Оформление заказа 
        private static void Checkout()
        {
            Console.WriteLine("== Оформление заказа ==");
            var cartItems = Core.Context.CartItems
                .Include(ci => ci.Products)
                .Where(ci => ci.user_id == currentUser.user_id)
                .ToList();

            if (!cartItems.Any())
            {
                Console.WriteLine("Нечего оформлять. Корзина пуста.");
                return;
            }

            foreach (var item in cartItems)
            {
                if (item.quantity > item.Products.stock_quantity)
                {
                    Console.WriteLine($"Ошибка: Недостаточно товара '{item.Products.name}'. В наличии: {item.Products.stock_quantity}.");
                    return;
                }
            }

            var points = Core.Context.PickupPoints.ToList();
            Console.WriteLine("Выберите пункт выдачи:");
            foreach (var p in points)
            {
                Console.WriteLine($"ID: {p.point_id} | {p.name} ({p.address}, {p.city})");
            }

            Console.Write("Введите ID пункта выдачи: ");
            if (!int.TryParse(Console.ReadLine(), out int pointId) || !Core.Context.PickupPoints.Any(p => p.point_id == pointId))
            {
                Console.WriteLine("Неверный ID пункта выдачи.");
                return;
            }

            // 6.3. Создание заказа
            using (var transaction = Core.Context.Database.BeginTransaction())
            {
                try
                {
                    decimal totalAmount = cartItems.Sum(item => item.quantity * item.Products.price);

                    Orders newOrder = new Orders
                    {
                        user_id = currentUser.user_id,
                        point_id = pointId,
                        status = "Pending",
                        total_amount = totalAmount,
                        created_at = DateTime.Now
                    };
                    Core.Context.Orders.Add(newOrder);
                    Core.Context.SaveChanges();

                    foreach (var cartItem in cartItems)
                    {
                        OrderItems orderItem = new OrderItems
                        {
                            order_id = newOrder.order_id,
                            product_id = cartItem.product_id,
                            quantity = cartItem.quantity,
                            price_at_purchase = cartItem.Products.price
                        };
                        Core.Context.OrderItems.Add(orderItem);

                        var productInDb = Core.Context.Products.Find(cartItem.product_id);
                        productInDb.stock_quantity -= cartItem.quantity;
                    }

                    Core.Context.CartItems.RemoveRange(cartItems);

                    Core.Context.SaveChanges();

                    transaction.Commit();

                    Console.WriteLine($"Успех! Ваш заказ (ID: {newOrder.order_id}) на сумму {totalAmount:C} оформлен.");
                    Console.WriteLine($"Пункт выдачи: {points.First(p => p.point_id == pointId).name}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine($"Ошибка при оформлении заказа: {ex.Message}");
                }
            }
        }


        #endregion

        private static void Pause()
        {
            Console.WriteLine("\nНажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
    }
}