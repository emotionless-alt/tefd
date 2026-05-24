using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ValidationDbDemo.Commands;
using ValidationDbDemo.Data;
using ValidationDbDemo.Models;

namespace ValidationDbDemo.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        public ICommand LoadProductsCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand UpdateProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand ClearInputCommand { get; }
        public ICommand SearchTextCommand { get; }

        public MainViewModel()
        {
            LoadProductsCommand = new AsyncRelayCommand(LoadProductsAsync);
            AddProductCommand = new AsyncRelayCommand(AddProductAsync);
            UpdateProductCommand = new AsyncRelayCommand(UpdateProductAsync);
            DeleteProductCommand = new AsyncRelayCommand(DeleteProductAsync);
            ClearInputCommand = new AsyncRelayCommand(ClearInputAsync);
            

            _ = InitializeAsync();
        }

        private string _name = "";
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                ValidateName();
            }
        }

        private Category? _selectedCategory;
        public Category? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                ValidateCategory();
            }
        }

        private string _priceText = "";
        public string PriceText
        {
            get => _priceText;
            set
            {
                _priceText = value;
                OnPropertyChanged();
                ValidatePrice();
            }
        }

        private Product? _selectedProduct;
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();

                if (value != null)
                {
                    ClearErrors(nameof(Name));
                    Name = value.Name;
                    ClearErrors(nameof(PriceText));
                    PriceText = value.Price.ToString();

                    ClearErrors(nameof(Category));
                    foreach (var category in Categories)
                    {
                        if (category.Id == value.CategoryId)
                        {
                            SelectedCategory = category;
                            break;
                        }
                    }
                    Info = "Товар выбран";
                }
            }
        }

        private string _info = "";
        public string Info
        {
            get => _info;
            set
            {
                _info = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName == null)
            {
                return Array.Empty<string>();
            }

            if (_errors.TryGetValue(propertyName, out var errors))
            {
                return errors;
            }

            return Array.Empty<string>();
        }

        private async Task InitializeAsync()
        {
            await CreateDatabaseAsync();
            await LoadDataAsync();
        }

        private async Task CreateDatabaseAsync()
        {
            using var db = new AppDbContext();
            await db.Database.EnsureCreatedAsync();
            bool hasCategories = await db.Categories.AnyAsync();
            if (!hasCategories)
            {
                var computer = new Category { Name = "Компьютеры" };
                var peripherals = new Category { Name = "Периферия" };
                var audio = new Category { Name = "Аудио" };
                db.Categories.AddRange(computer, peripherals, audio);

                db.Products.AddRange(
                    new Product { Name = "Ноутбук", Category = computer, Price = 85000 },
                    new Product { Name = "Полная сборка ПК", Category = computer, Price = 120000 },
                    new Product { Name = "Мышь", Category = peripherals, Price = 3000 },
                    new Product { Name = "Клавиатура", Category = peripherals, Price = 5000 },
                    new Product { Name = "Наушники", Category = audio, Price = 3000 }
                );

                await db.SaveChangesAsync();
            }
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;
            Info = "Загрузка данных...";

            await Task.Delay(500);

            await LoadCategoriesAsync();
            await LoadProductsAsync();

            Info = $"Категорий: {Categories.Count}, товаров: {Products.Count}";
            IsLoading = false;
        }

        private async Task LoadCategoriesAsync()
        {
            Categories.Clear();

            using var db = new AppDbContext();
            var categories = await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }

        private async Task LoadProductsAsync()
        {
            Products.Clear();

            using var db = new AppDbContext();
            var products = await db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderBy(p => p.Id)
                .ToListAsync();

            foreach (var product in products)
            {
                Products.Add(product);
            }
        }

        private async Task AddProductAsync()
        {
            ValidateAll();
            
            if (HasErrors)
            {
                Info = "Исправьте ошибки перед добавлением";
                return;
            }
            double price = double.Parse(PriceText);

            IsLoading = true;
            Info = "Добавление...";

            using var db = new AppDbContext();
            var product = new Product
            {
                Name = Name,
                CategoryId = SelectedCategory.Id,
                Price = price
            };
            db.Products.Add( product );
            await db.SaveChangesAsync();

            await ClearInputAsync();
            await LoadProductsAsync();

            Info = "Товар добавлен";
            IsLoading = false;
        }

        private async Task UpdateProductAsync()
        {
            if (SelectedProduct == null)
            {
                Info = "Выберите товар для изменения";
                return;
            }

            ValidateAll();

            if (HasErrors)
            {
                Info = "Исправьте ошибки перед добавлением";
                return;
            }
            double price = double.Parse(PriceText);

            IsLoading = true;
            Info = "Обновление...";

            using var db = new AppDbContext();
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == SelectedProduct.Id);
            if (product == null)
            {
                Info = "Товар не найден";
                IsLoading = false;
                return;
            }
            product.Name = Name;
            product.CategoryId = SelectedCategory.Id;
            product.Price = price;
            await db.SaveChangesAsync();

            await ClearInputAsync();
            await LoadProductsAsync();

            Info = "Товар обновлён";
            IsLoading = false;
        }

        private async Task DeleteProductAsync()
        {
            if (SelectedProduct == null)
            {
                Info = "Выберите товар для удаления";
                return;
            }

            IsLoading = true;
            Info = "Удаление...";

            using var db = new AppDbContext();
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == SelectedProduct.Id);
            if (product == null)
            {
                Info = "Товар не найден";
                IsLoading = false;
                return;
            }
            db.Products.Remove(product);
            await db.SaveChangesAsync();

            await ClearInputAsync();
            await LoadProductsAsync();

            Info = "Товар удалён";
            IsLoading = false;
        }

        private Task ClearInputAsync()
        {
            Name = "";
            PriceText = "";
            SelectedProduct = null;
            SelectedCategory = null;

            ClearErrors(nameof(Name));
            ClearErrors(nameof(Category));
            ClearErrors(nameof(PriceText));

            Info = "Поля отчищены";

            return Task.CompletedTask;
        }

        private void ValidateAll()
        {
            ValidateName();
            ValidateCategory();
            ValidatePrice();
        }

        private void ValidateName()
        {
            ClearErrors(nameof(Name));

            if (string.IsNullOrEmpty(Name))
            {
                AddError(nameof(Name), "Название товара обязательно");
            }
        }
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                
            }
        }


        private void ValidateCategory()
        {
            ClearErrors(nameof(Category));

            if (SelectedCategory == null)
            {
                AddError(nameof(Category), "Выберите категорию");
            }
        }

        private void ValidatePrice()
        {
            ClearErrors(nameof(PriceText));

            if (string.IsNullOrWhiteSpace(PriceText))
            {
                AddError(nameof(PriceText), "Цена обязательна");
                return;
            }
            if (!double.TryParse(PriceText, out double price))
            {
                AddError(nameof(PriceText), "Цена должна быть числом");
                return;
            }

            if (price <= 0)
            {
                AddError(nameof(PriceText), "Цена должна быть больше 0");
            }
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
            {
                _errors[propertyName] = new List<string>();
            }

            _errors[propertyName].Add(error);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(propertyName));
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
