using UnityEngine;
using UnityEditor;

namespace OHTools
{
    public class OHTextureImporterOverride : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            // 当不想导入的素材被修改时，临时禁用
            // return;
        
            TextureImporter textureImporter = (TextureImporter)assetImporter;
    
            // 仅在“导入设置缺失”时（即首次导入）才执行覆盖逻辑
            if (!textureImporter.importSettingsMissing)
            {
                // 如果已有导入配置，则跳过后续的强制修改
                return;
            }
    
            // ——首次导入时的强制配置——
    
            // 将纹理类型设置为 Sprite
            textureImporter.textureType = TextureImporterType.Sprite;
    
            // 使用 Bilinear 过滤
            textureImporter.filterMode = FilterMode.Bilinear;
    
            // 指定 Sprite 的像素单位
            textureImporter.spritePixelsPerUnit = 100f;
    
            // 禁用压缩（无损保存）
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
    
            // 使纹理可读写
            textureImporter.isReadable = true;
        }
    }
}
