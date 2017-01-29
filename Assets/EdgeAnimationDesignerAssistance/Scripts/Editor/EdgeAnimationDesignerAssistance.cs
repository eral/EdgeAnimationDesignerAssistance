// (C) 2016 ERAL
// Distributed under the Boost Software License, Version 1.0.
// (See copy at http://www.boost.org/LICENSE_1_0.txt)

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EdgeAnimationDesignerAssistance {
	public class EdgeAnimationDesignerAssistance {
		public static string GetAssetPathFromEdgeAnimationPath(string path) {
			if (!string.IsNullOrEmpty(path) && path.EndsWith(".anm")) {
				path = path.Substring(0, path.Length - 4) + ".asset";
			} else {
				throw new System.ArgumentException();
			}
			return path;
		}

		public static string GetEdgeAnimationPathFromAssetPath(string path) {
			if (!string.IsNullOrEmpty(path) && path.EndsWith(".asset")) {
				path = path.Substring(0, path.Length - 6) + ".anm";
			} else {
				throw new System.ArgumentException();
			}
			return path;
		}

		public static void Import(string srcPath, string dstPath) {
			var importer = new Importer();
			importer.Import(srcPath, dstPath);
		}

		public class Importer {
			public void Import(string srcPath, string dstPath) {
				this.srcPath = srcPath;
				this.dstPath = dstPath;
				edgeAnm = EdgeAnimation.FromFile(Application.dataPath + "/../" + this.srcPath);
				textureCache = new Dictionary<string, Texture2D>();
				spriteCache = new Dictionary<string, List<Sprite>>();
				clipCache = new List<AnimationClip>();

				CreateConfig();
				CreateSprite();
				CreateAnimationClip();

				GarbageCollection();

				AssetDatabase.ImportAsset(dstPath);
				textureCache = null;
				spriteCache = null;
				clipCache = null;
			}

			private string srcPath;
			private string dstPath;
			private EdgeAnimation edgeAnm;
			private Config config;
			private Dictionary<string, Texture2D> textureCache;
			private Dictionary<string, List<Sprite>> spriteCache;
			private List<AnimationClip> clipCache;

			private void CreateConfig() {
				if (IsExistAsset(dstPath)) {
					config = AssetDatabase.LoadAssetAtPath<Config>(dstPath);
				} else {
					config = ScriptableObject.CreateInstance<Config>();
					AssetDatabase.CreateAsset(config, dstPath);
				}
				config.source = AssetDatabase.LoadAssetAtPath<DefaultAsset>(srcPath);
			}

			private static bool IsExistAsset(string assetPath) {
				return AssetDatabase.GenerateUniqueAssetPath(assetPath) != assetPath;
			}

			private void CreateSprite() {
				foreach (var pattern in edgeAnm.patterns) {
					for (int i = 0, iMax = pattern.frames.Count; i < iMax; ++i) {
						var frame = pattern.frames[i];

						var texture = LoadTexture(frame.filename);
						var rect = new Rect(frame.srcX, texture.height - frame.srcY - frame.height, frame.width, frame.height);
						var offset = new Vector2(frame.destX / (float)frame.width, -frame.destY / (float)frame.height);
						var pivot = config.anchor - offset;
						var sprite = Sprite.Create(texture
												, rect
												, pivot
												, config.pixelsPerUnit
												, config.extrude
												, config.meshType
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
				var searchInFolders = new[]{Path.GetDirectoryName(this.srcPath)};

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
				var asset = AssetDatabase.LoadAllAssetRepresentationsAtPath(dstPath)
										.Where(x=>x.GetType() == typeof(Sprite))
										.Where(x=>x.name == sprite.name)
										.Select(x=>(Sprite)x)
										.FirstOrDefault();
				if (asset != null) {
					EditorUtility.CopySerialized(sprite, asset);
					sprite = asset;
				} else {
					AssetDatabase.AddObjectToAsset(sprite, config);
				}

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

					clip.frameRate = config.frameRate;
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
				var asset = AssetDatabase.LoadAllAssetRepresentationsAtPath(dstPath)
										.Where(x=>x.GetType() == typeof(AnimationClip))
										.Where(x=>x.name == clip.name)
										.Select(x=>(AnimationClip)x)
										.FirstOrDefault();
				if (asset != null) {
					EditorUtility.CopySerialized(clip, asset);
					clip = asset;
				} else {
					AssetDatabase.AddObjectToAsset(clip, config);
				}

				clipCache.Add(clip);
			}

			private void GarbageCollection() {
				var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(dstPath);

				var clips = assets.Where(x=>x.GetType() == typeof(AnimationClip))
									.Select(x=>(AnimationClip)x)
									.ToList();
				var removeclips = clips.Except(clipCache);
				foreach (var removeclip in removeclips) {
					Editor.DestroyImmediate(removeclip, true);
				}

				var sprites = assets.Where(x=>x.GetType() == typeof(Sprite))
									.Select(x=>(Sprite)x)
									.ToList();
				var removeSprites = sprites.Except(spriteCache.SelectMany(x=>x.Value));
				foreach (var removeSprite in removeSprites) {
					Editor.DestroyImmediate(removeSprite, true);
				}
			}
		}
	}
}
