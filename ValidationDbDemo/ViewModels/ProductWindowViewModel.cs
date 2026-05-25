using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ValidationDbDemo.Commands;
using ValidationDbDemo.Models;

namespace ValidationDbDemo.ViewModels
{
    public class ProductWindowViewModel
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public ObservableCollection<Category> Categories { get; } = new();

        public ICommand SaveCommand { get; }
        public Action? CloseAction { get; set; }

        public ProductWindowViewModel(
            ObservableCollection<Category> categories,
            Product? product = null
        )
        {
            Categories = categories;

            SaveCommand = new AsyncRelayCommand(SaveAsync);

            if (product != null)
            {
                ProductId = product.Id;
                Name = product.Name;
                PriceText = product.Price.ToString();

                foreach (var category in categories)
                {
                    if (category.Id == product.CategoryId)
                    {
                        SelectedCategory = category;
                        break;
                    }
                }
            }
        }
        public int ProductId { get; set; }

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
        public bool IsSaved { get; private set; }
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

        private void ValidateAll()
        {
            ValidateName();
            ValidateCategory();
            ValidatePrice();
        }
        private async Task SaveAsync()
        {
            ValidateAll();
            if (HasErrors)
            {
                return;
            }
            await Task.Delay(200);
            IsSaved = true;
            CloseAction?.Invoke();
        }

        private void ValidateName()
        {
            ClearErrors(nameof(Name));

            if (string.IsNullOrEmpty(Name))
            {
                AddError(nameof(Name), "Название товара обязательно");
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
