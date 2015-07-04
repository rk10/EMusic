using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EMusic.Models;
using EMusicServices;

namespace EMusic.Views
{
    /// <summary>
    /// Interaction logic for PlayerView.xaml
    /// </summary>
    public partial class PlayerView : UserControl
    {
        public PlayerView()
        {
            InitializeComponent();

            PlayVM = new PlayVM();
            this.DataContext = PlayVM;

            Loaded += (s, e) => PlayVM.Raise();
        }

        public PlayVM PlayVM { get; set; }

    }
}
