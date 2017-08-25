using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using CoreLocation;
using CustomRenderer;
using CustomRenderer.iOS;
using Foundation;
using MapKit;
using ObjCRuntime;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.iOS;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CustomMap), typeof(CustomMapRenderer))]
namespace CustomRenderer.iOS
{
    public class CustomMapRenderer : MapRenderer
    {
        UIView customPinView;
        List<CustomPin> customPins;
        MKPolylineRenderer polylineRenderer;

        protected override void OnElementChanged(ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                var nativeMap = Control as MKMapView;
                if (nativeMap != null)
                {
                    // for route
					nativeMap.RemoveOverlays(nativeMap.Overlays);
					nativeMap.OverlayRenderer = null;
					polylineRenderer = null;

                    // for pin
                    nativeMap.RemoveAnnotations(nativeMap.Annotations);
                    nativeMap.GetViewForAnnotation = null;
                    nativeMap.CalloutAccessoryControlTapped -= OnCalloutAccessoryControlTapped;
                    nativeMap.DidSelectAnnotationView -= OnDidSelectAnnotationView;
                    nativeMap.DidDeselectAnnotationView -= OnDidDeselectAnnotationView;
                }
            }

            if (e.NewElement != null)
            {
                var formsMap = (CustomMap)e.NewElement;
                var nativeMap = Control as MKMapView;
                customPins = formsMap.CustomPins;

                // for route
                nativeMap.OverlayRenderer = GetOverlayRenderer;
				CLLocationCoordinate2D[] coords = new CLLocationCoordinate2D[formsMap.RouteCoordinates.Count];

				int index = 0;
				foreach (var position in formsMap.RouteCoordinates)
				{
					coords[index] = new CLLocationCoordinate2D(position.Latitude, position.Longitude);
					index++;
				}

				var routeOverlay = MKPolyline.FromCoordinates(coords);
				nativeMap.AddOverlay(routeOverlay);

                // for pin
                nativeMap.GetViewForAnnotation = GetViewForAnnotation;
                nativeMap.CalloutAccessoryControlTapped += OnCalloutAccessoryControlTapped;
                nativeMap.DidSelectAnnotationView += OnDidSelectAnnotationView;
                nativeMap.DidDeselectAnnotationView += OnDidDeselectAnnotationView;
            }
        }

        MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            MKAnnotationView annotationView = null;

            if (annotation is MKUserLocation)
                return null;

            var anno = annotation as MKPointAnnotation;
            var customPin = GetCustomPin(anno);
            if (customPin == null)
            {
                throw new Exception("Custom pin not found");
            }

            annotationView = mapView.DequeueReusableAnnotation(customPin.Id);
            if (annotationView == null)
            {
                annotationView = new CustomMKAnnotationView(annotation, customPin.Id)
                {
                    Image = GenerateImage(customPin.Id),
                    CalloutOffset = new CGPoint(0, 0),
                    LeftCalloutAccessoryView = new UIImageView(UIImage.FromFile("monkey.png")),
                    RightCalloutAccessoryView = UIButton.FromType(UIButtonType.DetailDisclosure)
                };
                ((CustomMKAnnotationView)annotationView).Id = customPin.Id;
                ((CustomMKAnnotationView)annotationView).Url = customPin.Url;
            }
            annotationView.CanShowCallout = true;
            return annotationView;
        }

        void OnCalloutAccessoryControlTapped(object sender, MKMapViewAccessoryTappedEventArgs e)
        {
            var customView = e.View as CustomMKAnnotationView;
            if (!string.IsNullOrWhiteSpace(customView.Url))
            {
                UIApplication.SharedApplication.OpenUrl(new Foundation.NSUrl(customView.Url));
            }
        }

        void OnDidSelectAnnotationView(object sender, MKAnnotationViewEventArgs e)
        {
            var customView = e.View as CustomMKAnnotationView;
            customPinView = new UIView();

            if (customView.Id == "Xamarin")
            {
                customPinView.Frame = new CGRect(0, 0, 200, 84);
                var image = new UIImageView(new CGRect(0, 0, 200, 84));
                image.Image = UIImage.FromFile("xamarin.png");
                customPinView.AddSubview(image);
                customPinView.Center = new CGPoint(0, -(e.View.Frame.Height + 75));
                e.View.AddSubview(customPinView);
            }
        }

        void OnDidDeselectAnnotationView(object sender, MKAnnotationViewEventArgs e)
        {
            if (!e.View.Selected)
            {
                customPinView.RemoveFromSuperview();
                customPinView.Dispose();
                customPinView = null;
            }
        }

        CustomPin GetCustomPin(MKPointAnnotation annotation)
        {
            var position = new Position(annotation.Coordinate.Latitude, annotation.Coordinate.Longitude);
            return customPins.FirstOrDefault(pin => pin.Pin.Position == position);
        }

		MKOverlayRenderer GetOverlayRenderer(MKMapView mapView, IMKOverlay overlayWrapper)
		{
			if (polylineRenderer == null && !Equals(overlayWrapper, null))
			{
				var overlay = Runtime.GetNSObject(overlayWrapper.Handle) as IMKOverlay;
				polylineRenderer = new MKPolylineRenderer(overlay as MKPolyline)
				{
					FillColor = UIColor.Blue,
					StrokeColor = UIColor.Red,
					LineWidth = 3,
					Alpha = 0.4f
				};
			}
			return polylineRenderer;
		}

		private readonly CGColor[] colors = {
			UIColor.Red.CGColor,
			UIColor.Blue.CGColor,
			UIColor.Brown.CGColor,
			UIColor.DarkGray.CGColor,
			UIColor.Magenta.CGColor,
			UIColor.Orange.CGColor,
			UIColor.Purple.CGColor,
		};

		public UIImage GenerateImage(string From)
		{
			nfloat width = 32;
			nfloat height = 32;

            Random RNG = new Random();
			CGColor color = colors[RNG.Next(colors.Length - 1)];

			UIFont font = UIFont.FromName("Helvetica Light", 14);
			UIGraphics.BeginImageContextWithOptions(new CGSize(width, height), false, 0);

			var context = UIGraphics.GetCurrentContext();
			context.SetFillColor(color);
			context.AddArc(width / 2, height / 2, width / 2, 0, (nfloat)(2 * Math.PI), true);
			context.FillPath();

			var textAttributes = new UIStringAttributes
			{
				ForegroundColor = UIColor.White,
				BackgroundColor = UIColor.Clear,
				Font = font,
				ParagraphStyle = new NSMutableParagraphStyle { Alignment = UITextAlignment.Center },
			};

			string text;
			string[] splitFrom = From.Split(' ');
			if (splitFrom.Length > 1)
			{
				text = splitFrom[0][0].ToString() + splitFrom[1][0];
			}
			else if (splitFrom.Length > 0)
			{
				text = splitFrom[0][0].ToString();
			}
			else
			{
				text = "?";
			}

			NSString str = new NSString(text);

			var textSize = str.GetSizeUsingAttributes(textAttributes);
			str.DrawString(new CGRect(0, height / 2 - textSize.Height / 2,
				width, height), textAttributes);

			UIImage image = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();

			return image;
		}
    }
}
