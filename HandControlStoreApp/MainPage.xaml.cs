using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using WindowsPreview.Kinect;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HandControlStoreApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;

        }

        KinectSensor sensor;
        //Depth variables
        DepthFrameReader depthReader;
        ushort[] depthData;
        byte[] depthDataConverted;
        WriteableBitmap depthBitmap;

        //IR variables
        InfraredFrameReader irReader;
        ushort[] irData;
        byte[] irDataConverted;
        WriteableBitmap irBitmap;

        //Skeleton variables
        BodyFrameReader skeletonReader;
        Body[] bodies;

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {

            sensor = KinectSensor.GetDefault();
            FrameDescription fd = sensor.DepthFrameSource.FrameDescription;
            
            //Depth Frame Settings
            depthReader=sensor.DepthFrameSource.OpenReader();            
            depthData= new ushort[fd.LengthInPixels];
            depthDataConverted= new byte[fd.LengthInPixels*4];
            outputImage.Source = depthBitmap;
            depthReader.FrameArrived += depthReader_FrameArrived;
            
            //IR Frame Settings
            irReader = sensor.InfraredFrameSource.OpenReader();
            irData = new ushort[fd.LengthInPixels];
            irDataConverted = new byte[fd.LengthInPixels * 4];
            outputImage.Source = depthBitmap;
            irReader.FrameArrived += irReader_FrameArrived;

            //Skeleton Frame Settings
            skeletonReader = sensor.BodyFrameSource.OpenReader();
            bodies = new Body[6];
            skeletonReader.FrameArrived += skeletonReader_FrameArrived;

            //Multi Frame Settings
            MultiSourceFrameReader mfr = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body);
            mfr.MultiSourceFrameArrived += mfr_MultiSourceFrameArrived;
            
            sensor.Open();

            //Testing if circle is drawing on canvas
            Ellipse cCircle = new Ellipse() { Width = 50, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(0, 255, 0, 0)) };
            bodyCanvas.Children.Add(cCircle);
            Canvas.SetLeft(cCircle, 100);
            Canvas.SetTop(cCircle, 100);            

        }

        void skeletonReader_FrameArrived(BodyFrameReader sender, BodyFrameArrivedEventArgs args)
        {
            using (BodyFrame bodyFrame = args.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    bodyCanvas.Children.Clear();
                    foreach (Body body in bodies)
                    {
                        if (body.IsTracked)
                        {
                            Joint leftHand = body.Joints[JointType.HandLeft];
                            Joint rightHand = body.Joints[JointType.HandRight];

                            if (leftHand.TrackingState == TrackingState.Tracked)
                            {
                                DepthSpacePoint dsp = sensor.CoordinateMapper.MapCameraPointToDepthSpace(leftHand.Position);
                                Ellipse lHandCircle = new Ellipse() { Width = 50, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) };
                                bodyCanvas.Children.Add(lHandCircle);
                                Canvas.SetLeft(lHandCircle, dsp.X - 25);
                                Canvas.SetTop(lHandCircle, dsp.Y - 25);
                            }

                            if (rightHand.TrackingState == TrackingState.Tracked)
                            {
                                DepthSpacePoint dsp = sensor.CoordinateMapper.MapCameraPointToDepthSpace(rightHand.Position);
                                Ellipse rHandCircle = new Ellipse() { Width = 50, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(0, 255, 255, 0)) };
                                bodyCanvas.Children.Add(rHandCircle);
                                Canvas.SetLeft(rHandCircle, dsp.X - 25);
                                Canvas.SetTop(rHandCircle, dsp.Y - 25);
                            }
                        }

                    }
                }
            }
        }

        void depthReader_FrameArrived(DepthFrameReader sender, DepthFrameArrivedEventArgs args)
        {
            using (DepthFrame dFrame = args.FrameReference.AcquireFrame())
            {

                if (dFrame != null)
                {
                    dFrame.CopyFrameDataToArray(depthData);

                    for (int i = 0; i < depthData.Length; i++)
                    {
                        byte intensity = (byte)depthData[i];
                        depthDataConverted[i * 4] = intensity;
                        depthDataConverted[i * 4 + 1] = intensity;
                        depthDataConverted[i * 4 + 2] = intensity;
                        depthDataConverted[i * 4 + 3] = 255;

                    }

                    depthDataConverted.CopyTo(depthBitmap.PixelBuffer);
                    depthBitmap.Invalidate();
                }

            }
        }

        void irReader_FrameArrived(InfraredFrameReader sender, InfraredFrameArrivedEventArgs args)
        {
            using( InfraredFrame irFrame = args.FrameReference.AcquireFrame()){
                if (irFrame != null)
                {
                    irFrame.CopyFrameDataToArray(irData);

                    for (int i = 0; i < irData.Length; i++)
                    {
                        byte intensity = (byte)irData[i];
                        irDataConverted[i * 4] = intensity;
                        irDataConverted[i * 4 + 1] = intensity;
                        irDataConverted[i * 4 + 2] = intensity;
                        irDataConverted[i * 4 + 3] = 255;

                    }

                    irDataConverted.CopyTo(irBitmap.PixelBuffer);
                    irBitmap.Invalidate();

                }
            }
        }

        void mfr_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs args)
        {
            Debug.WriteLine("Working till here {0}","Multi Source");
            //Handling multiple frames at a time.
            using (MultiSourceFrame mulFrame = args.FrameReference.AcquireFrame())
            {
                if (mulFrame != null)
                {
                    using (BodyFrame bodyFrame = mulFrame.BodyFrameReference.AcquireFrame())
                    {
                        using (DepthFrame depthFrame = mulFrame.DepthFrameReference.AcquireFrame())
                        {

                            if (bodyFrame != null && depthFrame != null)
                            {
                                depthFrame.CopyFrameDataToArray(depthData);

                                for (int i = 0; i < depthData.Length; i++)
                                {
                                    byte intensity = (byte)depthData[i];
                                    depthDataConverted[i * 4] = intensity;
                                    depthDataConverted[i * 4 + 1] = intensity;
                                    depthDataConverted[i * 4 + 2] = intensity;
                                    depthDataConverted[i * 4 + 3] = 255;

                                }

                                depthDataConverted.CopyTo(depthBitmap.PixelBuffer);
                                depthBitmap.Invalidate();

                                bodyFrame.GetAndRefreshBodyData(bodies);

                                bodyCanvas.Children.Clear();
                                foreach (Body body in bodies)
                                {
                                    if (body.IsTracked)
                                    {
                                        Joint leftHand = body.Joints[JointType.HandLeft];
                                        Joint rightHand = body.Joints[JointType.HandRight];

                                        if (leftHand.TrackingState == TrackingState.Tracked)
                                        {                                            
                                            DepthSpacePoint dsp = sensor.CoordinateMapper.MapCameraPointToDepthSpace(leftHand.Position);
                                            Ellipse lHandCircle = new Ellipse() { Width = 50, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)) };
                                            bodyCanvas.Children.Add(lHandCircle);
                                            Canvas.SetLeft(lHandCircle, dsp.X - 25);
                                            Canvas.SetTop(lHandCircle, dsp.Y - 25);
                                        }

                                        if (rightHand.TrackingState == TrackingState.Tracked)
                                        {                                            
                                            DepthSpacePoint dsp = sensor.CoordinateMapper.MapCameraPointToDepthSpace(rightHand.Position);                                            
                                            Ellipse rHandCircle = new Ellipse() { Width = 50, Height = 50, Fill = new SolidColorBrush(Color.FromArgb(0, 255, 255, 0)) };
                                            bodyCanvas.Children.Add(rHandCircle);
                                            Canvas.SetLeft(rHandCircle, dsp.X - 25);
                                            Canvas.SetTop(rHandCircle, dsp.Y - 25);
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }

        
    }
}
