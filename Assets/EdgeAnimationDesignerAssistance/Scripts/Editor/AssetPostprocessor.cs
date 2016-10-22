using UnityEditor;

namespace EdgeAnimationDesignerAssistance {
	public class AssetPostprocessor : UnityEditor.AssetPostprocessor {
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
			foreach (var importedAsset in importedAssets) {
				if (IsEdgeAnimation(importedAsset)) {
					var dstPath = EdgeAnimationDesignerAssistance.GetAssetPathFromEdgeAnimationPath(importedAsset);
					EdgeAnimationDesignerAssistance.Import(importedAsset, dstPath);
				}
			}
		}

		private static bool IsEdgeAnimation(string assetPath) {
			return assetPath.EndsWith(".anm");
		}
	}
}
