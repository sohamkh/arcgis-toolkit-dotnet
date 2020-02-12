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

#if NETFX_CORE
using System;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Windows.Devices.Sensors;
using Windows.Media.Capture;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Esri.ArcGISRuntime.ARToolkit
{
    public partial class ARSceneView : SceneView
    {
        private CaptureElement? _cameraView;
        private OrientationSensor? _sensor;
        private MediaCapture? _mediaCapture;
        private bool _isTracking;
        private TransformationMatrix _sensorToScreenTransformation = TransformationMatrix.Create(-Math.PI / 2, 0, 0, 1, 0, 0, 0); // TODO: This value likely needs updating on screen orientation changes


        /// <summary>
        /// Initializes a new instance of the <see cref="ARSceneView"/> class.
        /// </summary>
        public ARSceneView() : base()
        {
            InitializeCommon();
            _isTracking = false;
            Loaded += ARSceneView_Loaded;
            Unloaded += ARSceneView_Unloaded;
            IsManualRendering = false;            
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var elm = GetTemplateChild("MapSurface") as FrameworkElement;

            if (elm != null && elm.Parent is Panel parent)
            {
                _cameraView = new CaptureElement()
                {
                    Stretch = Windows.UI.Xaml.Media.Stretch.Uniform,
                    Visibility = RenderVideoFeed ? Visibility.Visible : Visibility.Collapsed
                };
                parent.Children.Insert(parent.Children.IndexOf(elm), _cameraView);
            }
        }

        private TaskCompletionSource<object?> _loadTask = new TaskCompletionSource<object?>();
        private bool _isTrackingStarting;

        private async Task OnStartTracking()
        {
            if (_isTracking || _isTrackingStarting)
            {
                return;
            }
            _isTrackingStarting = true;
            try
            {
                await _loadTask.Task;
                if (_isTrackingStarting)
                {
                    await InitializeTracker();
                    _isTracking = true;
                }
            }
            finally
            {
                _isTrackingStarting = false;
            }
        }

        private void OnStopTracking()
        {
            _isTrackingStarting = false;
            if (!_isTracking)
            {
                return;
            }

            DisposeTracking();
            _isTracking = false;
        }

        private void OnResetTracking()
        {
        }

        private void ARSceneView_Loaded(object sender, RoutedEventArgs e)
        {
            _loadTask.TrySetResult(null);
        }

        private async Task InitializeTracker()
        {
            if (NorthAlign)
            {
                _sensor = OrientationSensor.GetDefault(SensorReadingType.Absolute, SensorOptimizationGoal.Precision);
            }
            else
            {
                _sensor = OrientationSensor.GetDefaultForRelativeReadings();
            }
            if (_sensor == null)
            {
                _sensor = OrientationSensor.GetDefault();
            }
            if (_sensor == null)
            {
                throw new NotSupportedException("No Orientation Sensor detected");
            }
            _sensor.ReadingChanged += Sensor_ReadingChanged;
            await StartCapturing();
        }

        private void ARSceneView_Unloaded(object sender, RoutedEventArgs e)
        {
            _loadTask = new TaskCompletionSource<object?>();
            if (_isTracking)
            {
                DisposeTracking();
            }
        }

        private void DisposeTracking()
        {
            if (_sensor != null)
            {
                _sensor.ReadingChanged -= Sensor_ReadingChanged;
            }

            _sensor = null;
            if (_cameraView != null)
            {
                _cameraView.Source = null;
            }

            StopCapturing();
        }

        private void Sensor_ReadingChanged(OrientationSensor sender, OrientationSensorReadingChangedEventArgs args)
        {
            var c = Camera;
            if (c == null || _controller == null)
            {
                return;
            }

            var l = c.Transformation;
            var q = args.Reading.Quaternion;

            _controller.TransformationMatrix = InitialTransformation + TransformationMatrix.Create(q.X, q.Z, -q.Y, q.W, 0, 0, 0) + _sensorToScreenTransformation;
        }

        private async Task StartCapturing()
        {
            if (_cameraView == null || !RenderVideoFeed || !_loadTask.Task.IsCompleted)
            {
                return;
            }

            var allVideoDevices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
            var desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null
                && x.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back);
            var cameraDevice = desiredDevice ?? allVideoDevices.FirstOrDefault();
            if(cameraDevice == null)
            {
                throw new NotSupportedException("No suitable camera found");
            }

            var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

            _mediaCapture = new MediaCapture();
            try
            {
                await _mediaCapture.InitializeAsync(settings);
            }
            catch (UnauthorizedAccessException)
            {
                // Access denied to media capture (requires webcam + microphone access
                throw;
            }

            // Set Field-of-view of SceneView to match camera focal length
            var hfl = _mediaCapture.MediaCaptureSettings?.Horizontal35mmEquivalentFocalLength;
            if (hfl.HasValue)
            {
                SetFieldOfView(Math.Atan2(17.5, hfl.Value) * 2 / Math.PI * 180);
            }

            _cameraView.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
        }

        private void StopCapturing()
        {
            if (_cameraView != null)
            {
                _cameraView.Source = null;
            }

            _mediaCapture?.Dispose();
            _mediaCapture = null;
        }

        private TransformationMatrix HitTest(Windows.Foundation.Point screenPoint)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the scene should attempt to the device compass to align the scene towards north.
        /// </summary>
        /// <remarks>
        /// Note that the accuracy of the compass can heavily affect the quality of alignment.
        /// </remarks>
        public bool NorthAlign { get; set; } = true;
    }
}
#endif