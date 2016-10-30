using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI
{
    public class FrameCounter
    {
        public long TotalFrames { get; private set; }
        public double TotalSeconds { get; private set; }
        public double AverageFramesPerSecond { get; private set; }
        public double CurrentFramesPerSecond { get; private set; }

        public const int MAXIMUM_SAMPLES = 10;

        private Queue<double> _sampleBuffer = new Queue<double>();

        public bool Update(double deltaTime) {
            CurrentFramesPerSecond = 1.0/deltaTime;

            _sampleBuffer.Enqueue(CurrentFramesPerSecond);

            if (_sampleBuffer.Count > MAXIMUM_SAMPLES) {
                _sampleBuffer.Dequeue();
                AverageFramesPerSecond = _sampleBuffer.Average(i => i);
            } else {
                AverageFramesPerSecond = CurrentFramesPerSecond;
            }

            TotalFrames++;
            TotalSeconds += deltaTime;
            return true;
        }

        public void DrawFPS(SpriteBatch spriteBatch, SpriteFont font) {
            if (!double.IsInfinity(AverageFramesPerSecond)) {
                string fpsStr = $"FPS: {AverageFramesPerSecond}";

                spriteBatch.Begin(samplerState: Camera2D.SamplerState);
                spriteBatch.DrawString(font, fpsStr, new Vector2(0), Color.White);
                spriteBatch.End();
            }
        }
    }
}