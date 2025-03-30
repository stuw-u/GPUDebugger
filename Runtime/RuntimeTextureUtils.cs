using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace GPUDebugger
{
    public static class RuntimeTextureUtils
    {
        public static long GetRuntimeMemorySize (Texture texture)
        {
            var dimension = texture.dimension;
            long byteSize = 0;
            int depth = 1;
            int msaa = 1;

            if (texture is Texture2DArray texArray) depth = texArray.depth;
            if (texture is Texture3D tex3D) depth = tex3D.depth;
            if (texture is CubemapArray cubeArray) depth = cubeArray.cubemapCount;
            if (texture is RenderTexture rt)
            {
                depth = rt.volumeDepth;
                if (rt.antiAliasing > 1)
                {
                    // One sample for every MSAA sample + 1 extra for the resolve target
                    msaa = rt.antiAliasing + 1;
                }
            }

            if (dimension == TextureDimension.Tex2D || dimension == TextureDimension.Cube || 
                dimension == TextureDimension.Tex2DArray || dimension == TextureDimension.CubeArray)
            {
                byteSize = GraphicsFormatUtility.ComputeMipChainSize(texture.width, texture.height, texture.graphicsFormat, texture.mipmapCount);
            }
            if(dimension == TextureDimension.Tex3D)
            {
                byteSize = GraphicsFormatUtility.ComputeMipChainSize(texture.width, texture.height, depth, texture.graphicsFormat, texture.mipmapCount);
                depth = 1;
            }
            if (dimension == TextureDimension.Cube || dimension == TextureDimension.CubeArray) byteSize *= 6;
            byteSize *= depth;
            byteSize *= msaa;

            
            return byteSize;
        }
    }
}
