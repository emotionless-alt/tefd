using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ValidationDbDemo.ViewModels;

namespace ValidationDbDemo;

public partial class ProductWindow : Window
{
    public ProductWindow()
    {
        InitializeComponent();

        if (DataContext is ProductWindowViewModel vm)
        {
            vm.CloseAction = Close;
        }
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}