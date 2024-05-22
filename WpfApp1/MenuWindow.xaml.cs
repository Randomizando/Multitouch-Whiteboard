using System.Windows;

namespace WpfApp1
{
    public partial class MenuWindow : Window
    {
        private WhiteboardMode whiteboardMode;

        public MenuWindow(WhiteboardMode whiteboard)
        {
            InitializeComponent();
            whiteboardMode = whiteboard;
        }

        private void PenMode_Click(object sender, RoutedEventArgs e)
        {
            whiteboardMode.StopDraggingElement();
            whiteboardMode.PenMode_Click(sender, e);
        }

        private void ChangePenColor_Click(object sender, RoutedEventArgs e)
        {
            whiteboardMode.StopDraggingElement();
            whiteboardMode.ChangePenColor_Click(sender, e);
        }

        private void EraserMode_Click(object sender, RoutedEventArgs e)
        {
            whiteboardMode.StopDraggingElement();
            whiteboardMode.EraserMode_Click(sender, e);
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            whiteboardMode.StopDraggingElement();
            whiteboardMode.InsertImage_Click(sender, e);
        }

        private void MoveImageMode_Click(object sender, RoutedEventArgs e)
        {
            whiteboardMode.StopDraggingElement();
            whiteboardMode.MoveImageMode_Click(sender, e);
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            whiteboardMode.StopDraggingElement();
            whiteboardMode.ClearAll_Click(sender, e);
        }

        private void PenSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (whiteboardMode != null)
            {
                whiteboardMode.UpdatePenSize(e.NewValue);
            }
        }

        private void EraserSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (whiteboardMode != null)
            {
                whiteboardMode.UpdateEraserSize(e.NewValue);
            }
        }
    }
}
