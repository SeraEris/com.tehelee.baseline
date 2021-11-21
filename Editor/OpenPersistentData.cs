using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Tehelee.Baseline
{
	public static class OpenPersistentData
	{
		[MenuItem( "Tehelee/Open Persistent Data", priority = 10 )]
		private static void Open( MenuCommand menuCommand ) =>
			EditorUtils.RevealInFinder( Application.persistentDataPath );
	}
}