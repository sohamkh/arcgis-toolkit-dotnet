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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Drawing;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UtilityNetworks;
using System.Collections.ObjectModel;
#if XAMARIN_FORMS
using Esri.ArcGISRuntime.Xamarin.Forms;
#else
using Esri.ArcGISRuntime.UI.Controls;
#endif

#if XAMARIN_FORMS
namespace Esri.ArcGISRuntime.Toolkit.Xamarin.Forms
#else
namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
#endif
{
    internal class TraceConfigurationsViewDataSource : IList<UtilityNamedTraceConfiguration>, INotifyCollectionChanged, INotifyPropertyChanged, IList
    {
        private GeoView _geoView;
        private Exception _traceException;
        // private IEnumerable<UtilityTraceResult> _utilityTraceResults;
        private UtilityNetwork _UN = null;
        private IList<UtilityNamedTraceConfiguration> _namedTraceConfigurationsList = new UtilityNamedTraceConfiguration[] { };

        private IList<UtilityNamedTraceConfiguration> ActiveTraceConfigurationList
        {
            get
            {
                return _namedTraceConfigurationsList;
            }
        }

        /// <summary>
        /// Sets the Exceptions that we recieve from the control.
        /// </summary>
        /// <param name="exception">The new exception that is generated.</param>
        public void SetTraceException(Exception exception)
        {
            _traceException = exception;
        }

        /*public void SetTraceResults(IEnumerable<UtilityTraceResult> utilityTraceResults)
        {
            _utilityTraceResults = utilityTraceResults;
        }*/

        public void SetUtilityNetwork(UtilityNetwork utilityNetwork)
        {
            _UN = utilityNetwork;
            GeoViewDocumentChanged(null, null);
        }

#if NETFX_CORE && !XAMARIN_FORMS
        private long _propertyChangedCallbackToken = 0;
#endif

        /// <summary>
        /// Sets the GeoView from which TraceConfigurations will be shown.
        /// </summary>
        /// <param name="view">The view from which to get Map/Scene TraceConfigurations.</param>
        public void SetGeoView(GeoView view)
        {
            if (_geoView == view)
            {
                return;
            }

            if (_geoView != null)
            {
#if !XAMARIN && !XAMARIN_FORMS
                if (_geoView is MapView mapview)
                {
#if NETFX_CORE
                    mapview.UnregisterPropertyChangedCallback(MapView.MapProperty, _propertyChangedCallbackToken);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).RemoveValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
#else
                (_geoView as INotifyPropertyChanged).PropertyChanged -= GeoView_PropertyChanged;
#endif
            }

            _geoView = view;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            if (_geoView != null)
            {
#if !XAMARIN && !XAMARIN_FORMS
                if (_geoView is MapView mapview)
                {
#if NETFX_CORE
                    _propertyChangedCallbackToken = mapview.RegisterPropertyChangedCallback(MapView.MapProperty, GeoViewDocumentChanged);
#else
                    DependencyPropertyDescriptor.FromProperty(MapView.MapProperty, typeof(MapView)).AddValueChanged(mapview, GeoViewDocumentChanged);
#endif
                }
#else

                (_geoView as INotifyPropertyChanged).PropertyChanged += GeoView_PropertyChanged;
#endif

                // Handle case where geoview loads map while events are being set up
                GeoViewDocumentChanged(null, null);
            }
        }

#if XAMARIN || XAMARIN_FORMS
        private void GeoView_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((sender is MapView && e.PropertyName == nameof(MapView.Map)) ||
                (sender is SceneView && e.PropertyName == nameof(SceneView.Scene)))
            {
                GeoViewDocumentChanged(sender, e);
            }
        }
#endif

        private void GeoViewDocumentChanged(object sender, object e)
        {
            if (_geoView is MapView mv && mv.Map is ILoadable mapLoadable)
            {
                // Listen for load completion
                var listener = new Internal.WeakEventListener<ILoadable, object, EventArgs>(mapLoadable);
                listener.OnEventAction = (instance, source, eventArgs) => Doc_Loaded(source, eventArgs);
                listener.OnDetachAction = (instance, weakEventListener) => instance.Loaded -= weakEventListener.OnEvent;
                mapLoadable.Loaded += listener.OnEvent;

                // Ensure event is raised even if already loaded
                _ = mv.Map.RetryLoadAsync();
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private async void Doc_Loaded(object sender, EventArgs e)
        {
            // Get new TraceConfigurations collection
            ObservableCollection<UtilityNamedTraceConfiguration> traceConfigurationCollection = new ObservableCollection<UtilityNamedTraceConfiguration>();
            if (sender is Map map && map.UtilityNetworks.Count > 0 && _UN != null)
            {
                await _UN.LoadAsync();
                var namedTraceConfigurations = await map.GetNamedTraceConfigurationsFromUtilityNetworkAsync(_UN);
                _namedTraceConfigurationsList = namedTraceConfigurations.ToList();
                traceConfigurationCollection = new ObservableCollection<UtilityNamedTraceConfiguration>(_namedTraceConfigurationsList);
            }
            else
            {
                return;
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            var listener = new Internal.WeakEventListener<INotifyCollectionChanged, object, NotifyCollectionChangedEventArgs>(traceConfigurationCollection);
            listener.OnEventAction = (instance, source, eventArgs) => HandleGeoViewTraceConfigurationsCollectionChanged(source, eventArgs);
            listener.OnDetachAction = (instance, weakEventListener) => instance.CollectionChanged -= weakEventListener.OnEvent;
            traceConfigurationCollection.CollectionChanged += listener.OnEvent;
        }

        private void HandleGeoViewTraceConfigurationsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(e);
        }

        private void RunOnUIThread(Action action)
        {
#if XAMARIN_FORMS
            global::Xamarin.Forms.Device.BeginInvokeOnMainThread(action);
#elif __IOS__
            _geoView.InvokeOnMainThread(action);
#elif __ANDROID__
            _geoView.PostDelayed(action, 500);
#elif NETFX_CORE
            _ = _geoView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () => action());
#else
            _geoView.Dispatcher.Invoke(action);
#endif
        }

        #region IList<UtilityNamedTraceConfiguration> implementation
        UtilityNamedTraceConfiguration IList<UtilityNamedTraceConfiguration>.this[int index] { get => ActiveTraceConfigurationList?[index]; set => throw new NotImplementedException(); }

        int ICollection<UtilityNamedTraceConfiguration>.Count => ActiveTraceConfigurationList?.Count ?? 0;

        bool ICollection<UtilityNamedTraceConfiguration>.IsReadOnly => true;

        bool IList.IsReadOnly => true;

        bool IList.IsFixedSize => false;

        int ICollection.Count => ActiveTraceConfigurationList?.Count ?? 0;

        object ICollection.SyncRoot => throw new NotImplementedException();

        bool ICollection.IsSynchronized => false;

        object IList.this[int index] { get => ActiveTraceConfigurationList?[index]; set => throw new NotImplementedException(); }

        void ICollection<UtilityNamedTraceConfiguration>.Add(UtilityNamedTraceConfiguration item) => throw new NotImplementedException();

        void ICollection<UtilityNamedTraceConfiguration>.Clear() => throw new NotImplementedException();

        bool ICollection<UtilityNamedTraceConfiguration>.Contains(UtilityNamedTraceConfiguration item) => ActiveTraceConfigurationList?.Contains(item) ?? false;

        void ICollection<UtilityNamedTraceConfiguration>.CopyTo(UtilityNamedTraceConfiguration[] array, int arrayIndex) => ActiveTraceConfigurationList?.CopyTo(array, arrayIndex);

        IEnumerator<UtilityNamedTraceConfiguration> IEnumerable<UtilityNamedTraceConfiguration>.GetEnumerator() => ActiveTraceConfigurationList?.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ActiveTraceConfigurationList?.GetEnumerator();

        int IList<UtilityNamedTraceConfiguration>.IndexOf(UtilityNamedTraceConfiguration item) => ActiveTraceConfigurationList?.IndexOf(item) ?? -1;

        void IList<UtilityNamedTraceConfiguration>.Insert(int index, UtilityNamedTraceConfiguration item) => throw new NotImplementedException();

        bool ICollection<UtilityNamedTraceConfiguration>.Remove(UtilityNamedTraceConfiguration item) => throw new NotImplementedException();

        void IList<UtilityNamedTraceConfiguration>.RemoveAt(int index) => throw new NotImplementedException();

        int IList.Add(object value) => throw new NotImplementedException();

        bool IList.Contains(object value) => ActiveTraceConfigurationList?.Contains(value) ?? false;

        void IList.Clear() => throw new NotImplementedException();

        int IList.IndexOf(object value) => ActiveTraceConfigurationList?.IndexOf(value as UtilityNamedTraceConfiguration) ?? -1;

        void IList.Insert(int index, object value) => throw new NotImplementedException();

        void IList.Remove(object value) => throw new NotImplementedException();

        void IList.RemoveAt(int index) => throw new NotImplementedException();

        void ICollection.CopyTo(Array array, int index) => (ActiveTraceConfigurationList as ICollection)?.CopyTo(array, index);
        #endregion IList<UtilityNamedTraceConfiguration> implementation

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            RunOnUIThread(() =>
            {
                CollectionChanged?.Invoke(this, args);
                OnPropertyChanged("Item[]");
                if (args.Action != NotifyCollectionChangedAction.Move)
                {
                    OnPropertyChanged(nameof(IList.Count));
                }
            });
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
