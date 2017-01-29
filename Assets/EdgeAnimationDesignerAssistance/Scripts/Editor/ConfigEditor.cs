using UnityEngine;
using UnityEditor;

namespace EdgeAnimationDesignerAssistance {
	[CustomEditor(typeof(Config))]
	public class ConfigEditor : Editor {
		private bool m_Dirty;
		private SerializedProperty m_Source;
		private GUIContent m_SourceContent;
		private SerializedProperty m_FrameRate;
		private GUIContent m_FrameRateContent;
		private SerializedProperty m_Anchor;
		private GUIContent m_AnchorContent;
		private SerializedProperty m_PixelsPerUnit;
		private GUIContent m_PixelsPerUnitContent;
		private SerializedProperty m_Extrude;
		private GUIContent m_ExtrudeContent;
		private SerializedProperty m_MeshType;
		private GUIContent m_MeshTypeContent;
		private GUIContent m_RevertButtonContent;
		private GUIContent m_ApplyButtonContent;

		protected virtual void OnEnable() {
			m_Dirty = false;
			m_SourceContent = new GUIContent("Source");
			m_Source = serializedObject.FindProperty("source");
			m_FrameRateContent = new GUIContent("Frame Rate");
			m_FrameRate = serializedObject.FindProperty("frameRate");
			m_AnchorContent = new GUIContent("Anchor");
			m_Anchor = serializedObject.FindProperty("anchor");
			m_PixelsPerUnitContent = new GUIContent("Pixels Per Unit");
			m_PixelsPerUnit = serializedObject.FindProperty("pixelsPerUnit");
			m_ExtrudeContent = new GUIContent("Extrude");
			m_Extrude = serializedObject.FindProperty("extrude");
			m_MeshTypeContent = new GUIContent("Mesh Type");
			m_MeshType = serializedObject.FindProperty("meshType");
			m_ApplyButtonContent = new GUIContent("Apply");
			m_RevertButtonContent = new GUIContent("Revert");
		}

		public override void OnInspectorGUI() {
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_Source, m_SourceContent);
			EditorGUILayout.PropertyField(m_FrameRate, m_FrameRateContent);
			EditorGUILayout.PropertyField(m_Anchor, m_AnchorContent);
			EditorGUILayout.PropertyField(m_PixelsPerUnit, m_PixelsPerUnitContent);
			EditorGUILayout.PropertyField(m_Extrude, m_ExtrudeContent);
			EditorGUILayout.PropertyField(m_MeshType, m_MeshTypeContent);
			if (EditorGUI.EndChangeCheck()) {
				m_Dirty = true;
			}

			var canApply = CanApply();

			using (new EditorGUILayout.HorizontalScope()) {
				var OldGUIEnabled = GUI.enabled;

				GUI.enabled = m_Dirty;
				if (GUILayout.Button(m_RevertButtonContent, EditorStyles.miniButtonLeft)) {
					Revert();
				}
				GUI.enabled = m_Dirty && canApply;
				if (GUILayout.Button(m_ApplyButtonContent, EditorStyles.miniButtonRight)) {
					Apply();
				}

				GUI.enabled = OldGUIEnabled;
			}
			if (!canApply) {
				EditorGUILayout.HelpBox("Source animation data not found", MessageType.Error, true);
			}
		}

		private bool CanApply() {
			var result = false;
			result = result || (m_Source.objectReferenceValue != null);
			if (!result) {
				var dstPath = AssetDatabase.GetAssetPath(serializedObject.targetObject);
				var srcPath = EdgeAnimationDesignerAssistance.GetEdgeAnimationPathFromAssetPath(dstPath);
				if (IsExistAsset(srcPath)) {
					result = true;
				}
			}
			return result;
		}

		private static bool IsExistAsset(string assetPath) {
			return AssetDatabase.GenerateUniqueAssetPath(assetPath) != assetPath;
		}

		private void Apply() {
			serializedObject.ApplyModifiedProperties();

			var dstPath = AssetDatabase.GetAssetPath(serializedObject.targetObject);
			var srcPath = AssetDatabase.GetAssetPath(m_Source.objectReferenceValue);
			if (string.IsNullOrEmpty(srcPath)) {
				srcPath = EdgeAnimationDesignerAssistance.GetEdgeAnimationPathFromAssetPath(dstPath);
			}
			EdgeAnimationDesignerAssistance.Import(srcPath, dstPath);
		}

		private void Revert() {
			serializedObject.Update();
		}
	}
}
