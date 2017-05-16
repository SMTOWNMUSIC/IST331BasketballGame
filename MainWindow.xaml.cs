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
using System.Media;

namespace IST331BasketballGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SoundPlayer sound = new SoundPlayer(@"C:\Users\Anju\Desktop\Game\Game\Music\song.wav");

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void StartButton(object sender, RoutedEventArgs e)
        {
            this.Hide();
            Window1 w1 = new Window1();
            w1.ShowDialog();
            this.Show();
        }

        private void volume_Click(object sender, RoutedEventArgs e)
        {
            sound.Play();
        }

        private void MuteClick(object sender, RoutedEventArgs e)
        {
            sound.Stop();
        }
    }
}
