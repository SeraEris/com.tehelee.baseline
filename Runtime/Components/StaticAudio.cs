using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tehelee.Baseline.Components
{
	[RequireComponent( typeof( AudioSource ) )]
	public class StaticAudio : MonoBehaviour
	{
		////////////////////////////////
		#region Static

		private static Dictionary<string, StaticAudio> lookup = new Dictionary<string, StaticAudio>();

		private static Dictionary<SoundCategories, float> _categoryVolumes;
		public static Dictionary<SoundCategories, float> categoryVolumes
		{
			get
			{
				if( object.Equals( null, _categoryVolumes ) )
				{
					_categoryVolumes = new Dictionary<SoundCategories, float>();

					SoundCategories[] categories = System.Enum.GetValues( typeof( SoundCategories ) ) as SoundCategories[];

					foreach( SoundCategories category in categories )
					{
						_categoryVolumes.Add( category, PlayerPrefs.GetFloat( string.Format( "Volume: {0}", category.ToString() ), category == SoundCategories.Master ? 0.25f : 1f ) );
					}
				}

				return _categoryVolumes;
			}
			set
			{
				_categoryVolumes = value;

				foreach( KeyValuePair<SoundCategories, float> kvp in value )
				{
					PlayerPrefs.SetFloat( string.Format( "Volume: {0}", kvp.Key.ToString() ), kvp.Value );
				}

				onVolumesChanged?.Invoke();
			}
		}

		public static bool Add( string key, StaticAudio source )
		{
			if( lookup.ContainsKey( key ) )
			{
				Debug.LogErrorFormat( source, "Static Audio already contains the key '{0}'.", key );
				return false;
			}
			else
			{
				lookup.Add( key, source );
			}

			return true;
		}

		public delegate void OnVolumesChanged();
		public static OnVolumesChanged onVolumesChanged;

		public static float GetCategoryVolume( SoundCategories category, bool factorMaster = true )
		{
			if( factorMaster )
				return category == SoundCategories.Master ? categoryVolumes[ SoundCategories.Master ] : categoryVolumes[ SoundCategories.Master ] * categoryVolumes[ category ];
			else
				return categoryVolumes[ category ];
		}

		public static void Play( string key, float pitch = 1f )
		{
			if( string.IsNullOrEmpty( key ) )
				return;

			string _key = key.ToLower();

			if( lookup.ContainsKey( _key ) )
				lookup[ _key ].Play( pitch );
		}

		public static void Stop( string key )
		{
			if( string.IsNullOrEmpty( key ) )
				return;

			string _key = key.ToLower();

			if( lookup.ContainsKey( _key ) )
				lookup[ _key ].Stop();
		}

		#endregion

		////////////////////////////////
		#region StaticAudio

		public string key;

		public SoundCategories soundCategory = SoundCategories.Master;

		[Range( 0f, 1f )]
		public float volume = 1f;

		private AudioSource audioSource;

		private void Awake()
		{
			audioSource = GetComponent<AudioSource>();

			if( !string.IsNullOrEmpty( key ) )
			{
				string _key = key.ToLower();

				StaticAudio.Add( _key, this );
			}
		}

		private void OnEnable()
		{
			StaticAudio.onVolumesChanged += VolumesChanged;

			VolumesChanged();
		}

		private void OnDisable()
		{
			StaticAudio.onVolumesChanged -= VolumesChanged;
		}

		private void VolumesChanged()
		{
			audioSource.volume = volume * StaticAudio.GetCategoryVolume( soundCategory );
		}

		public void Play( float pitch = 1f )
		{
			audioSource.pitch = pitch;
			audioSource.Play();
		}

		public void Stop()
		{
			if( audioSource.isPlaying )
				audioSource.Stop();
		}

		#endregion

		////////////////////////////////
	}
}