using System;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.UI.Controls;

namespace Esri.ArcGISRuntime.Samples
{
    public interface IIdentifyController
    {
        MapView MapView { get; set; }

        IPopupSource Target { get; set; }

        event EventHandler<IdentifyEventArgs> IdentifyCompleted;
    }
}
