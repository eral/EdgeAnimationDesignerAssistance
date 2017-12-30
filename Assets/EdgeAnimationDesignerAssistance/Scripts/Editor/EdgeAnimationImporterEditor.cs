// (C) 2016 ERAL
// Distributed under the Boost Software License, Version 1.0.
// (See copy at http://www.boost.org/LICENSE_1_0.txt)

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

namespace EdgeAnimationDesignerAssistance {
	[CustomEditor(typeof(EdgeAnimationImporter))]
	public class EdgeAnimationImporterEditor : ScriptedImporterEditor {
		private SerializedProperty m_FrameRate;
		private GUIContent m_FrameRateContent;
		private SerializedProperty m_Anchor;
		private GUIContent m_AnchorContent;
		private SerializedProperty m_PixelsPerUnit;
		private GUIContent m_PixelsPerUnitContent;
		private SerializedProperty m_Extrude;
		private GUIContent m_ExtrudeContent;
		private SerializedProperty m_MeshType;
		private SerializedProperty m_UserData;
		private GUIContent m_MeshTypeContent;

		public override void OnEnable() {
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
			m_UserData = serializedObject.FindProperty("m_UserData");
		}

		protected override void Apply() {
			var settings = new EdgeAnimationImportSettings{
									frameRate = m_FrameRate.intValue,
									anchor = m_Anchor.vector2Value,
									pixelsPerUnit = m_PixelsPerUnit.floatValue,
									extrude = (uint)m_Extrude.intValue,
									meshType = (SpriteMeshType)m_MeshType.intValue,
								};
			m_UserData.stringValue = JsonUtility.ToJson(settings);
			serializedObject.ApplyModifiedProperties();
		}

		public override void OnInspectorGUI() {
//			serializedObject.Update();
			EditorGUILayout.PropertyField(m_FrameRate, m_FrameRateContent);
			EditorGUILayout.PropertyField(m_Anchor, m_AnchorContent);
			EditorGUILayout.PropertyField(m_PixelsPerUnit, m_PixelsPerUnitContent);
			EditorGUILayout.PropertyField(m_Extrude, m_ExtrudeContent);
			EditorGUILayout.PropertyField(m_MeshType, m_MeshTypeContent);
			ApplyRevertGUI();
		}
	}
}
