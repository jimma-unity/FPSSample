#if (NET_4_6 || NET_STANDARD_2_0)

using System.IO;

using UnityEngine;

namespace Unity.Properties.Codegen
{
    [UnityEditor.AssetImporters.ScriptedImporter(3, new[] {".properties"})]
    public class SchemaImporter : UnityEditor.AssetImporters.ScriptedImporter
    {
        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
        {
            // Generate a dummy object to satisfy the asset pipeline
            var asset = ScriptableObject.CreateInstance<SchemaObject>();
            ctx.AddObjectToAsset("asset", asset);

            asset.JsonSchema = File.ReadAllText(ctx.assetPath);

            // Asset importer expects ONE and ONLY ONE call to `SetMainObject`
            ctx.SetMainObject(asset);
        }
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)