using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

using InputField = TMPro.TMP_InputField;

namespace Tehelee.Baseline.Components.UI
{
#if UNITY_EDITOR
	[ExecuteInEditMode]
#endif
	public class InputSlider : MonoBehaviour
	{
		////////////////////////////////
		#region Static

		private static readonly string[] decimalPrecisionFormats = new string[]
		{
			"{0}",
			"{0:0.0}",
			"{0:0.00}",
			"{0:0.000}",
			"{0:0.0000}",
			"{0:0.00000}",
			"{0:0.000000}",
			"{0:0.0000000}",
			"{0:0.00000000}",
			"{0:0.000000000}"
		};

		#endregion

		////////////////////////////////
		#region Attributes

		public Slider slider;
		public InputField input;

		public bool useWholeNumbers = true;

		public int minLimitInteger = 0;
		public int maxLimitInteger = 10;

		public float minLimitFloat = 0f;
		public float maxLimitFloat = 1f;

		[Range( 0, 9 )]
		public int decimalPrecision = 1;

		#endregion

		////////////////////////////////
		#region Members

#if UNITY_EDITOR

		private float lastSliderValue = float.MinValue;
		private string lastInputValue = null;
		private int lastDecimalPrecision = -1;
		private bool lastUseWholeNumbers = false;

#endif

		#endregion

		////////////////////////////////
		#region Properties

		public bool isReady => ( Utils.IsObjectAlive( slider ) && Utils.IsObjectAlive( input ) );

		public int wholeValue
		{
			get => Mathf.RoundToInt( slider.value );
			set
			{
				slider.value = value;
			}
		}

		public float value
		{
			get => slider.value;
			set
			{
				slider.value = value;
			}
		}

		#endregion

		////////////////////////////////
		#region Mono Methods

		protected virtual void Awake()
		{
			if( isReady )
				ApplySettings();
		}

		protected virtual void OnEnable()
		{
			if( !isReady )
				return;

			ApplySettings();

			slider.onValueChanged.AddListener( OnSliderChanged );
			input.onSubmit.AddListener( OnInputChanged );
		}

		protected virtual void OnDisable()
		{
			if( !isReady )
				return;

			slider.onValueChanged.RemoveListener( OnSliderChanged );
			input.onSubmit.RemoveListener( OnInputChanged );
		}

		protected virtual void Update()
		{
#if UNITY_EDITOR
			if( !Application.isPlaying )
			{
				if( isReady )
				{
					ApplySettings( true );

					if( lastInputValue != input.text )
					{
						OnInputChanged( input.text );

						lastInputValue = input.text;
						lastSliderValue = slider.value;
					}
					else if( lastSliderValue != slider.value )
					{
						OnSliderChanged( slider.value );

						lastSliderValue = slider.value;
						lastInputValue = input.text;
						
					}
					else if( lastUseWholeNumbers != useWholeNumbers )
					{
						OnInputChanged( input.text );
						
						lastUseWholeNumbers = useWholeNumbers;
					}
					else if( lastDecimalPrecision != decimalPrecision )
					{
						OnInputChanged( input.text );

						lastDecimalPrecision = decimalPrecision;
					}
				}
			}
#endif
		}

		#endregion

		////////////////////////////////
		#region InputSlider
			
		public void ApplySettings( bool skipAssignments = false )
		{
			float sliderMin, sliderMax;
			if( useWholeNumbers )
			{
				sliderMin = minLimitInteger;
				sliderMax = maxLimitInteger;
			}
			else
			{
				sliderMin = minLimitFloat;
				sliderMax = maxLimitFloat;
			}

			bool forceChanged = false;

			if( slider.value < sliderMin )
			{
				slider.SetValueWithoutNotify( sliderMin );
				forceChanged = true;
			}
			else if( slider.value > sliderMax )
			{
				slider.SetValueWithoutNotify( sliderMax );
				forceChanged = true;
			}

			slider.wholeNumbers = useWholeNumbers;

			slider.minValue = sliderMin;
			slider.maxValue = sliderMax;

			if( forceChanged || !skipAssignments )
			{
				InputReset();

				if( !useWholeNumbers )
					slider.SetValueWithoutNotify( float.Parse( input.text ) );
			}
		}

		private void OnSliderChanged( float value )
		{
			if( useWholeNumbers )
			{
				int _value = Mathf.RoundToInt( value );
				_value = Mathf.Clamp( _value, minLimitInteger, maxLimitInteger );
				slider.SetValueWithoutNotify( _value );
				input.SetTextWithoutNotify( _value.ToString() );
			}
			else
			{

				value = Mathf.Clamp( value, minLimitFloat, maxLimitFloat );
				string _value = string.Format( decimalPrecisionFormats[ decimalPrecision ], value );
				value = float.Parse( _value );

				slider.SetValueWithoutNotify( value );
				input.SetTextWithoutNotify( _value );
			}
		}

		private void OnInputChanged( string value )
		{
			value = value.Trim();
			if( Input.GetKey( KeyCode.Escape ) || string.IsNullOrEmpty( value ) )
			{
				InputReset();
				return;
			}

			if( useWholeNumbers )
			{
				int _value = Mathf.RoundToInt( float.Parse( value ) );
				_value = Mathf.Clamp( _value, minLimitInteger, maxLimitInteger );
				slider.SetValueWithoutNotify( _value );
				input.SetTextWithoutNotify( _value.ToString() );
			}
			else
			{
				float _value = float.Parse( value );
				_value = Mathf.Clamp( _value, minLimitFloat, maxLimitFloat );
				value = string.Format( decimalPrecisionFormats[ decimalPrecision ], _value );
				_value = float.Parse( value );

				slider.SetValueWithoutNotify( _value );
				input.SetTextWithoutNotify( value );
			}
		}

		private void InputReset()
		{
			if( useWholeNumbers )
			{
				input.text = Mathf.RoundToInt( slider.value ).ToString();
			}
			else
			{
				input.text = string.Format( decimalPrecisionFormats[ decimalPrecision ], slider.value );
			}
		}

		#endregion
	}

#if UNITY_EDITOR
	[CustomEditor( typeof( InputSlider ) )]
	public class EditorInputSlider : EditorUtils.InheritedEditor
	{
		public override float GetInspectorHeight()
		{
			float inspectorHeight = base.GetInspectorHeight();

			inspectorHeight += lineHeight * 6f + 4f;

			if( this[ "useWholeNumbers" ].boolValue )
			{
				inspectorHeight += lineHeight;
			}
			else
			{
				inspectorHeight += lineHeight * 2f + 4f;
			}
			
			return inspectorHeight;
		}

		public override void DrawInspector( ref Rect rect )
		{
			base.DrawInspector( ref rect );

			Rect cRect, bRect = new Rect( rect.x, rect.y, rect.width, lineHeight );

			EditorUtils.DrawDivider( bRect, new GUIContent( "Input Slider", "Provides an active link between a Slider and TMP.InputField." ) );
			bRect.y += lineHeight * 1.5f;

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Slider" ), this[ "slider" ], typeof( Slider ), true );
			bRect.y += lineHeight + 4f;

			EditorUtils.BetterObjectField( bRect, new GUIContent( "Input Field" ), this[ "input" ], typeof( InputField ), true );
			bRect.y += lineHeight * 1.5f;

			bRect.height = lineHeight * 1.5f;
			EditorUtils.BetterToggleField( bRect, new GUIContent( "Use Whole Numbers" ), this[ "useWholeNumbers" ] );
			bRect.height = lineHeight;
			bRect.y += lineHeight * 2f;
			
			cRect = new Rect( bRect.x, bRect.y, ( bRect.width - 10f ) * 0.5f, bRect.height );

			if( this[ "useWholeNumbers" ].boolValue )
			{
				EditorGUI.PropertyField( cRect, this[ "minLimitInteger" ], new GUIContent( "Min Limit" ) );
				cRect.x += cRect.width + 10f;
				EditorGUI.PropertyField( cRect, this[ "maxLimitInteger" ], new GUIContent( "Max Limit" ) );
				bRect.y += lineHeight;
			}
			else
			{
				EditorGUI.PropertyField( cRect, this[ "minLimitFloat" ], new GUIContent( "Min Limit" ) );
				cRect.x += cRect.width + 10f;
				EditorGUI.PropertyField( cRect, this[ "maxLimitFloat" ], new GUIContent( "Max Limit" ) );
				bRect.y += lineHeight + 4f;

				EditorGUI.IntSlider( bRect, this[ "decimalPrecision" ], 0, 9, new GUIContent( "Decimal Precision" ) );
				bRect.y += lineHeight;
			}
			
			bRect.y += lineHeight * 0.5f;

			rect.y = bRect.y;
		}
	}
#endif
}