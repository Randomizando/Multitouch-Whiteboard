using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1
{
    public partial class DesktopMode : Window
    {
        private bool isDrawing;
        private Polyline currentLine;
        private Brush penColor;
        private double penSize;

        public DesktopMode()
        {
            InitializeComponent();
            penColor = Brushes.Black;
            penSize = 2;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isDrawing = true;
                currentLine = new Polyline
                {
                    Stroke = penColor,
                    StrokeThickness = penSize
                };
                currentLine.Points.Add(e.GetPosition(DrawingCanvas));
                DrawingCanvas.Children.Add(currentLine);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing && currentLine != null)
            {
                currentLine.Points.Add(e.GetPosition(DrawingCanvas));
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;
                currentLine = null;
            }
        }

        public void UpdatePenColor(Brush color)
        {
            penColor = color;
        }

        public void UpdatePenSize(double size)
        {
            penSize = size;
        }

        public void ClearCanvas()
        {
            DrawingCanvas.Children.Clear();
        }

        public void ToggleVisibility()
        {
            if (this.IsVisible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }
    }
}
