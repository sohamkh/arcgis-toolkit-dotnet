using System;
using System.ComponentModel;
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
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UtilityNetworks;

namespace Esri.ArcGISRuntime.Toolkit.Samples.TraceConfigurationsView
{
    /// <summary>
    /// Interaction logic for TraceConfigurationsViewSample.xaml
    /// </summary>
    [SampleInfoAttribute(Category = "TraceConfigurationsView", DisplayName = "TraceConfigurationsView - Comprehensive", Description = "Full TraceConfigurationsView scenario")]
    public partial class TraceConfigurationsViewSample : UserControl
    {
        private const string webMapUrl = "";
        public TraceConfigurationsViewSample()
        {
            InitializeComponent();

            MyMapView.Map = new Map(new Uri(webMapUrl));

            // TraceConfigurationsViewControl.PropertyChanged += ControlPropertyChanged;
        }

        /*private void ControlPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (TraceConfigurationsViewControl.TraceException != null)
            {
                MessageBox.Show(TraceConfigurationsViewControl.TraceException.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (TraceConfigurationsViewControl.TraceResults != null)
            {
                StringBuilder msg = new StringBuilder();
                foreach (UtilityTraceResult traceResult in TraceConfigurationsViewControl.TraceResults)
                {
                    if (traceResult is UtilityElementTraceResult elementTraceResult)
                    {
                        msg.Append(elementTraceResult.Elements.Count.ToString() + " elements.\n");
                    }
                    if (traceResult is UtilityFunctionTraceResult functionTraceResult)
                    {
                        msg.Append(functionTraceResult.FunctionOutputs.Count.ToString() + " function outputs.\n");
                    }
                }
                MessageBox.Show(msg.ToString(), "Trace Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }*/
    }
}