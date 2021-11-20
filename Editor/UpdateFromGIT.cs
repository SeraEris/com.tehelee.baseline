using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEditorInternal;

namespace Tehelee.Baseline
{
	public static class UpdateFromGIT
	{
		static AddRequest request;
		
		[MenuItem( "Tehelee/Baseline/Update From GIT", priority = 100 )]
		private static void Update( MenuCommand menuCommand )
		{
			request = Client.Add( "https://github.com/Tehelee/com.tehelee.baseline.git" );
			EditorApplication.update += Progress;
		}

		static void Progress()
		{
			if (request.IsCompleted)
			{
				if (request.Status == StatusCode.Success)
					Debug.Log("Updated: " + request.Result.packageId);
				else if (request.Status >= StatusCode.Failure)
					Debug.Log(request.Error.message);

				EditorApplication.update -= Progress;
			}
		}
	}
}