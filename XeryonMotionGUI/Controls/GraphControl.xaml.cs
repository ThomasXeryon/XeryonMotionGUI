using Microsoft.UI.Xaml.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace XeryonMotionGUI.Controls
{
    public sealed partial class OxyPlotGraphControl : UserControl
    {
        public PlotModel PlotModel
        {
            get; private set;
        }

        public OxyPlotGraphControl()
        {
            this.InitializeComponent();
            InitializeDummyPlot();
        }

        private void InitializeDummyPlot()
        {
            // Create a new PlotModel with a title.
            PlotModel = new PlotModel { Title = "Zone 1 Frequency Graph" };

            // Create a dummy line series with some sample data.
            var lineSeries = new LineSeries
            {
                Title = "Dummy Data",
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };
            lineSeries.Points.Add(new DataPoint(0, 10));
            lineSeries.Points.Add(new DataPoint(1, 15));
            lineSeries.Points.Add(new DataPoint(2, 8));
            lineSeries.Points.Add(new DataPoint(3, 12));
            lineSeries.Points.Add(new DataPoint(4, 16));
            PlotModel.Series.Add(lineSeries);

            // Add horizontal and vertical axes.
            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Time" });
            PlotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Frequency" });

            // Assign the PlotModel to the PlotView.
            plotView.Model = PlotModel;
        }
    }
}
