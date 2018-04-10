// /*******************************************************************************
//  * Copyright 2018 Esri
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
using System.Windows;
#endif

namespace Esri.ArcGISRuntime.Samples
{
    public sealed class IdentifyController : IIdentifyController
    {
        private WeakReference<MapView> _mapViewWeakRef;
        private bool _isIdentifyInProgress = false;
        private bool _wasMapViewDoubleTapped;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentifyController"/> class.
        /// </summary>
        public IdentifyController()
        {
        }

        private MapView _mapView;

        /// <summary>
        /// Gets or sets the MapView on which to perform identify operations
        /// </summary>
        public MapView MapView
        {
            get => _mapView;
            set
            {
                if (_mapView != value)
                {
                    var oldMapView = _mapView;
                    var newMapView = value;
                    if (oldMapView != null)
                    {
                        oldMapView.GeoViewTapped -= MapView_Tapped;
                        oldMapView.GeoViewDoubleTapped -= MapView_DoubleTapped;
                    }

                    if (_mapViewWeakRef == null)
                    {
                        _mapViewWeakRef = new WeakReference<MapView>(newMapView);
                    }
                    else
                    {
                        _mapViewWeakRef.SetTarget(newMapView);
                    }

                    newMapView.GeoViewTapped += MapView_Tapped;
                    newMapView.GeoViewDoubleTapped += MapView_DoubleTapped;
                }
            }
        }

        /// <summary>
        /// Invoked when GeoViewDoubleTapped event is firing
        /// </summary>
        private void MapView_DoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            // set flag to true to help distinguish between a double tap and a single tap
            _wasMapViewDoubleTapped = true;
        }

        /// <summary>
        /// Invoked when GeoViewTapped event is firing
        /// </summary>
        private async void MapView_Tapped(object sender, GeoViewInputEventArgs e)
        {
            _isIdentifyInProgress = true;
            var mapView = (MapView)sender;
            var target = Target;

            // Wait for double tap to fire
            // Identify is only peformed on single tap. The delay is used to detect and ignore double taps
            await Task.Delay(500);

            // If view has been double tapped, set tapped to handled and flag back to false
            // If view has been tapped just once, perform identify
            if (_wasMapViewDoubleTapped == true)
            {
                e.Handled = true;
                _wasMapViewDoubleTapped = false;
            }
            else
            {
                if (target is ILoadable loadable && loadable.LoadStatus == LoadStatus.NotLoaded)
                {
                    await loadable.LoadAsync();
                }

                // get the tap location in screen units
                var tapScreenPoint = e.Position;
                var tapMapPoint = mapView.ScreenToLocation(tapScreenPoint);

                // set identify parameters
                var pixelTolerance = 10;
                var returnPopupsOnly = false;
                var maxResultCount = 10;

                GeoElement identifiedGeoElement = null;
                try
                {
                    if (target is Layer targetLayer)
                    {
                        // identify features in the target layer, passing the tap point, tolerance, types to return, and max results
                        var identifyResult = await mapView.IdentifyLayerAsync(targetLayer, tapScreenPoint, pixelTolerance, returnPopupsOnly, maxResultCount);

                        // get the closest identified point to the tapped location
                        identifiedGeoElement = FindNearestGeoElement(tapMapPoint, identifyResult?.GeoElements);
                    }
                    else if (target is GraphicsOverlay targetOverlay)
                    {
                        // identify features in the target layer, passing the tap point, tolerance, types to return, and max results
                        var identifyResult = await mapView.IdentifyGraphicsOverlayAsync(targetOverlay, tapScreenPoint, pixelTolerance, returnPopupsOnly, maxResultCount);

                        // get the closest identified point to the tapped location
                        identifiedGeoElement = FindNearestGeoElement(tapMapPoint, identifyResult?.Graphics);
                    }
                    else if (target is ArcGISSublayer sublayer)
                    {
                        var layer = mapView?.Map?.AllLayers?.OfType<ArcGISMapImageLayer>()?.Where(l => l.Sublayers.Contains(sublayer))?.FirstOrDefault();

                        // identify features in the target layer, passing the tap point, tolerance, types to return, and max results
                        var topLevelIdentifyResult = await mapView.IdentifyLayerAsync(layer, tapScreenPoint, pixelTolerance, returnPopupsOnly, maxResultCount);
                        var sublayerIdentifyResult = topLevelIdentifyResult?.SublayerResults?.Where(r => r.LayerContent.Equals(sublayer)).FirstOrDefault();

                        // get the closest identified point to the tapped location
                        identifiedGeoElement = FindNearestGeoElement(tapMapPoint, sublayerIdentifyResult?.GeoElements);
                    }
                }
                catch
                {
                    // TODO: Alert user if error occured when trying to identify
                }

                OnIdentifyCompleted(identifiedGeoElement);
            }

            _isIdentifyInProgress = false;
        }

        /// <summary>
        /// Gets or sets the layer or overlay on which to perform identify operations
        /// </summary>
        public IPopupSource Target { get; set; }

        /// <summary>
        /// Find the GeoElement closest to the specified location
        /// </summary>
        /// <returns>The closest GeoElement to the input point</returns>
        private GeoElement FindNearestGeoElement(Geometry.Geometry location, IReadOnlyList<GeoElement> geoElements)
        {
            if (geoElements == null || geoElements.Count == 0)
            {
                return null;
            }
            else if (geoElements.Count == 1)
            {
                return geoElements[0];
            }
            else
            {
                // Sort list of GeoElements by comparing the distance between them and the tapped screen location
                var sortableGeoElements = geoElements.ToList();
                sortableGeoElements.Sort((a, b) => GeometryEngine.Distance(location, a.Geometry).CompareTo(GeometryEngine.Distance(location, b.Geometry)));
                return sortableGeoElements[0];
            }
        }

        public event EventHandler<IdentifyEventArgs> IdentifyCompleted;

        private void OnIdentifyCompleted(GeoElement identifiedGeoElement) =>
            IdentifyCompleted?.Invoke(this, new IdentifyEventArgs(identifiedGeoElement));
    }
}
