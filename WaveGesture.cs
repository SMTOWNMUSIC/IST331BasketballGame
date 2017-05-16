using System;
using Microsoft.Kinect;

namespace IST331BasketballGame
{
    class WaveGesture

    {
        readonly int WINDOW_SIZE = 50; //Number of frames for that gesture to last

        IGestureSegment[] _segments;
        int _currentSegment = 0;
        int _frameCount = 0; //number of frames we ask for data is called window size

        public event EventHandler GestureRecognized;

        public WaveGesture()
        {
            WaveSegment1 waveSegment1 = new WaveSegment1(); //Hanlde Hand right of the elbow
            WaveSegment2 waveSegment2 = new WaveSegment2(); //Handle Hand left of the elbow

            //Gesture parts and specify their order in the the _segments array - waving
            _segments = new IGestureSegment[] { waveSegment1, waveSegment2 };
        }

        public void Update(Skeleton skeleton)
        {

            //check every segment 
            GesturePartResult result = _segments[_currentSegment].Update(skeleton);

            //check every segment for success 
            if (result == GesturePartResult.Succeeded)
            {
                if (_currentSegment + 1 < _segments.Length)
                {
                    _currentSegment++;  //Go to the next gesture part to check
                    _frameCount = 0;
                }
                else
                {
                    if (GestureRecognized != null)
                    {
                        GestureRecognized(this, new EventArgs());  //Gesture found
                        Reset(); //Start the gesture over
                    }
                }
            }
            else if (result == GesturePartResult.Failed || _frameCount == WINDOW_SIZE)
            {
                Reset();  //Start the gesture over
            }
            else
            {
                _frameCount++;  //Count the frames
            }
        }

        public void Reset()
        {
            _currentSegment = 0;
            _frameCount = 0;
        }
    }
}
