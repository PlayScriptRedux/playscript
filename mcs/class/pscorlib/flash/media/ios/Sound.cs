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
using System.Diagnostics;

using MonoTouch.AVFoundation;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;

using flash.events;

namespace flash.media {

	// We use a partial C# class for platform specific logic
	partial class Sound {

		private const int ReportNumberOfPlayersAbove = 10;

		private static List<Sound> mPlayingSounds = new List<Sound>(ReportNumberOfPlayersAbove);
		private static int MaxNumberOfPlayingSounds = 0;

		private AVAudioPlayer mPlayer;
		private SoundChannel mChannel;

		private static List<Action> sAsyncEventDispatcher = new List<Action>();
		private static object sSync = new object();

		// Consider if these are accessed on one thread only or multiple threads
		private static Dictionary<string, NSData> sCachedSoundData = new Dictionary<string, NSData>();
		private static Dictionary<string, List<AVAudioPlayer>> sRecycledAudioPlayers = new Dictionary<string, List<AVAudioPlayer>>();

		public SoundChannel Channel
		{
			get { return mChannel; }
		}

		public static void WarmCache(string[] soundFiles)
		{
			foreach (string soundFile in soundFiles)
			{
				string key = soundFile.ToLowerInvariant();
				if (sCachedSoundData.ContainsKey(soundFile))
				{
					continue;
				}
				NSUrl nsUrl = NSUrl.FromFilename(soundFile);
				NSData soundData = NSData.FromUrl(nsUrl);
				sCachedSoundData.Add(soundFile, soundData);
			}
		}

		private List<AVAudioPlayer> GetOrCreateRecycledAudioPlayerList()
		{
			List<AVAudioPlayer> audioPlayers;
			if (sRecycledAudioPlayers.TryGetValue(_url, out audioPlayers) == false)
			{
				audioPlayers = new List<AVAudioPlayer>();
				sRecycledAudioPlayers.Add(_url, audioPlayers);
			}
			return audioPlayers;
		}

		private AVAudioPlayer GetOrCreateAudioPlayer()
		{
			AVAudioPlayer audioPlayer;

			// Let's see if we can use a recycled audio player
			List<AVAudioPlayer> audioPlayers = GetOrCreateRecycledAudioPlayerList();
			int count = audioPlayers.Count;
			if (count != 0)
			{
				int lastIndex = count - 1;
				audioPlayer = audioPlayers[lastIndex];
				audioPlayers.RemoveAt(lastIndex);
			}
			else
			{
				// If not let's create one (assuming we have the corresponding NSData)
				NSData soundData;
				if (sCachedSoundData.TryGetValue(_url, out soundData) == false)
				{
					// We could not use a recycled audio player, and the sound has never been loaded.
					// Let's load it now (synchronously for the moment)
					NSUrl nsUrl = NSUrl.FromFilename(_url);
					soundData = NSData.FromUrl(nsUrl);			// TODO: Synchronous load! Replace this to asynchronous!

					sCachedSoundData.Add(_url, soundData);
					if (soundData == null)
					{
						// We could not load the data, does it exist.
						// We still store it on the cache for next time. And return the error state.
						return null;
					}
				}

				audioPlayer = AVAudioPlayer.FromData(soundData);
				audioPlayer.PrepareToPlay();

				// Decode error event is only needed for the sound that created the player
				// as subsequent sound will not have a decode error
				audioPlayer.DecoderError += OnDecodeError;
			}

			// FinishedPlaying event is specific to this sound, so register the event there
			audioPlayer.FinishedPlaying += OnFinishedPlaying;
			return audioPlayer;
		}

		void OnDecodeError(object sender, AVErrorEventArgs args)
		{
			lock (sSync)
			{
				sAsyncEventDispatcher.Add( () => { dispatchEvent(new IOErrorEvent(IOErrorEvent.IO_ERROR)); });
			}
		}

		void OnFinishedPlaying(object sender, AVStatusEventArgs args)
		{
			lock (sSync)
			{
				sAsyncEventDispatcher.Add( () => { mChannel.dispatchEvent(new Event(Event.SOUND_COMPLETE)); });
			}

			// We can't dispose the sound within the callback as it breaks the app state (callback from another thread?)
			// This will aynway automatically be taken care of next time a sound is played, and this player will be recycled.
		}

		public void unload()
		{
			mPlayer.FinishedPlaying -= OnFinishedPlaying;		// Remove the event for this sound, another sound will register the event for this player

			// For the moment, this sound does not need a player anymore (but another sound might need the same later, so we recycle it).
			GetOrCreateRecycledAudioPlayerList().Add(mPlayer);
			mPlayer = null;
		}

		private static void RemoveUnusedAudioPlayers()
		{
			// Remove all the sounds that are done playing
			int newCount = mPlayingSounds.Count;
			for (int i = newCount - 1 ; i  >= 0 ; --i)
			{
				Sound sound = mPlayingSounds[i];
				if (sound.mPlayer.Playing == false)
				{
					// We can remove this player as it is not playing anymore
					// Swap it with last available sound (has already been parsed so we know we can keep it)
					if (i != newCount - 1)
					{
						mPlayingSounds[i] = mPlayingSounds[newCount - 1];
					}
					--newCount;

					sound.unload();
				}
			}
			int toRemove = mPlayingSounds.Count - newCount;
			if (toRemove != 0)
			{
				// Use an if test, easier to put a breakpoint when something has to be removed.
				// Also we don't have to remove often
				mPlayingSounds.RemoveRange(newCount, mPlayingSounds.Count - newCount);
			}

			// Out of the remaining sounds, remove the sounds that are over the limit
			// We should probably remove the sounds that are alsmost done instead of the last one in the list (this is a bit aggressive).
			// Even better, we should just not start a new sound, also we should make sure to never kill music (or not skip music)
			// TODO: Improve this
			/*
			while (mPlayingSounds.Count >= MaxNumberOfPlayingSounds)
			{
				int index = mPlayingSounds.Count - 1;
				Sound sound = mPlayingSounds[index];
				mPlayingSounds.RemoveAt(index);
				sound.unload();
			}
			*/
		}

		private void internalLoad(String url)
		{
			_url = url.ToLowerInvariant();

			// Instead of doing this every frame, we only do it when we play a new sound (it is less often)
			// This still gives us opportunity to recycle audio players before a new one is needed
			RemoveUnusedAudioPlayers();

			mPlayer = GetOrCreateAudioPlayer();
			if (null == mPlayer)
			{
				return;
			}

			mPlayingSounds.Add(this);
			if (mPlayingSounds.Count > MaxNumberOfPlayingSounds)
			{
				MaxNumberOfPlayingSounds = mPlayingSounds.Count;		// Let's see how many sounds are played simultaneously
				if (MaxNumberOfPlayingSounds >= ReportNumberOfPlayersAbove)
				{
					Console.WriteLine("{0} sounds playing simultaneously.", MaxNumberOfPlayingSounds);
				}
			}

			mChannel = new SoundChannel();
			mChannel.Player = mPlayer;

			dispatchEvent(new Event(Event.COMPLETE));			// Send this when the sound has been loaded, however we should do this asynchrnously as much as possible...
		}

		private SoundChannel internalPlay(double startTime = 0, int loops = 0, SoundTransform sndTransform = null)
        {
			if (null == mPlayer && _url != null)
				internalLoad (_url);

			if (loops < 0)
				loops = 0;

			mPlayer.NumberOfLoops = loops;

			//DispatchQueue.DefaultGlobalQueue.DispatchAsync (() => {
				if (startTime > 0)
					mPlayer.PlayAtTime (startTime);
				else
					mPlayer.Play ();
			//});

			/*
			var t = MonoTouch.AudioToolbox.SystemSound.FromFile(_url);
			t.PlaySystemSound();
			*/
			return mChannel;
        }

		public static void AsyncDispatchEvents()
		{
			int count = 0;
			Action[] actions = null;
			lock (sSync)
			{
				count = sAsyncEventDispatcher.Count;
				if (count != 0)
				{
					actions = sAsyncEventDispatcher.ToArray();
					sAsyncEventDispatcher.Clear();
				}
			}

			for (int i = 0 ; i < count ; ++i)
			{
				actions[i]();
			}
		}
	}

}

#endif
