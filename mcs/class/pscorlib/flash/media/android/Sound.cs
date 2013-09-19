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

#if PLATFORM_MONODROID

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
			player = new ZMediaPlayer (url, channel);


			try {

				if ( playerQueue.Count == SIZE )
				{
					ZMediaPlayer oldPlayer = playerQueue.Dequeue();
					oldPlayer.Release();
				}

				player.Initialize();
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
				Console.WriteLine ("Warning: Sounds StartTime is not supported");

			try {
				// if the player is stopped, we need to initialize it again
				if (!player.isInitialize ())
					player.Initialize ();
			} catch (Exception e){
				Console.WriteLine ("Exception occur initializing sounds. " + e.Message);
				this.dispatchEvent (new IOErrorEvent (IOErrorEvent.IO_ERROR));
				player.Release ();
				player = null;
				return null;
			}

			player.Start ();

			return channel;

        }

		public class ZMediaPlayer : MediaPlayer, MediaPlayer.IOnCompletionListener {

			private String url;
			private SoundChannel channel;
			private int count;
			private int loopsCount;		
			private bool isInit;

			public ZMediaPlayer(String url, SoundChannel channel) 
			{
				this.url = url;
				this.channel = channel;
				this.channel.Player = this;
				this.count = 0;
				this.loopsCount = 0;
				this.isInit = false;

				this.SetOnCompletionListener( this );
			}

			public override void Stop ()
			{
				base.Stop ();
				this.isInit = false;
			}

			public void Initialize()
			{				
				Reset ();
				AssetFileDescriptor afd = Application.Context.Assets.OpenFd (url);
				SetDataSource (afd.FileDescriptor, afd.StartOffset, afd.Length);
				SetVolume (100, 100);
				Prepare ();

				this.isInit = true;
			}

			public void SetLoopsCount(int loopsCount)
			{
				this.loopsCount = loopsCount;
			}

			public bool isInitialize()
			{
				return this.isInit;
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

#endif
