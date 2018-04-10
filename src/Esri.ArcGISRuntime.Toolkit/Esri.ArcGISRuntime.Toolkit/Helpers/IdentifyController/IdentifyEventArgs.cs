using System;
using Esri.ArcGISRuntime.Data;

namespace Esri.ArcGISRuntime.Samples
{
    public class IdentifyEventArgs : EventArgs
    {
        internal IdentifyEventArgs(GeoElement identifiedGeoElement) => IdentifiedGeoElement = identifiedGeoElement;

        public GeoElement IdentifiedGeoElement { get; private set; }
    }
}
