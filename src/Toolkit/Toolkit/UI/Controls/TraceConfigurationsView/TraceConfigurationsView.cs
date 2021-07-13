// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Windows;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UtilityNetworks;
#if NETFX_CORE
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
#elif __IOS__
using Control = UIKit.UIViewController;
#elif __ANDROID__
using Android.App;
using Android.Views;
using Control = Android.Widget.FrameLayout;
#else
using System.Windows.Controls;
#endif

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    /// <summary>
    /// The TraceConfigurationsView view presents TraceConfigurations, either from the Map or Scene shown in the associated <see cref="GeoView" />.
    /// </summary>
    public partial class TraceConfigurationsView : Control
    {
        private TraceConfigurationsViewDataSource _dataSource = new TraceConfigurationsViewDataSource();
        private UtilityNetworksDataSource _utilityNetworksDataSource = new UtilityNetworksDataSource();
        private IEnumerable<UtilityTraceResult> _utilityTraceResults = new UtilityTraceResult[] { };
        List<UtilityElement> _startingLocations = new List<UtilityElement>();
        private bool _isBusy = false;
        private bool _zoomOnTrace = false;
        private readonly SimpleMarkerSymbol _startingSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, Color.Green, 20d);
        private readonly SimpleLineSymbol _outlineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Black, 1d);

        /// <summary>
        /// Event handler to check for Trace Exceptions and Results.
        /// </summary>
        // public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the MapView or SceneView associated with this view. When a TraceConfiguration is selected, the viewpoint of this
        /// geoview will be set to the TraceConfiguration's viewpoint. By default, TraceConfigurations from the geoview's Map or Scene
        /// property will be shown.
        /// </summary>
        /// <seealso cref="MapView"/>
        /// <seealso cref="SceneView"/>
        public GeoView GeoView
        {
            get => GeoViewImpl;
            set => GeoViewImpl = value;
        }

        private UtilityNetwork UtilityNetwork
        {
            get => UtilityNetworkImpl;
            set => UtilityNetworkImpl = value;
        }

        private bool IsBusy
        {
            get => _isBusy;
            set => _isBusy = value;
        }

        /// <summary>
        /// Gets or sets any exceptions associated with the trace.
        /// </summary>
        public Exception TraceException
        {
            get => TraceExceptionImpl;
            set => TraceExceptionImpl = value;
        }

        /// <summary>
        /// Gets trace results.
        /// </summary>
        public IEnumerable<UtilityTraceResult> TraceResults
        {
            get => _utilityTraceResults;

            // set => _utilityTraceResults = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether control should set viewpoint to trace results. By deafult this property is false.
        /// </summary>
        public bool ZoomOnTrace
        {
            get => _zoomOnTrace;
            set => _zoomOnTrace = value;
        }

        private void SetUtilityNetwork(UtilityNetwork utilityNetwork)
        {
            UtilityNetwork = utilityNetwork;
        }

        private async void SelectAndNavigateToTraceConfiguration(UtilityNamedTraceConfiguration namedTraceConfiguration)
        {
            try
            {
                IsBusy = true;
                ShowStatus("Loading Trace Results...", true);
                var parameters = new UtilityTraceParameters(namedTraceConfiguration, _startingLocations);

                // UtilityNetwork utilityNetwork = _dataSource.GetActiveUtilityNetwork;
                GeoView.DismissCallout();
                MapView mapView = (MapView)GeoViewImpl;
                if (TraceResults != null)
                {
                    ClearTrace();
                }

                if (mapView.GraphicsOverlays["Trace"] == null)
                {
                    var traceOverlay = new GraphicsOverlay();
                    traceOverlay.Id = "Trace";
                    mapView.GraphicsOverlays.Add(traceOverlay);
                }

                var overlay = mapView.GraphicsOverlays["Trace"];
                _utilityTraceResults = await UtilityNetwork.TraceAsync(parameters);
                TraceException = null;
                EnvelopeBuilder extent = null;
                StringBuilder result = new StringBuilder();
                foreach (UtilityTraceResult traceResult in TraceResults)
                {
                    if (traceResult is UtilityElementTraceResult elementTraceResult)
                    {
                        var layers = mapView.Map.OperationalLayers.OfType<GroupLayer>().Count() > 0 ? 
                            mapView?.Map?.OperationalLayers?.OfType<GroupLayer>()?.First()?.Layers?.OfType<FeatureLayer>() :
                            mapView.Map.OperationalLayers.OfType<FeatureLayer>();
                        // foreach (FeatureLayer layer in mapView?.Map?.OperationalLayers?.OfType<GroupLayer>()?.First()?.Layers?.OfType<FeatureLayer>())
                        foreach (var layer in layers)
                        {
                            IEnumerable<UtilityElement> elements = from element in elementTraceResult?.Elements
                                                                   where element.NetworkSource.Name == layer.FeatureTable.TableName
                                                                   select element;
                            IEnumerable<Feature> features = await UtilityNetwork.GetFeaturesForElementsAsync(elements);
                            if (ZoomOnTrace)
                            {
                                foreach (var f in features)
                                {
                                    if (extent == null)
                                    {
                                        extent = new EnvelopeBuilder(f.Geometry.Extent);
                                    }
                                    else
                                    {
                                        Esri.ArcGISRuntime.Geometry.Geometry otherExtent = f.Geometry.Extent;
                                        if (otherExtent.SpatialReference?.IsEqual(extent.SpatialReference) == false)
                                        {
                                            otherExtent = GeometryEngine.Project(otherExtent, extent.SpatialReference);
                                        }

                                        extent = new EnvelopeBuilder(GeometryEngine.CombineExtents(extent.ToGeometry(), otherExtent));
                                    }
                                }
                            }

                            layer.SelectFeatures(features);
                        }
                    }

                    if (traceResult is UtilityGeometryTraceResult geometryResult)
                    {
                        if (geometryResult.Polygon is Polygon polygon)
                        {
                            overlay.Graphics.Add(new Graphic(polygon, GetSymbol(polygon)));
                        }
                        else if (geometryResult.Polyline is Polyline polyline)
                        {
                            overlay.Graphics.Add(new Graphic(polyline, GetSymbol(polyline)));
                        }
                        else if (geometryResult.Multipoint is Multipoint multipoint)
                        {
                            overlay.Graphics.Add(new Graphic(multipoint, GetSymbol(multipoint)));
                        }
                    }

                    if (traceResult is UtilityFunctionTraceResult functionTraceResult)
                    {
                        result.Append("Function Results: \n");
                        foreach (UtilityTraceFunctionOutput functionOutput in functionTraceResult.FunctionOutputs)
                        {
                            result.Append(functionOutput.Function.FunctionType + "\t");
                            result.Append(functionOutput.Function.NetworkAttribute.Name + "\t");
                            result.Append(functionOutput.Result + "\t\n");
                        }

                        // result.Append(functionTraceResult.FunctionOutputs.Count.ToString() + " function outputs.\n");
                    }
                }

                if (extent?.Extent?.IsEmpty == false)
                {
                    _ = mapView.SetViewpointGeometryAsync(extent.ToGeometry());
                }

                ShowStatus(result.ToString());
                TraceConfigurationSelected?.Invoke(this, namedTraceConfiguration);
            }
            catch (Exception ex)
            {
                TraceException = ex;
                _utilityTraceResults = null;
                ShowStatus("Error: \n" + ex.Message);

                // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TraceException)));
            }
            finally
            {
                IsBusy = false;

                // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TraceResults)));
            }
        }

        private void ResetMap()
        {
            GeoView.DismissCallout();
            MapView mapView = (MapView)GeoViewImpl;
            if (mapView.GraphicsOverlays["StartingPoints"] != null)
            {
                mapView.GraphicsOverlays["StartingPoints"].Graphics.Clear();
            }

            _startingLocations = new List<UtilityElement>();

            ClearStatus();
            ClearTrace();
        }

        private void ClearTrace()
        {
            MapView mapView = (MapView)GeoViewImpl;
            var layers = mapView.Map.OperationalLayers.OfType<GroupLayer>().Count() > 0 ?
                            mapView?.Map?.OperationalLayers?.OfType<GroupLayer>()?.First()?.Layers?.OfType<FeatureLayer>() :
                            mapView.Map.OperationalLayers.OfType<FeatureLayer>();
            foreach (FeatureLayer layer in layers)
            {
                layer.ClearSelection();
            }

            if (mapView.GraphicsOverlays["Trace"] != null)
            {
                mapView.GraphicsOverlays["Trace"].Graphics.Clear();
            }
        }

        private Symbology.Symbol GetSymbol(Geometry.Geometry geometry)
        {
            if (geometry == null)
            {
                return null;
            }

            var random = new Random();
            var colorBytes = new byte[3];
            random.NextBytes(colorBytes);
            var color = Color.FromArgb(200, colorBytes[0], colorBytes[1], colorBytes[2]);

            switch (geometry.GeometryType)
            {
                case GeometryType.Multipoint:
                    {
                        return new SimpleMarkerSymbol()
                        {
                            Color = color,
                            Style = (SimpleMarkerSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleMarkerSymbolStyle)).Length - 1),
                            Size = 20d,
                            Outline = _outlineSymbol,
                        };
                    }

                case GeometryType.Polyline:
                    {
                        return new SimpleLineSymbol()
                        {
                            Color = color,
                            Style = (SimpleLineSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleLineSymbolStyle)).Length - 1),
                            Width = 5d,
                        };
                    }

                case GeometryType.Polygon:
                    {
                        return new SimpleFillSymbol()
                        {
                            Color = color,
                            Style = (SimpleFillSymbolStyle)random.Next(0, Enum.GetValues(typeof(SimpleFillSymbolStyle)).Length - 1),
                            Outline = _outlineSymbol,
                        };
                    }
            }

            return null;
        }

        private void AddViewTapped()
        {
            if (GeoView is MapView mv && mv.Map is ILoadable mapLoadable)
            {
                mv.GeoViewTapped += OnMapViewTapped;
            }
        }

        private void RemoveViewTapped()
        {
            GeoView.DismissCallout();
            if (GeoView is MapView mv && mv.Map is ILoadable mapLoadable)
            {
                mv.GeoViewTapped -= OnMapViewTapped;
            }
        }

        private async void OnMapViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (UtilityNetwork == null)
            {
                return;
            }

            if (GeoView.GraphicsOverlays["StartingPoints"] == null)
            {
                var startingPointsOverlay = new GraphicsOverlay();
                startingPointsOverlay.Id = "StartingPoints";
                GeoView.GraphicsOverlays.Add(startingPointsOverlay);
            }

            GeoView.DismissCallout();
            var overlay = GeoView.GraphicsOverlays["StartingPoints"];
            var identifyLayerResult = await GeoView.IdentifyLayersAsync(e.Position, 5, false);

            // var feature = identifyLayerResult?.FirstOrDefault()?.GeoElements?.FirstOrDefault() as ArcGISFeature;
            var feature = identifyLayerResult?.FirstOrDefault(r => r.GeoElements.Any(g => g is ArcGISFeature))?.GeoElements?.FirstOrDefault() as ArcGISFeature;
            if (feature != null)
            {
                var element = UtilityNetwork.CreateElement(feature);
                if (element.AssetType?.TerminalConfiguration?.Terminals.Count > 1)
                {
                    ShowCallout(element, e);

                    // element.Terminal = element.AssetType.TerminalConfiguration.Terminals[0];
                }
                else if (element.NetworkSource?.SourceType == UtilityNetworkSourceType.Edge && feature.Geometry is Polyline line)
                {
                    if (line.HasZ)
                    {
                        line = GeometryEngine.RemoveZ(line) as Polyline;
                    }

                    var percentAlong = GeometryEngine.FractionAlong(line, e.Location, double.NaN);
                    if (!double.IsNaN(percentAlong))
                    {
                        element.FractionAlongEdge = percentAlong;
                    }
                }

                _startingLocations.Add(element);

                var startingGraphic = new Graphic(feature.Geometry as MapPoint ?? e.Location, _startingSymbol);
                overlay.Graphics.Add(startingGraphic);
            }
        }

        /// <summary>
        /// Event raised when the user selects a TraceConfiguration.
        /// </summary>
        public event EventHandler<UtilityNamedTraceConfiguration> TraceConfigurationSelected;
    }
}