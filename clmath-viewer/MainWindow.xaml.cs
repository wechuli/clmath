using System;
using System.Windows;
using System.Windows.Input;
using SharpGL.WPF;

namespace clmath.viewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void Initialize(object sender, OpenGLRoutedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void Draw(object sender, OpenGLRoutedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private void MouseHandler(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ClickHandler(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}