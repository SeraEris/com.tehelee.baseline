using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEditorInternal;

namespace Tehelee.Baseline
{
	public static class UpdateFromGit
	{
		static AddRequest request;
		
		[MenuItem( "Tehelee/Baseline/Update From Git", priority = 100 )]
		private static void Update( MenuCommand menuCommand )
		{
			request = Client.Add( "https://github.com/Tehelee/com.tehelee.baseline.git" );
			
			ShowProgress( 0f );
			EditorApplication.update += Progress;
		}

		private static float lastProgress = 0f;
		private static void ShowProgress( float progress )
		{
			EditorUtility.DisplayProgressBar( "Updating Package: Tehelee - Baseline", "https://github.com/Tehelee/com.tehelee.baseline.git", progress );
			lastProgress = progress;
		}

		static void Progress()
		{
			ShowProgress( Mathf.Lerp( lastProgress, Random.value, 0.005f ) );
			
			if( request.IsCompleted )
			{
				if( request.Status >= StatusCode.Failure )
					Debug.LogError( request.Error.message );

				EditorUtility.ClearProgressBar();
				EditorApplication.update -= Progress;
			}
		}
	}
}