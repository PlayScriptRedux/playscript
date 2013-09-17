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

using System;
using System.Collections.Generic;
using flash.events;
using Android.Media;
using Android.App;
using Android.Content.Res;

namespace flash.media {

	partial class Sound {

		private static int SIZE = 10;
		private static Queue<ZMediaPlayer> playerQueue = new Queue<ZMediaPlayer>(SIZE);

		private ZMediaPlayer player;
		private SoundChannel channel = new SoundChannel ();

		private void internalLoad(String url)
		{
			player = new ZMediaPlayer (channel);


			try {

				if ( playerQueue.Count == SIZE )
				{
					ZMediaPlayer oldPlayer = playerQueue.Dequeue();
					oldPlayer.Release();
				}

				AssetFileDescriptor afd = Application.Context.Assets.OpenFd (url);
				player.SetDataSource (afd.FileDescriptor, afd.StartOffset, afd.Length);
				player.SetVolume (100, 100);
				player.Prepare ();

				playerQueue.Enqueue( player );

			} catch (Exception e) {
				Console.WriteLine ("Exception occur loading sounds. " + e.Message);
				this.dispatchEvent (new IOErrorEvent (IOErrorEvent.IO_ERROR));
				player.Release ();
				player = null;
				return;
			}
								
			this.dispatchEvent(new Event(Event.COMPLETE));
		}

		private SoundChannel internalPlay(double startTime=0, int loops=0, SoundTransform sndTransform=null)
        {
			if (player == null) {
				Console.WriteLine ("MediaPlayer is null");
				return null;
			}

			loops = Math.Max (0, loops);

			player.SetLoopsCount (loops);

			if (startTime > 0)
				Console.WriteLine ("StartTime is not supported");

			player.Start ();

			return channel;

        }

		class ZMediaPlayer : MediaPlayer, MediaPlayer.IOnCompletionListener {

			private SoundChannel channel;
			private int count;
			private int loopsCount;		

			public ZMediaPlayer(SoundChannel channel) 
			{
				this.channel = channel;
				this.channel.Player = this;
				this.count = 0;
				this.loopsCount = 0;

				this.SetOnCompletionListener( this );
			}

			public void SetLoopsCount(int loopsCount)
			{
				this.loopsCount = loopsCount;
			}

			public void OnCompletion (MediaPlayer mp)
			{
				if (count < loopsCount) {
					count++;
					mp.SeekTo (0);
					mp.Start ();
				} else if (count >= loopsCount) {
					channel.dispatchEvent (new Event (Event.SOUND_COMPLETE));
				}
			}								
		}
	}
}