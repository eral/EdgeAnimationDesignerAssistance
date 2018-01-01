﻿// (C) 2016 ERAL
// Distributed under the Boost Software License, Version 1.0.
// (See copy at http://www.boost.org/LICENSE_1_0.txt)

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.Linq;

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
		private GUIContent m_MeshTypeContent;
		private GUIContent[] m_MeshTypeDisplayedOptions;
		private int[] m_MeshTypeOptionValues;

		public override void OnEnable() {
			m_FrameRateContent = new GUIContent("Frame Rate");
			m_FrameRate = serializedObject.FindProperty("m_FrameRate");
			m_AnchorContent = new GUIContent("Anchor");
			m_Anchor = serializedObject.FindProperty("m_Anchor");
			m_PixelsPerUnitContent = new GUIContent("Pixels Per Unit");
			m_PixelsPerUnit = serializedObject.FindProperty("m_PixelsPerUnit");
			m_ExtrudeContent = new GUIContent("Extrude");
			m_Extrude = serializedObject.FindProperty("m_Extrude");
			m_MeshTypeContent = new GUIContent("Mesh Type");
			m_MeshType = serializedObject.FindProperty("m_MeshType");
			m_MeshTypeDisplayedOptions = System.Enum.GetNames(typeof(SpriteMeshType)).Select(x=>new GUIContent(x)).ToArray();
			m_MeshTypeOptionValues = (int[])System.Enum.GetValues(typeof(SpriteMeshType));
		}

		public override void OnInspectorGUI() {
			EditorGUILayout.PropertyField(m_FrameRate, m_FrameRateContent);
			EditorGUILayout.PropertyField(m_Anchor, m_AnchorContent);
			EditorGUILayout.PropertyField(m_PixelsPerUnit, m_PixelsPerUnitContent);
			EditorGUILayout.PropertyField(m_Extrude, m_ExtrudeContent);
			EditorGUILayout.IntPopup(m_MeshType, m_MeshTypeDisplayedOptions, m_MeshTypeOptionValues, m_MeshTypeContent);
			ApplyRevertGUI();
		}
	}
}
