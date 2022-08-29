using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaTest.ViewModels;

namespace AvaloniaTest.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
