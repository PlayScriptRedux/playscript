// Copyright 2013 Zynga Inc.
//	
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//		
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.

#if PLATFORM_MONOTOUCH

using System;
using System.Collections.Generic;
using MonoTouch.AVFoundation;
using MonoTouch.Foundation;
using flash.events;

namespace flash.media {

	// We use a partial C# class for platform specific logic
	partial class Sound {

		public const int NumberOfPlayers = 10;

		private static Queue<AVAudioPlayer> _players = new Queue<AVAudioPlayer>(NumberOfPlayers);

		private AVAudioPlayer _player;
		private SoundChannel _channel;

		private void internalLoad(String url)
		{
			_url = url;

			var mediaFile = NSUrl.FromFilename(_url);
			_player = AVAudioPlayer.FromUrl(mediaFile);

			if (null != _player)
			{
			    if (_players.Count == NumberOfPlayers) 
				{
				    _players.Dequeue().Dispose();
				}
				
				_players.Enqueue(_player);

				_player.PrepareToPlay();

				_channel = new SoundChannel();
				_channel.Player = _player;

				_player.DecoderError += delegate {
					this.dispatchEvent(new IOErrorEvent(IOErrorEvent.IO_ERROR));
				};

				_player.FinishedPlaying += delegate { 
					_channel.dispatchEvent (new Event (Event.SOUND_COMPLETE));
					//_player.Dispose(); 
				};

				this.dispatchEvent(new Event(Event.COMPLETE));

			}

		}

		private SoundChannel internalPlay(double startTime=0, int loops=0, SoundTransform sndTransform=null)
        {
			if (null == _player)
				return null;

			if (loops < 0)
				loops = 0;

			_player.NumberOfLoops = loops;

			if (startTime > 0)
				_player.PlayAtTime(startTime);
			else
                _player.Play();

			return _channel;
        }

	}

}

#endif