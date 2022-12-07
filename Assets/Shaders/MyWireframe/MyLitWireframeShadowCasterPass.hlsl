// MIT License

// Copyright (c) 2022 NedMakesGames

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// The shadow caster pass calculates the shadow silhouette of this mesh.
// It's much like the depth only pass, but has a few tweaks to take shadow
// bias offsets into account

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes {
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
};

struct Interpolators {
	float4 positionCS : SV_POSITION;
};

// These are set by Unity for the light currently "rendering" this shadow caster pass
float3 _LightDirection;

// This function offsets the clip space position by the depth and normal shadow biases
float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS) {
	float3 lightDirectionWS = _LightDirection;
	// From URP's ShadowCasterPass.hlsl
	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
	// We have to make sure that the shadow bias didn't push the shadow out of
	// the camera's view area. This is slightly different depending on the graphics API
#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
	positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif
	return positionCS;
}

Interpolators Vertex(Attributes input) {
	Interpolators output;

	VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS); // Found in URP/ShaderLib/ShaderVariablesFunctions.hlsl
	VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS); // Found in URP/ShaderLib/ShaderVariablesFunctions.hlsl

	output.positionCS = GetShadowCasterPositionCS(posnInputs.positionWS, normInputs.normalWS);
	return output;
}

float4 Fragment(Interpolators input) : SV_TARGET{
	return 0;
}