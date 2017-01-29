﻿// (C) 2016 ERAL
// Distributed under the Boost Software License, Version 1.0.
// (See copy at http://www.boost.org/LICENSE_1_0.txt)

using UnityEngine;
#if UNITY_EDITOR
	using UnityEditor;
#endif

namespace EdgeAnimationDesignerAssistance {
	public class Config : ScriptableObject {
#if UNITY_EDITOR
		public DefaultAsset source;
		public int frameRate = 60;
		public Vector2 anchor = new Vector2(0.5f, 0.5f);
		public float pixelsPerUnit = 100.0f;
		public uint extrude = 0;
		public SpriteMeshType meshType = SpriteMeshType.Tight;
#endif
	}
}
