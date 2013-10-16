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
using MonoTouch.CoreFoundation;
using flash.events;

namespace flash.media {

	// We use a partial C# class for platform specific logic
	partial class Sound {

		public const int NumberOfSounds = 1000;

		private static Queue<Sound> _sounds = new Queue<Sound>(NumberOfSounds);

		private AVAudioPlayer _player;
		private SoundChannel _channel;
		#pragma warning disable 414
		private bool _loaded = false;
		#pragma warning restore 414

		public AVAudioPlayer Player
		{
			get { return _player; }
			set { _player = value; }
		}

		public SoundChannel Channel
		{
			get { return _channel; }
			set { _channel = value; }
		}

		public void unload()
		{
			_loaded = false;

			_player.Dispose ();
			_player = null;

			_channel.dispatchEvent (new Event (Event.SOUND_COMPLETE));
		}

		private void internalLoad(String url)
		{
			_url = url;

			var mediaFile = NSUrl.FromFilename(_url);
			_player = AVAudioPlayer.FromUrl(mediaFile);

			if (null != _player)
			{

			    if (_sounds.Count == NumberOfSounds) 
				{
					Sound deqSound = _sounds.Dequeue ();
					int deqCnt = 0;
					while (deqSound.Player.Playing && deqCnt < NumberOfSounds)
					{
						_sounds.Enqueue (deqSound);
						deqSound = _sounds.Dequeue ();

						deqCnt ++;
					}

					if (deqSound.Player.Playing) 
					{
						Console.WriteLine("Disposing audio player which is playing");
					}

					deqSound.unload ();

				}

				_channel = new SoundChannel();
				_channel.Player = _player;

				_sounds.Enqueue(this);

				_player.PrepareToPlay();


				_player.DecoderError += delegate {
					_loaded = false;
					this.dispatchEvent(new IOErrorEvent(IOErrorEvent.IO_ERROR));
				};

				_player.FinishedPlaying += delegate { 
					_loaded = false;
					_channel.dispatchEvent (new Event (Event.SOUND_COMPLETE));
					//_player.Dispose(); 
				};

				_loaded = true;
				this.dispatchEvent(new Event(Event.COMPLETE));

			}

		}

		private SoundChannel internalPlay(double startTime=0, int loops=0, SoundTransform sndTransform=null)
        {
			if (null == _player && _url != null)
				internalLoad (_url);

			if (loops < 0)
				loops = 0;

			_player.NumberOfLoops = loops;

			DispatchQueue.DefaultGlobalQueue.DispatchAsync (() => {
				if (startTime > 0)
					_player.PlayAtTime (startTime);
				else
					_player.Play ();
			});

			/*
			var t = MonoTouch.AudioToolbox.SystemSound.FromFile(_url);
			t.PlaySystemSound();
			*/
			return _channel;
        }

	}

}

#endif
