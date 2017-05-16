using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Microsoft.Speech;
using System.Media;
using System.IO;
using System.Threading;

namespace IST331BasketballGame 
{

    public partial class Window : Window
    {
    //Class Level Variables
        KinectAudioSource _kinectSource;
        KinectSensor _kinectSensor; /*SENSOR*/
        SpeechRecognitionEngine _speechEngine;
        WriteableBitmap colorBitmap;
        Stream _stream;

        private const bool NO_KINECT = false; //Check if the user is testing with the kinect
        private const double LAUNCH_DIST = 280; //Distance that is considered to be a launch
        private Skeleton[] skeletonData;
        private double height, width; //Slope, Height, Width of eclipse
        private double limitLeft, limitRight, limitTop, lenientTop; //Limit for the ball to move left, right or up
        private bool isStart; //Check if the ball is allowed to roll
        private bool isLaunch = false; //Check if the ball is launched
        private bool isStop = false; //Check if the ball is at the end
        private bool isStrike = true; //Check if it is a strike 

        Boolean timedOut = false; //timed out checking
        double totalTime = 20; // time limit

        private int tries = 0;
        private bool isLock = false;
        private PinPoint startPosition; //Start position of the basketball
        private Image basketMade;

        //private String labelPt;
        private Dictionary<PinPoint, Image> centers = new Dictionary<PinPoint, Image>(); //Original
        private Dictionary<PinPoint, Image> cpcenters; //Clone
        
        static WaveGesture _gesture = new WaveGesture();

        public Window1()
        {

            InitializeComponent();
            getBasket();
            setBasket();
            getMoveLimits();
            resetGame();

            //Implement drag and drop feature if not use kinect
            if (NO_KINECT)
            {
            }
        }



        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {

            clearAll();
            //////////////////////////////////
            congrats.Visibility = Visibility.Hidden;
            scoreTotal.Visibility = Visibility.Hidden;

            var sensor = KinectSensor.KinectSensors.Where(
                         s => s.Status == KinectStatus.Connected).FirstOrDefault();

            if (sensor != null)
            {

                _kinectSensor = KinectSensor.KinectSensors[0];
                sensor.SkeletonStream.Enable();
                sensor.SkeletonFrameReady += Sensor_SkeletonFrameReady;
                _gesture.GestureRecognized += Gesture_GestureRecognized;
                sensor.Start();
                var recInstalled = SpeechRecognitionEngine.InstalledRecognizers();
                RecognizerInfo rec = (RecognizerInfo)recInstalled[0];
                _speechEngine = new SpeechRecognitionEngine(rec.Id);

                var choices = new Choices();
                choices.Add("start");
                choices.Add("continue");
                choices.Add("clear");
                choices.Add("end");

                var gb = new GrammarBuilder { Culture = rec.Culture };
                gb.Append(choices);
                var g = new Grammar(gb);

                _speechEngine.LoadGrammar(g);

                _speechEngine.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(SpeechEngineSpeechHypothesized);

                _speechEngine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(_speechEngineSpeechRecognized);

                var t = new Thread(StartAudioStream);
                t.Start();
            }
        }



        void StartAudioStream()
        {
            _kinectSource = _kinectSensor.AudioSource;
            _kinectSource.AutomaticGainControlEnabled = false;
            _kinectSource.EchoCancellationMode = EchoCancellationMode.None;
            _stream = _kinectSource.Start();
            _speechEngine.SetInputToAudioStream(_stream,
                            new SpeechAudioFormatInfo(
                                EncodingFormat.Pcm, 16000, 16, 1,
                                32000, 2, null));
            _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
        }



        void _speechEngineSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Text == "start")
            {
                ellipise.Visibility = Visibility.Visible;
                isStart = true; 
                score.Visibility = Visibility.Visible;
                score.Text = "SCORE: 0";
                timer.Visibility = Visibility.Visible;
                totalTime = 45;
                totalScore = 0;
                congrats.Visibility = Visibility.Hidden;
                timedOut = false;
                
            }


            else if (e.Result.Text == "clear")
            {
                clearAll();
            }

            else if (e.Result.Text == "continue")
            {
             //   basketStrikeText.Visibility = Visibility.Visible;
               // tryAgainText.Visibility = Visibility.Visible;
               congrats.Visibility = Visibility.Visible;
                congrats.Text = "Game Continues...";
            }

            else if (e.Result.Text == "end")
            {
                clearAll();
                congrats.Text = "END GAME";
            }
        }



        public void gameContinue(){


            }

        //clear method - clearing score, timer and basketball.
        public void clearAll()
        {
            
            ellipise.Visibility = Visibility.Hidden;
            isStart = false;
            score.Visibility = Visibility.Hidden;
            timer.Visibility = Visibility.Hidden;

        }



        public struct PinPoint
        {
            public double x, y, radius;
            public PinPoint(double x, double y, double radius)
            {
                this.x = x;
                this.y = y;
                this.radius = radius;
            }


            public bool isCollide(PinPoint p)
            {
                double distance = Math.Sqrt(Math.Pow(x - p.x, 2) + Math.Pow(y - p.y, 2));
                double maxDistance = p.radius + radius;
                return maxDistance > distance;
            }
        }



        void SpeechEngineSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (e.Result.Text == "start")
            {
                ellipise.Visibility = Visibility.Visible;
                isStart = true;
            }


            else if (e.Result.Text == "continue")
            {
                congrats.Text = "Continue Game...";
                ellipise.Visibility = Visibility.Visible;
                score.Visibility = Visibility.Visible;
                timer.Visibility = Visibility.Visible;
                isStart = true;
            }


            else if (e.Result.Text == "end")
            {
                congrats.Text = "Thanks for Playing";
            }
    

        }


        void Sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            // If countdown still continues...
            double constantInterval = 0.04;
            if (timedOut == false) // time remains
            { 
                totalTime -= constantInterval;
                timer.Text = ((float)totalTime).ToString();
                
            }

            //when count down ends
            if (totalTime <= 0) {

                timedOut = true;
                clearAll();
                congrats.Visibility = Visibility.Visible;
                scoreTotal.Visibility = Visibility.Visible;

                congrats.Text = "Game Over";
                scoreTotal.Text = "Total " + totalScore.ToString();
                basketStrikeText.Visibility = Visibility.Hidden;
                tryAgainText.Visibility = Visibility.Hidden;
            }
            

            using (var frame = e.OpenSkeletonFrame())
            {
                if (!isStart)
                {
                    return;
                }
                if (frame != null)
                {
                    Skeleton[] skeletons = new Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(skeletons);

                    if (skeletons.Length > 0)
                    {
                        var user = skeletons.Where(
                                   u => u.TrackingState == SkeletonTrackingState.Tracked).FirstOrDefault();

                        if (user != null)
                        {
                            JointCollection jointCollection = user.Joints;

                            double y = jointCollection[JointType.WristRight].Position.Y * -1000;
                            checkYMovement(y);

                            if (isStop)
                            { return; }

                            setLeft(jointCollection[JointType.WristRight].Position.X * 2000);
                            setTop(y);

                            _gesture.Update(user);
                            checkSurrounding();
                        }
                    }

                }

            }

        }


        void Gesture_GestureRecognized(object sender, EventArgs e)
        {

        }



        //Check which pins the bowling touches
        private void checkSurrounding()
        {
            //Background thread is not busy
            if (!isLock)
            {
                isLock = true;
                this.Dispatcher.Invoke(new Action(() =>
                {
                    PinPoint center = getEllipseCenter();
                    Thread.CurrentThread.IsBackground = true;
                    List<PinPoint> deleteLater = new List<PinPoint>();
                    foreach (KeyValuePair<PinPoint, Image> pin in cpcenters)
                    {
                        if (center.isCollide(pin.Key))
                        {
                            pin.Value.Visibility = Visibility.Hidden;
                            if (pin.Value == basketMade)
                            {
                                if (tries == 1)
                                {
                                    setStrike();
                                    deleteLater.Clear();
                                    break;
                                }
                            }
                            deleteLater.Add(pin.Key);
                        }
                    }
                    for (int i = 0; i < deleteLater.Count(); i++)
                    {
                        cpcenters.Remove(deleteLater[i]);
                    }
                    checkY(Canvas.GetTop(ellipise));
                    isLock = false;
                }));
            }
        }

        int totalScore = 0;

        // Striking ball
        private void setStrike()
        {

            isStop = true;
            isStrike = true;
            foreach (KeyValuePair<PinPoint, Image> pin in cpcenters)
            {
                pin.Value.Visibility = Visibility.Hidden;
            }

            basketStrikeText.Visibility = Visibility.Visible;
            totalScore += 1;

            score.Text = "SCORE: " + totalScore.ToString();
 
        }


        //visible Try Again message
        void displayTryAgain()
        {
            tryAgainText.Visibility = Visibility.Visible;
        }



        //Get height 
        private void getBasket()
        {
            height = ellipise.Height;
            width = ellipise.Width;
            startPosition = new PinPoint(Canvas.GetLeft(ellipise),
                Canvas.GetTop(ellipise), 0); //No need to use radius

        }


        // limiting the movement of ball using two bars - right and left
        private void getMoveLimits()
        {
            limitLeft = Canvas.GetLeft(leftWall) + leftWall.Width;
            limitRight = Canvas.GetLeft(rightWall);
            limitTop = 0;
            lenientTop = limitTop + 5;
        }



        //reset Game
        private void resetGame()
        {

            basketStrikeText.Visibility = Visibility.Hidden;
            tryAgainText.Visibility = Visibility.Hidden;
            isLaunch = false;
            isStop = false;
            ++tries;
            setLeft(startPosition.x);
            setTop(startPosition.y);

            if (!isStrike && tries <= 2)
            {
                return;
            }

            isStrike = false;
            tries = 1;
            cpcenters = new Dictionary<PinPoint, Image>();

            foreach (KeyValuePair<PinPoint, Image> item in centers)
            {
                item.Value.Visibility = Visibility.Visible;
                cpcenters.Add(item.Key, item.Value);
            }
        }


        //set Left - limit to the ball movement
        private void setLeft(double position)
        {
            if (position < limitLeft)
            {
                position = limitLeft;
            }
            else if (position + width > limitRight)
            {
                position = limitRight - width;
            }
            Canvas.SetLeft(ellipise, position);
        }

        private void setTop(double position)
        {
            if (position <= limitTop)
            {
                position = limitTop;
            }
            Canvas.SetTop(ellipise, position);
        }

  

        private void endgame(object sender, TextChangedEventArgs e)
        {

        }



        //Check movement of bowling
        private void checkY(double y)
        {
            if (isLaunch && y <= lenientTop) //When the ball hit the highest point
            {
                isStop = true;
                if (tries == 1)
                {
                    displayTryAgain();
                }
            }
            else if (!isLaunch && y < LAUNCH_DIST) //When user put hand high enough
            {
                isLaunch = true;
                isStop = false;
            }
        }



        //Check movement of hand or cursor
        private void checkYMovement(double y)
        {
            if (isStop && isLaunch && y >= LAUNCH_DIST) //Reset after launch
            {
                resetGame();
            }
        }


        //positioning basketball and hoop
        private void setBasket()
        {
            getCenter(hoopPosition);
            basketMade = hoopPosition;
        }

        //positioning Ball
        private PinPoint getEllipseCenter()
        {
            return new PinPoint(Canvas.GetLeft(ellipise) + (ellipise.Width / 2),
                Canvas.GetTop(ellipise) - (ellipise.Height / 2),
                ellipise.Height / 2);
        }

        //positioning hoop
        private void getCenter(Image img)
        {
            centers.Add(new PinPoint(Canvas.GetLeft(img) + (img.Width / 2),
                Canvas.GetTop(img) - (img.Height / 2),
                img.Width / 2),
                img);
        }
    }
}
