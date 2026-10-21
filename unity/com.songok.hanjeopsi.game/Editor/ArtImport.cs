using UnityEditor;
using UnityEngine;

namespace Hanjeopsi.EditorTools
{
    /// <summary>
    /// Assets/Resources/Art 아래 그림은 스프라이트로 가져온다.
    /// 2의 거듭제곱이 아닌 크기도 늘이지 않고, 피벗은 발 위치(아래 가운데).
    /// </summary>
    public class ArtImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 2048;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
        }
    }
}
