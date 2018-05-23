using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
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

namespace Esri.ArcGISRuntime.Toolkit.Samples.PopupView
{
    /// <summary>
    /// Interaction logic for PopupViewSample.xaml
    /// </summary>
    public partial class PopupViewSample : UserControl
    {
        public PopupViewSample()
        {
            InitializeComponent();
            url.Text = "http://www.arcgis.com/home/webmap/viewer.html?webmap=ffe7348e15cd44169fa353fc19a8d16f";
            LoadMap("http://www.arcgis.com/home/webmap/viewer.html?webmap=ffe7348e15cd44169fa353fc19a8d16f");
        }

        private async void LoadMap(string url)
        {
            //var portal = await CreatePortalAsync("http://rtc-100-3.esri.com/portal", "apptest", "app.test1234");
            //var item = await PortalItem.CreateAsync(portal, "ad1ca7bf4b214a3fbf7b6683efbc18cf");  //WebMap with Popup Definition
            //myMapView.Map = new Map(item);


            mapView.Map = new Esri.ArcGISRuntime.Mapping.Map(new Uri(url));
            try
            {
                await mapView.Map.LoadAsync();
                mapView.Map.OperationalLayers.Add(new Esri.ArcGISRuntime.Mapping.ArcGISMapImageLayer(new Uri("http://sampleserver6.arcgisonline.com/arcgis/rest/services/911CallsHotspot/MapServer")));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "Map Load Error");
            }
        }

        private async void mapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            if (e.Location == null)
                return;
            var view = sender as GeoView;
            var results = await view.IdentifyLayersAsync(e.Position, 3, false);
            List<GeoElement> elements = new List<GeoElement>();
            List<Popup> popups = new List<Popup>();
            foreach (var item in results)
            {
                elements.AddRange(item.GeoElements);
                popups.AddRange(item.Popups);
                foreach (var sub in item.SublayerResults)
                {
                    elements.AddRange(sub.GeoElements);
                    popups.AddRange(sub.Popups);
                }
            }
            var popup = popups.FirstOrDefault();
            if (popup != null)
            {
                var def = new CalloutDefinition(popup.GeoElement) { Tag = popup };
                def.OnButtonClick = StartEdit;
                def.ButtonImage = new RuntimeImage(new Uri("Images/edit.png", UriKind.Relative));
                view.ShowCalloutForGeoElement(popup.GeoElement, e.Position, def);
            }
            else
            {
                var elm = elements.FirstOrDefault();
                if (elm != null)
                {
                    var def = new CalloutDefinition(elm) { Tag = elm };
                    view.ShowCalloutForGeoElement(elm, e.Position, def);
                }
                else
                {
                    view.ShowCalloutAt(e.Location, new CalloutDefinition("No results"));
                }
            }
        }

        private void StartEdit(object tag)
        {
            var popup = tag as Popup;
            popupView.PopupManager = new PopupManager(popup) { SketchEditor = mapView.SketchEditor };
            //popupView.PopupManager.EditableDisplayFields.First().
        }

        private void LoadMapButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMap(url.Text);
        }
    }
}
