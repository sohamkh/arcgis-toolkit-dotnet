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
            Surface sceneSurface = new Surface();
            sceneSurface.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer")));
            Scene scene = new Scene(Basemap.CreateImagery())
            {
                BaseSurface = sceneSurface
            };

            // Create and add a building layer.
            ArcGISSceneLayer buildingsLayer = new ArcGISSceneLayer(new Uri("https://tiles.arcgis.com/tiles/P3ePLMYs2RVChkJx/arcgis/rest/services/Buildings_Brest/SceneServer/layers/0"));
            scene.OperationalLayers.Add(buildingsLayer);
            MapPoint start = new MapPoint(-4.494677, 48.384472, 24.772694, SpatialReferences.Wgs84);
            arview.OriginCamera = new Camera(start, 200, 0, 90, 0);
            arview.Scene = scene;

            try
            {
                await arview.StartTrackingAsync();
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
