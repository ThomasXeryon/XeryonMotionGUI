using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Microsoft.UI;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace XeryonMotionGUI.Classes
{
    public class Arrow
    {
        public Path Shaft
        {
            get; private set;
        }
        public Polygon Head
        {
            get; private set;
        }

        public Arrow()
        {
            // Initialize the shaft as a Path
            Shaft = new Path
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2,
                Fill = null
            };

            // Initialize the arrowhead as a Polygon
            Head = new Polygon
            {
                Fill = new SolidColorBrush(Colors.Black),
                Points = new PointCollection(),
                Stroke = null
            };
        }



        /// <summary>
        /// Adds the arrow to the specified Canvas.
        /// </summary>
        public void AddToCanvas(Canvas canvas)
        {
            canvas.Children.Add(Shaft);
            canvas.Children.Add(Head);
        }

        /// <summary>
        /// Removes the arrow from the specified Canvas.
        /// </summary>
        public void RemoveFromCanvas(Canvas canvas)
        {
            canvas.Children.Remove(Shaft);
            canvas.Children.Remove(Head);
        }

        /// <summary>
        /// Updates the position and shape of the arrow based on the source and target <see cref="DraggableElement"/>.
        /// Creates a three-bend path: left, then up, then right, with a slightly larger vertical offset,
        /// and rotates the arrowhead 90 degrees to the right.
        /// </summary>
        public void UpdatePosition(DraggableElement sourceElement, DraggableElement targetElement)
        {
            if (sourceElement == null || targetElement == null)
            {
                Debug.WriteLine("Source or Target element is null. Cannot update arrow position.");
                return;
            }

            // 1. Get source/target center points on the left border
            Point source = GetBlockLeftCenter(sourceElement);
            Point target = GetBlockLeftCenter(targetElement);

            // 2. Define how far left (horizontalOffset) and how high (verticalOffset) we go
            double horizontalOffset = 25;  // Move left by 50px (adjust as needed)
            double verticalOffset = 0;  // Go higher first (increase for bigger offset)

            // 3. Define the three bend points:
            //    - firstBend: left from source
            //    - secondBend: up from that horizontal line
            //    - thirdBend: right to line up with target
            Point firstBend = new Point(source.X - horizontalOffset, source.Y);
            Point secondBend = new Point(source.X - horizontalOffset, target.Y - verticalOffset);
            Point thirdBend = new Point(target.X, target.Y - verticalOffset);

            // 4. Build the shaft geometry
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure
            {
                StartPoint = source,
                IsClosed = false,
                IsFilled = false
            };

            // Add lines for each segment: left, up, right
            PolyLineSegment segment = new PolyLineSegment
            {
                Points = { firstBend, secondBend, thirdBend }
            };

            figure.Segments.Add(segment);
            geometry.Figures.Add(figure);

            Shaft.Data = geometry;

            // 5. Calculate arrowhead angle. The last segment is from thirdBend to target,
            //    but we want the arrowhead exactly at 'thirdBend'.
            double angle = Math.Atan2((target.Y - thirdBend.Y), (target.X - thirdBend.X)) * (180 / Math.PI);

            // Rotate arrowhead 90 degrees to the right
            //ngle -= 90.0;

            // 6. Define arrowhead shape. You can adjust size or shape as needed.
            double arrowHeadSize = 10; // arrowhead dimension
            // (0,0) is tip; shifting for 90 deg rotation
            Point arrowPoint1 = new Point(0, 0);
            Point arrowPoint2 = new Point(-arrowHeadSize, -arrowHeadSize / 2);
            Point arrowPoint3 = new Point(-arrowHeadSize, arrowHeadSize / 2);

            Head.Points.Clear();
            Head.Points.Add(arrowPoint1);
            Head.Points.Add(arrowPoint2);
            Head.Points.Add(arrowPoint3);

            // 7. Place arrowhead at the final bend
            Canvas.SetLeft(Head, thirdBend.X);
            Canvas.SetTop(Head, thirdBend.Y);

            // 8. Rotate arrowhead
            Head.RenderTransform = new RotateTransform
            {
                Angle = angle,
                CenterX = 0,
                CenterY = 0
            };
        }

        /// <summary>
        /// Returns the center point of the left border of the given DraggableElement.
        /// </summary>
        private Point GetBlockLeftCenter(DraggableElement block)
        {
            double left = Canvas.GetLeft(block);
            double top = Canvas.GetTop(block);
            double centerY = top + block.ActualHeight / 2;
            return new Point(left, centerY);
        }
    }
}
