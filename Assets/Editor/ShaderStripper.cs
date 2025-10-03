//#define SHADER_COMPILATION_LOGGING
//#define SKIP_SHADER_COMPILATION

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class ShaderStripper : IPreprocessShaders
{
	class ShaderVariantRule
	{
		public ShaderVariantRule(string shaderName, string passName, string stageName, List<string> requiredKeywords)
		{
			this.shaderName = shaderName;
			this.passName = passName;
			this.stageName = stageName;
			this.requiredKeywords = requiredKeywords;
		}

		public string shaderName;
		public string passName;
		public string stageName;
		public List<string> requiredKeywords;
	}
	
	private const string LOG_FILE_PATH = "Library/Shader Compilation Results.txt";

	private static readonly ShaderKeyword[] SKIPPED_VARIANTS = new ShaderKeyword[]
	{
		new ShaderKeyword( "DIRECTIONAL_COOKIE" ),
		new ShaderKeyword( "POINT_COOKIE" ),
		//new ShaderKeyword( "LIGHTPROBE_SH" ), // Apparently used by lightmapping, as well
	};

	public int callbackOrder { get { return 0; } }

	List<ShaderVariantRule> myVariantRules = LoadRules();

	private static readonly HashSet<string> ForwardPassNames = new HashSet<string>
    {
        "Forward",
        "ForwardOnly",
        "ForwardUnlit"
    };

	bool ShouldKeepVariant(string shaderName, string passName, ShaderKeywordSet keywords)
	{
		var enabledKeywordStrings = keywords.GetShaderKeywords().Select(k => k.name).ToList();
		
		foreach (var rule in myVariantRules)
		{
			if (rule.shaderName == shaderName /*&& rule.PassName == passName*/)
			{
				bool matches = rule.requiredKeywords.All(k => enabledKeywordStrings.Contains(k));
				if (matches)
					return true;
			}
		}
		return false;
	}

    private static List<ShaderVariantRule> LoadRules()
    {
        var rules = new List<ShaderVariantRule>
        {
            new("Deferred_Indirect_Fptl_Variant2", "TextMeshPro/Distance Field (instance 0x1DE)", "<Unnamed Pass 0>",
                new List<string>
                {
                    "vertex",
                }),
            new("Emissive/GlowingCore", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("Emissive/GlowingCore", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("Firstperson_Projection_Additive", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("Firstperson_Projection_Additive", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("Firstperson_Projection_SSS", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("Firstperson_Projection_SSS", "ShadowCaster", "pixel", new List<string>
            {
                "_DOUBLESIDED_ON",
            }),
            new("Firstperson_Projection_SSS", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("Firstperson_Projection_SSS", "ShadowCaster", "vertex", new List<string>
            {
                "_DOUBLESIDED_ON",
            }),
            new("Firstperson_Projection_Transparent", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("Firstperson_Projection_Transparent", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("Firstperson_Projection", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("Firstperson_Projection", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_TRIPLANAR0",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_HEIGHTMAP0",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_TRIPLANAR0",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_HEIGHTMAP0",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_TRIPLANAR0",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR0",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_TRIPLANAR0",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_TESSELLATION_DISPLACEMENT",
            }),
            new("HDRP/LayeredLit", "DepthOnly", "pixel", new List<string>
            {
                "_BENTNORMALMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_MASKMAP0",
                "_MASKMAP1",
                "_MASKMAP2",
                "_MASKMAP3",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
                "_NORMALMAP_TANGENT_SPACE0",
                "_NORMALMAP_TANGENT_SPACE1",
                "_NORMALMAP_TANGENT_SPACE2",
                "_NORMALMAP_TANGENT_SPACE3",
            }),
            new("HDRP/LayeredLit", "DepthOnly", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_MASKMAP0",
                "_MASKMAP1",
                "_MASKMAP2",
                "_MASKMAP3",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
                "_NORMALMAP_TANGENT_SPACE0",
                "_NORMALMAP_TANGENT_SPACE1",
                "_NORMALMAP_TANGENT_SPACE2",
                "_NORMALMAP_TANGENT_SPACE3",
            }),
            new("HDRP/LayeredLit", "DepthOnly", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
            }),
            new("HDRP/LayeredLit", "DepthOnly", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
            }),
            new("HDRP/LayeredLit", "GBuffer", "pixel", new List<string>
            {
                "DECALS_3RT",
                "SHADOWS_SHADOWMASK",
                "_BENTNORMALMAP0",
                "_EMISSIVE_COLOR_MAP",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_MASKMAP0",
                "_MASKMAP1",
                "_MASKMAP2",
                "_MASKMAP3",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
                "_NORMALMAP_TANGENT_SPACE0",
                "_NORMALMAP_TANGENT_SPACE1",
                "_NORMALMAP_TANGENT_SPACE2",
                "_NORMALMAP_TANGENT_SPACE3",
            }),
            new("HDRP/LayeredLit", "GBuffer", "pixel", new List<string>
            {
                "DECALS_3RT",
                "SHADOWS_SHADOWMASK",
                "_DETAIL_MAP2",
                "_DETAIL_MAP3",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_MASKMAP0",
                "_MASKMAP1",
                "_MASKMAP2",
                "_MASKMAP3",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
                "_NORMALMAP_TANGENT_SPACE0",
                "_NORMALMAP_TANGENT_SPACE1",
                "_NORMALMAP_TANGENT_SPACE2",
                "_NORMALMAP_TANGENT_SPACE3",
            }),
            new("HDRP/LayeredLit", "GBuffer", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
            }),
            new("HDRP/LayeredLit", "GBuffer", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
            }),
            new("HDRP/LayeredLit", "MotionVectors", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_NORMALMAP0",
                "_NORMALMAP1",
                "_NORMALMAP2",
                "_NORMALMAP3",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "INSTANCING_ON",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "INSTANCING_ON",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_DEPTHOFFSET_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHT_BASED_BLEND",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_PIXEL_DISPLACEMENT",
                "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_PIXEL_DISPLACEMENT",
                "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE",
                "_REQUIRE_UV2",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_REQUIRE_UV2",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "pixel", new List<string>
            {
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_REQUIRE_UV2",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "INSTANCING_ON",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "INSTANCING_ON",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_DENSITY_MODE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_DEPTHOFFSET_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHT_BASED_BLEND",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_PIXEL_DISPLACEMENT",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_PIXEL_DISPLACEMENT",
                "_REQUIRE_UV2",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_MAIN_LAYER_INFLUENCE_MODE",
                "_REQUIRE_UV2",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP0",
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_HEIGHT_BASED_BLEND",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_PLANAR1",
                "_LAYER_MAPPING_PLANAR2",
                "_LAYER_MAPPING_PLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_HEIGHTMAP3",
                "_INFLUENCEMASK_MAP",
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_LAYER_MAPPING_TRIPLANAR3",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP1",
                "_HEIGHTMAP2",
                "_LAYEREDLIT_3_LAYERS",
                "_LAYER_MAPPING_TRIPLANAR1",
                "_LAYER_MAPPING_TRIPLANAR2",
                "_MAIN_LAYER_INFLUENCE_MODE",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
            }),
            new("HDRP/LayeredLit", "ShadowCaster", "vertex", new List<string>
            {
                "_LAYEREDLIT_4_LAYERS",
                "_LAYER_MASK_VERTEX_COLOR_MUL",
                "_REQUIRE_UV2",
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "domain", new List<string>
            {
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_ALPHATEST_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_TESSELLATION_DISPLACEMENT",
                "_TESSELLATION_PHONG",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "domain", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_TESSELLATION_DISPLACEMENT",
                "_TESSELLATION_PHONG",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "hull", new List<string>
            {
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_ALPHATEST_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "hull", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_ALPHATEST_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "pixel", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_ALPHATEST_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/LitTessellation", "ShadowCaster", "vertex", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_TESSELLATION_DISPLACEMENT",
                "_VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/Lit", "DepthOnly", "pixel", new List<string>
            {
                "_ALPHATEST_ON",
                "_MASKMAP",
                "_NORMALMAP",
                "_NORMALMAP_TANGENT_SPACE",
            }),
            new("HDRP/Lit", "DepthOnly", "vertex", new List<string>
            {
                "_ALPHATEST_ON",
                "_NORMALMAP",
            }),
            new("HDRP/Lit", "GBuffer", "pixel", new List<string>
            {
                "DECALS_3RT",
                "SHADOWS_SHADOWMASK",
                "_MASKMAP",
                "_NORMALMAP",
                "_NORMALMAP_TANGENT_SPACE",
            }),
            new("HDRP/Lit", "GBuffer", "vertex", new List<string>
            {
                "_NORMALMAP",
            }),
            new("HDRP/Lit", "MotionVectors", "pixel", new List<string>
            {
                "_ALPHATEST_ON",
                "_MASKMAP",
            }),
            new("HDRP/Lit", "MotionVectors", "vertex", new List<string>
            {
                "_ALPHATEST_ON",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "INSTANCING_ON",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "INSTANCING_ON",
                "_ALPHATEST_ON",
                "_DOUBLESIDED_ON",
                "_HEIGHTMAP",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "INSTANCING_ON",
                "_ALPHATEST_ON",
                "_HEIGHTMAP",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "INSTANCING_ON",
                "_DOUBLESIDED_ON",
                "_HEIGHTMAP",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "_ALPHATEST_ON",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "_ALPHATEST_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_PIXEL_DISPLACEMENT",
                "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "_DEPTHOFFSET_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_PIXEL_DISPLACEMENT",
                "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_MAPPING_PLANAR",
                "_PIXEL_DISPLACEMENT",
                "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_PIXEL_DISPLACEMENT",
                "_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "_DOUBLESIDED_ON",
            }),
            new("HDRP/Lit", "ShadowCaster", "pixel", new List<string>
            {
                "_HEIGHTMAP",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "INSTANCING_ON",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "INSTANCING_ON",
                "_ALPHATEST_ON",
                "_DOUBLESIDED_ON",
                "_HEIGHTMAP",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "INSTANCING_ON",
                "_ALPHATEST_ON",
                "_HEIGHTMAP",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "INSTANCING_ON",
                "_DOUBLESIDED_ON",
                "_HEIGHTMAP",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "_ALPHATEST_ON",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "_ALPHATEST_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_PIXEL_DISPLACEMENT",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "_DEPTHOFFSET_ON",
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_PIXEL_DISPLACEMENT",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_MAPPING_PLANAR",
                "_PIXEL_DISPLACEMENT",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "_DISPLACEMENT_LOCK_TILING_SCALE",
                "_HEIGHTMAP",
                "_PIXEL_DISPLACEMENT",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "_DOUBLESIDED_ON",
            }),
            new("HDRP/Lit", "ShadowCaster", "vertex", new List<string>
            {
                "_HEIGHTMAP",
            }),
            new("HDRP/Unlit", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("HDRP/Unlit", "ShadowCaster", "pixel", new List<string>
            {
                "_EMISSIVE_COLOR_MAP",
            }),
            new("HDRP/Unlit", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("Hidden/BlitCopy", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/ApplyDistortion", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/ApplyDistortion", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/Blit", "Nearest", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/Blit", "Nearest", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/CameraMotionVectors", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/CameraMotionVectors", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/ClearStencilBuffer", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/ClearStencilBuffer", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/CombineLighting", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/CopyDepthBuffer", "Copy Depth", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/CopyDepthBuffer", "Copy Depth", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/DownsampleDepth", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/DownsampleDepth", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/FinalPass", "<Unnamed Pass 0>", "pixel", new List<string>
            {
                "APPLY_AFTER_POST",
            }),
            new("Hidden/HDRP/FinalPass", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/GGXConvolve", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/GGXConvolve", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/Material/Decal/DecalNormalBuffer", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/Material/Decal/DecalNormalBuffer", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/PreIntegratedFGD_CookTorrance", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/PreIntegratedFGD_CookTorrance", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/PreIntegratedFGD_Ward", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/PreIntegratedFGD_Ward", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/Sky/ProceduralSky", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/Sky/ProceduralSky", "<Unnamed Pass 0>", "pixel", new List<string>
            {
                "_ENABLE_SUN_DISK",
            }),
            new("Hidden/HDRP/Sky/ProceduralSky", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/Sky/ProceduralSky", "<Unnamed Pass 0>", "vertex", new List<string>
            {
                "_ENABLE_SUN_DISK",
            }),
            new("Hidden/HDRP/UpsampleTransparent", "<Unnamed Pass 2>", "pixel", new List<string>
            {
                "NEAREST_DEPTH",
            }),
            new("Hidden/HDRP/UpsampleTransparent", "<Unnamed Pass 2>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/preIntegratedFGD_CharlieFabricLambert", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/preIntegratedFGD_CharlieFabricLambert", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/HDRP/preIntegratedFGD_GGXDisneyDiffuse", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/HDRP/preIntegratedFGD_GGXDisneyDiffuse", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/Internal-GUITextureClip", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/Internal-GUITextureClip", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/Internal-GUITexture", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/InternalErrorShader", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/InternalErrorShader", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/ScriptableRenderPipeline/ShadowClear", "ClearShadow", "pixel", new List<string>
            {
            }),
            new("Hidden/ScriptableRenderPipeline/ShadowClear", "ClearShadow", "vertex", new List<string>
            {
            }),
            new("Hidden/TextCore/Distance Field SSD", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("Hidden/TextCore/Distance Field SSD", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("Hidden/VFX/Spaceship_Background_A_Engine_Thruster/System", "<Unnamed Pass 0>", "pixel",
                new List<string>
                {
                }),
            new("Hidden/VFX/Spaceship_Background_A_Engine_Thruster/System", "<Unnamed Pass 0>", "vertex",
                new List<string>
                {
                }),
            new("Hidden/VFX/Spaceship_Background_A_Engine_Thruster/System", "ForwardOnly", "pixel", new List<string>
            {
            }),
            new("Hidden/VFX/Spaceship_Background_A_Engine_Thruster/System", "ForwardOnly", "vertex", new List<string>
            {
            }),
            new("Planet", "ShadowCaster", "pixel", new List<string>
            {
            }),
            new("Planet", "ShadowCaster", "vertex", new List<string>
            {
            }),
            new("Standard", "ShadowCaster", "pixel", new List<string>
            {
                "SHADOWS_DEPTH",
            }),
            new("Standard", "ShadowCaster", "vertex", new List<string>
            {
                "SHADOWS_DEPTH",
            }),
            new("TextMeshPro/Distance Field", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("TextMeshPro/Distance Field", "<Unnamed Pass 0>", "pixel", new List<string>
            {
                "UNDERLAY_ON",
            }),
            new("TextMeshPro/Distance Field", "<Unnamed Pass 0>", "vertex", new List<string>
            {
            }),
            new("UI/Default", "Default", "pixel", new List<string>
            {
            }),
            new("UI/Default", "Default", "pixel", new List<string>
            {
                "UNITY_UI_ALPHACLIP",
            }),
            new("UI/Default", "Default", "vertex", new List<string>
            {
            }),
            new("UI/Default", "Default", "vertex", new List<string>
            {
                "UNITY_UI_ALPHACLIP",
            }),
            new("er: Hidden/BlitCopy", "<Unnamed Pass 0>", "pixel", new List<string>
            {
            }),
            new("ing", "SubsurfaceScattering", "Hidden/HDRP/CombineLighting (instance 0x154)", new List<string>
            {
                "0>",
                "<Unnamed",
                "Pass",
            }),
        };
        return rules;
    }


	public void OnProcessShader( Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data )
	{
		// Don't strip essential shaders
		string name = shader.name;
		if (name.StartsWith("Hidden/")
		    || name.StartsWith("Legacy Shaders/")
		    || name.StartsWith("Particles/")
		    || name.StartsWith("UI/")
		    || name.StartsWith("Shader Graphs/")
		    || name.StartsWith("Sprites/")
		    || name.StartsWith("TextMeshPro/"))
			return;
		
		for (int i = data.Count - 1; i >= 0; --i)
		{
			var compilerData = data[i];
			string passName = snippet.passName;
			ShaderKeywordSet keywords = compilerData.shaderKeywordSet;

			if (ShouldKeepVariant(name, passName, keywords))
				continue; // Don't strip

			data.RemoveAt(i);
		}

		//if (ForwardPassNames.Contains(snippet.passName))
        {
            // Remove all variants for this pass
           // data.Clear();
        }

        return;

#if SHADER_COMPILATION_LOGGING
		System.IO.File.AppendAllText( LOG_FILE_PATH, "\n\n\n\n===== " + shader.name + " " + snippet.passName + " " + snippet.passType + " " + snippet.shaderType + "\n" );
#endif

		if( snippet.passType == PassType.Deferred || snippet.passType == PassType.LightPrePassBase || snippet.passType == PassType.LightPrePassFinal || snippet.passType == PassType.ScriptableRenderPipeline || snippet.passType == PassType.ScriptableRenderPipelineDefaultUnlit )
		{
#if SHADER_COMPILATION_LOGGING
			System.IO.File.AppendAllText( LOG_FILE_PATH, "Skipped shader variant because it uses SRP or Deferred shading\n" );
#endif

			data.Clear();
		}

		for( int i = data.Count - 1; i >= 0; --i )
		{
			bool shouldSkipShaderVariant = false;
			foreach( ShaderKeyword keywordToSkip in SKIPPED_VARIANTS )
			{
				if( data[i].shaderKeywordSet.IsEnabled( keywordToSkip ) )
				{
					shouldSkipShaderVariant = true;
					break;
				}
			}

			if( shouldSkipShaderVariant )
			{
				data.RemoveAt( i );
				continue;
			}

#if SHADER_COMPILATION_LOGGING
			string keywords = "";
			foreach( ShaderKeyword keyword in data[i].shaderKeywordSet.GetShaderKeywords() )
				keywords += keyword.GetKeywordName() + " ";

			if( keywords.Length == 0 )
				keywords = "No keywords defined";

			System.IO.File.AppendAllText( LOG_FILE_PATH, "- " + keywords + "\n" );
#endif
		}

#if SKIP_SHADER_COMPILATION
		for( int i = data.Count - 1; i >= 0; --i )
			data.Clear();
#endif
	}
}
