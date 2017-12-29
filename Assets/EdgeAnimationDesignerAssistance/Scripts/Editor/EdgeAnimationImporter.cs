// (C) 2016 ERAL
// Distributed under the Boost Software License, Version 1.0.
// (See copy at http://www.boost.org/LICENSE_1_0.txt)

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace EdgeAnimationDesignerAssistance {
	[ScriptedImporter(1, "anm")]
	public class EdgeAnimationImporter : ScriptedImporter {
		public int frameRate = 60;
		public Vector2 anchor = new Vector2(0.5f, 0.5f);
		public float pixelsPerUnit = 100.0f;
		public uint extrude = 0;
		public SpriteMeshType meshType = SpriteMeshType.Tight;

		public override void OnImportAsset(AssetImportContext ctx) {
			this.ctx = ctx;
			edgeAnm = EdgeAnimation.FromFile(Application.dataPath + "/../" + this.ctx.assetPath);
			textureCache = new Dictionary<string, Texture2D>();
			spriteCache = new Dictionary<string, List<Sprite>>();
			clipCache = new List<AnimationClip>();

			SetImportSettings();
			CreateSprite();
			CreateAnimationClip();

			textureCache = null;
			spriteCache = null;
			clipCache = null;
		}

		private AssetImportContext ctx;
		private EdgeAnimation edgeAnm;
		private Dictionary<string, Texture2D> textureCache;
		private Dictionary<string, List<Sprite>> spriteCache;
		private List<AnimationClip> clipCache;

		private void SetImportSettings() {
			EdgeAnimationImportSettings settings;
			if (string.IsNullOrEmpty(userData)) {
				settings = new EdgeAnimationImportSettings();
			} else {
				settings = JsonUtility.FromJson<EdgeAnimationImportSettings>(userData);
			}
			frameRate = settings.frameRate;
			anchor = settings.anchor;
			pixelsPerUnit = settings.pixelsPerUnit;
			extrude = settings.extrude;
			meshType = settings.meshType;
		}

		private void CreateSprite() {
			foreach (var pattern in edgeAnm.patterns) {
				for (int i = 0, iMax = pattern.frames.Count; i < iMax; ++i) {
					var frame = pattern.frames[i];

					var texture = LoadTexture(frame.filename);
					var rect = new Rect(frame.srcX, texture.height - frame.srcY - frame.height, frame.width, frame.height);
					var offset = new Vector2(frame.destX / (float)frame.width, -frame.destY / (float)frame.height);
					var pivot = this.anchor - offset;
					var sprite = Sprite.Create(texture
											, rect
											, pivot
											, this.pixelsPerUnit
											, this.extrude
											, this.meshType
											);

					sprite.name = pattern.name + "#" + (i + 1);
					SaveSprite(sprite, pattern.name, i);
				}
			}
		}

		private static Vector2 ToVector2(EdgeAnimation.IntVector2 intVector2) {
			return new Vector2(intVector2.x, intVector2.y);
		}

		private Texture2D LoadTexture(string path) {
			if (textureCache.ContainsKey(path)) {
				return textureCache[path];
			}

			var filter = path.Replace('\\', '/')
								.Substring(0, path.Length - Path.GetExtension(path).Length);
			var searchInFolders = new[]{Path.GetDirectoryName(ctx.assetPath)};

			while (true) {
				while (true) {
					var findPaths = AssetDatabase.FindAssets(filter, searchInFolders);
					if (0 < findPaths.Length) {
						var assets = findPaths.Select(x=>AssetDatabase.GUIDToAssetPath(x))
											.Select(x=>AssetDatabase.LoadAssetAtPath<Texture2D>(x))
											.Where(x=>x != null)
											.ToArray();
						var result = assets.FirstOrDefault();
						textureCache.Add(path, result);
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

		private void SaveSprite(Sprite sprite, string patternName, int frameIndex) {
			sprite.hideFlags |= HideFlags.NotEditable;
			ctx.AddObjectToAsset(sprite.name, sprite);

			List<Sprite> sprites;
			if (spriteCache.ContainsKey(patternName)) {
				sprites = spriteCache[patternName];
			} else {
				sprites = new List<Sprite>();
				spriteCache.Add(patternName, sprites);
			}
			if (frameIndex < sprites.Count) {
				sprites[frameIndex] = sprite;
			} else if (frameIndex == sprites.Count) {
				sprites.Add(sprite);
			} else {
				throw new System.IndexOutOfRangeException();
			}
		}

		private Sprite LoadSprite(string pattern, int frameIndex) {
			if (!spriteCache.ContainsKey(pattern)) {
				throw new System.ArgumentOutOfRangeException();
			}
			var sprites = spriteCache[pattern];
			if (sprites.Count <= frameIndex) {
				throw new System.IndexOutOfRangeException();
			}
			return sprites[frameIndex];
		}

		private void CreateAnimationClip() {
			foreach (var pattern in edgeAnm.patterns) {
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

				clip.frameRate = this.frameRate;
				var settings = new AnimationClipSettings();
				settings.loopTime = true;
				settings.stopTime = totalDelay * 0.001f;
				settings.keepOriginalPositionY = true;
				AnimationUtility.SetAnimationClipSettings(clip, settings);

				clip.name = pattern.name;
				SaveAnimationClip(clip, pattern.name);
			}
		}

		private void SaveAnimationClip(AnimationClip clip, string patternName) {
			clip.hideFlags |= HideFlags.NotEditable;
			ctx.AddObjectToAsset(clip.name, clip);
			clipCache.Add(clip);
		}
	}
}
