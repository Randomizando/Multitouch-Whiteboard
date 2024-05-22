using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Microsoft.Win32;

namespace WpfApp1
{
    public partial class WhiteboardMode : Window
    {
        private enum ToolMode
        {
            Pen,
            Eraser,
            Image,
            MoveImage
        }

        private ToolMode currentToolMode;
        private Dictionary<int, Polyline> activeTouches;
        private Brush currentPenColor;
        private double currentPenSize;
        private double currentEraserSize;
        private Point lastPoint;
        private UIElement selectedElement;
        private bool isDragging;
        private Point clickPosition;
        private List<Thumb> resizeThumbs;

        public WhiteboardMode()
        {
            InitializeComponent();
            activeTouches = new Dictionary<int, Polyline>();
            currentToolMode = ToolMode.Pen;
            currentPenColor = Brushes.Black;
            currentPenSize = 2;
            currentEraserSize = 20;
            resizeThumbs = new List<Thumb>();
            UpdateEraserCursor();

            MenuWindow menuWindow = new MenuWindow(this);
            menuWindow.Show();
        }

        public void UpdatePenSize(double newSize)
        {
            currentPenSize = newSize;
        }

        public void UpdateEraserSize(double newSize)
        {
            currentEraserSize = newSize;
            UpdateEraserCursor();
        }

        private void UpdateEraserCursor()
        {
            if (EraserCursor != null)
            {
                EraserCursor.Width = currentEraserSize;
                EraserCursor.Height = currentEraserSize;
            }
        }

        private void Window_TouchDown(object sender, TouchEventArgs e)
        {
            var touchId = e.TouchDevice.Id;
            if (currentToolMode == ToolMode.Pen)
            {
                var newPolyline = new Polyline
                {
                    Stroke = currentPenColor,
                    StrokeThickness = currentPenSize
                };
                var touchPoint = e.GetTouchPoint(DrawingCanvas);
                newPolyline.Points.Add(touchPoint.Position);
                activeTouches[touchId] = newPolyline;
                DrawingCanvas.Children.Add(newPolyline);
                lastPoint = touchPoint.Position;
            }
            else if (currentToolMode == ToolMode.Eraser)
            {
                EraseAtPoint(e.GetTouchPoint(DrawingCanvas).Position);
                MoveEraserCursor(e.GetTouchPoint(DrawingCanvas).Position);
            }
            else if (currentToolMode == ToolMode.MoveImage)
            {
                var touchPoint = e.GetTouchPoint(DrawingCanvas).Position;
                StartDraggingElementAt(touchPoint);
            }
        }

        private void Window_TouchMove(object sender, TouchEventArgs e)
        {
            var touchId = e.TouchDevice.Id;
            if (activeTouches.ContainsKey(touchId) && currentToolMode == ToolMode.Pen)
            {
                var touchPoint = e.GetTouchPoint(DrawingCanvas);
                activeTouches[touchId].Points.Add(touchPoint.Position);
                lastPoint = touchPoint.Position;
            }
            else if (currentToolMode == ToolMode.Eraser)
            {
                EraseAtPoint(e.GetTouchPoint(DrawingCanvas).Position);
                MoveEraserCursor(e.GetTouchPoint(DrawingCanvas).Position);
            }
            else if (currentToolMode == ToolMode.MoveImage && isDragging)
            {
                var touchPoint = e.GetTouchPoint(DrawingCanvas).Position;
                DragElementTo(touchPoint);
            }
        }

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            var touchId = e.TouchDevice.Id;
            if (activeTouches.ContainsKey(touchId))
            {
                var polyline = activeTouches[touchId];
                var bounds = VisualTreeHelper.GetDescendantBounds(polyline);

                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    var renderTarget = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);
                    var dv = new DrawingVisual();

                    using (var ctx = dv.RenderOpen())
                    {
                        var vb = new VisualBrush(polyline);
                        ctx.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
                    }

                    renderTarget.Render(dv);

                    var image = new Image
                    {
                        Source = renderTarget,
                        Width = bounds.Width,
                        Height = bounds.Height,
                        Stretch = Stretch.None
                    };

                    Canvas.SetLeft(image, bounds.TopLeft.X);
                    Canvas.SetTop(image, bounds.TopLeft.Y);
                    DrawingCanvas.Children.Add(image);
                    DrawingCanvas.Children.Remove(polyline);

                    AddImageManipulationHandles(image);
                }

                activeTouches.Remove(touchId);
            }

            if (currentToolMode == ToolMode.Eraser)
            {
                EraserCursor.Visibility = Visibility.Collapsed;
            }

            if (currentToolMode == ToolMode.MoveImage && isDragging)
            {
                StopDraggingElement();
            }
        }

        private void EraseAtPoint(Point point)
        {
            var hitResults = new List<UIElement>();
            Rect eraserRect = new Rect(point.X - currentEraserSize / 2, point.Y - currentEraserSize / 2, currentEraserSize, currentEraserSize);

            List<UIElement> elementsToRemove = new List<UIElement>();

            foreach (var child in DrawingCanvas.Children)
            {
                if (child is Image image && image.Source is RenderTargetBitmap)
                {
                    var imageBounds = new Rect(Canvas.GetLeft(image), Canvas.GetTop(image), image.Width, image.Height);
                    if (eraserRect.IntersectsWith(imageBounds))
                    {
                        elementsToRemove.Add(image);
                    }
                }
            }

            foreach (var element in elementsToRemove)
            {
                RemoveAssociatedResizeThumbs(element);
                DrawingCanvas.Children.Remove(element);
            }
        }

        private void MoveEraserCursor(Point point)
        {
            if (EraserCursor != null)
            {
                EraserCursor.Width = currentEraserSize;
                EraserCursor.Height = currentEraserSize;
                Canvas.SetLeft(EraserCursor, point.X - currentEraserSize / 2);
                Canvas.SetTop(EraserCursor, point.Y - currentEraserSize / 2);
                EraserCursor.Visibility = Visibility.Visible;
            }
        }

        public void PenMode_Click(object sender, RoutedEventArgs e)
        {
            currentToolMode = ToolMode.Pen;
            HideResizeThumbs();
            if (EraserCursor != null)
            {
                EraserCursor.Visibility = Visibility.Collapsed;
            }
        }

        public void ChangePenColor_Click(object sender, RoutedEventArgs e)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                currentPenColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                    colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B));
            }
        }

        public void EraserMode_Click(object sender, RoutedEventArgs e)
        {
            currentToolMode = ToolMode.Eraser;
            HideResizeThumbs();
            if (EraserCursor != null)
            {
                EraserCursor.Visibility = Visibility.Visible;
            }
        }

        public void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";
            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                Image image = new Image
                {
                    Source = bitmap,
                    Width = 200,
                    Height = 200,
                    Stretch = Stretch.Uniform
                };

                Canvas.SetLeft(image, (DrawingCanvas.ActualWidth - image.Width) / 2);
                Canvas.SetTop(image, (DrawingCanvas.ActualHeight - image.Height) / 2);

                DrawingCanvas.Children.Add(image);

                AddImageManipulationHandles(image);
            }
        }

        public void MoveImageMode_Click(object sender, RoutedEventArgs e)
        {
            currentToolMode = ToolMode.MoveImage;
            ShowResizeThumbs();
            if (EraserCursor != null)
            {
                EraserCursor.Visibility = Visibility.Collapsed;
            }
        }

        private void AddImageManipulationHandles(Image image)
        {
            if (!(image.RenderTransform is TransformGroup))
            {
                var transformGroup = new TransformGroup();
                transformGroup.Children.Add(new ScaleTransform());
                transformGroup.Children.Add(new RotateTransform());
                transformGroup.Children.Add(new TranslateTransform());
                image.RenderTransform = transformGroup;
            }

            var resizeThumb = new Thumb
            {
                Width = 10,
                Height = 10,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.SizeNWSE,
                Visibility = Visibility.Collapsed
            };

            Canvas.SetLeft(resizeThumb, Canvas.GetLeft(image) + image.Width - resizeThumb.Width / 2);
            Canvas.SetTop(resizeThumb, Canvas.GetTop(image) + image.Height - resizeThumb.Height / 2);

            resizeThumb.DragDelta += (sender, e) =>
            {
                var newWidth = Math.Max(20, image.Width + e.HorizontalChange);
                var newHeight = Math.Max(20, image.Height + e.VerticalChange);
                image.Width = newWidth;
                image.Height = newHeight;

                Canvas.SetLeft(resizeThumb, Canvas.GetLeft(image) + image.Width - resizeThumb.Width / 2);
                Canvas.SetTop(resizeThumb, Canvas.GetTop(image) + image.Height - resizeThumb.Height / 2);
            };

            DrawingCanvas.Children.Add(resizeThumb);
            resizeThumbs.Add(resizeThumb);

            image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
            image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
            image.MouseMove += Image_MouseMove;
            image.MouseDown += Image_MouseDown;
        }

        private void RemoveAssociatedResizeThumbs(UIElement element)
        {
            var thumbsToRemove = resizeThumbs.Where(t => Canvas.GetLeft(t) >= Canvas.GetLeft(element) && Canvas.GetLeft(t) <= Canvas.GetLeft(element) + ((FrameworkElement)element).Width && Canvas.GetTop(t) >= Canvas.GetTop(element) && Canvas.GetTop(t) <= Canvas.GetTop(element) + ((FrameworkElement)element).Height).ToList();
            foreach (var thumb in thumbsToRemove)
            {
                DrawingCanvas.Children.Remove(thumb);
                resizeThumbs.Remove(thumb);
            }
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentToolMode == ToolMode.MoveImage)
            {
                selectedElement = sender as UIElement;
                isDragging = true;
                clickPosition = e.GetPosition(DrawingCanvas);
                DrawingCanvas.CaptureMouse();
                HideResizeThumbs();
                ShowResizeThumbsForSelected();
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (currentToolMode == ToolMode.MoveImage)
            {
                isDragging = false;
                DrawingCanvas.ReleaseMouseCapture();
                ShowResizeThumbsForSelected();
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedElement != null)
            {
                Point currentPosition = e.GetPosition(DrawingCanvas);
                if (selectedElement.RenderTransform is TransformGroup transformGroup)
                {
                    var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                    if (translateTransform != null)
                    {
                        translateTransform.X += currentPosition.X - clickPosition.X;
                        translateTransform.Y += currentPosition.Y - clickPosition.Y;
                        clickPosition = currentPosition;
                    }
                }
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                selectedElement = sender as UIElement;
                if (selectedElement != null)
                {
                    HideResizeThumbs();
                    ShowResizeThumbsForSelected();
                }
            }
        }

        private void StartDraggingElementAt(Point point)
        {
            var hitElement = VisualTreeHelper.HitTest(DrawingCanvas, point)?.VisualHit as UIElement;
            if (hitElement != null && DrawingCanvas.Children.Contains(hitElement) && hitElement is Image)
            {
                selectedElement = hitElement;
                isDragging = true;
                clickPosition = point;
                HideResizeThumbs();
                ShowResizeThumbsForSelected();
            }
        }

        private void DragElementTo(Point point)
        {
            if (selectedElement != null && selectedElement.RenderTransform is TransformGroup transformGroup)
            {
                var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                if (translateTransform != null)
                {
                    translateTransform.X += point.X - clickPosition.X;
                    translateTransform.Y += point.Y - clickPosition.Y;
                    clickPosition = point;
                }
            }
        }

        public void StopDraggingElement()
        {
            isDragging = false;
            selectedElement = null;
            DrawingCanvas.ReleaseMouseCapture();
        }

        public void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            DrawingCanvas.Children.Clear();
            DrawingCanvas.Children.Add(EraserCursor);
            resizeThumbs.Clear();
        }

        private void ShowResizeThumbs()
        {
            foreach (var thumb in resizeThumbs)
            {
                thumb.Visibility = Visibility.Visible;
            }
        }

        private void HideResizeThumbs()
        {
            foreach (var thumb in resizeThumbs)
            {
                thumb.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowResizeThumbsForSelected()
        {
            if (selectedElement == null) return;

            var bounds = VisualTreeHelper.GetDescendantBounds(selectedElement);
            foreach (var thumb in resizeThumbs)
            {
                var thumbBounds = new Rect(Canvas.GetLeft(thumb), Canvas.GetTop(thumb), thumb.Width, thumb.Height);
                if (bounds.Contains(new Point(thumbBounds.X, thumbBounds.Y)))
                {
                    thumb.Visibility = Visibility.Visible;
                }
            }
        }
    }
}
