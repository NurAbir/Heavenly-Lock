using System.Windows.Controls;
using HeavenlyLock.ViewModels;

namespace HeavenlyLock.Views;

public partial class GeneratorView : UserControl
{
    public GeneratorView()
    {
        InitializeComponent();
        DataContext = new GeneratorViewModel();
    }
}
