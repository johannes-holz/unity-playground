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

// This file contains the vertex and fragment functions for the forward lit pass
// This is the shader pass that computes visible colors for a material
// by reading material, light, shadow, etc. data

// Pull in URP library functions and our own common functions
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Textures
TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); // RGB = albedo, A = alpha

float4 _MainTex_ST; // This is automatically set by Unity. Used in TRANSFORM_TEX to apply UV tiling
float4 _ColorTint;
float _Smoothness;

// This attributes struct receives data about the mesh we're currently rendering
// Data is automatically placed in fields according to their semantic
struct Attributes {
	float3 positionOS : POSITION; // Position in object space
	float3 normalOS : NORMAL; // Normal in object space
	float2 uv : TEXCOORD0; // Material texture UVs
};

struct VertexOutput {
	float4 positionCS : SV_POSITION;

	float3 positionWS : TEXCOORD0; // Position in world space
	float2 uv : TEXCOORD1; // Material texture UVs
	float3 normalWS : TEXCOORD2; // Normal in world space
};

struct GeometryOutput {
	float4 positionCS : SV_POSITION;

	float3 positionWS : TEXCOORD0;
	float3 normalWS : TEXCOORD1;
	float2 uv : TEXCOORD2;
	float3 bary : TEXCOORD3;
};

VertexOutput Vertex(Attributes input) {
	VertexOutput output;

	VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
	VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

	output.uv = TRANSFORM_TEX(input.uv, _MainTex);
	output.positionWS = posnInputs.positionWS;
	output.normalWS = normalInputs.normalWS;

	output.positionCS = posnInputs.positionCS;

	return output;
}

[maxvertexcount(3)]
void Geometry(triangle VertexOutput input[3], inout TriangleStream<GeometryOutput> outputStream) {
	GeometryOutput output;

	output.positionWS = input[0].positionWS;
	output.normalWS = input[0].normalWS;
	output.uv = input[0].uv;
	output.positionCS = input[0].positionCS;
	output.bary = float3(1.0, 0.0, 0.0);
	outputStream.Append(output);

	output.positionWS = input[1].positionWS;
	output.normalWS = input[1].normalWS;
	output.uv = input[1].uv;
	output.positionCS = input[1].positionCS;
	output.bary = float3(0.0, 1.0, 0.0);
	outputStream.Append(output);

	output.positionWS = input[2].positionWS;
	output.normalWS = input[2].normalWS;
	output.uv = input[2].uv;
	output.positionCS = input[2].positionCS;
	output.bary = float3(0.0, 0.0, 1.0);
	outputStream.Append(output);
}


// The fragment function. This runs once per fragment, which you can think of as a pixel on the screen
// It must output the final color of this pixel
float4 Fragment(GeometryOutput input) : SV_TARGET{
	float3 unitWidth = fwidth(input.bary);
	float dist = length(_WorldSpaceCameraPos - input.positionWS);

	//float zDepth = input.positionCS.z / input.positionCS.w;
#if !defined(UNITY_REVERSED_Z) // basically only OpenGL
	zDepth = zDepth * 0.5 + 0.5; // remap -1 to 1 range to 0.0 to 1.0
#endif

	float3 aliased = smoothstep(float3(0.0, 0.0, 0.0), unitWidth * 2 / sqrt(dist), input.bary);
	//float minBary = min(input.bary.x, min(input.bary.y, input.bary.z));
	//float minEdge = min(edge.x, min(edge.y, edge.z));
	float minAliased = min(aliased.x, min(aliased.y, aliased.z));

	float2 uv = input.uv;
	// Sample the color map
	float4 colorSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

	// For lighting, create the InputData struct, which contains position and orientation data
	InputData lightingInput = (InputData)0; // Found in URP/ShaderLib/Input.hlsl
	lightingInput.positionWS = input.positionWS;
	lightingInput.normalWS = normalize(input.normalWS);
	lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS); // In ShaderVariablesFunctions.hlsl
	lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS); // In Shadows.hlsl

	// Calculate the surface data struct, which contains data from the material textures
	SurfaceData surfaceInput = (SurfaceData)0;
	surfaceInput.albedo = colorSample.rgb * _ColorTint.rgb;
	surfaceInput.alpha = colorSample.a * _ColorTint.a;
	surfaceInput.specular = 1;
	surfaceInput.smoothness = _Smoothness;

#if UNITY_VERSION >= 202120
	float4 color = UniversalFragmentBlinnPhong(lightingInput, surfaceInput);
	color = (1 - minAliased) * float4(1.0, 1.0, 1.0, 1.0) + minAliased * color;
	return color;
#else
	return UniversalFragmentBlinnPhong(lightingInput, surfaceInput.albedo, float4(surfaceInput.specular, 1), surfaceInput.smoothness, surfaceInput.emission, surfaceInput.alpha);
#endif
}