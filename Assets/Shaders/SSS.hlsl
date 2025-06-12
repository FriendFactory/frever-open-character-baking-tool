//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

// void SSS_float (float3 Normal, float1 Depth, float3 ColorSSS1, float3 ColorSSS2,
// float3 ColorSSS3, float1 LevelColor1, float1 LevelColor2, float1 Transmission, float1 Intensity,
// float3 LightDirection, out float3 Out) {
//     float remap1 = ((Depth)*4.0+-3.0);

//     float nulll = 0.0;

//     float M_ = (-1.0);

//     float _Gradient = saturate((nulll + ( ((dot(Normal,LightDirection)*(1.0 - remap1)) - M_) * (1.0 - nulll) ) / (2.0 - M_)));

//     float Gr = (1.0 - (_Gradient*0.9+0.1));

//     float3 _ColorSSS_ = lerp(lerp(ColorSSS1.rgb,ColorSSS2.rgb,saturate((0.0 + ( (_Gradient - 1.0) * (1.0 - 0.0) ) / (LevelColor1 - 1.0)))),ColorSSS3.rgb,saturate((0.0 + ( (_Gradient - LevelColor2) * (1.0 - 0.0) ) / (0.0 - LevelColor2))));

//     float3 FinalResult = (lerp((_ColorSSS_*saturate(lerp(_Gradient,(_Gradient*Gr),(_Gradient*(Transmission+0.3))))),_ColorSSS_,Transmission)*(Intensity*3.0));


//     Out = FinalResult;
// }

void SSSGradient_float (float3 Normal, float1 Depth, Gradient Gradient, float1 Transmission, 
float1 Intensity, float3 LightDirection, out float3 Out) {
    float remap1 = ((Depth)*4.0+-3.0);

    float nulll = 0.0;

    float M_ = (-1.0);

    float _Gradient = saturate((nulll + ( ((dot(Normal,LightDirection)*(1.0 - remap1)) - M_) * (1.0 - nulll) ) / (2.0 - M_)));

    float Gr = (1.0 - (_Gradient*0.9+0.1));

    float3 _ColorSSS_ = 0;

    float3 color = Gradient.colors[0].rgb;
    [unroll]
    for (int c = 1; c < 8; c++) 
    {
        float1 colorPos = saturate((1 - _Gradient - Gradient.colors[c-1].w) / (Gradient.colors[c].w - Gradient.colors[c-1].w)) * step(c, Gradient.colorsLength-1);
        color = lerp(color, Gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), Gradient.type));
    }

    //#ifndef UNITY_COLORSPACE_GAMMA
        color = SRGBToLinear(color);
    //#endif

        float1 alpha = Gradient.alphas[0].x;
        [unroll]
        for (int a = 1; a < 8; a++) 
        {
            float alphaPos = saturate((1 - _Gradient - Gradient.alphas[a-1].y) / (Gradient.alphas[a].y - Gradient.alphas[a-1].y)) * step(a, Gradient.alphasLength-1);
            alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type));
        }
        _ColorSSS_ = float4(color, alpha);

    float3 FinalResult = (lerp((_ColorSSS_*saturate(lerp(_Gradient,(_Gradient*Gr),(_Gradient*(Transmission+0.3))))),_ColorSSS_,Transmission)*(Intensity*3.0));

    Out = FinalResult;
}

#endif //MYHLSL_INCLUDE_INCLUDED