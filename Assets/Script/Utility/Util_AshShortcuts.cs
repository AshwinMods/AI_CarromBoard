#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;

public class Util_AshShortcuts : MonoBehaviour
{
	static Util_AshShortcuts()
	{
		SceneView.onSceneGUIDelegate -= OnSceneUpdate;
		SceneView.onSceneGUIDelegate += OnSceneUpdate;
	}

	static Vector2 lastMPos;
	static Vector3 GetWorldPos(Vector2 screenCoord)
	{
		Vector3 worldSpace = Camera.current.ScreenToWorldPoint(screenCoord);
		worldSpace.y = Camera.current.transform.position.y * 2 - worldSpace.y;
		return worldSpace;
	}
	static void OnSceneUpdate(SceneView sceneView)
	{
		if (Event.current != null && Event.current.isMouse)
			lastMPos = Event.current.mousePosition;
	}

	static Transform PrevNodeAtSameLevel(Transform p, int l, int c)
	{
		if (l < 0)
			return null;
		if (l == 0)
			return p;
		Transform res = null;
		for (int i = c; i > 0; i--)
		{
			res = PrevNodeAtSameLevel(p.GetChild(i), l - 1, p.childCount);
			if (res != null)
				return res;
		}
		return null;
	}
	static Transform NextNodeAtSameLevel(Transform p, int l, int c)
	{
		if (l < 0)
			return null;
		if (l == 0)
			return p;
		Transform res = null;
		for (int i = c; i < p.childCount; i++)
		{
			res = NextNodeAtSameLevel(p.GetChild(i), l - 1, 0);
			if (res != null)
				return res;
		}
		return null;
	}

	// % (ctrl), # (shift), & (alt). _ (char)
	static bool useShortcut = true;

	[MenuItem("AshTools/ToogleShortcuts", false)]
	static void ToogleShortcuts()
	{
		useShortcut = !useShortcut;
		Debug.Log("AshTools: Shortcuts Enabled " + useShortcut);
	}


	// Select Parent Object (Alt + R)
	[MenuItem("AshTools/Reset Transform &r", true, 0)]
	static bool ValidateResetTransform()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Reset Transform &r", false, 0)]
	static void ResetTransform()
	{
		if (Selection.activeTransform != null)
		{
			Selection.activeTransform.localPosition = Vector3.zero;
			Selection.activeTransform.localRotation = Quaternion.identity;
			Selection.activeTransform.localScale = Vector3.one;
		}
	}


	/*
	// Enable Selected Object
	[MenuItem("AshTools/Enable Selected Object &E", true, 00)]
	static bool ValidateEnableSelectedGameObject()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Enable Selected Object &E", false, 00)]
	static void EnableSelectedGameObject()
	{
		Selection.activeTransform.gameObject.SetActive(true);
	}
	// Disable Selected Object
	[MenuItem("AshTools/Disable Selected Object &D", true, 00)]
	static bool ValidateDisableSelectedGameObject()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Disable Selected Object &D", false, 00)]
	static void DisableSelectedGameObject()
	{
		Selection.activeTransform.gameObject.SetActive(false);
	}
    */


	// Select Parent Object (Alt + P)
	[MenuItem("AshTools/Select Parent &p", true, 11)]
	static bool ValidateSelectParentGameObject()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Select Parent &p", false, 11)]
	static void SelectParentGameObject()
	{
		if (Selection.activeTransform.parent != null)
			Selection.activeTransform = Selection.activeTransform.parent;
	}



	// Select Child Object (Alt + P)
	[MenuItem("AshTools/Select Child &\\", true, 11)]
	static bool ValidateSelectChildGameObject()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Select Child &\\", false, 11)]
	static void SelectChildGameObject()
	{
		if (Selection.activeTransform.childCount > 0)
			Selection.activeTransform = Selection.activeTransform.GetChild(0);
	}



	// Select Previous Object
	[MenuItem("AshTools/Prev Same Level Object (d=3) &[", true, 22)]
	static bool ValidateSelectPrevGameObject()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Prev Same level Object (d=3) &[", false, 22)]
	static void SelectPrevGameObject()
	{
		if (Selection.activeTransform.GetSiblingIndex() > 0)
		{
			if (Selection.activeTransform.parent != null)
			{
				Selection.activeTransform = Selection.activeTransform.parent.GetChild(Selection.activeTransform.GetSiblingIndex() - 1);
			}
			else
			{

			}
		}
		else
		{
			Transform c = null;
			Transform t = Selection.activeTransform;
			for (int d = 0; d < 3; d++)
			{
				c = PrevNodeAtSameLevel(t.parent, d + 1, t.GetSiblingIndex() - 1);
				if (c == null && t.parent != null)
					t = t.parent;
				else
					break;
			}
			if (c != null)
				Selection.activeTransform = c;
		}
	}

	// Select Next Object
	[MenuItem("AshTools/Next Same Level Object (d=3) &]", true, 22)]
	static bool ValidateSelectNextGameObject()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Next Same Level Object (d=3) &]", false, 22)]
	static void SelectNextGameObject()
	{
		if (Selection.activeTransform.GetSiblingIndex() < Selection.activeTransform.parent.childCount - 1)
			Selection.activeTransform = Selection.activeTransform.parent.GetChild(Selection.activeTransform.GetSiblingIndex() + 1);
		else
		{
			Transform c = null;
			Transform t = Selection.activeTransform;
			for (int d = 0; d < 3; d++)
			{
				c = NextNodeAtSameLevel(t.parent, d + 1, t.GetSiblingIndex() + 1);
				if (c == null && t.parent != null)
					t = t.parent;
				else
					break;
			}
			if (c != null)
				Selection.activeTransform = c;
		}
	}



	// Move Select Object To Cursor 
	[MenuItem("AshTools/Move Selected to Cursor &MO", true, 33)]
	static bool ValidateMoveGameObjectToCursor()
	{
		return Selection.activeTransform != null && useShortcut && Camera.current != null;
	}
	[MenuItem("AshTools/Move Selected to Cursor &MO", false, 33)]
	static void MoveGameObjectToCursor()
	{
		Vector3 pos = GetWorldPos(lastMPos);
		pos.z = Selection.activeTransform.position.z;
		Selection.activeTransform.position = pos;
		SceneView.RepaintAll();
	}


	/*
	// Save Changes to Prefab of all Selected Objects
	[MenuItem("AshTools/Save Changes to Prefabs &SP", true, 44)]
	static bool ValidateSaveChangesToPrefab()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Save Changes to Prefabs &SP", false, 44)]
	static void SaveChangesToPrefab()
	{
		for (int i = 0; i < Selection.objects.Length; i++) {
			PrefabUtility.ReplacePrefab ((GameObject)Selection.objects [i], 
				PrefabUtility.GetPrefabParent (Selection.objects [i]),
				ReplacePrefabOptions.ConnectToPrefab);
		}
	}
	// Save Changes to Prefab of all Selected Objects
	[MenuItem("AshTools/Revert Changes to Prefabs &RP", true, 44)]
	static bool ValidateRevertChangesToPrefab()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Revert Changes to Prefabs &RP", false, 44)]
	static void RevertChangesToPrefab()
	{
		for (int i = 0; i < Selection.objects.Length; i++) {
			PrefabUtility.ResetToPrefabState (Selection.objects [i]);
		}
	}
	// Disconnect from Prefab of all Selected Objects
	[MenuItem("AshTools/Disconnect from Prefabs &DC", true, 44)]
	static bool ValidateDisconnectFromPrefab()
	{
		return Selection.activeTransform != null && useShortcut;
	}
	[MenuItem("AshTools/Disconnect from Prefabs &DC", false, 44)]
	static void DisconnectFromPrefab()
	{
		for (int i = 0; i < Selection.objects.Length; i++) {
			PrefabUtility.DisconnectPrefabInstance(Selection.objects [i]);
		}
	}
    */


	// Select Parent Object (Alt + R)
	[MenuItem("AshTools/Selected Count &c", true, 0)]
	static bool ValidateSelecedCount()
	{
		return Selection.objects != null && useShortcut;
	}
	[MenuItem("AshTools/Selected Count &c", false, 0)]
	static void SelecedCount()
	{
		if (Selection.objects != null)
		{
			Debug.Log("Selected Count: " + Selection.objects.Length);
		}
	}
}
#endif
