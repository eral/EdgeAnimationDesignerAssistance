﻿// (C) 2016 ERAL
// Distributed under the Boost Software License, Version 1.0.
// (See copy at http://www.boost.org/LICENSE_1_0.txt)

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Experimental.AssetImporters;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace EdgeAnimationDesignerAssistance {
	[ScriptedImporter(1, "anm")]
	public class EdgeAnimationImporter : ScriptedImporter {
		public int frameRate {get{return m_FrameRate;} set{m_FrameRate = value;}}
		public Vector2 anchor {get{return m_Anchor;} set{m_Anchor = value;}}
		public float pixelsPerUnit {get{return m_PixelsPerUnit;} set{m_PixelsPerUnit = value;}}
		public uint extrude {get{return m_Extrude;} set{m_Extrude = value;}}
		public SpriteMeshType meshType {get{return m_MeshType;} set{m_MeshType = value;}}

		public override void OnImportAsset(AssetImportContext ctx) {
			m_Ctx = ctx;
			m_EdgeAnm = EdgeAnimation.FromFile(Application.dataPath + "/../" + m_Ctx.assetPath);
			m_TextureCache = new Dictionary<string, Texture2D>();
			m_SpriteCache = new Dictionary<string, List<Sprite>>();
			m_ClipCache = new List<AnimationClip>();

			CreateSprite();
			CreateAnimationClip();
			CreateAnimatorController();
			CreateMainObject();

			m_TextureCache = null;
			m_SpriteCache = null;
			m_ClipCache = null;
			m_ControllerCache = null;
		}

		private static readonly Vector3 kStateCenterPositon = new Vector3(408.0f, 96.0f, 0.0f);
		private const float kStateHeight = 48.0f;

		[SerializeField]
		private int m_FrameRate = 60;

		[SerializeField]
		private Vector2 m_Anchor = new Vector2(0.5f, 0.5f);

		[SerializeField]
		private float m_PixelsPerUnit = 100.0f;

		[SerializeField]
		private uint m_Extrude = 0;

		[SerializeField]
		private SpriteMeshType m_MeshType = SpriteMeshType.Tight;

		[System.NonSerialized]
		private AssetImportContext m_Ctx;

		[System.NonSerialized]
		private EdgeAnimation m_EdgeAnm;

		[System.NonSerialized]
		private Dictionary<string, Texture2D> m_TextureCache;

		[System.NonSerialized]
		private Dictionary<string, List<Sprite>> m_SpriteCache;

		[System.NonSerialized]
		private List<AnimationClip> m_ClipCache;

		[System.NonSerialized]
		private AnimatorController m_ControllerCache;

		private void CreateSprite() {
			foreach (var pattern in m_EdgeAnm.patterns) {
				for (int i = 0, iMax = pattern.frames.Count; i < iMax; ++i) {
					var frame = pattern.frames[i];

					var texture = LoadTexture(frame.filename);
					var rect = new Rect(frame.srcX, texture.height - frame.srcY - frame.height, frame.width, frame.height);
					var offset = new Vector2(frame.destX / (float)frame.width, -frame.destY / (float)frame.height);
					var pivot = m_Anchor - offset;
					var sprite = Sprite.Create(texture
											, rect
											, pivot
											, m_PixelsPerUnit
											, m_Extrude
											, m_MeshType
											);

					sprite.name = pattern.name + "#" + (i + 1);
					SaveAsset(sprite, pattern.name, i);
				}
			}
		}

		private static Vector2 ToVector2(EdgeAnimation.IntVector2 intVector2) {
			return new Vector2(intVector2.x, intVector2.y);
		}

		private Texture2D LoadTexture(string path) {
			if (m_TextureCache.ContainsKey(path)) {
				return m_TextureCache[path];
			}

			var filter = path.Replace('\\', '/')
								.Substring(0, path.Length - Path.GetExtension(path).Length);
			var searchInFolders = new[]{Path.GetDirectoryName(m_Ctx.assetPath)};

			while (true) {
				while (true) {
					var findPaths = AssetDatabase.FindAssets(filter, searchInFolders);
					if (0 < findPaths.Length) {
						var assets = findPaths.Select(x=>AssetDatabase.GUIDToAssetPath(x))
											.Select(x=>AssetDatabase.LoadAssetAtPath<Texture2D>(x))
											.Where(x=>x != null)
											.ToArray();
						var result = assets.FirstOrDefault();
						m_TextureCache.Add(path, result);
						return result;
					}

					var delimiterIndex = filter.IndexOf('/');
					if (delimiterIndex == -1) {
						break;
					}
					filter = filter.Substring(delimiterIndex + 1, filter.Length - delimiterIndex - 1);
				}

				if (string.IsNullOrEmpty(searchInFolders[0])) {
					break;
				}
				searchInFolders[0] = Path.GetDirectoryName(searchInFolders[0]);
			}
			return null;
		}

		private Sprite LoadSprite(string pattern, int frameIndex) {
			if (!m_SpriteCache.ContainsKey(pattern)) {
				throw new System.ArgumentOutOfRangeException();
			}
			var sprites = m_SpriteCache[pattern];
			if (sprites.Count <= frameIndex) {
				throw new System.IndexOutOfRangeException();
			}
			return sprites[frameIndex];
		}

		private void CreateAnimationClip() {
			foreach (var pattern in m_EdgeAnm.patterns) {
				var clip = new AnimationClip();

				var keyframes = new ObjectReferenceKeyframe[pattern.frames.Count];
				var totalDelay = 0;
				for (int i = 0, iMax = pattern.frames.Count; i < iMax; ++i) {
					var frame = pattern.frames[i];

					var keyframe = new ObjectReferenceKeyframe();
					keyframe.time = totalDelay * 0.001f;
					keyframe.value = LoadSprite(pattern.name, i);
					keyframes[i] = keyframe;

					totalDelay += frame.delay;
				}
				AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keyframes);

				clip.frameRate = m_FrameRate;
				var settings = new AnimationClipSettings();
				settings.loopTime = true;
				settings.stopTime = totalDelay * 0.001f;
				settings.keepOriginalPositionY = true;
				AnimationUtility.SetAnimationClipSettings(clip, settings);

				clip.name = pattern.name;
				SaveAsset(clip);
			}
		}

		private void CreateAnimatorController() {
			var controller = new AnimatorController();
			controller.name = "Controller";
			controller.AddParameter("Pattern", AnimatorControllerParameterType.Int);

			controller.AddLayer("Base Layer");
			var stateMachine = controller.layers[0].stateMachine;
			SaveAsset(stateMachine);
			
			var statePositon = kStateCenterPositon;
			statePositon.y -= kStateHeight * 0.5f * m_ClipCache.Count;
			for (int i = 0, iMax = m_ClipCache.Count; i < iMax; ++i) {
				var clip = m_ClipCache[i];
				var state = stateMachine.AddState(clip.name + " State", statePositon);
				state.motion = clip;
				SaveAsset(state);

				var transition = stateMachine.AddAnyStateTransition(state);
				transition.name = clip.name + " Transition";
				transition.AddCondition(AnimatorConditionMode.Equals, i, "Pattern");
				transition.duration = 0.0f;
				transition.offset = 0.0f;
				transition.exitTime = 1.0f;
				transition.canTransitionToSelf = false;
				SaveAsset(transition);

				statePositon.y += kStateHeight;
			}
			SaveAsset(controller);
		}

		private void CreateMainObject() {
			var go = new GameObject(string.Empty, typeof(SpriteRenderer), typeof(Animator));
			var spriteRenderer = go.GetComponent<SpriteRenderer>();
			spriteRenderer.sprite = m_SpriteCache[m_EdgeAnm.patterns[0].name][0];
			var animator = go.GetComponent<Animator>();
			animator.runtimeAnimatorController = m_ControllerCache;

			SaveAsset(go);
			m_Ctx.SetMainObject(go);
		}

		private void SaveAsset(Object asset) {
			asset.hideFlags |= HideFlags.NotEditable;
			m_Ctx.AddObjectToAsset(asset.name, asset);
		}

		private void SaveAsset(Sprite sprite, string patternName, int frameIndex) {
			SaveAsset(sprite);

			List<Sprite> sprites;
			if (m_SpriteCache.ContainsKey(patternName)) {
				sprites = m_SpriteCache[patternName];
			} else {
				sprites = new List<Sprite>();
				m_SpriteCache.Add(patternName, sprites);
			}
			if (frameIndex < sprites.Count) {
				sprites[frameIndex] = sprite;
			} else if (frameIndex == sprites.Count) {
				sprites.Add(sprite);
			} else {
				throw new System.IndexOutOfRangeException();
			}
		}

		private void SaveAsset(AnimationClip clip) {
			SaveAsset((Object)clip);
			m_ClipCache.Add(clip);
		}

		private void SaveAsset(AnimatorController controller) {
			SaveAsset((Object)controller);
			m_ControllerCache = controller;
		}
	}
}
