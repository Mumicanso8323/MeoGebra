using System.Windows;
using MeoGebra.ViewModels;

namespace MeoGebra {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
