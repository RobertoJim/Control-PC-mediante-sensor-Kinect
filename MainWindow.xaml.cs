namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.IO;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using Coding4Fun.Kinect.Wpf;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        //Variable para configurar la inclinación vertical del roton de kinect
        int grados = 0;

        //Se define la variable speechEngine que se utilizará para crear la "máquina" de reconocimiento de voz
        private SpeechRecognitionEngine speechEngine;


        // Active Kinect sensor       
        private KinectSensor sensor;

        //La API siempre devuelve 6 esqueletos aunque no los haya, por lo que esta variable tiene que ser 6 
        //para que no haya error en la linea 190 (skeletonFrameData.CopySkeletonDataTo(skeletons);)
        const int skeletonCount = 6;
        Skeleton[] skeletons = new Skeleton[skeletonCount];

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "es-ES".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }


        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {           
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
              
                var parametros = new TransformSmoothParameters
                {
                     Smoothing = 0.0f,
                     Correction = 0.0f,
                     Prediction = 0.0f,
                     JitterRadius = 1.0f,
                     MaxDeviationRadius = 0.5f
                };
                this.sensor.SkeletonStream.Enable(parametros); //Cambiando los parametros se puede hacer el movivimiento del cursor mas "suave"


                //Habilita la función que permite detectar esqueletos 
               //this.sensor.SkeletonStream.Enable();

                // Se llama a la función cada vez que llegue un frame
                sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady); 

                
                //Enable the color video stream
                sensor.ColorStream.Enable();

                //Connect up the video event handler
                sensor.ColorFrameReady += myKinect_ColorFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
                
                this.sensor.ElevationAngle = grados; // posición vertial del rotor de kinect a 0 grados

                

                RecognizerInfo ri = GetKinectRecognizer();

                if (null != ri)
                {


                    this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                    //Se definen las palabras que tienen que ser reconocidas
                    var directions = new Choices();
                    directions.Add(new SemanticResultValue("arriba", "ARRIBA"));
                    directions.Add(new SemanticResultValue("abajo", "ABAJO"));
                    directions.Add(new SemanticResultValue("click", "CLICK"));
                    directions.Add(new SemanticResultValue("doble click", "DOBLE CLICK"));
                    directions.Add(new SemanticResultValue("click derecho", "CLICK DERECHO"));
                    directions.Add(new SemanticResultValue("boton central", "BOTON CENTRAL"));

                    var gb = new GrammarBuilder { Culture = ri.Culture };
                    gb.Append(directions);

                    var g = new Grammar(gb);



                    speechEngine.LoadGrammar(g);


                    speechEngine.SpeechRecognized += SpeechRecognized;


                    // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                    // This will prevent recognition accuracy from degrading over time.
                    ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                    speechEngine.SetInputToAudioStream(
                        sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                    speechEngine.RecognizeAsync(RecognizeMode.Multiple);
                }

            }

        }


        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }


        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return;
                }

                //Se copia el array de esqueketos en skeletons
                skeletonFrameData.CopySkeletonDataTo(skeletons);

                //Se busca el primer esqueleto y se configurar para que se rastree y conocer la posicion de las articulaciones
                Skeleton first = (from s in skeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                if (first != null)
                {

                    //Funcion de la libreria coding4Fun para convvertir el valor a XY y escalar la posicion
                    //Los dos primeros parametros son la resulocion de mi pantalla
                    //Los dos ultimos se utilizan para escalar a un quinto de la distancia original, para que se mas facil llegar a los bordes
                    //de la pantalla sin tener que estirar los brazos
                    Joint ScaledJoint = first.Joints[JointType.HandRight].ScaleTo(1920, 1080, 0.5f, 0.4f);

                    int topofscreen;
                    int leftofscreen;

                    leftofscreen = Convert.ToInt32(ScaledJoint.Position.X);
                    topofscreen = Convert.ToInt32(ScaledJoint.Position.Y);

                    ClickMouse.SetCursorPos(leftofscreen, topofscreen);                   
                }
            }
        }

        void myKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame == null) return;

                byte[] colorData = new byte[colorFrame.PixelDataLength];

                colorFrame.CopyPixelDataTo(colorData);

                kinectVideo.Source = BitmapSource.Create(
                                     colorFrame.Width, colorFrame.Height,    // image dimensions
                                     96, 96,    // resolution - 96 dpi for video frames
                                     PixelFormats.Bgr32,    // video format
                                     null,      // palette - none
                                     colorData,     //video data
                                     colorFrame.Width * colorFrame.BytesPerPixel // stride
                    );
            }
        }

        private void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Valor umbral a partir del cual se reconoce, por debajo de este umbral es como si no se hubiese hablado
            const double ConfidenceThreshold = 0.3;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "ARRIBA":
                        if (grados < 27) //Para que no salte una excepcion, ya que el maximo de grados son 27
                        {
                            grados += 3;
                            this.sensor.ElevationAngle = grados; 
                        }
                        else
                        {
                            MessageBox.Show("Máximo de grados alcanzado");
                        }
                        break;

                    case "ABAJO":
                        if (grados > -27)//Para que no salte una excepcion, ya que el minimo de grados son -27
                        {
                            grados -= 3;
                            this.sensor.ElevationAngle = grados;
                        }
                        else
                        {
                            MessageBox.Show("Mínimo de grados alcanzado");
                        }
                        break;

                    case "CLICK":
                        ClickMouse.SetMouseLeftButtonDown(); //Presionar boton izquierdo
                        ClickMouse.SetMouseLeftButtonUp(); //Soltar boton izquierdo
                        break; 

                    case "DOBLE CLICK":

                        ClickMouse.SetMouseLeftButtonDown(); //Presionar boton izquierdo
                        ClickMouse.SetMouseLeftButtonUp();  //Soltar boton izquierdo
                        ClickMouse.SetMouseLeftButtonDown(); //Presionar boton izquierdo
                        ClickMouse.SetMouseLeftButtonUp();  //Soltar boton izquierdo
                        break;

                    case "CLICK DERECHO":

                        ClickMouse.SetMouseRightButtonDown(); //Presionar boton derecho
                        ClickMouse.SetMouseRightButtonUp();  //Soltar boton derecho
                        break;

                    case "BOTON CENTRAL":

                        ClickMouse.SetMouseMiddleButtonDown();  //Presionar boton central
                        ClickMouse.SetMouseMiddleButtonUp();    //Soltar boton central
                        break;
                }
            }
        }
    }
}