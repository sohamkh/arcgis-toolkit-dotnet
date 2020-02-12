using System;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace ARToolkit.SampleApp.Samples
{
    [SampleInfo(DisplayName = "Look around", Description = "A sample that doesn't rely on motion but only features the ability to look around based on a motion sensor")]
    public sealed partial class LookAroundSample : Page
    {
        public LookAroundSample()
        {
            this.InitializeComponent();
            Init();
        }

        private async void Init()
        {
            arview.RenderVideoFeed = false;
            arview.NorthAlign = false;
            arview.OriginCamera = new Esri.ArcGISRuntime.Mapping.Camera(new MapPoint(-119.622075, 37.720650, 2105), 0, 90, 0); //Yosemite

            Surface sceneSurface = new Surface();
            sceneSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            Scene scene = new Scene(Basemap.CreateImagery())
            {
                BaseSurface = sceneSurface
            };
            arview.Scene = scene;

            try
            {
                await arview.StartTrackingAsync(Esri.ArcGISRuntime.ARToolkit.ARLocationTrackingMode.Ignore);
            }
            catch (Exception ex)
            {
                await new MessageDialog("Failed to start tracking: \n" + ex.Message).ShowAsync();
                if (Frame.CanGoBack)
                    Frame.GoBack();
                return;
            }
        }
    }
}
