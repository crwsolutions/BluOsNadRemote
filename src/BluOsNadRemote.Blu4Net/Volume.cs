using BluOsNadRemote.Blu4Net.Channel;
using System;

namespace BluOsNadRemote.Blu4Net
{
    public class Volume
    {
        public double Decibel { get; }

        public bool IsMuted { get; }

        public int Percentage { get; }

        public Volume(StatusResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            Decibel = response.Decibel;
            IsMuted = response.Volume == 0;
            Percentage = response.Volume;
        }

        public Volume(VolumeResponse response)
        {
            ArgumentNullException.ThrowIfNull(response);

            Decibel = response.Decibel;
            IsMuted = response.Mute == 1;
            Percentage = response.Volume;
        }

        public override string ToString()
        {
            return $"{Percentage}% {Decibel}db";
        }
    }
}
